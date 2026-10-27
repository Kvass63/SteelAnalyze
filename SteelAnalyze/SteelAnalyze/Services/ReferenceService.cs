using Microsoft.EntityFrameworkCore;
using SteelAnalyze.Data;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;

namespace SteelAnalyze.Services
{
    // База эталонов: снимки с известным классом, с которыми сравниваются проверяемые снимки
    public class ReferenceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmbeddingExtractor _extractor;
        private readonly ImageStorage _storage;

        public ReferenceService(ApplicationDbContext db, IEmbeddingExtractor extractor, ImageStorage storage)
        {
            _db = db;
            _extractor = extractor;
            _storage = storage;
        }

        // Список эталонов, при необходимости только одного класса
        public List<ReferenceDto> GetAll(int? defectClassId)
        {
            var query = WithDetails();
            if (defectClassId != null) query = query.Where(r => r.DefectClassId == defectClassId);

            return query.OrderBy(r => r.Id).AsEnumerable().Select(ReferenceDto.From).ToList();
        }

        public ServiceResult<ReferenceDto> GetById(int id)
        {
            var reference = WithDetails().FirstOrDefault(r => r.Id == id);
            if (reference == null) return NotFound(id);

            return ReferenceDto.From(reference);
        }

        // Полный путь к файлу снимка эталона на диске
        public ServiceResult<string> GetImageFullPath(int id)
        {
            var reference = _db.ReferenceSamples.FirstOrDefault(r => r.Id == id);
            if (reference == null) return NotFound(id);

            var fullPath = _storage.GetFullPath(reference.ImagePath);
            if (!File.Exists(fullPath)) return ServiceError.NotFound("Файл снимка отсутствует в хранилище");

            return fullPath;
        }

        // Добавить эталон; эмбеддинг считается по снимку
        public ServiceResult<ReferenceDto> Create(IFormFile? image, int defectClassId, int adminId)
        {
            if (!_extractor.IsAvailable) return ServiceError.ModelUnavailable();

            var error = ImageStorage.Validate(image);
            if (error != null) return ServiceError.InvalidInput(error);
            if (!_db.DefectClasses.Any(c => c.Id == defectClassId)) return ClassMissing(defectClassId);

            var content = ImageStorage.ReadAllBytes(image!);
            var hash = ImageStorage.GetContentHash(content);
            if (_db.ReferenceSamples.Any(r => r.ContentHash == hash))
                return ServiceError.Conflict("Этот снимок уже есть среди эталонов");

            float[] embedding;
            try
            {
                embedding = _extractor.Extract(content);
            }
            catch (InvalidDataException)
            {
                return ServiceError.InvalidInput("Файл не является изображением");
            }

            var reference = new ReferenceSample
            {
                DefectClassId = defectClassId,
                OriginalFileName = image!.FileName,
                ContentHash = hash,
                Embedding = EmbeddingCodec.ToBytes(embedding),
                ModelVersion = _extractor.ModelVersion!,
                AddedByUserId = adminId,
                ImagePath = _storage.Save(ImageStorage.ReferencesFolder, image!.FileName, content)
            };
            _db.ReferenceSamples.Add(reference);
            _db.SaveChanges();

            return ReferenceDto.From(WithDetails().First(r => r.Id == reference.Id));
        }

        // Исправить класс эталона
        public ServiceResult ChangeClass(int id, int defectClassId)
        {
            var reference = _db.ReferenceSamples.FirstOrDefault(r => r.Id == id);
            if (reference == null) return NotFound(id);
            if (!_db.DefectClasses.Any(c => c.Id == defectClassId)) return ClassMissing(defectClassId);

            reference.DefectClassId = defectClassId;
            _db.SaveChanges();

            return ServiceResult.Success;
        }

        // Удалить эталон вместе с файлом снимка
        public ServiceResult Delete(int id)
        {
            var reference = _db.ReferenceSamples.FirstOrDefault(r => r.Id == id);
            if (reference == null) return NotFound(id);

            _db.ReferenceSamples.Remove(reference);
            _db.SaveChanges();
            _storage.Delete(reference.ImagePath);

            return ServiceResult.Success;
        }

        private IQueryable<ReferenceSample> WithDetails() =>
            _db.ReferenceSamples.Include(r => r.DefectClass).Include(r => r.AddedBy);

        private static ServiceError NotFound(int id) => ServiceError.NotFound($"Эталон с Id={id} не найден");

        private static ServiceError ClassMissing(int defectClassId) =>
            ServiceError.InvalidInput($"Класс с Id={defectClassId} не существует");
    }
}
