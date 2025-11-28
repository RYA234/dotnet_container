using BlazorApp.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BlazorApp.Tests.Services;

/// <summary>
/// OrderServiceの単体テスト
/// Moqを使用したモッキングの実装例
/// </summary>
public class OrderServiceTests
{
    private readonly Mock<IPricingService> _mockPricingService;
    private readonly IOrderService _orderService;

    public OrderServiceTests()
    {
        _mockPricingService = new Mock<IPricingService>();
        _orderService = new OrderService(_mockPricingService.Object);
    }

    [Fact]
    public void CalculateFinalPrice_割引なし_合計金額を返す()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            ProductName = "テスト商品",
            Quantity = 2,
            Price = 1000m
        };

        _mockPricingService
            .Setup(x => x.CalculateDiscount(2000m, 2))
            .Returns(0m);

        // Act
        var result = _orderService.CalculateFinalPrice(order);

        // Assert
        result.Should().Be(2000m);
        _mockPricingService.Verify(x => x.CalculateDiscount(2000m, 2), Times.Once);
    }

    [Fact]
    public void CalculateFinalPrice_割引あり_割引後の金額を返す()
    {
        // Arrange
        var order = new Order
        {
            Id = 2,
            ProductName = "割引商品",
            Quantity = 5,
            Price = 500m
        };

        _mockPricingService
            .Setup(x => x.CalculateDiscount(2500m, 5))
            .Returns(250m);

        // Act
        var result = _orderService.CalculateFinalPrice(order);

        // Assert
        result.Should().Be(2250m);
        _mockPricingService.Verify(x => x.CalculateDiscount(2500m, 5), Times.Once);
    }

    [Theory]
    [InlineData(10, 100, 1000, 0, 1000)]
    [InlineData(5, 200, 1000, 100, 900)]
    [InlineData(1, 500, 500, 50, 450)]
    public void CalculateFinalPrice_様々なケース_正しい金額を返す(
        int quantity,
        decimal price,
        decimal totalAmount,
        decimal discount,
        decimal expected)
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            ProductName = "テスト商品",
            Quantity = quantity,
            Price = price
        };

        _mockPricingService
            .Setup(x => x.CalculateDiscount(totalAmount, quantity))
            .Returns(discount);

        // Act
        var result = _orderService.CalculateFinalPrice(order);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Order_TotalAmount_正しく計算される()
    {
        // Arrange & Act
        var order = new Order
        {
            Id = 1,
            ProductName = "商品A",
            Quantity = 3,
            Price = 750m
        };

        // Assert
        order.TotalAmount.Should().Be(2250m);
    }
}
