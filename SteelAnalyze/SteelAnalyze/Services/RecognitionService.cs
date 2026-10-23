using Microsoft.EntityFrameworkCore;
using SteelAnalyze.Data;
using SteelAnalyze.Models;

namespace SteelAnalyze.Services
{
    // Распознавание снимка: эмбеддинг -> ближайшие эталоны -> класс -> вердикт -> запись в журнал
    public class RecognitionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmbeddingExtractor _extractor;
        private readonly ImageStorage _storage;

        public RecognitionService(ApplicationDbContext db, IEmbeddingExtractor extractor, ImageStorage storage)
        {
            _db = db;
            _extractor = extractor;
            _storage = storage;
        }

        public Inspection Inspect(byte[] image, string fileName, int operatorId)
        {
            var embedding = _extractor.Extract(image);

            // Только эталоны, посчитанные текущей моделью
            var references = _db.ReferenceSamples
                .AsNoTracking()
                .Where(r => r.ModelVersion == _extractor.ModelVersion)
                .Select(r => new { r.Id, r.DefectClassId, r.Embedding })
                .AsEnumerable()
                .Select(r => new ReferenceVector(r.Id, r.DefectClassId, EmbeddingCodec.FromBytes(r.Embedding)))
                .ToList();

            var result = DefectClassifier.Classify(embedding, references);

            var classes = _db.DefectClasses.ToDictionary(c => c.Id);
            var predictedClass = classes[result.PredictedClassId];

            var inspection = new Inspection
            {
                OriginalFileName = fileName,
                OperatorId = operatorId,
                PredictedClassId = predictedClass.Id,
                Similarity = result.Similarity,
                NeighborVotes = result.Neighbors.Count(n => n.DefectClassId == predictedClass.Id),
                // Подсказка о возможном втором дефекте: рядом есть эталон другого, критического класса
                SecondDefectSuspected = result.Neighbors.Any(n =>
                    n.DefectClassId != predictedClass.Id && classes[n.DefectClassId].IsCritical()),
                Neighbors = result.Neighbors
                    .Select(n => new InspectionNeighbor
                    {
                        Rank = n.Rank,
                        DefectClassId = n.DefectClassId,
                        Similarity = n.Similarity,
                        ReferenceSampleId = n.ReferenceId
                    })
                    .ToList()
            };
            inspection.EvaluateVerdict(predictedClass.IsCritical());

            inspection.ImagePath = _storage.Save(ImageStorage.InspectionsFolder, fileName, image);
            _db.Inspections.Add(inspection);
            _db.SaveChanges();

            return inspection;
        }
    }
}
