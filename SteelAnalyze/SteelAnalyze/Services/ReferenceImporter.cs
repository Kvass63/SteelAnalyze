using SteelAnalyze.Data;
using SteelAnalyze.Dtos;
using SteelAnalyze.Models;

namespace SteelAnalyze.Services
{
    // Загрузка партии эталонов из папки на сервере: <папка>/<код класса>/<снимки>
    public class ReferenceImporter
    {
        private const int SaveBatchSize = 100;

        // Одновременно идёт только один импорт: иначе второй не видит ещё не сохранённые снимки первого и добавляет их повторно
        private static readonly SemaphoreSlim ImportLock = new(1, 1);

        private readonly ApplicationDbContext _db;
        private readonly IEmbeddingExtractor _extractor;
        private readonly ImageStorage _storage;
        private readonly ILogger<ReferenceImporter> _logger;

        public ReferenceImporter(ApplicationDbContext db, IEmbeddingExtractor extractor, ImageStorage storage, ILogger<ReferenceImporter> logger)
        {
            _db = db;
            _extractor = extractor;
            _storage = storage;
            _logger = logger;
        }

        // null — другой импорт ещё не закончился
        public ReferenceImportResult? TryImport(string folder, int adminId)
        {
            if (!ImportLock.Wait(0)) return null;
            try
            {
                return Import(folder, adminId);
            }
            finally
            {
                ImportLock.Release();
            }
        }

        private ReferenceImportResult Import(string folder, int adminId)
        {
            var classes = _db.DefectClasses.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
            var knownHashes = _db.ReferenceSamples.Select(r => r.ContentHash).ToHashSet();

            var added = 0;
            var duplicates = 0;
            var unknownClasses = new List<string>();
            var failedFiles = new List<string>();

            foreach (var classFolder in Directory.GetDirectories(folder).Order())
            {
                var code = Path.GetFileName(classFolder);
                if (!classes.TryGetValue(code, out var defectClass))
                {
                    unknownClasses.Add(code);
                    continue;
                }

                foreach (var file in Directory.GetFiles(classFolder).Order())
                {
                    var fileName = Path.GetFileName(file);
                    if (ImageStorage.Validate(fileName, new FileInfo(file).Length) != null)
                    {
                        failedFiles.Add($"{code}/{fileName}");
                        continue;
                    }

                    var content = File.ReadAllBytes(file);
                    var hash = ImageStorage.GetContentHash(content);
                    if (knownHashes.Contains(hash))
                    {
                        duplicates++;
                        continue;
                    }

                    float[] embedding;
                    try
                    {
                        embedding = _extractor.Extract(content);
                    }
                    catch (InvalidDataException)
                    {
                        failedFiles.Add($"{code}/{fileName}");
                        continue;
                    }

                    _db.ReferenceSamples.Add(new ReferenceSample
                    {
                        DefectClassId = defectClass.Id,
                        OriginalFileName = fileName,
                        ContentHash = hash,
                        Embedding = EmbeddingCodec.ToBytes(embedding),
                        ModelVersion = _extractor.ModelVersion!,
                        AddedByUserId = adminId,
                        ImagePath = _storage.Save(ImageStorage.ReferencesFolder, fileName, content)
                    });
                    knownHashes.Add(hash);

                    added++;
                    if (added % SaveBatchSize == 0)
                    {
                        _db.SaveChanges();
                        _logger.LogInformation("Импорт эталонов из {Folder}: добавлено {Added}", folder, added);
                    }
                }
            }

            _db.SaveChanges();
            _logger.LogInformation("Импорт эталонов из {Folder} завершён: добавлено {Added}, повторов {Duplicates}, ошибок {Failed}",
                folder, added, duplicates, failedFiles.Count);

            return new ReferenceImportResult(added, duplicates, unknownClasses, failedFiles);
        }
    }
}
