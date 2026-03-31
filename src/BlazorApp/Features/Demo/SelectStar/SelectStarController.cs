using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo.SelectStar;

/// <summary>
/// SELECT *問題デモ（全カラム取得 vs 必要カラムのみ取得）のコントローラー
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/select-star-demo/internal-design.md</para>
/// <para><strong>責務:</strong> SELECT *（全カラム）と必要カラムのみの転送量・AWS費用の差を示すAPIエンドポイントを提供する</para>
/// <para><strong>エンドポイント一覧:</strong></para>
/// <list type="bullet">
/// <item><description>GET  /Demo/SelectStar                      - デモ画面表示</description></item>
/// <item><description>POST /api/demo/select-star/setup           - テストデータ生成（大容量テキスト含む）</description></item>
/// <item><description>GET  /api/demo/select-star/all-columns     - SELECT *（全カラム）</description></item>
/// <item><description>GET  /api/demo/select-star/specific-columns - SELECT Id,Name,Email（必要カラムのみ）</description></item>
/// </list>
/// </remarks>
[Route("Demo")]
public class SelectStarController : Controller
{
    private readonly ISelectStarService _selectStarService;
    private readonly ILogger<SelectStarController> _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="selectStarService">SELECT *デモサービス</param>
    /// <param name="logger">ロガー</param>
    public SelectStarController(ISelectStarService selectStarService, ILogger<SelectStarController> logger)
    {
        _selectStarService = selectStarService;
        _logger = logger;
    }

    /// <summary>デモ画面を表示する</summary>
    [Route("SelectStar")]
    public IActionResult SelectStar()
    {
        return View("~/Features/Demo/SelectStar/Views/SelectStar.cshtml");
    }

    /// <summary>
    /// テストデータをセットアップする（Bio:10KB, Preferences:5KB, ActivityLog:20KB のデータを10,000件挿入）
    /// </summary>
    /// <returns>セットアップ結果（行数・処理時間）</returns>
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

    /// <summary>
    /// SELECT * で全カラムを取得する（Bad実装）
    /// </summary>
    /// <returns>転送量・AWS費用推計・先頭3件のデータ</returns>
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

    /// <summary>
    /// 必要カラムのみ（Id, Name, Email）を取得する（Good実装）
    /// </summary>
    /// <returns>転送量・AWS費用推計・先頭3件のデータ</returns>
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
