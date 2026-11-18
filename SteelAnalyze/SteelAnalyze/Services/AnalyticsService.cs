using SteelAnalyze.Data;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;

namespace SteelAnalyze.Services
{
    public class AnalyticsService
    {
        private readonly ApplicationDbContext _db;

        public AnalyticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<DefectsByClassRow> GetDefectsByClass(DateTime? from, DateTime? to)
        {
            var inspections = FilterByPeriod(from, to);
            var total = inspections.Count();

            var rows = (from inspection in inspections
                        join defectClass in _db.DefectClasses on inspection.PredictedClassId equals defectClass.Id
                        group inspection by new { defectClass.Id, defectClass.Code, defectClass.Name, defectClass.SeverityLevel } into g
                        orderby g.Count() descending
                        select new
                        {
                            g.Key.Id,
                            g.Key.Code,
                            g.Key.Name,
                            g.Key.SeverityLevel,
                            Count = g.Count(),
                            AverageSimilarity = g.Average(i => i.Similarity)
                        }).ToList();

            return rows.Select(r => new DefectsByClassRow
            {
                DefectClassId = r.Id,
                Code = r.Code,
                Name = r.Name,
                SeverityLevel = r.SeverityLevel,
                IsCritical = DefectClass.IsCriticalSeverity(r.SeverityLevel),
                Count = r.Count,
                Share = (double)r.Count / total,
                AverageSimilarity = r.AverageSimilarity
            }).ToList();
        }

        public List<VerdictsByDayRow> GetVerdictsByDay(DateTime? from, DateTime? to) =>
            FilterByPeriod(from, to)
                .GroupBy(i => i.InspectedAt.Date)
                .Select(g => new VerdictsByDayRow
                {
                    Date = g.Key,
                    Total = g.Count(),
                    Rejected = g.Count(i => (i.FinalVerdict ?? i.Verdict) == Verdict.Rejected),
                    AcceptedWithRemark = g.Count(i => (i.FinalVerdict ?? i.Verdict) == Verdict.AcceptedWithRemark),
                    AwaitingReview = g.Count(i => i.Verdict == Verdict.ManualReview && i.FinalVerdict == null)
                })
                .OrderBy(x => x.Date)
                .ToList();

        public List<ReferenceCoverageRow> GetReferenceCoverage() =>
            _db.DefectClasses
                .Select(c => new ReferenceCoverageRow
                {
                    DefectClassId = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    ReferenceCount = _db.ReferenceSamples.Count(r => r.DefectClassId == c.Id),
                    InspectionCount = _db.Inspections.Count(i => i.PredictedClassId == c.Id),
                    AverageSimilarity = _db.Inspections
                        .Where(i => i.PredictedClassId == c.Id)
                        .Average(i => (double?)i.Similarity)
                })
                .OrderBy(x => x.ReferenceCount)
                .ThenBy(x => x.Code)
                .ToList();

        public List<OperatorActivityRow> GetOperatorActivity(DateTime? from, DateTime? to)
        {
            var inspections = FilterByPeriod(from, to);

            return (from inspection in inspections
                    join user in _db.Users on inspection.OperatorId equals user.Id
                    join defectClass in _db.DefectClasses on inspection.PredictedClassId equals defectClass.Id
                    group new { inspection, defectClass.SeverityLevel } by new { user.Id, user.Username, user.Role } into g
                    orderby g.Count() descending, g.Key.Username
                    select new OperatorActivityRow
                    {
                        UserId = g.Key.Id,
                        Username = g.Key.Username,
                        Role = g.Key.Role,
                        Total = g.Count(),
                        Rejected = g.Count(x => (x.inspection.FinalVerdict ?? x.inspection.Verdict) == Verdict.Rejected),
                        AwaitingReview = g.Count(x => x.inspection.Verdict == Verdict.ManualReview && x.inspection.FinalVerdict == null),
                        CriticalDefects = g.Count(x => x.SeverityLevel >= DefectClass.CriticalSeverity),
                        AverageSimilarity = g.Average(x => x.inspection.Similarity)
                    }).ToList();
        }

        private IQueryable<Inspection> FilterByPeriod(DateTime? from, DateTime? to)
        {
            IQueryable<Inspection> query = _db.Inspections;
            if (from != null) query = query.Where(i => i.InspectedAt >= from);
            if (to != null) query = query.Where(i => i.InspectedAt < to.Value.Date.AddDays(1));
            return query;
        }
    }
}
