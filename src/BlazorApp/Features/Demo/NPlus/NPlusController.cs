using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo.NPlus;

/// <summary>
/// N+1問題デモ（ループ内クエリ vs JOIN）のコントローラー
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/n-plus-one-demo/internal-design.md</para>
/// <para><strong>責務:</strong> N+1問題が発生するBad実装とJOINで解決したGood実装のAPIエンドポイントを提供する</para>
/// <para><strong>エンドポイント一覧:</strong></para>
/// <list type="bullet">
/// <item><description>GET /Demo/Performance          - デモ画面表示</description></item>
/// <item><description>GET /api/demo/n-plus-one/bad  - N+1問題あり（1+N回クエリ）</description></item>
/// <item><description>GET /api/demo/n-plus-one/good - N+1問題なし（JOINで1回クエリ）</description></item>
/// </list>
/// </remarks>
[Route("Demo")]
public class NPlusController : Controller
{
    private readonly INPlusOneService _nPlusOneService;
    private readonly ILogger<NPlusController> _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="nPlusOneService">N+1問題デモサービス</param>
    /// <param name="logger">ロガー</param>
    public NPlusController(INPlusOneService nPlusOneService, ILogger<NPlusController> logger)
    {
        _nPlusOneService = nPlusOneService;
        _logger = logger;
    }

    /// <summary>デモ画面を表示する</summary>
    [Route("Performance")]
    public IActionResult Performance()
    {
        return View("~/Features/Demo/NPlus/Views/Performance.cshtml");
    }

    /// <summary>
    /// N+1問題あり: ループ内で部署情報を個別取得する（1+N回クエリ）
    /// </summary>
    /// <returns>ユーザー一覧・発行クエリ数・処理時間</returns>
    [HttpGet("/api/demo/n-plus-one/bad")]
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

    /// <summary>
    /// N+1問題なし: JOINで1回のクエリにより全データを取得する
    /// </summary>
    /// <returns>ユーザー一覧・発行クエリ数=1・処理時間</returns>
    [HttpGet("/api/demo/n-plus-one/good")]
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
