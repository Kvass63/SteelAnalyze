using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Data;
using SteelAnalyze.Models;

namespace SteelAnalyze.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AnalyticsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // LINQ ЗАПРОС 1: Сколько раз найден каждый класс дефекта за период, его доля и среднее сходство
        // (Объединение журнала со справочником через Join и группировка через GroupBy)
        [HttpGet("defects-by-class")]
        public ActionResult<IEnumerable<object>> GetDefectsByClass(DateTime? from, DateTime? to)
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

            // Доля и критичность досчитываются уже в памяти
            return Ok(rows.Select(r => new
            {
                DefectClassId = r.Id,
                r.Code,
                r.Name,
                r.SeverityLevel,
                IsCritical = r.SeverityLevel >= DefectClass.CriticalSeverity,
                r.Count,
                Share = (double)r.Count / total,
                r.AverageSimilarity
            }));
        }

        // LINQ ЗАПРОС 2: Итоги по дням — сколько брака, годных с замечанием и ожидающих ручной проверки
        // (Группировка по дате и подсчёт с условием; учитывается решение инженера, если оно есть)
        [HttpGet("verdicts-by-day")]
        public ActionResult<IEnumerable<object>> GetVerdictsByDay(DateTime? from, DateTime? to)
        {
            var query = FilterByPeriod(from, to)
                .GroupBy(i => i.InspectedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Count(),
                    Rejected = g.Count(i => (i.FinalVerdict ?? i.Verdict) == Verdict.Rejected),
                    AcceptedWithRemark = g.Count(i => (i.FinalVerdict ?? i.Verdict) == Verdict.AcceptedWithRemark),
                    AwaitingReview = g.Count(i => i.Verdict == Verdict.ManualReview && i.FinalVerdict == null)
                })
                .OrderBy(x => x.Date);

            return Ok(query.ToList());
        }

        // LINQ ЗАПРОС 3: Покрытие базы эталонов — сколько эталонов у каждого класса и как уверенно он распознаётся
        // (Подзапросы к эталонам и журналу для каждого класса справочника; только администратор)
        [HttpGet("reference-coverage")]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<IEnumerable<object>> GetReferenceCoverage()
        {
            var query = _db.DefectClasses
                .Select(c => new
                {
                    DefectClassId = c.Id,
                    c.Code,
                    c.Name,
                    ReferenceCount = _db.ReferenceSamples.Count(r => r.DefectClassId == c.Id),
                    InspectionCount = _db.Inspections.Count(i => i.PredictedClassId == c.Id),
                    // null, если снимков этого класса в журнале ещё нет
                    AverageSimilarity = _db.Inspections
                        .Where(i => i.PredictedClassId == c.Id)
                        .Average(i => (double?)i.Similarity)
                })
                .OrderBy(x => x.ReferenceCount)
                .ThenBy(x => x.Code);

            return Ok(query.ToList());
        }

        private IQueryable<Inspection> FilterByPeriod(DateTime? from, DateTime? to)
        {
            IQueryable<Inspection> query = _db.Inspections;
            if (from != null) query = query.Where(i => i.InspectedAt >= from);
            if (to != null) query = query.Where(i => i.InspectedAt < to.Value.Date.AddDays(1)); // день "to" входит в период
            return query;
        }
    }
}
