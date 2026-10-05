using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Data;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;

namespace SteelAnalyze.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InspectionSessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public InspectionSessionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // READ: Получить все сессии (любой авторизованный пользователь)
        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<InspectionSession>> GetAll() => Ok(_db.Sessions);

        // READ: Получить сессию по ID
        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<InspectionSession> GetById(int id)
        {
            var session = _db.Sessions.FirstOrDefault(s => s.Id == id);
            if (session == null) return NotFound();
            return Ok(session);
        }

        // CREATE: Создать новую сессию (только администратор)
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<InspectionSession> Create(SessionInput input)
        {
            if (!ValidateCategories(input.Defects, out var error)) return BadRequest(new { message = error });

            var session = new InspectionSession
            {
                Id = _db.Sessions.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1,
                FileName = input.FileName
            };
            FillDefects(session, input.Defects);
            if (session.Defects.Count == 0) session.EvaluateSessionStatus();

            _db.Sessions.Add(session);
            return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
        }

        // UPDATE: Полностью заменить имя файла и список дефектов (только администратор)
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Update(int id, SessionInput input)
        {
            var existing = _db.Sessions.FirstOrDefault(s => s.Id == id);
            if (existing == null) return NotFound();
            if (!ValidateCategories(input.Defects, out var error)) return BadRequest(new { message = error });

            existing.FileName = input.FileName;
            existing.Defects = new List<DefectInstance>();
            FillDefects(existing, input.Defects);
            existing.EvaluateSessionStatus(); // статус пересчитывается и для пустого списка

            return NoContent();
        }

        // DELETE: Удалить сессию (только администратор)
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id)
        {
            var session = _db.Sessions.FirstOrDefault(s => s.Id == id);
            if (session == null) return NotFound();

            _db.Sessions.Remove(session);
            return NoContent();
        }

        private bool ValidateCategories(IEnumerable<DefectInput> defects, out string error)
        {
            var unknown = defects.FirstOrDefault(d => !_db.Categories.Any(c => c.Id == d.CategoryId));
            error = unknown == null ? string.Empty : $"Категория с Id={unknown.CategoryId} не существует";
            return unknown == null;
        }

        // Id дефектов назначает сервер; статус считает модель через AddDefect
        private void FillDefects(InspectionSession session, IEnumerable<DefectInput> defects)
        {
            var nextId = _db.Sessions.SelectMany(s => s.Defects).Select(d => d.Id).DefaultIfEmpty(0).Max() + 1;
            foreach (var d in defects)
            {
                session.AddDefect(new DefectInstance
                {
                    Id = nextId++,
                    CategoryId = d.CategoryId,
                    ImageUrl = d.ImageUrl,
                    ConfidenceScore = d.ConfidenceScore
                });
            }
        }
    }
}
