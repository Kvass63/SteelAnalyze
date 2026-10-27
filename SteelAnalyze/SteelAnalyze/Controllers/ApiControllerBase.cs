using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
    // Общая часть контроллеров: превращает результат сервиса в ответ HTTP
    public abstract class ApiControllerBase : ControllerBase
    {
        // Данные -> 200, ошибка -> её код
        protected ActionResult OkOrError<T>(ServiceResult<T> result) =>
            result.Error == null ? Ok(result.Value) : ErrorResponse(result.Error);

        // Успех -> 204, ошибка -> её код
        protected ActionResult NoContentOrError(ServiceResult result) =>
            result.Error == null ? NoContent() : ErrorResponse(result.Error);

        // Ответ с кодом ошибки и текстом { "message": ... }
        protected ObjectResult ErrorResponse(ServiceError error)
        {
            var statusCode = error.Kind switch
            {
                ServiceErrorKind.NotFound => StatusCodes.Status404NotFound,
                ServiceErrorKind.Conflict => StatusCodes.Status409Conflict,
                ServiceErrorKind.InvalidInput => StatusCodes.Status400BadRequest,
                ServiceErrorKind.InvalidCredentials => StatusCodes.Status401Unauthorized,
                ServiceErrorKind.ModelUnavailable => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { message = error.Message });
        }
    }
}
