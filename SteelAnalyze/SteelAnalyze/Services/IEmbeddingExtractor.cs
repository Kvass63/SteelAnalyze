namespace SteelAnalyze.Services
{
    public interface IEmbeddingExtractor
    {
        bool IsAvailable { get; }

        string? ModelVersion { get; }

        float[] Extract(byte[] image);
    }

    public class UnavailableEmbeddingExtractor : IEmbeddingExtractor
    {
        public bool IsAvailable => false;

        public string? ModelVersion => null;

        public float[] Extract(byte[] image) =>
            throw new InvalidOperationException("Модель распознавания не загружена");
    }
}
