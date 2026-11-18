namespace SteelAnalyze.Services
{
    public record ReferenceVector(int ReferenceId, int DefectClassId, float[] Embedding);

    public record NeighborMatch(int Rank, int ReferenceId, int DefectClassId, double Similarity);

    public record ClassificationResult(int PredictedClassId, double Similarity, IReadOnlyList<NeighborMatch> Neighbors);

    public static class DefectClassifier
    {
        public const int NeighborCount = 5;

        public static ClassificationResult Classify(float[] query, IReadOnlyList<ReferenceVector> references)
        {
            if (references.Count == 0) throw new InvalidOperationException("База эталонов пуста");

            var neighbors = references
                .Select(r => new { r.ReferenceId, r.DefectClassId, Similarity = GetSimilarity(query, r.Embedding) })
                .OrderByDescending(x => x.Similarity)
                .Take(NeighborCount)
                .Select((x, index) => new NeighborMatch(index + 1, x.ReferenceId, x.DefectClassId, x.Similarity))
                .ToList();

            var predictedClassId = neighbors
                .GroupBy(n => n.DefectClassId)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Min(n => n.Rank))
                .First().Key;

            return new ClassificationResult(predictedClassId, neighbors[0].Similarity, neighbors);
        }

        private static double GetSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) throw new ArgumentException("Эмбеддинги разной длины");

            double dot = 0;
            for (var i = 0; i < a.Length; i++) dot += a[i] * b[i];

            return Math.Clamp(dot, 0.0, 1.0);
        }
    }
}
