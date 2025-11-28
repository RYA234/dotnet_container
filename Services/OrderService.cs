namespace BlazorApp.Services;

/// <summary>
/// 注文情報
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public decimal TotalAmount => Quantity * Price;
}

/// <summary>
/// 価格計算サービス
/// </summary>
public interface IPricingService
{
    decimal CalculateDiscount(decimal amount, int quantity);
}

/// <summary>
/// 注文処理サービス
/// </summary>
public interface IOrderService
{
    decimal CalculateFinalPrice(Order order);
}

/// <summary>
/// 注文処理サービスの実装
/// </summary>
public class OrderService : IOrderService
{
    private readonly IPricingService _pricingService;

    public OrderService(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public decimal CalculateFinalPrice(Order order)
    {
        var totalAmount = order.TotalAmount;
        var discount = _pricingService.CalculateDiscount(totalAmount, order.Quantity);
        return totalAmount - discount;
    }
}
