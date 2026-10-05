using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // LINQ ЗАПРОС 1: Получить все надежные дефекты (Confidence >= 0.75) 
        // с названиями их категорий и статусом сессии (Объединение сессий, дефектов и категорий через Join)
        [HttpGet("high-confidence-defects")]
        public ActionResult<IEnumerable<object>> GetHighConfidenceDefects()
        {
            var query = from session in _db.Sessions
                        from defect in session.Defects
                        join category in _db.Categories on defect.CategoryId equals category.Id
                        where defect.IsReliable() // Использование бизнес-метода модели в LINQ
                        select new
                        {
                            SessionFile = session.FileName,
                            SessionStatus = session.Status,
                            DefectType = category.Name,
                            Severity = category.SeverityLevel,
                            Confidence = defect.ConfidenceScore,
                            Risk = defect.GetRiskAssessment() // Использование бизнес-метода модели
                        };

            return Ok(query.ToList());
        }

        // LINQ ЗАПРОС 2: Статистика по количеству дефектов каждого типа во всех забракованных (Rejected) сессиях
        // (Использование Where, SelectMany и GroupBy)
        [HttpGet("defects-summary-in-rejected")]
        public ActionResult<IEnumerable<object>> GetDefectsSummaryInRejectedSessions()
        {
            var query = _db.Sessions
                .Where(s => s.Status == "Rejected")
                .SelectMany(s => s.Defects) // "Плоское" объединение всех дефектов из забракованных сессий
                .GroupBy(d => d.CategoryId)
                .Select(group => new
                {
                    CategoryId = group.Key,
                    CategoryName = _db.Categories.FirstOrDefault(c => c.Id == group.Key)?.Name, // Подзапрос для получения имени
                    TotalCount = group.Count(),
                    AverageConfidence = group.Average(d => d.ConfidenceScore)
                })
                .OrderByDescending(x => x.TotalCount);

            return Ok(query.ToList());
        }

        // LINQ ЗАПРОС 3: Найти сессии, содержащие критические дефекты (Severity >= 8)
        // (Использование Any и объединения данных)
        [HttpGet("sessions-with-critical-defects")]
        public ActionResult<IEnumerable<object>> GetSessionsWithCriticalDefects()
        {
            var criticalCategoryIds = _db.Categories
                .Where(c => c.IsCritical()) // Бизнес-логика: фильтр критических категорий
                .Select(c => c.Id)
                .ToList();

            var query = _db.Sessions
                .Where(s => s.Defects.Any(d => criticalCategoryIds.Contains(d.CategoryId)))
                .Select(s => new
                {
                    s.Id,
                    s.FileName,
                    s.InspectedAt,
                    CriticalDefectsCount = s.Defects.Count(d => criticalCategoryIds.Contains(d.CategoryId)),
                    // Формируем список только критических дефектов для этой сессии
                    CriticalDefectsInfo = s.Defects
                        .Where(d => criticalCategoryIds.Contains(d.CategoryId))
                        .Select(d => new
                        {
                            d.ImageUrl,
                            d.ConfidenceScore
                        })
                });

            return Ok(query.ToList());
        }
    }
}