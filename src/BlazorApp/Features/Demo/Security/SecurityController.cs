using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Demo.Security;

[Route("Demo")]
public class SecurityController : Controller
{
    [Route("Security")]
    public IActionResult Security()
    {
        return View("~/Features/Demo/Security/Views/Security.cshtml");
    }
}
