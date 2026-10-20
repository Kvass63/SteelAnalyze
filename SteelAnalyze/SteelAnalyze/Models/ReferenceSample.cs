using SteelAnalyze.Auth;

namespace SteelAnalyze.Models
{
    // Эталон: снимок с известным классом дефекта и его эмбеддинг
    public class ReferenceSample
    {
        public int Id { get; set; }

        public int DefectClassId { get; set; }
        public DefectClass? DefectClass { get; set; }

        public string ImagePath { get; set; } = string.Empty; // Путь к файлу внутри хранилища снимков
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty; // Хеш содержимого файла: по нему находятся повторно загруженные снимки
        public byte[] Embedding { get; set; } = []; // Вектор признаков, сохранённый как набор байтов
        public string ModelVersion { get; set; } = string.Empty; // Какой моделью посчитан эмбеддинг
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public int AddedByUserId { get; set; }
        public AppUser? AddedBy { get; set; }
    }
}
