using BlazorApp.Features.Demo.DTOs;
using BlazorApp.Shared.DTOs;
using BlazorApp.Shared.Exceptions;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// バリデーションデモ用サービス。
/// インメモリのマスターデータを使い、Service層での業務ルールバリデーションをデモする。
/// </summary>
public class ValidationDemoService : IValidationDemoService
{
    /// <summary>デモ用顧客マスタ（顧客コード → 与信限度額）</summary>
    private static readonly Dictionary<string, decimal> Customers = new()
    {
        { "C001", 10000m },
        { "C002", 5000m },
        { "C003", 50000m },
    };

    /// <summary>デモ用注文済みキー（重複チェック用）</summary>
    private static readonly HashSet<string> ExistingOrders = new();

    /// <inheritdoc />
    public void ValidateOrder(OrderRequest request)
    {
        var errors = new List<ValidationError>();

        // 顧客コード存在チェック
        if (request.CustomerCode != null && !Customers.ContainsKey(request.CustomerCode))
        {
            errors.Add(new ValidationError("CustomerCode", $"顧客コード '{request.CustomerCode}' は存在しません"));
        }

        // 与信限度額チェック（顧客が存在する場合のみ）
        if (request.CustomerCode != null && Customers.TryGetValue(request.CustomerCode, out var creditLimit))
        {
            if (request.TotalAmount > creditLimit)
            {
                errors.Add(new ValidationError("TotalAmount", $"与信限度額（{creditLimit:C0}）を超えています"));
            }
        }

        // 重複注文チェック
        var orderKey = $"{request.CustomerCode}_{request.OrderDate:yyyyMMdd}";
        if (ExistingOrders.Contains(orderKey))
        {
            errors.Add(new ValidationError("CustomerCode", "同日に同じ顧客コードの注文がすでに存在します"));
        }

        if (errors.Count > 0)
            throw new BlazorApp.Shared.Exceptions.ValidationException(errors);

        // 正常時：注文を登録（デモ用のインメモリ保存）
        ExistingOrders.Add(orderKey);
    }

    /// <summary>
    /// デモ用リセット（重複チェック用データをクリアする）。
    /// テストや再実行のために使用する。
    /// </summary>
    public static void Reset() => ExistingOrders.Clear();
}
