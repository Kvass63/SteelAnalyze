using System.ComponentModel.DataAnnotations;

namespace SteelAnalyze.Dtos
{
    public class LoginRequest
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class DefectInput
    {
        public int CategoryId { get; set; }
        [Required] public string ImageUrl { get; set; } = string.Empty;
        [Range(0.0, 1.0)] public double ConfidenceScore { get; set; }
    }

    public class SessionInput
    {
        [Required] public string FileName { get; set; } = string.Empty;
        public List<DefectInput> Defects { get; set; } = new();
    }

    public class CategoryInput
    {
        [Required] public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Range(1, 10)] public int SeverityLevel { get; set; }
    }
}
