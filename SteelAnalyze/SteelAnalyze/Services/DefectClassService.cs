using SteelAnalyze.Data;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;

namespace SteelAnalyze.Services
{
    // Справочник классов дефектов
    public class DefectClassService
    {
        private readonly ApplicationDbContext _db;

        public DefectClassService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Сначала самые опасные классы
        public List<DefectClass> GetAll() =>
            _db.DefectClasses.OrderByDescending(c => c.SeverityLevel).ThenBy(c => c.Code).ToList();

        public ServiceResult<DefectClass> GetById(int id)
        {
            var defectClass = _db.DefectClasses.FirstOrDefault(c => c.Id == id);
            if (defectClass == null) return NotFound(id);

            return defectClass;
        }

        public ServiceResult<DefectClass> Create(DefectClassInput input)
        {
            if (_db.DefectClasses.Any(c => c.Code == input.Code)) return CodeTaken(input.Code);

            var defectClass = new DefectClass
            {
                Code = input.Code,
                Name = input.Name,
                Description = input.Description,
                SeverityLevel = input.SeverityLevel
            };
            _db.DefectClasses.Add(defectClass);
            _db.SaveChanges();

            return defectClass;
        }

        public ServiceResult Update(int id, DefectClassInput input)
        {
            var defectClass = _db.DefectClasses.FirstOrDefault(c => c.Id == id);
            if (defectClass == null) return NotFound(id);
            if (_db.DefectClasses.Any(c => c.Code == input.Code && c.Id != id)) return CodeTaken(input.Code);

            defectClass.Code = input.Code;
            defectClass.Name = input.Name;
            defectClass.Description = input.Description;
            defectClass.SeverityLevel = input.SeverityLevel;
            _db.SaveChanges();

            return ServiceResult.Success;
        }

        // Класс нельзя удалить, пока на него ссылаются эталоны или журнал
        public ServiceResult Delete(int id)
        {
            var defectClass = _db.DefectClasses.FirstOrDefault(c => c.Id == id);
            if (defectClass == null) return NotFound(id);

            var isUsed = _db.ReferenceSamples.Any(r => r.DefectClassId == id)
                || _db.Inspections.Any(i => i.PredictedClassId == id)
                || _db.InspectionNeighbors.Any(n => n.DefectClassId == id);
            if (isUsed)
                return ServiceError.Conflict("Класс используется в эталонах или журнале и не может быть удалён");

            _db.DefectClasses.Remove(defectClass);
            _db.SaveChanges();

            return ServiceResult.Success;
        }

        private static ServiceError NotFound(int id) => ServiceError.NotFound($"Класс с Id={id} не найден");

        private static ServiceError CodeTaken(string code) => ServiceError.Conflict($"Класс с кодом '{code}' уже существует");
    }
}
