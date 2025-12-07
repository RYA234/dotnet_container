using Microsoft.AspNetCore.Mvc;
using BlazorApp.Services;

namespace BlazorApp.Features.Orders;

public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Calculate(string productName, int quantity, decimal price)
    {
        try
        {
            var order = new Order
            {
                ProductName = productName,
                Quantity = quantity,
                Price = price
            };

            var finalPrice = _orderService.CalculateFinalPrice(order);

            ViewBag.ProductName = productName;
            ViewBag.Quantity = quantity;
            ViewBag.Price = price;
            ViewBag.TotalAmount = order.TotalAmount;
            ViewBag.FinalPrice = finalPrice;
            ViewBag.Discount = order.TotalAmount - finalPrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating order for {ProductName}", productName);
            ViewBag.Error = ex.Message;
        }

        return View("Index");
    }
}
