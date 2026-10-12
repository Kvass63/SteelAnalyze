namespace SteelAnalyze.Services
{
    // Хранение снимков файлами на диске; в базе лежит только путь внутри хранилища
    public class ImageStorage
    {
        public const string ReferencesFolder = "references";
        public const string InspectionsFolder = "inspections";

        private const long MaxFileSize = 10 * 1024 * 1024;

        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".bmp"] = "image/bmp"
        };

        private readonly string _root;

        public ImageStorage(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _root = Path.Combine(environment.ContentRootPath, configuration["Storage:Root"] ?? "storage");
        }

        // Возвращает текст ошибки или null, если файл можно принять
        public static string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0) return "Файл снимка не передан";
            if (file.Length > MaxFileSize) return "Файл снимка больше 10 МБ";
            if (!ContentTypes.ContainsKey(Path.GetExtension(file.FileName))) return "Допустимые форматы снимка: JPG, PNG, BMP";
            return null;
        }

        public static byte[] ReadAllBytes(IFormFile file)
        {
            using var buffer = new MemoryStream();
            file.CopyTo(buffer);
            return buffer.ToArray();
        }

        // Сохраняет файл под новым уникальным именем и возвращает путь внутри хранилища
        public string Save(string folder, string originalFileName, byte[] content)
        {
            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(originalFileName).ToLowerInvariant();
            Directory.CreateDirectory(Path.Combine(_root, folder));
            File.WriteAllBytes(Path.Combine(_root, folder, fileName), content);
            return $"{folder}/{fileName}";
        }

        public string GetFullPath(string imagePath) => Path.Combine(_root, imagePath);

        public static string GetContentType(string imagePath) =>
            ContentTypes.GetValueOrDefault(Path.GetExtension(imagePath), "application/octet-stream");

        public void Delete(string imagePath)
        {
            var fullPath = GetFullPath(imagePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }
}
