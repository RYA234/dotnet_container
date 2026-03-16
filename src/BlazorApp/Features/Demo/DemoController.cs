using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Demo;

[Route("Demo")]
public class DemoController : Controller
{
    public IActionResult Index()
    {
        return View("~/Features/Demo/Views/Index.cshtml");
    }
}
