using Microsoft.EntityFrameworkCore;
using SteelAnalyze.Data;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;

namespace SteelAnalyze.Services
{
    // Журнал распознавания
    public class InspectionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmbeddingExtractor _extractor;
        private readonly RecognitionService _recognition;
        private readonly ImageStorage _storage;

        public InspectionService(ApplicationDbContext db, IEmbeddingExtractor extractor, RecognitionService recognition, ImageStorage storage)
        {
            _db = db;
            _extractor = extractor;
            _recognition = recognition;
            _storage = storage;
        }

        // Проверить снимок: распознать дефект, вынести вердикт и записать в журнал
        public ServiceResult<InspectionDto> Create(IFormFile? image, int operatorId)
        {
            if (!_extractor.IsAvailable) return ServiceError.ModelUnavailable();

            var error = ImageStorage.Validate(image);
            if (error != null) return ServiceError.InvalidInput(error);
            if (!_db.ReferenceSamples.Any(r => r.ModelVersion == _extractor.ModelVersion))
                return ServiceError.Conflict("База эталонов пуста: распознавать не с чем");

            Inspection inspection;
            try
            {
                inspection = _recognition.Inspect(ImageStorage.ReadAllBytes(image!), image!.FileName, operatorId);
            }
            catch (InvalidDataException)
            {
                return ServiceError.InvalidInput("Файл не является изображением");
            }

            return InspectionDto.From(WithDetails().First(i => i.Id == inspection.Id));
        }

        // Журнал с фильтрами по периоду, классу и вердикту системы; сначала новые записи
        public List<InspectionDto> GetAll(DateTime? from, DateTime? to, int? defectClassId, Verdict? verdict)
        {
            var query = WithDetails();
            if (from != null) query = query.Where(i => i.InspectedAt >= from);
            if (to != null) query = query.Where(i => i.InspectedAt < to.Value.Date.AddDays(1)); // день "to" входит в период
            if (defectClassId != null) query = query.Where(i => i.PredictedClassId == defectClassId);
            if (verdict != null) query = query.Where(i => i.Verdict == verdict);

            return query.OrderByDescending(i => i.InspectedAt).AsEnumerable().Select(InspectionDto.From).ToList();
        }

        public ServiceResult<InspectionDto> GetById(int id)
        {
            var inspection = WithDetails().FirstOrDefault(i => i.Id == id);
            if (inspection == null) return NotFound(id);

            return InspectionDto.From(inspection);
        }

        // Полный путь к файлу проверенного снимка на диске
        public ServiceResult<string> GetImageFullPath(int id)
        {
            var inspection = _db.Inspections.FirstOrDefault(i => i.Id == id);
            if (inspection == null) return NotFound(id);

            var fullPath = _storage.GetFullPath(inspection.ImagePath);
            if (!File.Exists(fullPath)) return ServiceError.NotFound("Файл снимка отсутствует в хранилище");

            return fullPath;
        }

        // Решение инженера по снимку, отправленному на ручную проверку
        public ServiceResult Review(int id, Verdict finalVerdict, string? comment, int reviewerId)
        {
            var inspection = _db.Inspections.FirstOrDefault(i => i.Id == id);
            if (inspection == null) return NotFound(id);
            if (inspection.Verdict != Verdict.ManualReview)
                return ServiceError.Conflict("Запись не отправлялась на ручную проверку");
            if (finalVerdict == Verdict.ManualReview)
                return ServiceError.InvalidInput("Решение должно быть Rejected или AcceptedWithRemark");

            inspection.Review(finalVerdict, reviewerId, comment);
            _db.SaveChanges();

            return ServiceResult.Success;
        }

        // Удалить запись журнала вместе с файлом снимка
        public ServiceResult Delete(int id)
        {
            var inspection = _db.Inspections.FirstOrDefault(i => i.Id == id);
            if (inspection == null) return NotFound(id);

            _db.Inspections.Remove(inspection);
            _db.SaveChanges();
            _storage.Delete(inspection.ImagePath);

            return ServiceResult.Success;
        }

        private IQueryable<Inspection> WithDetails() =>
            _db.Inspections
                .Include(i => i.Operator)
                .Include(i => i.PredictedClass)
                .Include(i => i.ReviewedBy)
                .Include(i => i.Neighbors).ThenInclude(n => n.DefectClass);

        private static ServiceError NotFound(int id) => ServiceError.NotFound($"Запись журнала с Id={id} не найдена");
    }
}
