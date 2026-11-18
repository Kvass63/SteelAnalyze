using SteelAnalyze.Auth;

namespace SteelAnalyze.Models
{
    public class ReferenceSample
    {
        public int Id { get; set; }

        public int DefectClassId { get; set; }
        public DefectClass? DefectClass { get; set; }

        public string ImagePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public byte[] Embedding { get; set; } = [];
        public string ModelVersion { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public int AddedByUserId { get; set; }
        public AppUser? AddedBy { get; set; }
    }
}
