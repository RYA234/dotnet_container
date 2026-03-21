using System.Diagnostics;
using BlazorApp.Shared.Data;
using Microsoft.Data.Sqlite;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// DB接続デモ用サービス。
/// IDbConnectionFactory 経由で接続管理・クエリ発行のベストプラクティスをデモする。
/// </summary>
public class DatabaseConnectionDemoService : IDatabaseConnectionDemoService
{
    private readonly IDbConnectionFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseConnectionDemoService> _logger;

    public DatabaseConnectionDemoService(
        IDbConnectionFactory factory,
        IConfiguration configuration,
        ILogger<DatabaseConnectionDemoService> logger)
    {
        _factory = factory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DbConnectionTestResult> TestConnectionAsync()
    {
        var sw = Stopwatch.StartNew();
        var connected = await _factory.TestConnectionAsync();
        sw.Stop();

        var rawConnectionString = _configuration.GetConnectionString("DemoSQLite") ?? "Data Source=demo.db;";
        var maskedConnectionString = LogMaskingHelper.Mask(rawConnectionString);

        return new DbConnectionTestResult(connected, _factory.DatabaseType, maskedConnectionString, sw.ElapsedMilliseconds);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetTablesAsync()
    {
        using var connection = await _factory.CreateOpenConnectionAsync() as SqliteConnection
            ?? throw new InvalidOperationException("SQLite接続が必要です");

        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";

        var tables = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        _logger.LogInformation("テーブル一覧取得: {TableCount}件", tables.Count);
        return tables;
    }

    /// <inheritdoc />
    public async Task<int> GetRowCountAsync(string tableName)
    {
        // SQLインジェクション対策: テーブル名はホワイトリスト検証
        var tables = await GetTablesAsync();
        if (!tables.Contains(tableName))
            throw new ArgumentException($"テーブル '{tableName}' は存在しません");

        using var connection = await _factory.CreateOpenConnectionAsync() as SqliteConnection
            ?? throw new InvalidOperationException("SQLite接続が必要です");

        var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        _logger.LogInformation("行数取得: {TableName} = {Count}件", tableName, count);
        return count;
    }
}
