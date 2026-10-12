using SteelAnalyze.Auth;

namespace SteelAnalyze.Models
{
    public enum Verdict
    {
        Rejected,           // Брак
        AcceptedWithRemark, // Годен с замечанием
        ManualReview        // На ручную проверку
    }

    public enum SimilarityZone
    {
        Green,  // Система уверена
        Yellow, // Требуется внимание
        Red     // Система не узнала дефект
    }

    // Запись журнала распознавания: один снимок и результат по нему
    public class Inspection
    {
        // Пороги зон сходства (взяты из ВКР)
        public const double GreenZoneThreshold = 0.80;
        public const double YellowZoneThreshold = 0.60;

        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty; // Путь к файлу внутри хранилища снимков
        public string OriginalFileName { get; set; } = string.Empty;
        public DateTime InspectedAt { get; set; } = DateTime.UtcNow;

        public int OperatorId { get; set; }
        public AppUser? Operator { get; set; }

        public int PredictedClassId { get; set; }
        public DefectClass? PredictedClass { get; set; }

        public double Similarity { get; set; } // Сходство с ближайшим эталоном от 0.0 до 1.0
        public bool SecondDefectSuspected { get; set; } // Среди ближайших эталонов есть другой критический класс
        public Verdict Verdict { get; set; } // Вердикт системы

        // Решение инженера по качеству после ручной проверки
        public Verdict? FinalVerdict { get; set; }
        public int? ReviewedByUserId { get; set; }
        public AppUser? ReviewedBy { get; set; }
        public string? ReviewComment { get; set; }

        public List<InspectionNeighbor> Neighbors { get; set; } = new();

        // Бизнес-логика: Определение зоны сходства
        public SimilarityZone GetZone()
        {
            if (Similarity >= GreenZoneThreshold) return SimilarityZone.Green;
            if (Similarity >= YellowZoneThreshold) return SimilarityZone.Yellow;
            return SimilarityZone.Red;
        }

        // Бизнес-логика: Автоматическое вынесение вердикта по зоне сходства и опасности класса
        public void EvaluateVerdict(bool predictedClassIsCritical)
        {
            var zone = GetZone();

            if (zone == SimilarityZone.Red)
            {
                Verdict = Verdict.ManualReview; // Система не узнала дефект
            }
            else if (predictedClassIsCritical)
            {
                Verdict = Verdict.Rejected; // Опасный дефект бракуется и при неполной уверенности
            }
            else if (zone == SimilarityZone.Yellow || SecondDefectSuspected)
            {
                Verdict = Verdict.ManualReview; // За неопасным дефектом может скрываться опасный
            }
            else
            {
                Verdict = Verdict.AcceptedWithRemark;
            }
        }

        // Бизнес-логика: Ждёт ли запись решения инженера
        public bool AwaitsReview() => Verdict == Verdict.ManualReview && FinalVerdict == null;

        // Бизнес-логика: Запись решения инженера по ручной проверке
        public void Review(Verdict finalVerdict, int reviewerId, string? comment)
        {
            FinalVerdict = finalVerdict;
            ReviewedByUserId = reviewerId;
            ReviewComment = comment;
        }
    }

    // Один из ближайших эталонов, найденных для снимка
    public class InspectionNeighbor
    {
        public int Id { get; set; }
        public int InspectionId { get; set; }
        public int Rank { get; set; } // Место от 1 (самый похожий)

        public int DefectClassId { get; set; }
        public DefectClass? DefectClass { get; set; }

        public double Similarity { get; set; }

        // Пусто, если эталон удалён из базы после распознавания
        public int? ReferenceSampleId { get; set; }
        public ReferenceSample? ReferenceSample { get; set; }
    }
}
