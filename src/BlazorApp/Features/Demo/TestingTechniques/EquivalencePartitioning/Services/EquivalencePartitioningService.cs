using BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Models;

namespace BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Services;

/// <summary>
/// 同値分割デモのビジネスロジック
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/Testing_techniques/01-equivalence-partitioning/internal-design.md</para>
/// <para><strong>同値クラス:</strong></para>
/// <list type="bullet">
/// <item><description>無効クラス（負）: age &lt; 0 → エラー</description></item>
/// <item><description>有効クラス（子供）: 0 ≤ age ≤ 12 → 子供</description></item>
/// <item><description>有効クラス（一般）: 13 ≤ age ≤ 64 → 一般</description></item>
/// <item><description>有効クラス（シニア）: age ≥ 65 → シニア</description></item>
/// <item><description>無効クラス（非数値）: null → エラー</description></item>
/// </list>
/// </remarks>
public class EquivalencePartitioningService : IEquivalencePartitioningService
{
    public AgeClassificationResult ClassifyAge(int? age)
    {
        if (age == null)
            return new AgeClassificationResult
            {
                IsValid = false,
                Category = "エラー",
                EquivalenceClass = "無効クラス（非数値）",
                Description = "数値を入力してください"
            };

        if (age < 0)
            return new AgeClassificationResult
            {
                Age = age,
                IsValid = false,
                Category = "エラー",
                EquivalenceClass = "無効クラス（負）",
                Description = $"{age} は有効な年齢ではありません（0以上を入力してください）"
            };

        if (age <= 12)
            return new AgeClassificationResult
            {
                Age = age,
                IsValid = true,
                Category = "子供",
                EquivalenceClass = "有効クラス（子供：0〜12歳）",
                Description = $"{age}歳は子供区分です（0〜12歳の同値クラスに属します）"
            };

        if (age <= 64)
            return new AgeClassificationResult
            {
                Age = age,
                IsValid = true,
                Category = "一般",
                EquivalenceClass = "有効クラス（一般：13〜64歳）",
                Description = $"{age}歳は一般区分です（13〜64歳の同値クラスに属します）"
            };

        return new AgeClassificationResult
        {
            Age = age,
            IsValid = true,
            Category = "シニア",
            EquivalenceClass = "有効クラス（シニア：65歳以上）",
            Description = $"{age}歳はシニア区分です（65歳以上の同値クラスに属します）"
        };
    }

    public List<BatchTestCaseResult> RunBatchTest()
    {
        var cases = new List<(int Age, string ClassName, string Expected)>
        {
            (-1,  "無効クラス（負）",       "エラー"),
            ( 6,  "有効クラス（子供）",     "子供"),
            (30,  "有効クラス（一般）",     "一般"),
            (70,  "有効クラス（シニア）",   "シニア"),
        };

        return cases.Select(c =>
        {
            var result = ClassifyAge(c.Age);
            return new BatchTestCaseResult
            {
                InputAge = c.Age,
                EquivalenceClass = c.ClassName,
                ExpectedCategory = c.Expected,
                ActualCategory = result.Category,
                Passed = result.Category == c.Expected
            };
        }).ToList();
    }
}
