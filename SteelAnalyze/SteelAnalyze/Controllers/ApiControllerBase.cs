using Microsoft.AspNetCore.Mvc;
using SteelAnalyze.Services;

namespace SteelAnalyze.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected ActionResult OkOrError<T>(ServiceResult<T> result) =>
            result.Error == null ? Ok(result.Value) : ErrorResponse(result.Error);

        protected ActionResult NoContentOrError(ServiceResult result) =>
            result.Error == null ? NoContent() : ErrorResponse(result.Error);

        protected ObjectResult ErrorResponse(ServiceError error)
        {
            var statusCode = error.Kind switch
            {
                ServiceErrorKind.NotFound => StatusCodes.Status404NotFound,
                ServiceErrorKind.Conflict => StatusCodes.Status409Conflict,
                ServiceErrorKind.InvalidInput => StatusCodes.Status400BadRequest,
                ServiceErrorKind.InvalidCredentials => StatusCodes.Status401Unauthorized,
                ServiceErrorKind.ModelUnavailable => StatusCodes.Status503ServiceUnavailable,
                ServiceErrorKind.NotSteelSurface => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { message = error.Message });
        }
    }
}
