using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo;

public class DemoController : Controller
{
    private readonly INPlusOneService _nPlusOneService;
    private readonly ILogger<DemoController> _logger;

    public DemoController(INPlusOneService nPlusOneService, ILogger<DemoController> logger)
    {
        _nPlusOneService = nPlusOneService;
        _logger = logger;
    }

    // MVC View Actions
    public IActionResult Index()
    {
        return View("~/Features/Demo/Views/Index.cshtml");
    }

    public IActionResult Performance()
    {
        return View("~/Features/Demo/Views/Performance.cshtml");
    }

    public IActionResult ErrorHandling()
    {
        return View("~/Features/Demo/Views/ErrorHandling.cshtml");
    }

    public IActionResult Security()
    {
        return View("~/Features/Demo/Views/Security.cshtml");
    }

    public IActionResult DataStructures()
    {
        return View("~/Features/Demo/Views/DataStructures.cshtml");
    }

    // API Endpoints
    [HttpGet("api/demo/n-plus-one/bad")]
    public async Task<IActionResult> NPlusOneBad()
    {
        try
        {
            var result = await _nPlusOneService.GetUsersBad();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in N+1 bad endpoint");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/demo/n-plus-one/good")]
    public async Task<IActionResult> NPlusOneGood()
    {
        try
        {
            var result = await _nPlusOneService.GetUsersGood();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in N+1 good endpoint");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
