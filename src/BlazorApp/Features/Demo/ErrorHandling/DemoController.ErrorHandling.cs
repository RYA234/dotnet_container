using Microsoft.AspNetCore.Mvc;
using BlazorApp.Shared.DTOs;
using BlazorApp.Shared.Exceptions;

namespace BlazorApp.Features.Demo;

public partial class DemoController
{
    public IActionResult ErrorHandling()
    {
        return View("~/Features/Demo/ErrorHandling/Views/ErrorHandling.cshtml");
    }

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
