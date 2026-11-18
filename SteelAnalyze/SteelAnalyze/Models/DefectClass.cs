namespace SteelAnalyze.Models
{
    public class DefectClass
    {
        public const int CriticalSeverity = 8;

        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SeverityLevel { get; set; }

        public bool IsCritical() => IsCriticalSeverity(SeverityLevel);

        public static bool IsCriticalSeverity(int severityLevel) => severityLevel >= CriticalSeverity;
    }
}
