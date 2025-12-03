namespace BlazorApp.Services;

public class PricingService : IPricingService
{
    public decimal CalculateDiscount(decimal amount, int quantity)
    {
        // シンプルな例: まとめ買い割引（10個以上で 10%）
        if (quantity >= 10)
        {
            return amount * 0.10m;
        }
        return 0m;
    }
}
