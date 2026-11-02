using Caudal.Application.Services.AppUser.CreateAccount;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Caudal.WebApp.Controllers;

[Route("account")]
public class AccountController : BaseController
{
    private readonly IUserCreateAccountUseCase _createAccount;
    public AccountController(IUserCreateAccountUseCase createAccount)
    {
        _createAccount = createAccount ?? throw new ArgumentNullException(nameof(createAccount));
    }
    [HttpGet("login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("create-account")]
    public async Task<IActionResult> CreateUserAccount([FromBody] UserRequest request, CancellationToken cancellationToken)
    {
        // Malformed JSON leaves request null, and this controller has no [ApiController] to reject it.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _createAccount.CreateAccountAsync(request, cancellationToken);

        return result.Match<IActionResult>(
            _ => Ok(),
            errors => ToErrorResult(errors)
        );
    }

    [HttpGet("restore-password")]
    public IActionResult RestorePassword()
    {
        return View();
    }

    [HttpGet("restore-password-code")]
    public IActionResult RestorePasswordCode()
    {
        return View();
    }

    [HttpGet("restore-password-new")]
    public IActionResult RestorePasswordNew()
    {
        return View();
    }

    [HttpGet("restore-password-done")]
    public IActionResult RestorePasswordDone()
    {
        return View();
    }
}
