using BlazorApp.Services;
using FluentAssertions;
using Xunit;

namespace BlazorApp.Tests.Services;

/// <summary>
/// CalculatorServiceの単体テスト
/// xUnit, FluentAssertionsを使用した実装例
/// </summary>
public class CalculatorServiceTests
{
    private readonly ICalculatorService _calculatorService;

    public CalculatorServiceTests()
    {
        _calculatorService = new CalculatorService();
    }

    #region Add Tests

    [Fact]
    public void Add_正の数の加算_正しい結果を返す()
    {
        // Arrange
        int a = 5;
        int b = 3;

        // Act
        var result = _calculatorService.Add(a, b);

        // Assert
        result.Should().Be(8);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 2)]
    [InlineData(-1, -1, -2)]
    [InlineData(100, 200, 300)]
    public void Add_様々な入力値_正しい結果を返す(int a, int b, int expected)
    {
        // Act
        var result = _calculatorService.Add(a, b);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Subtract Tests

    [Fact]
    public void Subtract_正の数の減算_正しい結果を返す()
    {
        // Arrange
        int a = 10;
        int b = 3;

        // Act
        var result = _calculatorService.Subtract(a, b);

        // Assert
        result.Should().Be(7);
    }

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(0, 0, 0)]
    [InlineData(-5, -3, -2)]
    [InlineData(3, 5, -2)]
    public void Subtract_様々な入力値_正しい結果を返す(int a, int b, int expected)
    {
        // Act
        var result = _calculatorService.Subtract(a, b);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Multiply Tests

    [Fact]
    public void Multiply_正の数の乗算_正しい結果を返す()
    {
        // Arrange
        int a = 4;
        int b = 5;

        // Act
        var result = _calculatorService.Multiply(a, b);

        // Assert
        result.Should().Be(20);
    }

    [Theory]
    [InlineData(2, 3, 6)]
    [InlineData(0, 5, 0)]
    [InlineData(-2, 3, -6)]
    [InlineData(-2, -3, 6)]
    public void Multiply_様々な入力値_正しい結果を返す(int a, int b, int expected)
    {
        // Act
        var result = _calculatorService.Multiply(a, b);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Divide Tests

    [Fact]
    public void Divide_正の数の除算_正しい結果を返す()
    {
        // Arrange
        int a = 10;
        int b = 2;

        // Act
        var result = _calculatorService.Divide(a, b);

        // Assert
        result.Should().Be(5.0);
    }

    [Theory]
    [InlineData(10, 2, 5.0)]
    [InlineData(7, 2, 3.5)]
    [InlineData(-10, 2, -5.0)]
    [InlineData(10, -2, -5.0)]
    public void Divide_様々な入力値_正しい結果を返す(int a, int b, double expected)
    {
        // Act
        var result = _calculatorService.Divide(a, b);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Divide_ゼロで除算_DivideByZeroExceptionをスローする()
    {
        // Arrange
        int a = 10;
        int b = 0;

        // Act
        Action act = () => _calculatorService.Divide(a, b);

        // Assert
        act.Should().Throw<DivideByZeroException>()
            .WithMessage("ゼロで除算することはできません");
    }

    #endregion
}
