using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
    // Журнал распознавания
    [Route("api/inspections")]
    [ApiController]
    public class InspectionsController : ApiControllerBase
    {
        private readonly InspectionService _inspections;

        public InspectionsController(InspectionService inspections)
        {
            _inspections = inspections;
        }

        // CREATE: Проверить снимок — распознать дефект, вынести вердикт и записать в журнал (любой авторизованный пользователь)
        [HttpPost]
        [Authorize]
        public ActionResult<InspectionDto> Create(IFormFile? image)
        {
            var result = _inspections.Create(image, User.GetUserId());
            if (result.Error != null) return ErrorResponse(result.Error);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        // READ: Журнал с фильтрами по периоду, классу и вердикту системы
        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<InspectionDto>> GetAll(DateTime? from, DateTime? to, int? defectClassId, Verdict? verdict) =>
            Ok(_inspections.GetAll(from, to, defectClassId, verdict));

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<InspectionDto> GetById(int id) => OkOrError(_inspections.GetById(id));

        // READ: Файл проверенного снимка
        [HttpGet("{id}/image")]
        [Authorize]
        public IActionResult GetImage(int id)
        {
            var result = _inspections.GetImageFullPath(id);
            if (result.Error != null) return ErrorResponse(result.Error);

            return PhysicalFile(result.Value!, ImageStorage.GetContentType(result.Value!));
        }

        // UPDATE: Решение инженера по снимку, отправленному на ручную проверку (только администратор)
        [HttpPut("{id}/review")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Review(int id, ReviewInput input) =>
            NoContentOrError(_inspections.Review(id, input.FinalVerdict, input.Comment, User.GetUserId()));

        // DELETE: Удалить запись журнала вместе с файлом снимка (только администратор)
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id) => NoContentOrError(_inspections.Delete(id));
    }
}
