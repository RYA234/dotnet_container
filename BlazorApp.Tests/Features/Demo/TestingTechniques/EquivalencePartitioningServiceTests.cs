using BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Services;
using FluentAssertions;

namespace BlazorApp.Tests.Features.Demo.TestingTechniques;

public class EquivalencePartitioningServiceTests
{
    private readonly EquivalencePartitioningService _sut = new();

    // ============================================================
    // ClassifyAge: 無効クラス
    // ============================================================

    [Fact]
    public void ClassifyAge_負数_エラーを返す()
    {
        var result = _sut.ClassifyAge(-1);

        result.IsValid.Should().BeFalse();
        result.Category.Should().Be("エラー");
        result.EquivalenceClass.Should().Contain("無効クラス（負）");
    }

    [Fact]
    public void ClassifyAge_null_エラーを返す()
    {
        var result = _sut.ClassifyAge(null);

        result.IsValid.Should().BeFalse();
        result.Category.Should().Be("エラー");
        result.EquivalenceClass.Should().Contain("無効クラス（非数値）");
    }

    // ============================================================
    // ClassifyAge: 有効クラス（子供 0〜12）
    // ============================================================

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(12)]
    public void ClassifyAge_子供範囲_子供を返す(int age)
    {
        var result = _sut.ClassifyAge(age);

        result.IsValid.Should().BeTrue();
        result.Category.Should().Be("子供");
        result.EquivalenceClass.Should().Contain("子供");
    }

    // ============================================================
    // ClassifyAge: 有効クラス（一般 13〜64）
    // ============================================================

    [Theory]
    [InlineData(13)]
    [InlineData(30)]
    [InlineData(64)]
    public void ClassifyAge_一般範囲_一般を返す(int age)
    {
        var result = _sut.ClassifyAge(age);

        result.IsValid.Should().BeTrue();
        result.Category.Should().Be("一般");
        result.EquivalenceClass.Should().Contain("一般");
    }

    // ============================================================
    // ClassifyAge: 有効クラス（シニア 65以上）
    // ============================================================

    [Theory]
    [InlineData(65)]
    [InlineData(70)]
    [InlineData(100)]
    public void ClassifyAge_シニア範囲_シニアを返す(int age)
    {
        var result = _sut.ClassifyAge(age);

        result.IsValid.Should().BeTrue();
        result.Category.Should().Be("シニア");
        result.EquivalenceClass.Should().Contain("シニア");
    }

    // ============================================================
    // RunBatchTest: 4代表値が全合格
    // ============================================================

    [Fact]
    public void RunBatchTest_全代表値が合格する()
    {
        var results = _sut.RunBatchTest();

        results.Should().HaveCount(4);
        results.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
    }

    [Fact]
    public void RunBatchTest_各代表値の期待カテゴリが正しい()
    {
        var results = _sut.RunBatchTest();

        results.Should().ContainSingle(r => r.InputAge == -1 && r.ExpectedCategory == "エラー");
        results.Should().ContainSingle(r => r.InputAge == 6  && r.ExpectedCategory == "子供");
        results.Should().ContainSingle(r => r.InputAge == 30 && r.ExpectedCategory == "一般");
        results.Should().ContainSingle(r => r.InputAge == 70 && r.ExpectedCategory == "シニア");
    }
}
