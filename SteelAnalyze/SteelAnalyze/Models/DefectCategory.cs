namespace SteelAnalyze.Models
{
    // 1. Справочник типов дефектов
    public class DefectCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Например: "Crack", "Scratch"
        public string Description { get; set; } = string.Empty;
        public int SeverityLevel { get; set; } // От 1 до 10

        // Бизнес-логика: Определение критичности дефекта
        public bool IsCritical() => SeverityLevel >= 8;

        // Бизнес-логика: Форматирование информации для отчетов
        public string GetFormattedInfo() => $"[{Name}] (Уровень опасности: {SeverityLevel}/10)";
    }

    // 2. Конкретный обнаруженный дефект на изображении
    public class DefectInstance
    {
        public int Id { get; set; }
        public int CategoryId { get; set; } // Ссылка на DefectCategory
        public string ImageUrl { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; } // Уверенность от 0.0 до 1.0
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        // Бизнес-логика: Проверка надежности детекции
        public bool IsReliable() => ConfidenceScore >= 0.75;

        // Бизнес-логика: Оценка риска конкретного дефекта
        public string GetRiskAssessment()
        {
            if (ConfidenceScore < 0.5) return "Низкий риск (требуется перепроверка)";
            if (ConfidenceScore < 0.85) return "Средний риск (внимание оператора)";
            return "Высокий риск (критическое отклонение)";
        }
    }

    // 3. Сессия инспекции (агрегирующий корень)
    public class InspectionSession
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime InspectedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public List<DefectInstance> Defects { get; set; } = new();

        // Бизнес-логика: Добавление дефекта с автоматической переоценкой статуса сессии
        public void AddDefect(DefectInstance defect)
        {
            Defects.Add(defect);
            EvaluateSessionStatus();
        }

        // Бизнес-логика: Автоматическое определение статуса партии/файла
        public void EvaluateSessionStatus()
        {
            // Если есть хотя бы один надежный дефект, сессия считается забракованной
            if (Defects.Any(d => d.IsReliable()))
            {
                Status = "Rejected";
            }
            else if (Defects.Count == 0)
            {
                Status = "Approved";
            }
            else
            {
                Status = "Pending"; // Есть дефекты, но они ненадежные, нужно ручное вмешательство
            }
        }
    }
}