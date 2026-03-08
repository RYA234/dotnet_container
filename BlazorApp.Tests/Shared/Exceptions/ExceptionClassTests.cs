using BlazorApp.Shared.DTOs;
using BlazorApp.Shared.Exceptions;
using FluentAssertions;

namespace BlazorApp.Tests.Shared.Exceptions;

/// <summary>
/// カスタム例外クラスの単体テスト。
/// テスト設計書: error-handling-test.md TC-EH-001 〜 TC-EH-014
/// </summary>
public class ExceptionClassTests
{
    // =============================================
    // NotFoundException (TC-EH-001 〜 TC-EH-004)
    // =============================================

    /// <summary>TC-EH-001: コンストラクタでプロパティが正しく設定される</summary>
    [Fact]
    public void NotFoundException_コンストラクタ_プロパティが正しく設定される()
    {
        // Arrange & Act
        var ex = new NotFoundException("User", "123");

        // Assert
        ex.ResourceType.Should().Be("User");
        ex.ResourceId.Should().Be("123");
        ex.ErrorCode.Should().Be("NOT_FOUND");
        ex.StatusCode.Should().Be(404);
        ex.Message.Should().Contain("User");
        ex.Message.Should().Contain("123");
    }

    /// <summary>TC-EH-002: Detailsに正しい値が設定される</summary>
    [Fact]
    public void NotFoundException_Details_正しい値が設定される()
    {
        // Arrange & Act
        var ex = new NotFoundException("User", "123");

        // Assert
        ex.Details.Should().ContainKey("resourceType").WhoseValue.Should().Be("User");
        ex.Details.Should().ContainKey("resourceId").WhoseValue.Should().Be("123");
    }

    /// <summary>TC-EH-003: 異なるリソースタイプでも正しく動作</summary>
    [Fact]
    public void NotFoundException_異なるリソースタイプ_正しく動作する()
    {
        // Arrange & Act
        var ex = new NotFoundException("Order", "456");

        // Assert
        ex.ResourceType.Should().Be("Order");
        ex.ResourceId.Should().Be("456");
        ex.Message.Should().Contain("Order");
        ex.Message.Should().Contain("456");
    }

    /// <summary>TC-EH-004: 空文字列のリソースIDでも動作</summary>
    [Fact]
    public void NotFoundException_空のリソースID_動作する()
    {
        // Arrange & Act
        var ex = new NotFoundException("User", "");

        // Assert
        ex.ResourceId.Should().Be("");
        ex.ErrorCode.Should().Be("NOT_FOUND");
        ex.StatusCode.Should().Be(404);
    }

    // =============================================
    // ValidationException (TC-EH-005 〜 TC-EH-007)
    // =============================================

    /// <summary>TC-EH-005: バリデーションエラーリストが正しく設定される</summary>
    [Fact]
    public void ValidationException_複数エラー_リストが正しく設定される()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("Email", "メールアドレスが不正です"),
            new("Name", "名前は必須です")
        };

        // Act
        var ex = new ValidationException(errors);

        // Assert
        ex.Errors.Should().HaveCount(2);
        ex.ErrorCode.Should().Be("VALIDATION_ERROR");
        ex.StatusCode.Should().Be(400);
    }

    /// <summary>TC-EH-006: 空のエラーリストでも例外が生成される</summary>
    [Fact]
    public void ValidationException_空リスト_例外が生成される()
    {
        // Arrange & Act
        var ex = new ValidationException(new List<ValidationError>());

        // Assert
        ex.Errors.Should().BeEmpty();
        ex.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    /// <summary>TC-EH-007: 単一エラーの簡易コンストラクタが正しく動作する</summary>
    [Fact]
    public void ValidationException_単一エラー_正しく動作する()
    {
        // Arrange & Act
        var ex = new ValidationException("Email", "メールアドレスが不正です");

        // Assert
        ex.Errors.Should().HaveCount(1);
        ex.Errors[0].Field.Should().Be("Email");
        ex.Errors[0].Message.Should().Be("メールアドレスが不正です");
    }

    // =============================================
    // BusinessRuleException (TC-EH-009 〜 TC-EH-011)
    // =============================================

    /// <summary>TC-EH-009: ルール名とメッセージが正しく設定される</summary>
    [Fact]
    public void BusinessRuleException_コンストラクタ_プロパティが正しく設定される()
    {
        // Arrange & Act
        var ex = new BusinessRuleException("メールアドレスは既に使用されています", "UNIQUE_EMAIL");

        // Assert
        ex.RuleName.Should().Be("UNIQUE_EMAIL");
        ex.Message.Should().Be("メールアドレスは既に使用されています");
        ex.ErrorCode.Should().Be("BUSINESS_RULE_VIOLATION");
        ex.StatusCode.Should().Be(400);
    }

    /// <summary>TC-EH-010: Detailsにルール名が含まれる</summary>
    [Fact]
    public void BusinessRuleException_Details_ルール名が含まれる()
    {
        // Arrange & Act
        var ex = new BusinessRuleException("メールアドレスは既に使用されています", "UNIQUE_EMAIL");

        // Assert
        ex.Details.Should().ContainKey("ruleName").WhoseValue.Should().Be("UNIQUE_EMAIL");
    }

    /// <summary>TC-EH-011: 異なるビジネスルールでも正しく動作</summary>
    [Fact]
    public void BusinessRuleException_異なるルール_正しく動作する()
    {
        // Arrange & Act
        var ex = new BusinessRuleException("18歳以上である必要があります", "MIN_AGE");

        // Assert
        ex.RuleName.Should().Be("MIN_AGE");
        ex.Message.Should().Contain("18");
    }

    // =============================================
    // InfrastructureException (TC-EH-012 〜 TC-EH-014)
    // =============================================

    /// <summary>TC-EH-012: サービス名とメッセージが正しく設定される</summary>
    [Fact]
    public void InfrastructureException_コンストラクタ_プロパティが正しく設定される()
    {
        // Arrange & Act
        var ex = new InfrastructureException("API呼び出しに失敗しました", "ExternalAPI");

        // Assert
        ex.Service.Should().Be("ExternalAPI");
        ex.Message.Should().Be("API呼び出しに失敗しました");
        ex.ErrorCode.Should().Be("INFRASTRUCTURE_ERROR");
        ex.StatusCode.Should().Be(500);
    }

    /// <summary>TC-EH-013: Detailsにサービス名が含まれる</summary>
    [Fact]
    public void InfrastructureException_Details_サービス名が含まれる()
    {
        // Arrange & Act
        var ex = new InfrastructureException("DB接続に失敗しました", "Database");

        // Assert
        ex.Details.Should().ContainKey("service").WhoseValue.Should().Be("Database");
    }

    /// <summary>TC-EH-014: innerExceptionを指定しても動作する</summary>
    [Fact]
    public void InfrastructureException_InnerException付き_動作する()
    {
        // Arrange
        var inner = new InvalidOperationException("接続タイムアウト");

        // Act
        var ex = new InfrastructureException("DB接続に失敗しました", "Database", inner);

        // Assert
        ex.Service.Should().Be("Database");
        ex.ErrorCode.Should().Be("INFRASTRUCTURE_ERROR");
    }
}
