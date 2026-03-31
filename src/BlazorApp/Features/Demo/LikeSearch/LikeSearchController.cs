using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;

namespace BlazorApp.Features.Demo.LikeSearch;

/// <summary>
/// LIKE検索デモ（前方一致 vs 部分一致のインデックス効率比較）のコントローラー
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/like-search-demo/internal-design.md</para>
/// <para><strong>責務:</strong> LIKE検索の前方一致（インデックス使用可）と部分一致（フルスキャン）のAPIエンドポイントを提供する</para>
/// <para><strong>エンドポイント一覧:</strong></para>
/// <list type="bullet">
/// <item><description>GET  /Demo/LikeSearch             - デモ画面表示</description></item>
/// <item><description>POST /api/demo/like-search/setup  - テストデータ生成</description></item>
/// <item><description>GET  /api/demo/like-search/prefix - 前方一致検索（keyword%）</description></item>
/// <item><description>GET  /api/demo/like-search/partial - 部分一致検索（%keyword%）</description></item>
/// </list>
/// </remarks>
[Route("Demo")]
public class LikeSearchController : Controller
{
    private readonly ILikeSearchService _likeSearchService;
    private readonly ILogger<LikeSearchController> _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="likeSearchService">LIKE検索デモサービス</param>
    /// <param name="logger">ロガー</param>
    public LikeSearchController(ILikeSearchService likeSearchService, ILogger<LikeSearchController> logger)
    {
        _likeSearchService = likeSearchService;
        _logger = logger;
    }

    /// <summary>デモ画面を表示する</summary>
    [Route("LikeSearch")]
    public IActionResult LikeSearch()
    {
        return View("~/Features/Demo/LikeSearch/Views/LikeSearch.cshtml");
    }

    /// <summary>
    /// テストデータをセットアップする
    /// </summary>
    /// <returns>セットアップ結果（行数・処理時間）</returns>
    [HttpPost("/api/demo/like-search/setup")]
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

    /// <summary>
    /// 前方一致で検索する（keyword%）。インデックスが使用されるため高速
    /// </summary>
    /// <param name="keyword">検索キーワード（前方一致）</param>
    /// <returns>検索結果（ヒット件数・処理時間）</returns>
    [HttpGet("/api/demo/like-search/prefix")]
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

    /// <summary>
    /// 部分一致で検索する（%keyword%）。インデックスが使用されないためフルスキャンになる
    /// </summary>
    /// <param name="keyword">検索キーワード（部分一致）</param>
    /// <returns>検索結果（ヒット件数・処理時間）</returns>
    [HttpGet("/api/demo/like-search/partial")]
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
