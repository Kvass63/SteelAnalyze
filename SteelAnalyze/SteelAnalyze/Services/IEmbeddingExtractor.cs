namespace SteelAnalyze.Services
{
    // Превращает снимок в эмбеддинг — вектор признаков, по которому снимки сравниваются между собой.
    // Настоящая реализация запускает нейросеть; она подключается после обучения модели.
    public interface IEmbeddingExtractor
    {
        // Загружена ли модель
        bool IsAvailable { get; }

        // Возвращает вектор единичной длины.
        // Бросает InvalidDataException, если файл не является изображением.
        float[] Extract(byte[] image);
    }

    // Используется, пока файла обученной модели нет: распознавание недоступно
    public class UnavailableEmbeddingExtractor : IEmbeddingExtractor
    {
        public bool IsAvailable => false;

        public float[] Extract(byte[] image) =>
            throw new InvalidOperationException("Модель распознавания не загружена");
    }
}
