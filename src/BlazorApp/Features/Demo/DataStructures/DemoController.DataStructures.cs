using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Demo;

public partial class DemoController
{
    public IActionResult DataStructures()
    {
        return View("~/Features/Demo/DataStructures/Views/DataStructures.cshtml");
    }
}
