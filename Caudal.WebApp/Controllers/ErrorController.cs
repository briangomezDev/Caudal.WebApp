using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Caudal.WebApp.Models;

namespace Caudal.WebApp.Controllers;

public class ErrorController : Controller
{
    // Not restricted to GET: a failed POST is re-executed here with its original method.
    [Route("Error/{statusCode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(int statusCode)
    {
        if (statusCode == StatusCodes.Status404NotFound)
        {
            return View("NotFound");
        }

        return View("SystemError", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
