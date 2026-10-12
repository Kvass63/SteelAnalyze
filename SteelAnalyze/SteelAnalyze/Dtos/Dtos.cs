using System.ComponentModel.DataAnnotations;
using SteelAnalyze.Models;

namespace SteelAnalyze.Dtos
{
    // ---------- Входные данные ----------

    public class LoginRequest
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class DefectClassInput
    {
        [Required, RegularExpression("^[a-z0-9_]+$", ErrorMessage = "Код: строчные латинские буквы, цифры и подчёркивание")]
        public string Code { get; set; } = string.Empty;
        [Required] public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Range(1, 10)] public int SeverityLevel { get; set; }
    }

    public class ReferenceUpdateInput
    {
        public int DefectClassId { get; set; }
    }

    public class ReviewInput
    {
        public Verdict FinalVerdict { get; set; }
        public string? Comment { get; set; }
    }

    // ---------- Ответы ----------

    public record ReferenceDto(
        int Id,
        int DefectClassId,
        string DefectClassCode,
        string DefectClassName,
        DateTime AddedAt,
        string AddedBy)
    {
        public static ReferenceDto From(ReferenceSample r) => new(
            r.Id,
            r.DefectClassId,
            r.DefectClass!.Code,
            r.DefectClass.Name,
            r.AddedAt,
            r.AddedBy!.Username);
    }

    public record NeighborDto(
        int Rank,
        int DefectClassId,
        string DefectClassCode,
        string DefectClassName,
        double Similarity,
        int? ReferenceSampleId);

    public record InspectionDto(
        int Id,
        string OriginalFileName,
        DateTime InspectedAt,
        string Operator,
        int PredictedClassId,
        string PredictedClassCode,
        string PredictedClassName,
        int SeverityLevel,
        double Similarity,
        SimilarityZone Zone,
        bool SecondDefectSuspected,
        Verdict Verdict,
        Verdict? FinalVerdict,
        string? ReviewedBy,
        string? ReviewComment,
        List<NeighborDto> Neighbors)
    {
        public static InspectionDto From(Inspection i) => new(
            i.Id,
            i.OriginalFileName,
            i.InspectedAt,
            i.Operator!.Username,
            i.PredictedClassId,
            i.PredictedClass!.Code,
            i.PredictedClass.Name,
            i.PredictedClass.SeverityLevel,
            i.Similarity,
            i.GetZone(),
            i.SecondDefectSuspected,
            i.Verdict,
            i.FinalVerdict,
            i.ReviewedBy?.Username,
            i.ReviewComment,
            i.Neighbors
                .OrderBy(n => n.Rank)
                .Select(n => new NeighborDto(n.Rank, n.DefectClassId, n.DefectClass!.Code, n.DefectClass.Name, n.Similarity, n.ReferenceSampleId))
                .ToList());
    }
}
