namespace BlazorApp.Shared.Exceptions;

/// <summary>
/// アプリケーション固有の例外の抽象基底クラス。
/// すべてのカスタム例外はこのクラスを継承する。
/// </summary>
public abstract class ApplicationException : Exception
{
    /// <summary>エラーコード（例: "NOT_FOUND", "VALIDATION_ERROR"）</summary>
    public string ErrorCode { get; }

    /// <summary>HTTPステータスコード（例: 400, 404, 500）</summary>
    public int StatusCode { get; }

    /// <summary>エラーの追加情報（例: resourceType, resourceId）</summary>
    public Dictionary<string, object> Details { get; }

    /// <param name="message">エラーメッセージ</param>
    /// <param name="errorCode">エラーコード</param>
    /// <param name="statusCode">HTTPステータスコード</param>
    /// <param name="details">追加情報（省略可）</param>
    protected ApplicationException(string message, string errorCode, int statusCode, Dictionary<string, object>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details ?? new Dictionary<string, object>();
    }
}
