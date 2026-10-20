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
    // База эталонов: снимки с известным классом, с которыми сравниваются проверяемые снимки
    [Route("api/references")]
    [ApiController]
    public class ReferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmbeddingExtractor _extractor;
        private readonly ImageStorage _storage;

        public ReferencesController(ApplicationDbContext db, IEmbeddingExtractor extractor, ImageStorage storage)
        {
            _db = db;
            _extractor = extractor;
            _storage = storage;
        }

        // READ: Список эталонов, при необходимости только одного класса
        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<ReferenceDto>> GetAll(int? defectClassId)
        {
            var query = WithDetails();
            if (defectClassId != null) query = query.Where(r => r.DefectClassId == defectClassId);

            return Ok(query.OrderBy(r => r.Id).AsEnumerable().Select(ReferenceDto.From).ToList());
        }

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<ReferenceDto> GetById(int id)
        {
            var reference = WithDetails().FirstOrDefault(r => r.Id == id);
            return reference == null ? NotFound() : Ok(ReferenceDto.From(reference));
        }

        // READ: Файл снимка эталона
        [HttpGet("{id}/image")]
        [Authorize]
        public IActionResult GetImage(int id)
        {
            var reference = _db.ReferenceSamples.FirstOrDefault(r => r.Id == id);
            if (reference == null) return NotFound();

            var fullPath = _storage.GetFullPath(reference.ImagePath);
            if (!System.IO.File.Exists(fullPath)) return NotFound(new { message = "Файл снимка отсутствует в хранилище" });

            return PhysicalFile(fullPath, ImageStorage.GetContentType(reference.ImagePath));
        }

        // CREATE: Добавить эталон; эмбеддинг считает сервер (только администратор)
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<ReferenceDto> Create(IFormFile? image, [FromForm] int defectClassId)
        {
            if (!_extractor.IsAvailable)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Модель распознавания не загружена" });

            var error = ImageStorage.Validate(image);
            if (error != null) return BadRequest(new { message = error });
            if (!_db.DefectClasses.Any(c => c.Id == defectClassId))
                return BadRequest(new { message = $"Класс с Id={defectClassId} не существует" });

            var content = ImageStorage.ReadAllBytes(image!);
            float[] embedding;
            try
            {
                embedding = _extractor.Extract(content);
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { message = "Файл не является изображением" });
            }

            var reference = new ReferenceSample
            {
                DefectClassId = defectClassId,
                Embedding = EmbeddingCodec.ToBytes(embedding),
                ModelVersion = _extractor.ModelVersion!,
                AddedByUserId = User.GetUserId(),
                ImagePath = _storage.Save(ImageStorage.ReferencesFolder, image!.FileName, content)
            };
            _db.ReferenceSamples.Add(reference);
            _db.SaveChanges();

            var created = WithDetails().First(r => r.Id == reference.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ReferenceDto.From(created));
        }

        // UPDATE: Исправить класс эталона (только администратор)
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Update(int id, ReferenceUpdateInput input)
        {
            var reference = _db.ReferenceSamples.FirstOrDefault(r => r.Id == id);
            if (reference == null) return NotFound();
            if (!_db.DefectClasses.Any(c => c.Id == input.DefectClassId))
                return BadRequest(new { message = $"Класс с Id={input.DefectClassId} не существует" });

            reference.DefectClassId = input.DefectClassId;
            _db.SaveChanges();

            return NoContent();
        }

        // DELETE: Удалить эталон вместе с файлом снимка (только администратор)
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id)
        {
            var reference = _db.ReferenceSamples.FirstOrDefault(r => r.Id == id);
            if (reference == null) return NotFound();

            _db.ReferenceSamples.Remove(reference);
            _db.SaveChanges();
            _storage.Delete(reference.ImagePath);

            return NoContent();
        }

        private IQueryable<ReferenceSample> WithDetails() =>
            _db.ReferenceSamples.Include(r => r.DefectClass).Include(r => r.AddedBy);
    }
}
