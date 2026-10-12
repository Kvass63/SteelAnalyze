using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Auth;
using SteelAnalyze.Data;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;

namespace SteelAnalyze.Controllers
{
    [Route("api/defect-classes")]
    [ApiController]
    public class DefectClassesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DefectClassesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<DefectClass>> GetAll() =>
            Ok(_db.DefectClasses.OrderByDescending(c => c.SeverityLevel).ThenBy(c => c.Code).ToList());

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<DefectClass> GetById(int id)
        {
            var defectClass = _db.DefectClasses.FirstOrDefault(c => c.Id == id);
            return defectClass == null ? NotFound() : Ok(defectClass);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<DefectClass> Create(DefectClassInput input)
        {
            if (_db.DefectClasses.Any(c => c.Code == input.Code))
                return Conflict(new { message = $"Класс с кодом '{input.Code}' уже существует" });

            var defectClass = new DefectClass
            {
                Code = input.Code,
                Name = input.Name,
                Description = input.Description,
                SeverityLevel = input.SeverityLevel
            };
            _db.DefectClasses.Add(defectClass);
            _db.SaveChanges();

            return CreatedAtAction(nameof(GetById), new { id = defectClass.Id }, defectClass);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Update(int id, DefectClassInput input)
        {
            var defectClass = _db.DefectClasses.FirstOrDefault(c => c.Id == id);
            if (defectClass == null) return NotFound();
            if (_db.DefectClasses.Any(c => c.Code == input.Code && c.Id != id))
                return Conflict(new { message = $"Класс с кодом '{input.Code}' уже существует" });

            defectClass.Code = input.Code;
            defectClass.Name = input.Name;
            defectClass.Description = input.Description;
            defectClass.SeverityLevel = input.SeverityLevel;
            _db.SaveChanges();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id)
        {
            var defectClass = _db.DefectClasses.FirstOrDefault(c => c.Id == id);
            if (defectClass == null) return NotFound();

            var isUsed = _db.ReferenceSamples.Any(r => r.DefectClassId == id)
                || _db.Inspections.Any(i => i.PredictedClassId == id)
                || _db.InspectionNeighbors.Any(n => n.DefectClassId == id);
            if (isUsed)
                return Conflict(new { message = "Класс используется в эталонах или журнале и не может быть удалён" });

            _db.DefectClasses.Remove(defectClass);
            _db.SaveChanges();

            return NoContent();
        }
    }
}
