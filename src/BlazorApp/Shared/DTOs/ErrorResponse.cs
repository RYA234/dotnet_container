namespace BlazorApp.Shared.DTOs;

/// <summary>
/// APIのエラーレスポンスのDTO。
/// 開発環境では StackTrace を含み、本番環境では省略する。
/// </summary>
public class ErrorResponse
{
    /// <summary>ユーザー向けエラーメッセージ</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>エラーコード（例: "NOT_FOUND", "VALIDATION_ERROR"）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>エラーの追加情報（例: resourceType, ruleName）。情報がない場合は null</summary>
    public Dictionary<string, object>? Details { get; set; }

    /// <summary>バリデーションエラーの詳細リスト。ValidationException 以外では null</summary>
    public List<ValidationError>? ValidationErrors { get; set; }

    /// <summary>エラー発生日時（UTC）</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>スタックトレース。開発環境のみ付与し、本番環境では null</summary>
    public string? StackTrace { get; set; }
}
