using BlazorApp.Features.Demo.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlazorApp.Tests.Features.Demo;

/// <summary>
/// ログデモのテストクラス。
/// TC-LOG-001〜016（構造化ログ・パフォーマンス計測）と
/// TC-LOG-031〜037（ログマスキング）をカバーする。
/// </summary>
public class LoggingDemoTests
{
    private readonly Mock<ILogger<LoggingDemoService>> _mockLogger;
    private readonly LoggingDemoService _service;

    public LoggingDemoTests()
    {
        _mockLogger = new Mock<ILogger<LoggingDemoService>>();
        _service = new LoggingDemoService(_mockLogger.Object);
    }

    /// <summary>ILogger.Log を検証するヘルパー</summary>
    private void VerifyLog(LogLevel level, string containsMessage, Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(containsMessage)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    // ==========================================
    // 構造化ログのテスト (TC-LOG-001 〜 005)
    // ==========================================

    /// <summary>TC-LOG-001: ユーザー作成成功時に Information ログ出力</summary>
    [Fact]
    public void TC_LOG_001_LogAllLevels_OutputsInformationLog()
    {
        // Arrange & Act
        _service.LogAllLevels();

        // Assert
        VerifyLog(LogLevel.Information, "Information", Times.Once());
    }

    /// <summary>TC-LOG-002: 例外相当の状況で Error ログ出力</summary>
    [Fact]
    public void TC_LOG_002_LogAllLevels_OutputsErrorLog()
    {
        // Arrange & Act
        _service.LogAllLevels();

        // Assert
        VerifyLog(LogLevel.Error, "Error", Times.Once());
    }

    /// <summary>TC-LOG-003: メソッド呼び出し時に Debug ログ出力</summary>
    [Fact]
    public void TC_LOG_003_LogAllLevels_OutputsDebugLog()
    {
        // Arrange & Act
        _service.LogAllLevels();

        // Assert
        VerifyLog(LogLevel.Debug, "Debug", Times.Once());
    }

    /// <summary>TC-LOG-004: 警告事象で Warning ログ出力</summary>
    [Fact]
    public void TC_LOG_004_LogAllLevels_OutputsWarningLog()
    {
        // Arrange & Act
        _service.LogAllLevels();

        // Assert
        VerifyLog(LogLevel.Warning, "Warning", Times.Once());
    }

    /// <summary>TC-LOG-005: LogAllLevels で4レベルすべて出力される</summary>
    [Fact]
    public void TC_LOG_005_LogAllLevels_OutputsAllFourLevels()
    {
        // Arrange & Act
        _service.LogAllLevels();

        // Assert
        VerifyLog(LogLevel.Debug, "Debug", Times.Once());
        VerifyLog(LogLevel.Information, "Information", Times.Once());
        VerifyLog(LogLevel.Warning, "Warning", Times.Once());
        VerifyLog(LogLevel.Error, "Error", Times.Once());
    }

    // ==========================================
    // パフォーマンス計測ログのテスト (TC-LOG-010 〜 016)
    // ==========================================

    /// <summary>TC-LOG-010: 操作開始時に Debug ログ出力</summary>
    [Fact]
    public void TC_LOG_010_LogPerformance_OutputsDebugLog()
    {
        // Arrange & Act
        _service.LogPerformance("TestOperation", 500);

        // Assert
        VerifyLog(LogLevel.Debug, "Starting operation", Times.Once());
    }

    /// <summary>TC-LOG-011: 通常速度の操作完了時に Information ログ出力</summary>
    [Fact]
    public void TC_LOG_011_LogPerformance_Normal_OutputsInformationLog()
    {
        // Arrange & Act
        _service.LogPerformance("TestOperation", 500);

        // Assert
        VerifyLog(LogLevel.Information, "Operation completed", Times.Once());
    }

    /// <summary>TC-LOG-012: 操作名がログに含まれる（構造化データ TC-LOG-007相当）</summary>
    [Fact]
    public void TC_LOG_012_LogPerformance_OperationNameInLog()
    {
        // Arrange & Act
        _service.LogPerformance("GetUserOrders", 500);

        // Assert
        VerifyLog(LogLevel.Information, "GetUserOrders", Times.Once());
    }

    /// <summary>TC-LOG-013: LogPerformance の戻り値が正しい</summary>
    [Fact]
    public void TC_LOG_013_LogPerformance_ReturnsCorrectResult()
    {
        // Arrange & Act
        var result = _service.LogPerformance("TestOperation", 500);

        // Assert
        result.OperationName.Should().Be("TestOperation");
        result.ElapsedMs.Should().Be(500);
        result.IsSlowOperation.Should().BeFalse();
    }

    /// <summary>TC-LOG-014: 1000ms超で Warning ログ出力（遅い操作の検出）</summary>
    [Fact]
    public void TC_LOG_014_LogPerformance_SlowOperation_OutputsWarning()
    {
        // Arrange & Act
        _service.LogPerformance("SlowOperation", 1100);

        // Assert
        VerifyLog(LogLevel.Warning, "Slow operation detected", Times.Once());
    }

    /// <summary>TC-LOG-015: 500ms の操作は Warning なし</summary>
    [Fact]
    public void TC_LOG_015_LogPerformance_Fast_NoWarning()
    {
        // Arrange & Act
        _service.LogPerformance("FastOperation", 500);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>TC-LOG-016: 1000ms（閾値ちょうど）は Warning なし（境界値）</summary>
    [Fact]
    public void TC_LOG_016_LogPerformance_ExactThreshold_NoWarning()
    {
        // Arrange & Act
        var result = _service.LogPerformance("BoundaryOperation", 1000);

        // Assert
        result.IsSlowOperation.Should().BeFalse();
        VerifyLog(LogLevel.Information, "Operation completed", Times.Once());
    }

    // ==========================================
    // ログマスキングのテスト (TC-LOG-031 〜 037)
    // ==========================================

    /// <summary>TC-LOG-031: password をマスキング</summary>
    [Fact]
    public void TC_LOG_031_Mask_Password_IsMasked()
    {
        // Arrange
        var input = "Connecting with password=abc123";

        // Act
        var result = LogMaskingHelper.Mask(input);

        // Assert
        result.Should().Contain("password=***");
        result.Should().NotContain("abc123");
    }

    /// <summary>TC-LOG-032: apikey をマスキング</summary>
    [Fact]
    public void TC_LOG_032_Mask_ApiKey_IsMasked()
    {
        // Arrange
        var input = "apikey=sk_test_123456";

        // Act
        var result = LogMaskingHelper.Mask(input);

        // Assert
        result.Should().Contain("apikey=***");
        result.Should().NotContain("sk_test_123456");
    }

    /// <summary>TC-LOG-033: token をマスキング</summary>
    [Fact]
    public void TC_LOG_033_Mask_Token_IsMasked()
    {
        // Arrange
        var input = "token=eyJhbGci...";

        // Act
        var result = LogMaskingHelper.Mask(input);

        // Assert
        result.Should().Contain("token=***");
        result.Should().NotContain("eyJhbGci");
    }

    /// <summary>TC-LOG-034: secret をマスキング</summary>
    [Fact]
    public void TC_LOG_034_Mask_Secret_IsMasked()
    {
        // Arrange
        var input = "secret=my-secret-value";

        // Act
        var result = LogMaskingHelper.Mask(input);

        // Assert
        result.Should().Contain("secret=***");
        result.Should().NotContain("my-secret-value");
    }

    /// <summary>TC-LOG-035: 複数の機密情報を一度にマスキング</summary>
    [Fact]
    public void TC_LOG_035_Mask_MultipleSecrets_AllMasked()
    {
        // Arrange
        var input = "password=abc123 and apikey=sk_test_456";

        // Act
        var result = LogMaskingHelper.Mask(input);

        // Assert
        result.Should().Contain("password=***");
        result.Should().Contain("apikey=***");
        result.Should().NotContain("abc123");
        result.Should().NotContain("sk_test_456");
    }

    /// <summary>TC-LOG-036: 非機密情報はマスキングされない</summary>
    [Fact]
    public void TC_LOG_036_Mask_NonSensitive_NotMasked()
    {
        // Arrange
        var input = "User email is test@example.com";

        // Act
        var result = LogMaskingHelper.Mask(input);

        // Assert
        result.Should().Be(input);
    }

    /// <summary>TC-LOG-037: 大文字小文字を区別せずマスキング</summary>
    [Fact]
    public void TC_LOG_037_Mask_CaseInsensitive_IsMasked()
    {
        // Arrange
        var input = "Password=SecurePass123!";

        // Act
        var result = LogMaskingHelper.Mask(input);

        // Assert
        result.Should().Contain("Password=***");
        result.Should().NotContain("SecurePass123!");
    }
}
