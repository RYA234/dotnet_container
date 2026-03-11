using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.DTOs;
using BlazorApp.Features.Demo.Services;
using BlazorApp.Shared.DTOs;
using BlazorApp.Shared.Exceptions;

namespace BlazorApp.Features.Demo;

public class DemoController : Controller
{
    private readonly INPlusOneService _nPlusOneService;
    private readonly IFullScanService _fullScanService;
    private readonly IValidationDemoService _validationDemoService;
    private readonly ILoggingDemoService _loggingDemoService;
    private readonly IDatabaseConnectionDemoService _dbConnectionDemoService;
    private readonly ILogger<DemoController> _logger;

    public DemoController(INPlusOneService nPlusOneService, IFullScanService fullScanService, IValidationDemoService validationDemoService, ILoggingDemoService loggingDemoService, IDatabaseConnectionDemoService dbConnectionDemoService, ILogger<DemoController> logger)
    {
        _nPlusOneService = nPlusOneService;
        _fullScanService = fullScanService;
        _validationDemoService = validationDemoService;
        _loggingDemoService = loggingDemoService;
        _dbConnectionDemoService = dbConnectionDemoService;
        _logger = logger;
    }

    // MVC View Actions
    public IActionResult Index()
    {
        return View("~/Features/Demo/Views/Index.cshtml");
    }

    public IActionResult Performance()
    {
        return View("~/Features/Demo/Views/Performance.cshtml");
    }

    public IActionResult ErrorHandling()
    {
        return View("~/Features/Demo/Views/ErrorHandling.cshtml");
    }

    public IActionResult Security()
    {
        return View("~/Features/Demo/Views/Security.cshtml");
    }

    public IActionResult DataStructures()
    {
        return View("~/Features/Demo/Views/DataStructures.cshtml");
    }

    public IActionResult Logging()
    {
        return View("~/Features/Demo/Views/Logging.cshtml");
    }

    public IActionResult Validation()
    {
        return View("~/Features/Demo/Views/Validation.cshtml");
    }

    public IActionResult DatabaseConnection()
    {
        return View("~/Features/Demo/Views/DatabaseConnection.cshtml");
    }

    public IActionResult FullScan()
    {
        return View("~/Features/Demo/Views/FullScan.cshtml");
    }

    // API Endpoints
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

    // =============================================
    // フルテーブルスキャンデモ用 API エンドポイント
    // =============================================

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

    // =============================================
    // エラーハンドリングデモ用 API エンドポイント
    // 各例外を意図的に発火させ、Middlewareの動作を体験する
    // =============================================

    [HttpGet("api/demo/error/validation")]
    public IActionResult ThrowValidation()
    {
        var errors = new List<ValidationError>
        {
            new("Email", "メールアドレスは必須です"),
            new("Password", "パスワードは8文字以上で入力してください")
        };
        throw new ValidationException(errors);
    }

    [HttpGet("api/demo/error/not-found")]
    public IActionResult ThrowNotFound()
    {
        throw new NotFoundException("User", "999");
    }

    [HttpGet("api/demo/error/business-rule")]
    public IActionResult ThrowBusinessRule()
    {
        throw new BusinessRuleException("注文金額が与信限度額を超えています", "CreditLimitExceeded");
    }

    [HttpGet("api/demo/error/infrastructure")]
    public IActionResult ThrowInfrastructure()
    {
        throw new InfrastructureException("データベースへの接続に失敗しました", "Database");
    }

    [HttpGet("api/demo/error/unexpected")]
    public IActionResult ThrowUnexpected()
    {
        throw new InvalidOperationException("予期しないエラーが発生しました（NullReferenceException等のランタイムエラー相当）");
    }

    // =============================================
    // バリデーションデモ用 API エンドポイント
    // Data Annotations と Service層バリデーションを体験する
    // =============================================

    [HttpPost("api/demo/validation/order")]
    public IActionResult CreateOrder([FromBody] OrderRequest request)
    {
        // 単項目チェック（Data Annotations → ModelState）
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e => new ValidationError(kv.Key, e.ErrorMessage)))
                .ToList();
            throw new ValidationException(errors);
        }

        // 業務ルールバリデーション（Service層）
        _validationDemoService.ValidateOrder(request);

        return Ok(new { message = "注文が正常に登録されました", customerCode = request.CustomerCode, totalAmount = request.TotalAmount });
    }

    [HttpPost("api/demo/validation/reset")]
    public IActionResult ResetValidationDemo()
    {
        ValidationDemoService.Reset();
        return Ok(new { message = "デモデータをリセットしました" });
    }

    // =============================================
    // ログデモ用 API エンドポイント
    // ログレベルの使い分け・パフォーマンス計測・マスキングを体験する
    // =============================================

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

    // =============================================
    // DB接続デモ用 API エンドポイント
    // IDbConnectionFactory 経由の接続管理・クエリ発行を体験する
    // =============================================

    [HttpGet("api/demo/db/test")]
    public async Task<IActionResult> DbConnectionTest()
    {
        var result = await _dbConnectionDemoService.TestConnectionAsync();
        return Ok(new
        {
            isConnected = result.IsConnected,
            databaseType = result.DatabaseType,
            connectionString = result.ConnectionString,
            elapsedMs = result.ElapsedMs,
            message = result.IsConnected ? "接続成功" : "接続失敗"
        });
    }

    [HttpGet("api/demo/db/tables")]
    public async Task<IActionResult> DbGetTables()
    {
        var tables = await _dbConnectionDemoService.GetTablesAsync();
        var tableList = tables.ToList();
        return Ok(new
        {
            tables = tableList,
            count = tableList.Count,
            message = $"{tableList.Count}件のテーブルが見つかりました"
        });
    }

    [HttpGet("api/demo/db/count")]
    public async Task<IActionResult> DbGetRowCount([FromQuery] string table = "Users")
    {
        var count = await _dbConnectionDemoService.GetRowCountAsync(table);
        return Ok(new
        {
            tableName = table,
            rowCount = count,
            message = $"{table} テーブルに {count} 件のデータがあります"
        });
    }
}

/// <summary>マスキングデモ用リクエスト</summary>
public record MaskRequest(string Input);
