using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Dtos;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly AuthService _auth;

        public AuthController(AuthService auth)
        {
            _auth = auth;
        }

        // Выдача JWT по логину и паролю
        [HttpPost("login")]
        public ActionResult<LoginResponse> Login(LoginRequest request) =>
            OkOrError(_auth.Login(request.Username, request.Password));
    }
}
