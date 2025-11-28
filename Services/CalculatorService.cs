namespace BlazorApp.Services;

/// <summary>
/// 基本的な計算機能を提供するサービス
/// </summary>
public interface ICalculatorService
{
    int Add(int a, int b);
    int Subtract(int a, int b);
    int Multiply(int a, int b);
    double Divide(int a, int b);
}

/// <summary>
/// 計算機能の実装
/// </summary>
public class CalculatorService : ICalculatorService
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Subtract(int a, int b)
    {
        return a - b;
    }

    public int Multiply(int a, int b)
    {
        return a * b;
    }

    public double Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("ゼロで除算することはできません");
        }
        return (double)a / b;
    }
}
