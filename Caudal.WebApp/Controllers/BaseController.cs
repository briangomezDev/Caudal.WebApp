using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Caudal.WebApp.Controllers;

public abstract class BaseController : Controller
{
    protected IActionResult ToErrorResult(List<Error> errors)
    {
        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        var firstError = errors.First(error => error.Type != ErrorType.Validation);

        var statusCode = firstError.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: firstError.Code, detail: firstError.Description);
    }
}
