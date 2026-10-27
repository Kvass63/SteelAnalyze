using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Dtos;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
    // База эталонов: снимки с известным классом, с которыми сравниваются проверяемые снимки
    [Route("api/references")]
    [ApiController]
    public class ReferencesController : ApiControllerBase
    {
        private readonly ReferenceService _references;
        private readonly ReferenceImporter _importer;

        public ReferencesController(ReferenceService references, ReferenceImporter importer)
        {
            _references = references;
            _importer = importer;
        }

        // READ: Список эталонов, при необходимости только одного класса
        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<ReferenceDto>> GetAll(int? defectClassId) => Ok(_references.GetAll(defectClassId));

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<ReferenceDto> GetById(int id) => OkOrError(_references.GetById(id));

        // READ: Файл снимка эталона
        [HttpGet("{id}/image")]
        [Authorize]
        public IActionResult GetImage(int id)
        {
            var result = _references.GetImageFullPath(id);
            if (result.Error != null) return ErrorResponse(result.Error);

            return PhysicalFile(result.Value!, ImageStorage.GetContentType(result.Value!));
        }

        // CREATE: Добавить эталон; эмбеддинг считает сервер (только администратор)
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<ReferenceDto> Create(IFormFile? image, [FromForm] int defectClassId)
        {
            var result = _references.Create(image, defectClassId, User.GetUserId());
            if (result.Error != null) return ErrorResponse(result.Error);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        // CREATE: Загрузить партию эталонов из папки на сервере; класс снимка — имя его подпапки (только администратор)
        [HttpPost("import")]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<ReferenceImportResult> Import(ReferenceImportInput input) =>
            OkOrError(_importer.Import(input.Folder, User.GetUserId()));

        // UPDATE: Исправить класс эталона (только администратор)
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Update(int id, ReferenceUpdateInput input) =>
            NoContentOrError(_references.ChangeClass(id, input.DefectClassId));

        // DELETE: Удалить эталон вместе с файлом снимка (только администратор)
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id) => NoContentOrError(_references.Delete(id));
    }
}
