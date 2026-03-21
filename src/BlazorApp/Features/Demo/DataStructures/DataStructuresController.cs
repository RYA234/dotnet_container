using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Demo.DataStructures;

[Route("Demo")]
public class DataStructuresController : Controller
{
    [Route("DataStructures")]
    public IActionResult DataStructures()
    {
        return View("~/Features/Demo/DataStructures/Views/DataStructures.cshtml");
    }
}
