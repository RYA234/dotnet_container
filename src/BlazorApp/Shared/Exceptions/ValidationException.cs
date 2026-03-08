using BlazorApp.Shared.DTOs;

namespace BlazorApp.Shared.Exceptions;

/// <summary>
/// 入力値のバリデーションエラーを表す例外。
/// 複数フィールドのエラーをまとめて返す場合に使用する。
/// HTTPステータス: 400 Bad Request
/// </summary>
public class ValidationException : ApplicationException
{
    /// <summary>バリデーションエラーのリスト（フィールドごとのエラーメッセージ）</summary>
    public List<ValidationError> Errors { get; }

    /// <summary>複数フィールドのエラーを指定して生成する</summary>
    /// <param name="errors">バリデーションエラーのリスト</param>
    public ValidationException(List<ValidationError> errors)
        : base("入力値が不正です", "VALIDATION_ERROR", 400)
    {
        Errors = errors;
    }

    /// <summary>単一フィールドのエラーを指定して生成する</summary>
    /// <param name="field">エラーが発生したフィールド名</param>
    /// <param name="message">エラーメッセージ</param>
    public ValidationException(string field, string message)
        : this(new List<ValidationError> { new ValidationError(field, message) })
    {
    }
}
