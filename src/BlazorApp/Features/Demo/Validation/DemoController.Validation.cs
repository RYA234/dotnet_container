using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.DTOs;
using BlazorApp.Features.Demo.Services;
using BlazorApp.Shared.DTOs;
using BlazorApp.Shared.Exceptions;

namespace BlazorApp.Features.Demo;

public partial class DemoController
{
    public IActionResult Validation()
    {
        return View("~/Features/Demo/Validation/Views/Validation.cshtml");
    }

    [HttpPost("api/demo/validation/order")]
    public IActionResult CreateOrder([FromBody] OrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e => new ValidationError(kv.Key, e.ErrorMessage)))
                .ToList();
            throw new ValidationException(errors);
        }

        _validationDemoService.ValidateOrder(request);

        return Ok(new { message = "注文が正常に登録されました", customerCode = request.CustomerCode, totalAmount = request.TotalAmount });
    }

    [HttpPost("api/demo/validation/reset")]
    public IActionResult ResetValidationDemo()
    {
        ValidationDemoService.Reset();
        return Ok(new { message = "デモデータをリセットしました" });
    }
}
