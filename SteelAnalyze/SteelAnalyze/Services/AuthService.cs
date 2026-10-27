using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SteelAnalyze.Auth;
using SteelAnalyze.Data;
using SteelAnalyze.Dtos;

namespace SteelAnalyze.Services
{
    // Вход в систему: проверка логина и пароля и выдача JWT
    public class AuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly JwtSettings _jwt;

        public AuthService(ApplicationDbContext db, IOptions<JwtSettings> jwt)
        {
            _db = db;
            _jwt = jwt.Value;
        }

        public ServiceResult<LoginResponse> Login(string username, string password)
        {
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null || !user.VerifyPassword(password)) return ServiceError.InvalidCredentials();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role)
            };
            var jwt = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.LifetimeMinutes),
                signingCredentials: new SigningCredentials(_jwt.GetSecurityKey(), SecurityAlgorithms.HmacSha256));

            return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(jwt), user.Username, user.Role);
        }
    }
}
