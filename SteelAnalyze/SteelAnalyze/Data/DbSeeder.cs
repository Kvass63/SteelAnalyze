using SteelAnalyze.Auth;
using SteelAnalyze.Models;

namespace SteelAnalyze.Data
{
    // Начальное заполнение пустой базы
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            if (!db.Users.Any())
            {
                db.Users.Add(AppUser.Create("admin", "admin123", Roles.Admin));
                db.Users.Add(AppUser.Create("operator", "operator123", Roles.User));
            }

            // 10 классов из ВКР; опасность оценена по описаниям дефектов и может быть изменена инженером
            if (!db.DefectClasses.Any())
            {
                db.DefectClasses.AddRange(
                    new DefectClass { Code = "hole", Name = "Отверстие", SeverityLevel = 10, Description = "Сквозное или поверхностное нарушение целостности металла округлой формы" },
                    new DefectClass { Code = "crack", Name = "Трещина", SeverityLevel = 9, Description = "Узкий разрыв металла, концентрирующий напряжения" },
                    new DefectClass { Code = "crescent_gap", Name = "Серповидный зазор", SeverityLevel = 8, Description = "Дугообразный зазор при неполном заполнении металлом или некачественной сварке полос" },
                    new DefectClass { Code = "inclusion", Name = "Включение", SeverityLevel = 7, Description = "Неметаллические частицы в структуре металла, вытянутые вдоль прокатки" },
                    new DefectClass { Code = "weld_line", Name = "Сварной шов", SeverityLevel = 6, Description = "Видимая линия сварного соединения полос" },
                    new DefectClass { Code = "pitted_surface", Name = "Точечная коррозия", SeverityLevel = 6, Description = "Множественные мелкие углубления, локальное утонение металла" },
                    new DefectClass { Code = "roll_mark", Name = "Отпечаток валка", SeverityLevel = 5, Description = "Периодически повторяющиеся следы рабочих валков" },
                    new DefectClass { Code = "oxide_scale", Name = "Окалина", SeverityLevel = 5, Description = "Окисленный слой, оставшийся после горячей обработки" },
                    new DefectClass { Code = "scratch", Name = "Царапина", SeverityLevel = 4, Description = "Узкая канавка от контакта с острыми предметами или абразивом" },
                    new DefectClass { Code = "stain", Name = "Пятно", SeverityLevel = 2, Description = "Локальное изменение цвета без нарушения рельефа" });
            }

            db.SaveChanges();
        }
    }
}
