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
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<DefectCategory>> GetAll() => Ok(_db.Categories);

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<DefectCategory> GetById(int id)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Id == id);
            return category == null ? NotFound() : Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult<DefectCategory> Create(CategoryInput input)
        {
            var category = new DefectCategory
            {
                Id = _db.Categories.Select(c => c.Id).DefaultIfEmpty(0).Max() + 1,
                Name = input.Name,
                Description = input.Description,
                SeverityLevel = input.SeverityLevel
            };
            _db.Categories.Add(category);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Update(int id, CategoryInput input)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null) return NotFound();

            category.Name = input.Name;
            category.Description = input.Description;
            category.SeverityLevel = input.SeverityLevel;
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Delete(int id)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null) return NotFound();
            if (_db.Sessions.Any(s => s.Defects.Any(d => d.CategoryId == id)))
                return Conflict(new { message = "Категория используется в дефектах и не может быть удалена" });

            _db.Categories.Remove(category);
            return NoContent();
        }
    }
}
