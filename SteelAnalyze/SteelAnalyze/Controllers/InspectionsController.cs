using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteelAnalyze.Auth;
using SteelAnalyze.Data;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
    // Журнал распознавания
    [Route("api/inspections")]
    [ApiController]
    public class InspectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmbeddingExtractor _extractor;
        private readonly RecognitionService _recognition;
        private readonly ImageStorage _storage;

        public InspectionsController(ApplicationDbContext db, IEmbeddingExtractor extractor, RecognitionService recognition, ImageStorage storage)
        {
            _db = db;
            _extractor = extractor;
            _recognition = recognition;
            _storage = storage;
        }

        // CREATE: Проверить снимок — распознать дефект, вынести вердикт и записать в журнал (любой авторизованный пользователь)
        [HttpPost]
        [Authorize]
        public ActionResult<InspectionDto> Create(IFormFile? image)
        {
            if (!_extractor.IsAvailable)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Модель распознавания не загружена" });

            var error = ImageStorage.Validate(image);
            if (error != null) return BadRequest(new { message = error });
            if (!_db.ReferenceSamples.Any())
                return Conflict(new { message = "База эталонов пуста: распознавать не с чем" });

            Inspection inspection;
            try
            {
                inspection = _recognition.Inspect(ImageStorage.ReadAllBytes(image!), image!.FileName, User.GetUserId());
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { message = "Файл не является изображением" });
            }

            var created = WithDetails().First(i => i.Id == inspection.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, InspectionDto.From(created));
        }

        // READ: Журнал с фильтрами по периоду, классу и вердикту системы
        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<InspectionDto>> GetAll(DateTime? from, DateTime? to, int? defectClassId, Verdict? verdict)
        {
            var query = WithDetails();
            if (from != null) query = query.Where(i => i.InspectedAt >= from);
            if (to != null) query = query.Where(i => i.InspectedAt < to.Value.Date.AddDays(1)); // день "to" входит в период
            if (defectClassId != null) query = query.Where(i => i.PredictedClassId == defectClassId);
            if (verdict != null) query = query.Where(i => i.Verdict == verdict);

            return Ok(query.OrderByDescending(i => i.InspectedAt).AsEnumerable().Select(InspectionDto.From).ToList());
        }

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<InspectionDto> GetById(int id)
        {
            var inspection = WithDetails().FirstOrDefault(i => i.Id == id);
            return inspection == null ? NotFound() : Ok(InspectionDto.From(inspection));
        }

        // READ: Файл проверенного снимка
        [HttpGet("{id}/image")]
        [Authorize]
        public IActionResult GetImage(int id)
        {
            var inspection = _db.Inspections.FirstOrDefault(i => i.Id == id);
            if (inspection == null) return NotFound();

            var fullPath = _storage.GetFullPath(inspection.ImagePath);
            if (!System.IO.File.Exists(fullPath)) return NotFound(new { message = "Файл снимка отсутствует в хранилище" });

            return PhysicalFile(fullPath, ImageStorage.GetContentType(inspection.ImagePath));
        }

        // UPDATE: Решение инженера по снимку, отправленному на ручную проверку (только администратор)
        [HttpPut("{id}/review")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Review(int id, ReviewInput input)
        {
            var inspection = _db.Inspections.FirstOrDefault(i => i.Id == id);
            if (inspection == null) return NotFound();
            if (inspection.Verdict != Verdict.ManualReview)
                return Conflict(new { message = "Запись не отправлялась на ручную проверку" });
            if (input.FinalVerdict == Verdict.ManualReview)
                return BadRequest(new { message = "Решение должно быть Rejected или AcceptedWithRemark" });

            inspection.Review(input.FinalVerdict, User.GetUserId(), input.Comment);
            _db.SaveChanges();

            return NoContent();
        }

        // DELETE: Удалить запись журнала вместе с файлом снимка (только администратор)
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id)
        {
            var inspection = _db.Inspections.FirstOrDefault(i => i.Id == id);
            if (inspection == null) return NotFound();

            _db.Inspections.Remove(inspection);
            _db.SaveChanges();
            _storage.Delete(inspection.ImagePath);

            return NoContent();
        }

        private IQueryable<Inspection> WithDetails() =>
            _db.Inspections
                .Include(i => i.Operator)
                .Include(i => i.PredictedClass)
                .Include(i => i.ReviewedBy)
                .Include(i => i.Neighbors).ThenInclude(n => n.DefectClass);
    }
}
