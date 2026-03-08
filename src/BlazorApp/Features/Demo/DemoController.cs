using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;
using BlazorApp.Shared.DTOs;
using BlazorApp.Shared.Exceptions;

namespace BlazorApp.Features.Demo;

public class DemoController : Controller
{
    private readonly INPlusOneService _nPlusOneService;
    private readonly ILogger<DemoController> _logger;

    public DemoController(INPlusOneService nPlusOneService, ILogger<DemoController> logger)
    {
        _nPlusOneService = nPlusOneService;
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
}
