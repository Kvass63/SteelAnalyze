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

    public class ReferenceImportInput
    {
        // Папка на сервере с подпапками по кодам классов: <Folder>/crack/*.jpg, <Folder>/hole/*.jpg, ...
        [Required] public string Folder { get; set; } = string.Empty;
    }

    public class ReviewInput
    {
        public Verdict FinalVerdict { get; set; }
        public string? Comment { get; set; }
    }

    // ---------- Ответы ----------

    public record LoginResponse(string Token, string Username, string Role);

    public record ReferenceDto(
        int Id,
        int DefectClassId,
        string DefectClassCode,
        string DefectClassName,
        string OriginalFileName,
        DateTime AddedAt,
        string AddedBy)
    {
        public static ReferenceDto From(ReferenceSample r) => new(
            r.Id,
            r.DefectClassId,
            r.DefectClass!.Code,
            r.DefectClass.Name,
            r.OriginalFileName,
            r.AddedAt,
            r.AddedBy!.Username);
    }

    public record ReferenceImportResult(
        int Added,                  // Добавлено эталонов
        int Duplicates,             // Пропущено: такой снимок уже есть среди эталонов
        List<string> UnknownClasses, // Подпапки, для которых нет класса с таким кодом
        List<string> FailedFiles);  // Файлы, которые не удалось принять как снимок

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
        int NeighborVotes,
        SimilarityZone Zone,
        bool SecondDefectSuspected,
        Verdict Verdict,
        Verdict? FinalVerdict,
        bool AwaitsReview,
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
            i.NeighborVotes,
            i.GetZone(),
            i.SecondDefectSuspected,
            i.Verdict,
            i.FinalVerdict,
            i.AwaitsReview(),
            i.ReviewedBy?.Username,
            i.ReviewComment,
            i.Neighbors
                .OrderBy(n => n.Rank)
                .Select(n => new NeighborDto(n.Rank, n.DefectClassId, n.DefectClass!.Code, n.DefectClass.Name, n.Similarity, n.ReferenceSampleId))
                .ToList());
    }

    // ---------- Строки отчётов ----------
    // Свойства с init, а не позиционные записи: база должна уметь сортировать по ним (OrderBy после Select)

    public class DefectsByClassRow
    {
        public int DefectClassId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int SeverityLevel { get; init; }
        public bool IsCritical { get; init; }
        public int Count { get; init; }
        public double Share { get; init; }             // Доля от всех снимков за период
        public double AverageSimilarity { get; init; }
    }

    public class VerdictsByDayRow
    {
        public DateTime Date { get; init; }
        public int Total { get; init; }
        public int Rejected { get; init; }
        public int AcceptedWithRemark { get; init; }
        public int AwaitingReview { get; init; }
    }

    public class ReferenceCoverageRow
    {
        public int DefectClassId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int ReferenceCount { get; init; }
        public int InspectionCount { get; init; }
        public double? AverageSimilarity { get; init; } // null, если снимков класса в журнале нет
    }

    public class OperatorActivityRow
    {
        public int UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public int Total { get; init; }           // Сколько снимков проверил
        public int Rejected { get; init; }        // Из них брак (с учётом решения инженера)
        public int AwaitingReview { get; init; }  // Из них ждут решения инженера
        public int CriticalDefects { get; init; } // Из них с критическим классом дефекта
        public double AverageSimilarity { get; init; }
    }
}
