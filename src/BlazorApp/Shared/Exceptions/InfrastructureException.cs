namespace BlazorApp.Shared.Exceptions;

/// <summary>
/// DB・外部APIなどのインフラ層で発生したエラーを表す例外。
/// ユーザー操作ではなくシステム障害が原因のため、Error レベルでログを記録する。
/// HTTPステータス: 500 Internal Server Error
/// </summary>
public class InfrastructureException : ApplicationException
{
    /// <summary>障害が発生したサービス名（例: "Database", "ExternalAPI"）</summary>
    public string Service { get; }

    /// <param name="message">エラーメッセージ</param>
    /// <param name="service">障害サービス名</param>
    /// <param name="innerException">元の例外（省略可）</param>
    public InfrastructureException(string message, string service, Exception? innerException = null)
        : base(message, "INFRASTRUCTURE_ERROR", 500,
            new Dictionary<string, object>
            {
                { "service", service }
            })
    {
        Service = service;
    }
}
