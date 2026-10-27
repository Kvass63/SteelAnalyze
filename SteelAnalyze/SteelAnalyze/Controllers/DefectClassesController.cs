using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
    [Route("api/defect-classes")]
    [ApiController]
    public class DefectClassesController : ApiControllerBase
    {
        private readonly DefectClassService _classes;

        public DefectClassesController(DefectClassService classes)
        {
            _classes = classes;
        }

        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<DefectClass>> GetAll() => Ok(_classes.GetAll());

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<DefectClass> GetById(int id) => OkOrError(_classes.GetById(id));

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<DefectClass> Create(DefectClassInput input)
        {
            var result = _classes.Create(input);
            if (result.Error != null) return ErrorResponse(result.Error);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Update(int id, DefectClassInput input) => NoContentOrError(_classes.Update(id, input));

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id) => NoContentOrError(_classes.Delete(id));
    }
}
