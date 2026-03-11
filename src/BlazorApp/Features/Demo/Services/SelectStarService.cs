using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public class SelectStarService : ISelectStarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SelectStarService> _logger;
    private readonly int _totalRows;

    private const double AwsCostPerGb = 0.01;

    public SelectStarService(IConfiguration configuration, ILogger<SelectStarService> logger, int totalRows = 10_000)
    {
        _configuration = configuration;
        _logger = logger;
        _totalRows = totalRows;
    }

    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("SelectStarDemo");
        return new SqliteConnection(connectionString);
    }

    private async Task EnsureTableCreatedAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Profiles (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL,
                Email       TEXT    NOT NULL,
                Bio         TEXT,
                Preferences TEXT,
                ActivityLog TEXT,
                CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now'))
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
        countCmd.CommandText = "SELECT COUNT(*) FROM Profiles";
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

        // 大容量テキストの生成
        var bio = string.Concat(Enumerable.Repeat("私は東京都出身のソフトウェアエンジニアです。Web開発を得意としており、特にC#と.NETの経験が豊富です。", 100));
        var preferences = string.Concat(Enumerable.Repeat("{\"theme\":\"dark\",\"language\":\"ja\",\"notifications\":{\"email\":true,\"push\":true}}", 62));
        var activityLog = string.Concat(Enumerable.Repeat("[{\"timestamp\":\"2024-01-01T00:00:00Z\",\"action\":\"login\",\"ip\":\"192.168.1.1\"}]", 200));

        var lastNames = new[] { "田中", "鈴木", "佐藤", "高橋", "伊藤", "渡辺", "山本", "中村", "小林", "加藤" };
        var firstNames = new[] { "太郎", "花子", "次郎", "美咲", "健一", "恵子", "大輔", "裕子", "隆", "由美" };

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
                values.Append($"('{name}', 'user{rowNum}@example.com', @bio, @pref, @log)");
            }
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO Profiles (Name, Email, Bio, Preferences, ActivityLog) VALUES {values}";
            insertCmd.Parameters.AddWithValue("@bio", bio);
            insertCmd.Parameters.AddWithValue("@pref", preferences);
            insertCmd.Parameters.AddWithValue("@log", activityLog);
            await insertCmd.ExecuteNonQueryAsync();
        }
        transaction.Commit();

        sw.Stop();
        _logger.LogInformation("SelectStar setup completed: {RowCount} rows, {ExecutionTimeMs}ms", _totalRows, sw.ElapsedMilliseconds);

        return new SetupResponse
        {
            Success = true,
            RowCount = _totalRows,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Message = $"セットアップ完了: {_totalRows:N0}件のデータを生成しました（Bio:10KB, Preferences:5KB, ActivityLog:20KB）"
        };
    }

    public async Task<SelectStarResponse> GetAllColumnsAsync()
    {
        var sw = Stopwatch.StartNew();
        var result = new List<ProfileFull>();

        using var connection = GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Profiles";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new ProfileFull
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? "",
                Bio = reader["Bio"].ToString() ?? "",
                Preferences = reader["Preferences"].ToString() ?? "",
                ActivityLog = reader["ActivityLog"].ToString() ?? "",
                CreatedAt = reader["CreatedAt"].ToString() ?? ""
            });
        }

        sw.Stop();

        var (dataSize, dataSizeLabel, awsCost) = CalcDataSize(result);
        _logger.LogInformation("SelectStar all-columns: {RowCount} rows, {DataSize} bytes, {ExecutionTimeMs}ms", result.Count, dataSize, sw.ElapsedMilliseconds);

        return new SelectStarResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            RowCount = result.Count,
            DataSize = dataSize,
            DataSizeLabel = dataSizeLabel,
            AwsCostEstimate = awsCost,
            Sql = "SELECT * FROM Profiles",
            Message = $"SELECT *: {result.Count:N0}件 = {dataSizeLabel} を転送しました（AWS転送料: ${awsCost:F6}）",
            Data = result.Take(3).ToList()
        };
    }

    public async Task<SelectStarResponse> GetSpecificColumnsAsync()
    {
        var sw = Stopwatch.StartNew();
        var result = new List<ProfileSummary>();

        using var connection = GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Email FROM Profiles";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new ProfileSummary
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? ""
            });
        }

        sw.Stop();

        var (dataSize, dataSizeLabel, awsCost) = CalcDataSize(result);
        _logger.LogInformation("SelectStar specific-columns: {RowCount} rows, {DataSize} bytes, {ExecutionTimeMs}ms", result.Count, dataSize, sw.ElapsedMilliseconds);

        return new SelectStarResponse
        {
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            RowCount = result.Count,
            DataSize = dataSize,
            DataSizeLabel = dataSizeLabel,
            AwsCostEstimate = awsCost,
            Sql = "SELECT Id, Name, Email FROM Profiles",
            Message = $"必要カラムのみ: {result.Count:N0}件 = {dataSizeLabel} を転送しました（AWS転送料: ${awsCost:F6}）",
            Data = result.Take(3).ToList()
        };
    }

    private static (long bytes, string label, double awsCost) CalcDataSize(object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = (long)Encoding.UTF8.GetByteCount(json);

        string label = bytes switch
        {
            >= 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0 / 1024.0:F1} GB",
            >= 1024 * 1024         => $"{bytes / 1024.0 / 1024.0:F1} MB",
            >= 1024                => $"{bytes / 1024.0:F1} KB",
            _                      => $"{bytes} B"
        };

        var awsCost = (bytes / 1024.0 / 1024.0 / 1024.0) * AwsCostPerGb;
        return (bytes, label, awsCost);
    }
}
