using System.Data;
using Microsoft.Data.Sqlite;

namespace BlazorApp.Shared.Data;

/// <summary>
/// SQLite用のDB接続ファクトリ。
/// ファイルベースのため接続プーリングは限定的だが、開発・デモ用途に適している。
/// </summary>
public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteConnectionFactory> _logger;

    /// <inheritdoc />
    public string DatabaseType => "SQLite";

    public SqliteConnectionFactory(string connectionString, ILogger<SqliteConnectionFactory> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        _logger.LogDebug("Creating SQLite connection: {ConnectionString}", MaskConnectionString(_connectionString));
        return new SqliteConnection(_connectionString);
    }

    /// <inheritdoc />
    public async Task<IDbConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        _logger.LogDebug("SQLite connection opened: DatabaseType={DatabaseType}", DatabaseType);
        return connection;
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogInformation("SQLite connection test succeeded: {ConnectionString}", MaskConnectionString(_connectionString));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQLite connection test failed: {ConnectionString}", MaskConnectionString(_connectionString));
            return false;
        }
    }

    private static string MaskConnectionString(string connectionString)
        => System.Text.RegularExpressions.Regex.Replace(connectionString, @"(?i)(password|pwd)=[^;]+", "$1=***");
}
