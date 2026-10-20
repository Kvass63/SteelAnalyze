namespace SteelAnalyze.Services
{
    // Превращает снимок в эмбеддинг — вектор признаков, по которому снимки сравниваются между собой
    public interface IEmbeddingExtractor
    {
        // Загружена ли модель
        bool IsAvailable { get; }

        // Отпечаток файла модели: у эмбеддингов разных моделей нет ничего общего,
        // поэтому у каждого эталона запоминается, какой моделью он посчитан
        string? ModelVersion { get; }

        // Возвращает вектор единичной длины.
        // Бросает InvalidDataException, если файл не является изображением.
        float[] Extract(byte[] image);
    }

    // Используется, если файла модели нет: распознавание недоступно
    public class UnavailableEmbeddingExtractor : IEmbeddingExtractor
    {
        public bool IsAvailable => false;

        public string? ModelVersion => null;

        public float[] Extract(byte[] image) =>
            throw new InvalidOperationException("Модель распознавания не загружена");
    }
}
