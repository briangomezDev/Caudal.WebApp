using Microsoft.AspNetCore.Mvc;

namespace Caudal.WebApp.Controllers;

public class PlatformController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
