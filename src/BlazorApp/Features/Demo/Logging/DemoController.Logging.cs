using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo;

/// <summary>マスキングデモ用リクエスト</summary>
public record MaskRequest(string Input);

public partial class DemoController
{
    public IActionResult Logging()
    {
        return View("~/Features/Demo/Logging/Views/Logging.cshtml");
    }

    [HttpGet("api/demo/logging/levels")]
    public IActionResult LogAllLevels()
    {
        _loggingDemoService.LogAllLevels();
        return Ok(new
        {
            message = "4つのログレベル（Debug/Information/Warning/Error）をサーバーコンソールに出力しました",
            note = "ブラウザのレスポンスではなく、サーバーのログ出力を確認してください"
        });
    }

    [HttpGet("api/demo/logging/performance")]
    public IActionResult LogPerformance([FromQuery] long elapsedMs = 500)
    {
        var result = _loggingDemoService.LogPerformance("SimulatedOperation", elapsedMs);
        return Ok(new
        {
            operationName = result.OperationName,
            elapsedMs = result.ElapsedMs,
            isSlowOperation = result.IsSlowOperation,
            logLevel = result.IsSlowOperation ? "Warning" : "Information",
            message = result.IsSlowOperation
                ? $"Slow operation detected: {elapsedMs}ms > 1000ms → Warning ログ出力"
                : $"Operation completed: {elapsedMs}ms ≤ 1000ms → Information ログ出力"
        });
    }

    [HttpPost("api/demo/logging/mask")]
    public IActionResult LogMask([FromBody] MaskRequest request)
    {
        var masked = _loggingDemoService.MaskAndLog(request.Input);
        return Ok(new
        {
            original = request.Input,
            masked,
            message = "機密情報をマスキングしてログ出力しました"
        });
    }
}
