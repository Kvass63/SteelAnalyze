using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Dtos;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
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

        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<ReferenceDto>> GetAll(int? defectClassId) => Ok(_references.GetAll(defectClassId));

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<ReferenceDto> GetById(int id) => OkOrError(_references.GetById(id));

        [HttpGet("{id}/image")]
        [Authorize]
        public IActionResult GetImage(int id)
        {
            var result = _references.GetImageFullPath(id);
            if (result.Error != null) return ErrorResponse(result.Error);

            return PhysicalFile(result.Value!, ImageStorage.GetContentType(result.Value!));
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<ReferenceDto> Create(IFormFile? image, [FromForm] int defectClassId)
        {
            var result = _references.Create(image, defectClassId, User.GetUserId());
            if (result.Error != null) return ErrorResponse(result.Error);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPost("import")]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<ReferenceImportResult> Import(ReferenceImportInput input) =>
            OkOrError(_importer.Import(input.Folder, User.GetUserId()));

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Update(int id, ReferenceUpdateInput input) =>
            NoContentOrError(_references.ChangeClass(id, input.DefectClassId));

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id) => NoContentOrError(_references.Delete(id));
    }
}
