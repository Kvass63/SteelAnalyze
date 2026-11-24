using SteelAnalyze.Data;

namespace SteelAnalyze.Services
{
    public static class ReferenceEmbeddingUpdater
    {
        private const int SaveBatchSize = 100;

        public static void Update(ApplicationDbContext db, IEmbeddingExtractor extractor, ImageStorage storage, ILogger logger)
        {
            if (!extractor.IsAvailable) return;

            var outdated = db.ReferenceSamples.Where(r => r.ModelVersion != extractor.ModelVersion).ToList();
            if (outdated.Count == 0) return;

            logger.LogInformation("Пересчёт эмбеддингов эталонов под модель {Version}: {Count} шт.", extractor.ModelVersion, outdated.Count);

            var updated = 0;
            foreach (var reference in outdated)
            {
                var fullPath = storage.GetFullPath(reference.ImagePath);
                if (!File.Exists(fullPath))
                {
                    logger.LogWarning("Эталон {Id}: файл снимка {Path} отсутствует, эмбеддинг не пересчитан", reference.Id, reference.ImagePath);
                    continue;
                }

                try
                {
                    reference.Embedding = EmbeddingCodec.ToBytes(extractor.Extract(File.ReadAllBytes(fullPath)));
                    reference.ModelVersion = extractor.ModelVersion!;
                }
                catch (InvalidDataException)
                {
                    logger.LogWarning("Эталон {Id}: файл {Path} не читается как изображение", reference.Id, reference.ImagePath);
                    continue;
                }

                updated++;
                if (updated % SaveBatchSize == 0)
                {
                    db.SaveChanges();
                    logger.LogInformation("Пересчитано {Updated} из {Count}", updated, outdated.Count);
                }
            }

            db.SaveChanges();
            logger.LogInformation("Пересчёт завершён: {Updated} из {Count}", updated, outdated.Count);
        }
    }
}
