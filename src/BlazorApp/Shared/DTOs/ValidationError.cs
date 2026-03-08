namespace BlazorApp.Shared.DTOs;

/// <summary>
/// 単一フィールドのバリデーションエラー情報を保持するDTO。
/// ValidationException の Errors リストの要素として使用する。
/// </summary>
public class ValidationError
{
    /// <summary>エラーが発生したフィールド名（例: "Email", "Password"）</summary>
    public string Field { get; }

    /// <summary>エラーメッセージ（例: "メールアドレスは必須です"）</summary>
    public string Message { get; }

    /// <param name="field">フィールド名</param>
    /// <param name="message">エラーメッセージ</param>
    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}
