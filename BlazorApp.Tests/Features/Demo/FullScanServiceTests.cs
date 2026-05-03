using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using BlazorApp.Features.Demo.Services;
using Microsoft.Data.Sqlite;

namespace BlazorApp.Tests.Features.Demo;

public class FullScanServiceTests : IDisposable
{
    private readonly FullScanService _service;
    private readonly string _connectionString;
    private readonly SqliteConnection _sharedConnection;

    // テスト用は1000件（本番は100万件）
    private const int TestRowCount = 1000;

    public FullScanServiceTests()
    {
        _connectionString = $"Data Source=FullScanTest_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        _sharedConnection = new SqliteConnection(_connectionString);
        _sharedConnection.Open();

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ConnectionStrings:FullScanDemo", _connectionString }
        });
        var configuration = configBuilder.Build();

        var logger = new Mock<ILogger<FullScanService>>();
        _service = new FullScanService(configuration, logger.Object, TestRowCount);
    }

    [Fact]
    public async Task SetupAsync_正常系_指定件数のデータが生成される()
    {
        var result = await _service.SetupAsync();

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RowCount.Should().Be(TestRowCount);
        result.Message.Should().Contain("セットアップ完了");
    }

    [Fact]
    public async Task SetupAsync_冪等性_2回呼んでも重複しない()
    {
        await _service.SetupAsync();
        var result = await _service.SetupAsync();

        result.RowCount.Should().Be(TestRowCount);
    }

    [Fact]
    public async Task SearchWithoutIndex_ヒットあり_1件返る()
    {
        await _service.SetupAsync();

        var result = await _service.SearchWithoutIndexAsync("user0000500@example.com");

        result.RowCount.Should().Be(1);
        result.HasIndex.Should().BeFalse();
        result.Data.Should().HaveCount(1);
        result.Data[0].Email.Should().Be("user0000500@example.com");
        result.Message.Should().Contain("インデックスなし");
    }

    [Fact]
    public async Task SearchWithoutIndex_ヒットなし_0件返る()
    {
        await _service.SetupAsync();

        var result = await _service.SearchWithoutIndexAsync("notexist@example.com");

        result.RowCount.Should().Be(0);
        result.HasIndex.Should().BeFalse();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateIndex_正常系_成功する()
    {
        await _service.SetupAsync();

        var result = await _service.CreateIndexAsync();

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("インデックス");
    }

    [Fact]
    public async Task CreateIndex_冪等性_2回呼んでもエラーにならない()
    {
        await _service.SetupAsync();
        await _service.CreateIndexAsync();
        var result = await _service.CreateIndexAsync();

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SearchWithIndex_ヒットあり_1件返る()
    {
        await _service.SetupAsync();
        await _service.CreateIndexAsync();

        var result = await _service.SearchWithIndexAsync("user0000500@example.com");

        result.RowCount.Should().Be(1);
        result.HasIndex.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Email.Should().Be("user0000500@example.com");
        result.Message.Should().Contain("インデックスあり");
    }

    [Fact]
    public async Task SearchWithIndex_ヒットなし_0件返る()
    {
        await _service.SetupAsync();
        await _service.CreateIndexAsync();

        var result = await _service.SearchWithIndexAsync("notexist@example.com");

        result.RowCount.Should().Be(0);
        result.HasIndex.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchWithIndex_WithoutIndexと同じデータを返す()
    {
        await _service.SetupAsync();
        await _service.CreateIndexAsync();

        var withoutIndex = await _service.SearchWithoutIndexAsync("user0000500@example.com");
        var withIndex = await _service.SearchWithIndexAsync("user0000500@example.com");

        withoutIndex.RowCount.Should().Be(withIndex.RowCount);
        withoutIndex.Data[0].Id.Should().Be(withIndex.Data[0].Id);
        withoutIndex.Data[0].Email.Should().Be(withIndex.Data[0].Email);
    }

    public void Dispose()
    {
        _sharedConnection?.Close();
        _sharedConnection?.Dispose();
    }
}
