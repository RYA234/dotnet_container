using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Demo;

public partial class DemoController
{
    public IActionResult FullScan()
    {
        return View("~/Features/Demo/FullScan/Views/FullScan.cshtml");
    }

    [HttpPost("api/demo/full-scan/setup")]
    public async Task<IActionResult> FullScanSetup()
    {
        try
        {
            var result = await _fullScanService.SetupAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in full-scan setup endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }

    [HttpGet("api/demo/full-scan/without-index")]
    public async Task<IActionResult> FullScanWithoutIndex([FromQuery] string? email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { error = "emailパラメータが必要です", code = "MISSING_PARAM" });

        try
        {
            var result = await _fullScanService.SearchWithoutIndexAsync(email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in full-scan without-index endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }

    [HttpPost("api/demo/full-scan/create-index")]
    public async Task<IActionResult> FullScanCreateIndex()
    {
        try
        {
            var result = await _fullScanService.CreateIndexAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in full-scan create-index endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }

    [HttpGet("api/demo/full-scan/with-index")]
    public async Task<IActionResult> FullScanWithIndex([FromQuery] string? email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { error = "emailパラメータが必要です", code = "MISSING_PARAM" });

        try
        {
            var result = await _fullScanService.SearchWithIndexAsync(email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in full-scan with-index endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }
}
