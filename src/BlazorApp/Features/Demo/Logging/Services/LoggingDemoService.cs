namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// ログデモ用サービス。
/// ログレベルの使い分け・パフォーマンス計測・機密情報マスキングのデモ実装。
/// </summary>
public class LoggingDemoService : ILoggingDemoService
{
    private readonly ILogger<LoggingDemoService> _logger;

    /// <summary>パフォーマンス警告を出す閾値（ms）</summary>
    private const int SlowOperationThresholdMs = 1000;

    /// <summary>遅いSQLクエリを警告する閾値（ms）</summary>
    private const int SlowQueryThresholdMs = 150;

    public LoggingDemoService(ILogger<LoggingDemoService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void LogAllLevels()
    {
        // Debug: 開発時のデバッグ情報（本番では出力しない）
        _logger.LogDebug("【Debug】SQL実行: SELECT * FROM Users WHERE IsActive = 1");

        // Information: 重要な処理の記録（本番でも出力）
        _logger.LogInformation("【Information】ユーザーが作成されました: UserId=123, Email=user@example.com");

        // Warning: 回復可能な問題・注意が必要な事象
        _logger.LogWarning("【Warning】レスポンスが遅延しています: 処理時間=1500ms（閾値=1000ms）");

        // Error: 回復不可能なエラー・例外
        _logger.LogError("【Error】データベース接続に失敗しました: Server=db.example.com, ErrorCode=10060");
    }

    /// <inheritdoc />
    public PerformanceResult LogPerformance(string operationName, long elapsedMs)
    {
        _logger.LogDebug("Starting operation: {OperationName}", operationName);

        var isSlowOperation = elapsedMs > SlowOperationThresholdMs;

        if (isSlowOperation)
        {
            _logger.LogWarning(
                "Slow operation detected: {OperationName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                operationName, elapsedMs, SlowOperationThresholdMs);
        }
        else
        {
            _logger.LogInformation(
                "Operation completed: {OperationName} in {ElapsedMs}ms",
                operationName, elapsedMs);
        }

        return new PerformanceResult(operationName, elapsedMs, isSlowOperation);
    }

    /// <inheritdoc />
    public string MaskAndLog(string input)
    {
        var masked = LogMaskingHelper.Mask(input);
        _logger.LogInformation("接続情報: {ConnectionInfo}", masked);
        return masked;
    }
}
