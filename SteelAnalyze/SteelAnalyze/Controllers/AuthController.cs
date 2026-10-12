using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SteelAnalyze.Auth;
using SteelAnalyze.Data;
using SteelAnalyze.Dtos;

namespace SteelAnalyze.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly JwtSettings _jwt;

        public AuthController(ApplicationDbContext db, IOptions<JwtSettings> jwt)
        {
            _db = db;
            _jwt = jwt.Value;
        }

        // Выдача JWT по логину и паролю
        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var user = _db.Users.FirstOrDefault(u => u.Username == request.Username);
            if (user == null || !user.VerifyPassword(request.Password))
                return Unauthorized(new { message = "Неверный логин или пароль" });

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role)
            };
            var jwt = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.LifetimeMinutes),
                signingCredentials: new SigningCredentials(_jwt.GetSecurityKey(), SecurityAlgorithms.HmacSha256));

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(jwt),
                username = user.Username,
                role = user.Role
            });
        }
    }
}
