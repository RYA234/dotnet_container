using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// バリデーションデモ用サービスのインターフェース。
/// Service層での業務ルールバリデーションを担当する。
/// </summary>
public interface IValidationDemoService
{
    /// <summary>
    /// 注文リクエストに対して業務ルールバリデーションを実行する。
    /// - 顧客コードの存在チェック
    /// - 与信限度額チェック
    /// - 重複注文チェック
    /// </summary>
    /// <param name="request">注文リクエスト</param>
    /// <exception cref="BlazorApp.Shared.Exceptions.ValidationException">業務ルール違反の場合</exception>
    void ValidateOrder(OrderRequest request);
}
