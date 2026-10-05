using SteelAnalyze.Models;

namespace SteelAnalyze.Data
{
    public class ApplicationDbContext
    {
        public List<DefectCategory> Categories { get; set; } = new();
        public List<InspectionSession> Sessions { get; set; } = new();

        public ApplicationDbContext()
        {
            // Инициализация тестовыми данными (Seed Data)
            Categories = new List<DefectCategory>
            {
                new DefectCategory { Id = 1, Name = "Crack", Description = "Трещина", SeverityLevel = 9 },
                new DefectCategory { Id = 2, Name = "Scratch", Description = "Царапина", SeverityLevel = 4 },
                new DefectCategory { Id = 3, Name = "Inclusion", Description = "Включение", SeverityLevel = 6 }
            };

            var session1 = new InspectionSession { Id = 1, FileName = "batch_001.jpg" };
            session1.AddDefect(new DefectInstance { Id = 101, CategoryId = 1, ConfidenceScore = 0.92, ImageUrl = "/img/1.jpg" });
            session1.AddDefect(new DefectInstance { Id = 102, CategoryId = 2, ConfidenceScore = 0.45, ImageUrl = "/img/2.jpg" });

            var session2 = new InspectionSession { Id = 2, FileName = "batch_002.jpg" };
            session2.AddDefect(new DefectInstance { Id = 201, CategoryId = 3, ConfidenceScore = 0.88, ImageUrl = "/img/3.jpg" });

            Sessions = new List<InspectionSession> { session1, session2 };
        }
    }
}