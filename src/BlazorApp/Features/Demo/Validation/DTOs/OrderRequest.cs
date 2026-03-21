using System.ComponentModel.DataAnnotations;

namespace BlazorApp.Features.Demo.DTOs;

/// <summary>
/// バリデーションデモ用の注文リクエストDTO。
/// Data Annotations によるバリデーションルールを宣言的に定義する。
/// </summary>
public class OrderRequest
{
    /// <summary>顧客コード（必須・最大10文字）</summary>
    [Required(ErrorMessage = "顧客コードは必須です")]
    [MaxLength(10, ErrorMessage = "顧客コードは10文字以内で入力してください")]
    public string? CustomerCode { get; set; }

    /// <summary>注文日（必須）</summary>
    [Required(ErrorMessage = "注文日は必須です")]
    public DateTime? OrderDate { get; set; }

    /// <summary>数量（1以上）</summary>
    [Range(1, int.MaxValue, ErrorMessage = "数量は1以上で入力してください")]
    public int Quantity { get; set; }

    /// <summary>メールアドレス（必須・形式チェック）</summary>
    [Required(ErrorMessage = "メールアドレスは必須です")]
    [EmailAddress(ErrorMessage = "メールアドレスの形式が不正です")]
    public string? Email { get; set; }

    /// <summary>合計金額（0より大きい値）</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "金額は0より大きい値を入力してください")]
    public decimal TotalAmount { get; set; }
}
