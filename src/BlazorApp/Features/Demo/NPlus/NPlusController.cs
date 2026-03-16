using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo.NPlus;

[Route("Demo")]
public class NPlusController : Controller
{
    private readonly INPlusOneService _nPlusOneService;
    private readonly ILogger<NPlusController> _logger;

    public NPlusController(INPlusOneService nPlusOneService, ILogger<NPlusController> logger)
    {
        _nPlusOneService = nPlusOneService;
        _logger = logger;
    }

    [Route("Performance")]
    public IActionResult Performance()
    {
        return View("~/Features/Demo/NPlus/Views/Performance.cshtml");
    }

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
