using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Production;

public class ProductionController : Controller
{
    private readonly ILogger<ProductionController> _logger;

    public ProductionController(ILogger<ProductionController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("~/Features/Production/Views/Index.cshtml");
    }
}
