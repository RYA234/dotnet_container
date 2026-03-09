namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// ログデモ用サービスのインターフェース。
/// ログレベルの使い分け・パフォーマンス計測・マスキングをデモする。
/// </summary>
public interface ILoggingDemoService
{
    /// <summary>
    /// Debug / Information / Warning / Error の4レベルを一度に出力するデモ。
    /// </summary>
    void LogAllLevels();

    /// <summary>
    /// 処理時間を計測し、閾値（1000ms）を超えた場合に Warning を出力する。
    /// </summary>
    /// <param name="operationName">操作名</param>
    /// <param name="elapsedMs">処理にかかったミリ秒（テスト時は直接指定）</param>
    /// <returns>操作名と計測時間を含む結果</returns>
    PerformanceResult LogPerformance(string operationName, long elapsedMs);

    /// <summary>
    /// 入力文字列の機密情報（password/apikey/token/secret）をマスキングしてログ出力する。
    /// </summary>
    /// <param name="input">ログに出力したい文字列</param>
    /// <returns>マスキング済みの文字列</returns>
    string MaskAndLog(string input);
}

/// <summary>パフォーマンス計測の結果</summary>
public record PerformanceResult(string OperationName, long ElapsedMs, bool IsSlowOperation);
