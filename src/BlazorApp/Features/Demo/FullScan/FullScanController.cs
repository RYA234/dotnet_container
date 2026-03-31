using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo.FullScan;

/// <summary>
/// フルスキャンデモ（インデックスの有無による検索速度比較）のコントローラー
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/full-scan-demo/internal-design.md</para>
/// <para><strong>責務:</strong> インデックスなし・インデックスあり検索のAPIエンドポイントを提供し、実行時間を比較する</para>
/// <para><strong>エンドポイント一覧:</strong></para>
/// <list type="bullet">
/// <item><description>GET  /Demo/FullScan              - デモ画面表示</description></item>
/// <item><description>POST /api/demo/full-scan/setup   - テストデータ生成</description></item>
/// <item><description>GET  /api/demo/full-scan/without-index - インデックスなし検索</description></item>
/// <item><description>POST /api/demo/full-scan/create-index  - インデックス作成</description></item>
/// <item><description>GET  /api/demo/full-scan/with-index    - インデックスあり検索</description></item>
/// </list>
/// </remarks>
[Route("Demo")]
public class FullScanController : Controller
{
    private readonly IFullScanService _fullScanService;
    private readonly ILogger<FullScanController> _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="fullScanService">フルスキャンデモサービス</param>
    /// <param name="logger">ロガー</param>
    public FullScanController(IFullScanService fullScanService, ILogger<FullScanController> logger)
    {
        _fullScanService = fullScanService;
        _logger = logger;
    }

    /// <summary>デモ画面を表示する</summary>
    [Route("FullScan")]
    public IActionResult FullScan()
    {
        return View("~/Features/Demo/FullScan/Views/FullScan.cshtml");
    }

    /// <summary>
    /// テストデータをセットアップする
    /// </summary>
    /// <returns>セットアップ結果（行数・処理時間）</returns>
    [HttpPost("/api/demo/full-scan/setup")]
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

    /// <summary>
    /// インデックスなしでEmailを検索する（フルスキャン）
    /// </summary>
    /// <param name="email">検索するメールアドレス（前方一致）</param>
    /// <returns>検索結果（ヒット件数・処理時間・実行計画）</returns>
    [HttpGet("/api/demo/full-scan/without-index")]
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

    /// <summary>
    /// Emailカラムにインデックスを作成する
    /// </summary>
    /// <returns>作成結果（処理時間）</returns>
    [HttpPost("/api/demo/full-scan/create-index")]
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

    /// <summary>
    /// インデックスありでEmailを検索する（インデックスを使用）
    /// </summary>
    /// <param name="email">検索するメールアドレス（前方一致）</param>
    /// <returns>検索結果（ヒット件数・処理時間・実行計画）</returns>
    [HttpGet("/api/demo/full-scan/with-index")]
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
