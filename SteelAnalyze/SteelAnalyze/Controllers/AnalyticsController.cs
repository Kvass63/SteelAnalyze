using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Dtos;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
    // Сводные отчёты: только для авторизованных пользователей
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AnalyticsController : ApiControllerBase
    {
        private readonly AnalyticsService _analytics;

        public AnalyticsController(AnalyticsService analytics)
        {
            _analytics = analytics;
        }

        // Отчёт 1: Сколько раз найден каждый класс дефекта за период
        [HttpGet("defects-by-class")]
        public ActionResult<IEnumerable<DefectsByClassRow>> GetDefectsByClass(DateTime? from, DateTime? to) =>
            Ok(_analytics.GetDefectsByClass(from, to));

        // Отчёт 2: Итоги вердиктов по дням
        [HttpGet("verdicts-by-day")]
        public ActionResult<IEnumerable<VerdictsByDayRow>> GetVerdictsByDay(DateTime? from, DateTime? to) =>
            Ok(_analytics.GetVerdictsByDay(from, to));

        // Отчёт 3: Покрытие базы эталонов (только администратор)
        [HttpGet("reference-coverage")]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<IEnumerable<ReferenceCoverageRow>> GetReferenceCoverage() =>
            Ok(_analytics.GetReferenceCoverage());
    }
}
