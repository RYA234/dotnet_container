using System.ComponentModel.DataAnnotations;
using BlazorApp.Features.Demo.DTOs;
using BlazorApp.Features.Demo.Services;
using BlazorApp.Shared.DTOs;
using FluentAssertions;
using ValidationException = BlazorApp.Shared.Exceptions.ValidationException;

namespace BlazorApp.Tests.Features.Demo;

/// <summary>
/// バリデーションデモのテストクラス。
/// TC-VL-001 〜 TC-VL-026 をカバーする。
/// </summary>
public class ValidationDemoTests
{
    // ==========================================
    // ヘルパー：Data Annotations の検証
    // ==========================================

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    // ==========================================
    // [Required] 属性のテスト (TC-VL-001 〜 003)
    // ==========================================

    /// <summary>TC-VL-001: 必須フィールドが null の場合、バリデーションエラー</summary>
    [Fact]
    public void TC_VL_001_Required_Null_ReturnsValidationError()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = null,
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("CustomerCode"));
        results.First(r => r.MemberNames.Contains("CustomerCode")).ErrorMessage
            .Should().Be("顧客コードは必須です");
    }

    /// <summary>TC-VL-002: 必須フィールドが空文字の場合、バリデーションエラー</summary>
    [Fact]
    public void TC_VL_002_Required_EmptyString_ReturnsValidationError()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "",
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("CustomerCode"));
    }

    /// <summary>TC-VL-003: 必須フィールドに値がある場合、バリデーション成功</summary>
    [Fact]
    public void TC_VL_003_Required_WithValue_PassesValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001",
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("CustomerCode"));
    }

    // ==========================================
    // [MaxLength] 属性のテスト (TC-VL-004 〜 006)
    // ==========================================

    /// <summary>TC-VL-004: 最大長を超える場合、バリデーションエラー</summary>
    [Fact]
    public void TC_VL_004_MaxLength_Exceeded_ReturnsValidationError()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "12345678901", // 11文字（上限10）
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("CustomerCode"));
        results.First(r => r.MemberNames.Contains("CustomerCode")).ErrorMessage
            .Should().Be("顧客コードは10文字以内で入力してください");
    }

    /// <summary>TC-VL-005: 最大長ちょうどの場合、バリデーション成功</summary>
    [Fact]
    public void TC_VL_005_MaxLength_Exact_PassesValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "1234567890", // 10文字
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("CustomerCode"));
    }

    /// <summary>TC-VL-006: 最大長未満の場合、バリデーション成功</summary>
    [Fact]
    public void TC_VL_006_MaxLength_BelowLimit_PassesValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001", // 4文字
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("CustomerCode"));
    }

    // ==========================================
    // [Range] 属性のテスト (TC-VL-007 〜 010)
    // ==========================================

    /// <summary>TC-VL-007: 最小値未満の場合、バリデーションエラー</summary>
    [Fact]
    public void TC_VL_007_Range_BelowMinimum_ReturnsValidationError()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001",
            OrderDate = DateTime.Now,
            Quantity = 0, // 最小値1
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Quantity"));
        results.First(r => r.MemberNames.Contains("Quantity")).ErrorMessage
            .Should().Be("数量は1以上で入力してください");
    }

    /// <summary>TC-VL-008: 負の値の場合、バリデーションエラー</summary>
    [Fact]
    public void TC_VL_008_Range_Negative_ReturnsValidationError()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001",
            OrderDate = DateTime.Now,
            Quantity = -1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Quantity"));
    }

    /// <summary>TC-VL-009: 最小値ちょうどの場合、バリデーション成功</summary>
    [Fact]
    public void TC_VL_009_Range_ExactMinimum_PassesValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001",
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Quantity"));
    }

    /// <summary>TC-VL-010: 最小値より大きい場合、バリデーション成功</summary>
    [Fact]
    public void TC_VL_010_Range_AboveMinimum_PassesValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001",
            OrderDate = DateTime.Now,
            Quantity = 100,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Quantity"));
    }

    // ==========================================
    // [EmailAddress] 属性のテスト (TC-VL-011 〜 013)
    // ==========================================

    /// <summary>TC-VL-011: メール形式が不正の場合、バリデーションエラー</summary>
    [Fact]
    public void TC_VL_011_Email_InvalidFormat_ReturnsValidationError()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001",
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "not-an-email",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    /// <summary>TC-VL-012: @ がない場合、バリデーションエラー</summary>
    [Fact]
    public void TC_VL_012_Email_MissingAtSign_ReturnsValidationError()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001",
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "userexample.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    /// <summary>TC-VL-013: 正しいメール形式の場合、バリデーション成功</summary>
    [Fact]
    public void TC_VL_013_Email_ValidFormat_PassesValidation()
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerCode = "C001",
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = 1000m
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Email"));
    }

    // ==========================================
    // 業務ルールバリデーションのテスト (TC-VL-020 〜 026)
    // ==========================================

    private static ValidationDemoService CreateService()
    {
        ValidationDemoService.Reset();
        return new ValidationDemoService();
    }

    private static OrderRequest ValidOrderRequest(string customerCode = "C001", decimal totalAmount = 1000m)
        => new OrderRequest
        {
            CustomerCode = customerCode,
            OrderDate = DateTime.Now,
            Quantity = 1,
            Email = "user@example.com",
            TotalAmount = totalAmount
        };

    /// <summary>TC-VL-020: 存在しない顧客コードの場合、ValidationException をスロー</summary>
    [Fact]
    public void TC_VL_020_BusinessRule_UnknownCustomer_ThrowsValidationException()
    {
        // Arrange
        var service = CreateService();
        var request = ValidOrderRequest(customerCode: "C999");

        // Act
        var act = () => service.ValidateOrder(request);

        // Assert
        act.Should().Throw<ValidationException>()
            .Which.Errors.Should().Contain(e =>
                e.Field == "CustomerCode" && e.Message.Contains("C999"));
    }

    /// <summary>TC-VL-021: 存在する顧客コードの場合、例外なし</summary>
    [Fact]
    public void TC_VL_021_BusinessRule_KnownCustomer_NoException()
    {
        // Arrange
        var service = CreateService();
        var request = ValidOrderRequest(customerCode: "C001");

        // Act
        var act = () => service.ValidateOrder(request);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>TC-VL-022: 与信限度額を超える場合、ValidationException をスロー</summary>
    [Fact]
    public void TC_VL_022_BusinessRule_CreditLimitExceeded_ThrowsValidationException()
    {
        // Arrange
        var service = CreateService();
        var request = ValidOrderRequest(customerCode: "C002", totalAmount: 20000m); // C002の与信は5000

        // Act
        var act = () => service.ValidateOrder(request);

        // Assert
        act.Should().Throw<ValidationException>()
            .Which.Errors.Should().Contain(e =>
                e.Field == "TotalAmount" && e.Message.Contains("与信限度額"));
    }

    /// <summary>TC-VL-023: 与信限度額以下の場合、例外なし</summary>
    [Fact]
    public void TC_VL_023_BusinessRule_WithinCreditLimit_NoException()
    {
        // Arrange
        var service = CreateService();
        var request = ValidOrderRequest(customerCode: "C001", totalAmount: 9999m); // C001の与信は10000

        // Act
        var act = () => service.ValidateOrder(request);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>TC-VL-024: 与信限度額ちょうどの場合、例外なし（境界値）</summary>
    [Fact]
    public void TC_VL_024_BusinessRule_ExactCreditLimit_NoException()
    {
        // Arrange
        var service = CreateService();
        var request = ValidOrderRequest(customerCode: "C001", totalAmount: 10000m); // C001の与信は10000

        // Act
        var act = () => service.ValidateOrder(request);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>TC-VL-025: 重複するコードで登録する場合、ValidationException をスロー</summary>
    [Fact]
    public void TC_VL_025_BusinessRule_DuplicateOrder_ThrowsValidationException()
    {
        // Arrange
        var service = CreateService();
        var request = ValidOrderRequest(customerCode: "C001");
        service.ValidateOrder(request); // 1回目は成功

        // Act（同じ顧客・同じ日で2回目）
        var act = () => service.ValidateOrder(request);

        // Assert
        act.Should().Throw<ValidationException>()
            .Which.Errors.Should().Contain(e => e.Message.Contains("同日に同じ顧客コード"));
    }

    /// <summary>TC-VL-026: 重複しないコードで登録する場合、例外なし</summary>
    [Fact]
    public void TC_VL_026_BusinessRule_NoDuplicate_NoException()
    {
        // Arrange
        var service = CreateService();
        var request1 = ValidOrderRequest(customerCode: "C001");
        var request2 = ValidOrderRequest(customerCode: "C002"); // 別顧客
        service.ValidateOrder(request1);

        // Act
        var act = () => service.ValidateOrder(request2);

        // Assert
        act.Should().NotThrow();
    }
}
