using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Demo;

public partial class DemoController
{
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
