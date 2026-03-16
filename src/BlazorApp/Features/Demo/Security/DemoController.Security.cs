using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Demo;

public partial class DemoController
{
    public IActionResult Security()
    {
        return View("~/Features/Demo/Security/Views/Security.cshtml");
    }
}
