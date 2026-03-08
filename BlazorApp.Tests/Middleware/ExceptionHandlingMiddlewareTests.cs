using System.Text.Json;
using BlazorApp.Middleware;
using BlazorApp.Shared.DTOs;
using BlazorApp.Shared.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlazorApp.Tests.Middleware;

/// <summary>
/// ExceptionHandlingMiddleware の単体テスト。
/// テスト設計書: error-handling-test.md TC-EH-027 〜 TC-EH-031
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
    private readonly Mock<IHostEnvironment> _mockEnv;

    public ExceptionHandlingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _mockEnv = new Mock<IHostEnvironment>();
        _mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
    }

    /// <summary>テスト用のレスポンスボディを読み取るヘルパー</summary>
    private static async Task<ErrorResponse?> ReadErrorResponse(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<ErrorResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>テスト用の HttpContext を作成するヘルパー</summary>
    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    // =============================================
    // TC-EH-027: NotFoundException → 404
    // =============================================

    /// <summary>TC-EH-027: NotFoundException発生時に404を返す</summary>
    [Fact]
    public async Task Invoke_NotFoundException_404を返す()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("User", "123"),
            _mockLogger.Object,
            _mockEnv.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(404);
        context.Response.ContentType.Should().Be("application/json");

        var response = await ReadErrorResponse(context);
        response.Should().NotBeNull();
        response!.Code.Should().Be("NOT_FOUND");
    }

    // =============================================
    // TC-EH-028: ValidationException → 400
    // =============================================

    /// <summary>TC-EH-028: ValidationException発生時に400を返す</summary>
    [Fact]
    public async Task Invoke_ValidationException_400を返す()
    {
        // Arrange
        var context = CreateHttpContext();
        var errors = new List<ValidationError> { new("Email", "メールアドレスが不正です") };
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(errors),
            _mockLogger.Object,
            _mockEnv.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var response = await ReadErrorResponse(context);
        response!.Code.Should().Be("VALIDATION_ERROR");
    }

    // =============================================
    // TC-EH-030: 予期しない例外 → 500
    // =============================================

    /// <summary>TC-EH-030: 予期しない例外発生時に500を返す</summary>
    [Fact]
    public async Task Invoke_予期しない例外_500を返す()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("予期しないエラー"),
            _mockLogger.Object,
            _mockEnv.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);

        var response = await ReadErrorResponse(context);
        response!.Code.Should().Be("INTERNAL_ERROR");
    }

    // =============================================
    // TC-EH-031: レスポンスがJSON形式
    // =============================================

    /// <summary>TC-EH-031: レスポンスがJSON形式で返る</summary>
    [Fact]
    public async Task Invoke_例外発生_JSONレスポンスを返す()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("User", "999"),
            _mockLogger.Object,
            _mockEnv.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var act = () => JsonDocument.Parse(body);
        act.Should().NotThrow("有効なJSONであること");
    }

    // =============================================
    // BusinessRuleException → 400
    // =============================================

    [Fact]
    public async Task Invoke_BusinessRuleException_400を返す()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new BusinessRuleException("在庫が不足しています", "INSUFFICIENT_INVENTORY"),
            _mockLogger.Object,
            _mockEnv.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var response = await ReadErrorResponse(context);
        response!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    // =============================================
    // InfrastructureException → 500
    // =============================================

    [Fact]
    public async Task Invoke_InfrastructureException_500を返す()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InfrastructureException("DB接続に失敗しました", "Database"),
            _mockLogger.Object,
            _mockEnv.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);

        var response = await ReadErrorResponse(context);
        response!.Code.Should().Be("INFRASTRUCTURE_ERROR");
    }

    // =============================================
    // 正常系: 例外なしは通過する
    // =============================================

    [Fact]
    public async Task Invoke_例外なし_200のまま通過する()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            ctx =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            _mockLogger.Object,
            _mockEnv.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
    }

    // =============================================
    // 本番環境: StackTrace が含まれない
    // =============================================

    [Fact]
    public async Task Invoke_本番環境_StackTraceが含まれない()
    {
        // Arrange
        var prodEnv = new Mock<IHostEnvironment>();
        prodEnv.Setup(e => e.EnvironmentName).Returns("Production");

        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("User", "1"),
            _mockLogger.Object,
            prodEnv.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await ReadErrorResponse(context);
        response!.StackTrace.Should().BeNull();
    }
}
