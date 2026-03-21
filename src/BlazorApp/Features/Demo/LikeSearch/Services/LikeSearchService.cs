using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public class LikeSearchService : ILikeSearchService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LikeSearchService> _logger;
    private readonly int _totalRows;

    public LikeSearchService(IConfiguration configuration, ILogger<LikeSearchService> logger, int totalRows = 100_000)
    {
        _configuration = configuration;
        _logger = logger;
        _totalRows = totalRows;
    }

    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("LikeSearchDemo");
        return new SqliteConnection(connectionString);
    }

    private async Task EnsureTableCreatedAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS SearchUsers (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT    NOT NULL,
                Email     TEXT    NOT NULL,
                CreatedAt TEXT    NOT NULL DEFAULT (datetime('now'))
            );
            CREATE INDEX IF NOT EXISTS IX_SearchUsers_Name ON SearchUsers(Name);";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<SetupResponse> SetupAsync()
    {
        var sw = Stopwatch.StartNew();

        using var connection = GetConnection();
        await connection.OpenAsync();
        await EnsureTableCreatedAsync(connection);

        var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM SearchUsers";
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

        var lastNames = new[] { "山田", "鈴木", "佐藤", "高橋", "伊藤", "渡辺", "山本", "中村", "小林", "加藤",
                                 "山口", "山崎", "山下", "川田", "田中", "斎藤", "松本", "井上", "木村", "林" };
        var firstNames = new[] { "太郎", "花子", "次郎", "美咲", "健一", "恵子", "大輔", "裕子", "隆", "由美",
                                  "一郎", "幸子", "浩二", "洋子", "明", "直子", "誠", "智子", "豊", "節子" };

        const int batchSize = 500;
        using var transaction = connection.BeginTransaction();
        for (int batch = 0; batch < _totalRows / batchSize; batch++)
        {
            var values = new StringBuilder();
            for (int i = 0; i < batchSize; i++)
            {
                var rowNum = batch * batchSize + i + 1;
                var name = lastNames[rowNum % lastNames.Length] + firstNames[rowNum % firstNames.Length];
                if (i > 0) values.Append(',');
                values.Append($"('{name}', 'user{rowNum}@example.com')");
            }
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO SearchUsers (Name, Email) VALUES {values}";
            await insertCmd.ExecuteNonQueryAsync();
        }
        transaction.Commit();

        sw.Stop();
        _logger.LogInformation("LikeSearch setup completed: {RowCount} rows, {ExecutionTimeMs}ms", _totalRows, sw.ElapsedMilliseconds);

        return new SetupResponse
        {
            Success = true,
            RowCount = _totalRows,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Message = $"セットアップ完了: {_totalRows:N0}件のデータを生成しました（IX_SearchUsers_Name インデックスあり）"
        };
    }

    public async Task<LikeSearchResponse> SearchPrefixAsync(string keyword)
    {
        var sw = Stopwatch.StartNew();
        var result = new List<SearchUserInfo>();
        var pattern = keyword + "%";

        using var connection = GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Email FROM SearchUsers WHERE Name LIKE @keyword";
        cmd.Parameters.AddWithValue("@keyword", pattern);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new SearchUserInfo
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? ""
            });
        }

        sw.Stop();
        _logger.LogInformation("LikeSearch prefix: keyword={Keyword}, {RowCount} rows, {ExecutionTimeMs}ms", keyword, result.Count, sw.ElapsedMilliseconds);

        return new LikeSearchResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            RowCount = result.Count,
            UsesIndex = true,
            SearchType = "prefix",
            Sql = "SELECT Id, Name, Email FROM SearchUsers WHERE Name LIKE @keyword",
            Keyword = pattern,
            Message = $"前方一致（LIKE '{pattern}'）: インデックスを使用して {sw.ElapsedMilliseconds}ms で {result.Count:N0}件を取得しました",
            Data = result.Take(10).ToList()
        };
    }

    public async Task<LikeSearchResponse> SearchPartialAsync(string keyword)
    {
        var sw = Stopwatch.StartNew();
        var result = new List<SearchUserInfo>();
        var pattern = "%" + keyword + "%";

        using var connection = GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Email FROM SearchUsers WHERE Name LIKE @keyword";
        cmd.Parameters.AddWithValue("@keyword", pattern);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new SearchUserInfo
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? ""
            });
        }

        sw.Stop();
        _logger.LogInformation("LikeSearch partial: keyword={Keyword}, {RowCount} rows, {ExecutionTimeMs}ms", keyword, result.Count, sw.ElapsedMilliseconds);

        return new LikeSearchResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            RowCount = result.Count,
            UsesIndex = false,
            SearchType = "partial",
            Sql = "SELECT Id, Name, Email FROM SearchUsers WHERE Name LIKE @keyword",
            Keyword = pattern,
            Message = $"中間一致（LIKE '{pattern}'）: インデックス無効・フルスキャンで {sw.ElapsedMilliseconds}ms かかりました",
            Data = result.Take(10).ToList()
        };
    }
}
