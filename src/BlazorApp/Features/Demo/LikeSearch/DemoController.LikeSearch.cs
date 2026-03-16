using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Features.Demo;

public partial class DemoController
{
    public IActionResult LikeSearch()
    {
        return View("~/Features/Demo/LikeSearch/Views/LikeSearch.cshtml");
    }

    [HttpPost("api/demo/like-search/setup")]
    public async Task<IActionResult> LikeSearchSetup()
    {
        try
        {
            var result = await _likeSearchService.SetupAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in like-search setup endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }

    [HttpGet("api/demo/like-search/prefix")]
    public async Task<IActionResult> LikeSearchPrefix([FromQuery] string? keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return BadRequest(new { error = "keywordパラメータが必要です", code = "MISSING_PARAM" });

        try
        {
            var result = await _likeSearchService.SearchPrefixAsync(keyword);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in like-search prefix endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }

    [HttpGet("api/demo/like-search/partial")]
    public async Task<IActionResult> LikeSearchPartial([FromQuery] string? keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return BadRequest(new { error = "keywordパラメータが必要です", code = "MISSING_PARAM" });

        try
        {
            var result = await _likeSearchService.SearchPartialAsync(keyword);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in like-search partial endpoint");
            return StatusCode(500, new { error = ex.Message, code = "INTERNAL_ERROR" });
        }
    }
}
