namespace SteelAnalyze.Models
{
    // Справочник классов дефектов
    public class DefectClass
    {
        // Опасность, начиная с которой класс считается критическим
        public const int CriticalSeverity = 8;

        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // Короткое имя, например "crack"
        public string Name { get; set; } = string.Empty; // Название, например "Трещина"
        public string Description { get; set; } = string.Empty;
        public int SeverityLevel { get; set; } // От 1 до 10

        // Бизнес-логика: Определение критичности дефекта
        public bool IsCritical() => IsCriticalSeverity(SeverityLevel);

        // Бизнес-логика: Критична ли опасность с таким уровнем
        public static bool IsCriticalSeverity(int severityLevel) => severityLevel >= CriticalSeverity;
    }
}
