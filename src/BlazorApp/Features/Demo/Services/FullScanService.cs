using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public class FullScanService : IFullScanService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FullScanService> _logger;
    private readonly int _totalRows;

    public FullScanService(IConfiguration configuration, ILogger<FullScanService> logger, int totalRows = 1_000_000)
    {
        _configuration = configuration;
        _logger = logger;
        _totalRows = totalRows;
    }

    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("FullScanDemo");
        return new SqliteConnection(connectionString);
    }

    private async Task EnsureTableCreatedAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS LargeUsers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Email TEXT NOT NULL,
                Name TEXT NOT NULL,
                CreatedAt TEXT DEFAULT (datetime('now'))
            );";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<SetupResponse> SetupAsync()
    {
        var sw = Stopwatch.StartNew();

        using var connection = GetConnection();
        await connection.OpenAsync();
        await EnsureTableCreatedAsync(connection);

        var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM LargeUsers";
        var existing = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        if (existing > 0)
        {
            sw.Stop();
            return new SetupResponse
            {
                Success = true,
                RowCount = existing,
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                Message = $"既存データあり: {existing:N0}件のデータが存在します"
            };
        }

        const int batchSize = 500;

        var lastNames = new[] { "田中", "鈴木", "佐藤", "高橋", "伊藤", "渡辺", "山本", "中村", "小林", "加藤" };
        var firstNames = new[] { "太郎", "花子", "次郎", "美咲", "健一", "恵子", "大輔", "裕子", "隆", "由美" };

        using var transaction = connection.BeginTransaction();
        for (int batch = 0; batch < _totalRows / batchSize; batch++)
        {
            var values = new StringBuilder();
            for (int i = 0; i < batchSize; i++)
            {
                var rowNum = batch * batchSize + i + 1;
                var name = lastNames[rowNum % lastNames.Length] + firstNames[rowNum % firstNames.Length];
                if (i > 0) values.Append(',');
                values.Append($"('user{rowNum:D7}@example.com', '{name}')");
            }
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO LargeUsers (Email, Name) VALUES {values}";
            await insertCmd.ExecuteNonQueryAsync();
        }
        transaction.Commit();

        sw.Stop();
        _logger.LogInformation("FullScan setup completed: {RowCount} rows, {ExecutionTimeMs}ms", _totalRows, sw.ElapsedMilliseconds);

        return new SetupResponse
        {
            Success = true,
            RowCount = _totalRows,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Message = $"セットアップ完了: {_totalRows:N0}件のデータを生成しました"
        };
    }

    public async Task<FullScanResponse> SearchWithoutIndexAsync(string email)
    {
        var sw = Stopwatch.StartNew();
        var result = new List<LargeUserInfo>();

        using var connection = GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Email, Name FROM LargeUsers WHERE Email = @email";
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new LargeUserInfo
            {
                Id = Convert.ToInt32(reader["Id"]),
                Email = reader["Email"].ToString() ?? "",
                Name = reader["Name"].ToString() ?? ""
            });
        }

        sw.Stop();
        _logger.LogInformation("FullScan without-index: email={Email}, {ExecutionTimeMs}ms", email, sw.ElapsedMilliseconds);

        return new FullScanResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            RowCount = result.Count,
            HasIndex = false,
            Data = result,
            Message = $"インデックスなし: 全件をスキャンしました（{sw.ElapsedMilliseconds}ms）"
        };
    }

    public async Task<SetupResponse> CreateIndexAsync()
    {
        var sw = Stopwatch.StartNew();

        using var connection = GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS IX_LargeUsers_Email ON LargeUsers(Email)";
        await cmd.ExecuteNonQueryAsync();

        sw.Stop();
        _logger.LogInformation("FullScan index created: {ExecutionTimeMs}ms", sw.ElapsedMilliseconds);

        return new SetupResponse
        {
            Success = true,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Message = $"IX_LargeUsers_Email インデックスを作成しました（{sw.ElapsedMilliseconds}ms）"
        };
    }

    public async Task<FullScanResponse> SearchWithIndexAsync(string email)
    {
        var sw = Stopwatch.StartNew();
        var result = new List<LargeUserInfo>();

        using var connection = GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Email, Name FROM LargeUsers WHERE Email = @email";
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new LargeUserInfo
            {
                Id = Convert.ToInt32(reader["Id"]),
                Email = reader["Email"].ToString() ?? "",
                Name = reader["Name"].ToString() ?? ""
            });
        }

        sw.Stop();
        _logger.LogInformation("FullScan with-index: email={Email}, {ExecutionTimeMs}ms", email, sw.ElapsedMilliseconds);

        return new FullScanResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            RowCount = result.Count,
            HasIndex = true,
            Data = result,
            Message = $"インデックスあり: インデックスを使って高速に取得しました（{sw.ElapsedMilliseconds}ms）"
        };
    }
}
