using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Error;

public class ErrorController : Controller
{
    [Route("Error/{statusCode:int}")]
    public IActionResult HandleError(int statusCode)
    {
        ViewData["StatusCode"] = statusCode;
        return View("HandleError");
    }
}
