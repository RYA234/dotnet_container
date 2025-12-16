using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Inventory;

public class InventoryController : Controller
{
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(ILogger<InventoryController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("~/Features/Inventory/Views/Index.cshtml");
    }
}
