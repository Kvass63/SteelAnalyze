using System.Security.Claims;

namespace SteelAnalyze.Auth
{
    public static class ClaimsExtensions
    {
        // Id пользователя записывается в токен при входе (см. AuthController)
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("В токене нет идентификатора пользователя"));
    }
}
