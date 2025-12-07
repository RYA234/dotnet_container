using Microsoft.AspNetCore.Mvc;
using BlazorApp.Services;

namespace BlazorApp.Controllers;

public class CalculatorController : Controller
{
    private readonly ICalculatorService _calculatorService;
    private readonly ILogger<CalculatorController> _logger;

    public CalculatorController(ICalculatorService calculatorService, ILogger<CalculatorController> logger)
    {
        _calculatorService = calculatorService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Calculate(int a, int b, string op)
    {
        try
        {
            object result = op switch
            {
                "+" => _calculatorService.Add(a, b),
                "-" => _calculatorService.Subtract(a, b),
                "*" => _calculatorService.Multiply(a, b),
                "/" => _calculatorService.Divide(a, b),
                _ => throw new InvalidOperationException($"Unknown operator: {op}")
            };

            ViewBag.A = a;
            ViewBag.B = b;
            ViewBag.Op = op;
            ViewBag.Result = result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating {A} {Op} {B}", a, op, b);
            ViewBag.Error = ex.Message;
        }

        return View("Index");
    }
}
