using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace BlazorApp.Tests.Infrastructure;

/// <summary>
/// TestContainers（PostgreSQL）の動作確認テスト
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/testcontainers/internal-design.md</para>
/// <para><strong>責務:</strong> PostgreSQLコンテナを起動し、CREATE TABLE → INSERT → SELECT が通ることを確認する</para>
/// <para><strong>前提:</strong> ローカル実行時は Docker が起動していること</para>
/// </remarks>
public class PostgreSqlContainerSmokeTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlContainerSmokeTests()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// PostgreSQLコンテナが起動し、CREATE TABLE → INSERT → SELECT が通ることを確認する
    /// </summary>
    [Fact]
    public async Task PostgreSqlContainer_CreateInsertSelect_ShouldWork()
    {
        // Arrange
        var connectionString = _container.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Act: CREATE TABLE
        await using (var cmd = new NpgsqlCommand(
            "CREATE TABLE test_items (id SERIAL PRIMARY KEY, name TEXT NOT NULL);",
            connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Act: INSERT
        await using (var cmd = new NpgsqlCommand(
            "INSERT INTO test_items (name) VALUES ('hello'), ('world');",
            connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Act: SELECT
        var results = new List<string>();
        await using (var cmd = new NpgsqlCommand(
            "SELECT name FROM test_items ORDER BY id;",
            connection))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].Should().Be("hello");
        results[1].Should().Be("world");
    }
}
