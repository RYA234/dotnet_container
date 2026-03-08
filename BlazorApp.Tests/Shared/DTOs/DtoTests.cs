using BlazorApp.Shared.DTOs;
using FluentAssertions;

namespace BlazorApp.Tests.Shared.DTOs;

/// <summary>
/// DTO クラスの単体テスト。
/// ValidationError / ErrorResponse のプロパティ・デフォルト値を検証する。
/// </summary>
public class DtoTests
{
    // =============================================
    // ValidationError
    // =============================================

    [Fact]
    public void ValidationError_コンストラクタ_プロパティが正しく設定される()
    {
        // Arrange & Act
        var error = new ValidationError("Email", "メールアドレスは必須です");

        // Assert
        error.Field.Should().Be("Email");
        error.Message.Should().Be("メールアドレスは必須です");
    }

    [Fact]
    public void ValidationError_空文字_設定される()
    {
        // Arrange & Act
        var error = new ValidationError("", "");

        // Assert
        error.Field.Should().Be("");
        error.Message.Should().Be("");
    }

    // =============================================
    // ErrorResponse
    // =============================================

    [Fact]
    public void ErrorResponse_デフォルトコンストラクタ_Timestampが現在時刻に設定される()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var response = new ErrorResponse();

        // Assert
        var after = DateTime.UtcNow;
        response.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void ErrorResponse_デフォルト値_NullableフィールドはNull()
    {
        // Arrange & Act
        var response = new ErrorResponse();

        // Assert
        response.Details.Should().BeNull();
        response.ValidationErrors.Should().BeNull();
        response.StackTrace.Should().BeNull();
    }

    [Fact]
    public void ErrorResponse_プロパティを設定_正しく保持される()
    {
        // Arrange & Act
        var response = new ErrorResponse
        {
            Error = "リソースが見つかりません",
            Code = "NOT_FOUND",
            Details = new Dictionary<string, object> { { "resourceType", "User" } },
            ValidationErrors = new List<ValidationError> { new("Email", "不正なメール") }
        };

        // Assert
        response.Error.Should().Be("リソースが見つかりません");
        response.Code.Should().Be("NOT_FOUND");
        response.Details.Should().ContainKey("resourceType");
        response.ValidationErrors.Should().HaveCount(1);
    }
}
