using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SteelAnalyze.Auth;
using SteelAnalyze.Dtos;

namespace SteelAnalyze.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserStore _users;
        private readonly JwtSettings _jwt;

        public AuthController(UserStore users, IOptions<JwtSettings> jwt)
        {
            _users = users;
            _jwt = jwt.Value;
        }

        // Выдача JWT по логину и паролю
        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var user = _users.Validate(request.Username, request.Password);
            if (user == null) return Unauthorized(new { message = "Неверный логин или пароль" });

            var claims = new List<Claim>
            {
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
