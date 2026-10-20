using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SteelAnalyze.Auth;
using SteelAnalyze.Data;
using SteelAnalyze.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Секция Jwt отсутствует в конфигурации");
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = jwtSettings.GetSecurityKey(),
            ValidateIssuerSigningKey = true,
        };
    });
builder.Services.AddAuthorization();

// Перечисления (вердикт, зона) передаются в JSON словами, а не числами
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<ImageStorage>();
// Нейросеть: файл модели лежит рядом с программой; без него сервер работает, но распознавание отвечает 503
var modelPath = Path.Combine(AppContext.BaseDirectory, builder.Configuration["Model:Path"] ?? "model.onnx");
if (File.Exists(modelPath))
    builder.Services.AddSingleton<IEmbeddingExtractor>(new OnnxEmbeddingExtractor(modelPath));
else
    builder.Services.AddSingleton<IEmbeddingExtractor, UnavailableEmbeddingExtractor>();
builder.Services.AddScoped<RecognitionService>();

var app = builder.Build();

// Создание и обновление базы по миграциям, начальное заполнение, пересчёт эталонов после замены модели
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);

    var extractor = app.Services.GetRequiredService<IEmbeddingExtractor>();
    if (extractor.IsAvailable)
        app.Logger.LogInformation("Модель распознавания загружена: {Path} (версия {Version})", modelPath, extractor.ModelVersion);
    else
        app.Logger.LogWarning("Файл модели {Path} не найден: распознавание недоступно", modelPath);
    ReferenceEmbeddingUpdater.Update(db, extractor, app.Services.GetRequiredService<ImageStorage>(), app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// В разработке не перенаправляем http -> https: при редиректе клиенты (Postman) отбрасывают заголовок Authorization
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
