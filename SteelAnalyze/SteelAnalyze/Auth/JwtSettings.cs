using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SteelAnalyze.Auth
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int LifetimeMinutes { get; set; } = 60;

        public SymmetricSecurityKey GetSecurityKey() => new(Encoding.UTF8.GetBytes(Key));
    }
}
