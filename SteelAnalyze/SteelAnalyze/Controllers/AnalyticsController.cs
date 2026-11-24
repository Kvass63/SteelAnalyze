using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Dtos;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
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

        [HttpGet("defects-by-class")]
        public ActionResult<IEnumerable<DefectsByClassRow>> GetDefectsByClass(DateTime? from, DateTime? to) =>
            Ok(_analytics.GetDefectsByClass(from, to));

        [HttpGet("verdicts-by-day")]
        public ActionResult<IEnumerable<VerdictsByDayRow>> GetVerdictsByDay(DateTime? from, DateTime? to) =>
            Ok(_analytics.GetVerdictsByDay(from, to));

        [HttpGet("reference-coverage")]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<IEnumerable<ReferenceCoverageRow>> GetReferenceCoverage() =>
            Ok(_analytics.GetReferenceCoverage());

        [HttpGet("operators")]
        public ActionResult<IEnumerable<OperatorActivityRow>> GetOperatorActivity(DateTime? from, DateTime? to) =>
            Ok(_analytics.GetOperatorActivity(from, to));
    }
}
