using SteelAnalyze.Auth;

namespace SteelAnalyze.Models
{
    public enum Verdict
    {
        Rejected,
        AcceptedWithRemark,
        ManualReview
    }

    public enum SimilarityZone
    {
        Green,
        Yellow,
        Red
    }

    public enum ReviewOutcome
    {
        Done,
        NotUnderReview,
        InvalidVerdict
    }

    public class Inspection
    {
        public const double AnomalySimilarityThreshold = 0.90;
        public const int GreenZoneVotes = 5;
        public const int YellowZoneVotes = 3;

        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public DateTime InspectedAt { get; set; } = DateTime.UtcNow;

        public int OperatorId { get; set; }
        public AppUser? Operator { get; set; }

        public int PredictedClassId { get; set; }
        public DefectClass? PredictedClass { get; set; }

        public double Similarity { get; set; }
        public int NeighborVotes { get; set; }
        public bool SecondDefectSuspected { get; set; }
        public Verdict Verdict { get; set; }

        public Verdict? FinalVerdict { get; set; }
        public int? ReviewedByUserId { get; set; }
        public AppUser? ReviewedBy { get; set; }
        public string? ReviewComment { get; set; }

        public List<InspectionNeighbor> Neighbors { get; set; } = new();

        public SimilarityZone GetZone()
        {
            if (Similarity < AnomalySimilarityThreshold || NeighborVotes < YellowZoneVotes) return SimilarityZone.Red;
            if (NeighborVotes >= GreenZoneVotes) return SimilarityZone.Green;
            return SimilarityZone.Yellow;
        }

        public void EvaluateVerdict(bool predictedClassIsCritical)
        {
            var zone = GetZone();

            if (zone == SimilarityZone.Red)
            {
                Verdict = Verdict.ManualReview;
            }
            else if (predictedClassIsCritical)
            {
                Verdict = Verdict.Rejected;
            }
            else if (zone == SimilarityZone.Yellow || SecondDefectSuspected)
            {
                Verdict = Verdict.ManualReview;
            }
            else
            {
                Verdict = Verdict.AcceptedWithRemark;
            }
        }

        public bool AwaitsReview() => Verdict == Verdict.ManualReview && FinalVerdict == null;

        public ReviewOutcome Review(Verdict finalVerdict, int reviewerId, string? comment)
        {
            if (Verdict != Verdict.ManualReview) return ReviewOutcome.NotUnderReview;
            if (finalVerdict == Verdict.ManualReview) return ReviewOutcome.InvalidVerdict;

            FinalVerdict = finalVerdict;
            ReviewedByUserId = reviewerId;
            ReviewComment = comment;
            return ReviewOutcome.Done;
        }
    }

    public class InspectionNeighbor
    {
        public int Id { get; set; }
        public int InspectionId { get; set; }
        public int Rank { get; set; }

        public int DefectClassId { get; set; }
        public DefectClass? DefectClass { get; set; }

        public double Similarity { get; set; }

        public int? ReferenceSampleId { get; set; }
        public ReferenceSample? ReferenceSample { get; set; }
    }
}
