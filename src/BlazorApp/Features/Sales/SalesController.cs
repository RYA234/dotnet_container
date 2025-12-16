using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Sales;

public class SalesController : Controller
{
    private readonly ILogger<SalesController> _logger;

    public SalesController(ILogger<SalesController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("~/Features/Sales/Views/Index.cshtml");
    }
}
