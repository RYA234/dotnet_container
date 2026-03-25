using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo.SelectStar;

[Route("Demo")]
public class SelectStarController : Controller
{
    private readonly ISelectStarService _selectStarService;
    private readonly ILogger<SelectStarController> _logger;

    public SelectStarController(ISelectStarService selectStarService, ILogger<SelectStarController> logger)
    {
        _selectStarService = selectStarService;
        _logger = logger;
    }

    [Route("SelectStar")]
    public IActionResult SelectStar()
    {
        return View("~/Features/Demo/SelectStar/Views/SelectStar.cshtml");
    }

    [HttpPost("/api/demo/select-star/setup")]
    public async Task<IActionResult> SelectStarSetup()
    {
        try
        {
            var result = await _selectStarService.SetupAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in select-star setup endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }

    [HttpGet("/api/demo/select-star/all-columns")]
    public async Task<IActionResult> SelectStarAllColumns()
    {
        try
        {
            var result = await _selectStarService.GetAllColumnsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in select-star all-columns endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }

    [HttpGet("/api/demo/select-star/specific-columns")]
    public async Task<IActionResult> SelectStarSpecificColumns()
    {
        try
        {
            var result = await _selectStarService.GetSpecificColumnsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in select-star specific-columns endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }
}
