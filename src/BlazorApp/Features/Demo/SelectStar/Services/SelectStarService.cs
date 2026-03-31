using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// SELECT *問題デモのサービス実装
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/select-star-demo/internal-design.md</para>
/// <para><strong>責務:</strong> SELECT *（全カラム取得）と必要カラムのみ取得を比較し、転送量・AWS費用の差を示す</para>
/// <para><strong>依存関係:</strong></para>
/// <list type="bullet">
/// <item><description>IConfiguration: 接続文字列（SelectStarDemo）取得</description></item>
/// <item><description>ILogger: 実行時間・データ量のログ出力</description></item>
/// </list>
/// </remarks>
public class SelectStarService : ISelectStarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SelectStarService> _logger;
    private readonly int _totalRows;

    /// <summary>AWS データ転送料金（$0.01/GB）。コスト推計に使用</summary>
    private const double AwsCostPerGb = 0.01;

    /// <summary>INSERT時のバッチサイズ。SQLiteのパラメータ数上限（999）以内に収まる値</summary>
    private const int BatchSize = 500;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="configuration">接続文字列を含む設定</param>
    /// <param name="logger">ロガー</param>
    /// <param name="totalRows">セットアップ時に挿入する総行数（デフォルト: 10,000件）</param>
    public SelectStarService(IConfiguration configuration, ILogger<SelectStarService> logger, int totalRows = 10_000)
    {
        _configuration = configuration;
        _logger = logger;
        _totalRows = totalRows;
    }

    /// <summary>
    /// テストデータのセットアップ（Profilesテーブル作成 + 大容量データ挿入）
    /// </summary>
    /// <returns>セットアップ結果（行数・処理時間）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong></para>
    /// <list type="number">
    /// <item><description>Profilesテーブルが未作成の場合に作成</description></item>
    /// <item><description>既存データがある場合はスキップして早期リターン</description></item>
    /// <item><description>Bio(10KB) / Preferences(5KB) / ActivityLog(20KB) の大容量テキストを生成</description></item>
    /// <item><description><see cref="BatchSize"/>件ずつバッチINSERTをトランザクション内で実行</description></item>
    /// </list>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// INSERT INTO Profiles (Name, Email, Bio, Preferences, ActivityLog)
    /// VALUES (...), (...), ...   -- BatchSize件ずつ
    /// </code>
    /// <para><strong>期待実行時間:</strong> 約5〜15秒（10,000件・大容量テキスト含む）</para>
    /// </remarks>
    public async Task<SetupResponse> SetupAsync()
    {
        var sw = Stopwatch.StartNew();

        using var connection = GetConnection();
        await connection.OpenAsync();
        await EnsureTableCreatedAsync(connection);

        var existing = await CountExistingRowsAsync(connection);
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

        await InsertBatchDataAsync(connection);

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

    /// <summary>
    /// SELECT * による全カラム取得（Bad実装）
    /// </summary>
    /// <returns>検索結果（転送量・AWS費用推計・先頭3件のデータ）</returns>
    /// <remarks>
    /// <para><strong>SQL文:</strong></para>
    /// <code>SELECT * FROM Profiles</code>
    /// <para><strong>期待転送量:</strong> 約350MB（Bio/Preferences/ActivityLogを含む全カラム）</para>
    /// </remarks>
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

    /// <summary>
    /// 必要カラムのみ取得（Good実装）
    /// </summary>
    /// <returns>検索結果（転送量・AWS費用推計・先頭3件のデータ）</returns>
    /// <remarks>
    /// <para><strong>SQL文:</strong></para>
    /// <code>SELECT Id, Name, Email FROM Profiles</code>
    /// <para><strong>期待転送量:</strong> 約1MB未満（必要最小限のカラムのみ）</para>
    /// </remarks>
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

    /// <summary>接続文字列からSQLite接続を生成する</summary>
    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("SelectStarDemo");
        return new SqliteConnection(connectionString);
    }

    /// <summary>
    /// Profilesテーブルが存在しない場合に作成する
    /// </summary>
    /// <remarks>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// CREATE TABLE IF NOT EXISTS Profiles (
    ///     Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ///     Name TEXT NOT NULL, Email TEXT NOT NULL,
    ///     Bio TEXT, Preferences TEXT, ActivityLog TEXT,
    ///     CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
    /// );
    /// </code>
    /// </remarks>
    private static async Task EnsureTableCreatedAsync(SqliteConnection connection)
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

    /// <summary>Profilesテーブルの既存行数を返す</summary>
    private static async Task<int> CountExistingRowsAsync(SqliteConnection connection)
    {
        var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM Profiles";
        return Convert.ToInt32(await countCmd.ExecuteScalarAsync());
    }

    /// <summary>
    /// <see cref="BatchSize"/>件ずつバッチINSERTでProfilesデータを挿入する
    /// </summary>
    /// <remarks>
    /// トランザクションをまとめることでSQLiteのfsync呼び出しを削減し、挿入速度を向上させる。
    /// Bio/Preferences/ActivityLogはパラメータバインドで渡し、SQLインジェクションを防ぐ。
    /// </remarks>
    private async Task InsertBatchDataAsync(SqliteConnection connection)
    {
        // SELECT *問題を際立たせるための大容量テキスト（カラムごとの想定サイズ: Bio≈10KB, Preferences≈5KB, ActivityLog≈20KB）
        var bio = string.Concat(Enumerable.Repeat("私は東京都出身のソフトウェアエンジニアです。Web開発を得意としており、特にC#と.NETの経験が豊富です。", 100));
        var preferences = string.Concat(Enumerable.Repeat("{\"theme\":\"dark\",\"language\":\"ja\",\"notifications\":{\"email\":true,\"push\":true}}", 62));
        var activityLog = string.Concat(Enumerable.Repeat("[{\"timestamp\":\"2024-01-01T00:00:00Z\",\"action\":\"login\",\"ip\":\"192.168.1.1\"}]", 200));

        var lastNames = new[] { "田中", "鈴木", "佐藤", "高橋", "伊藤", "渡辺", "山本", "中村", "小林", "加藤" };
        var firstNames = new[] { "太郎", "花子", "次郎", "美咲", "健一", "恵子", "大輔", "裕子", "隆", "由美" };

        using var transaction = connection.BeginTransaction();
        for (int batch = 0; batch < _totalRows / BatchSize; batch++)
        {
            var values = new StringBuilder();
            for (int i = 0; i < BatchSize; i++)
            {
                var rowNum = batch * BatchSize + i + 1;
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
    }

    /// <summary>
    /// オブジェクトをJSONシリアライズしてバイト数・ラベル・AWS転送費用を計算する
    /// </summary>
    /// <param name="data">計算対象のオブジェクト</param>
    /// <returns>(バイト数, 表示用ラベル, AWSコスト推計)</returns>
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
