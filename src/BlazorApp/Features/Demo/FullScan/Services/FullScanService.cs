using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// フルスキャンデモのサービス実装
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/FullScan/internal-design.md</para>
/// <para><strong>責務:</strong> SQLiteを使ってインデックスあり/なしの検索パフォーマンス差を実測する</para>
/// <para><strong>依存関係:</strong></para>
/// <list type="bullet">
/// <item><description>IConfiguration: 接続文字列（FullScanDemo）取得</description></item>
/// <item><description>ILogger: 実行時間・件数のログ出力</description></item>
/// </list>
/// </remarks>
public class FullScanService : IFullScanService
{
    /// <summary>SQLiteへの1回のINSERTで扱う行数。SQLiteのパラメータ上限(999)を考慮した値</summary>
    private const int BatchSize = 500;

    private readonly IConfiguration _configuration;
    private readonly ILogger<FullScanService> _logger;

    /// <summary>セットアップ時に生成するデータ件数。デフォルトは100万件</summary>
    private readonly int _totalRows;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="configuration">接続文字列を含む設定</param>
    /// <param name="logger">ロガー</param>
    /// <param name="totalRows">生成するデータ件数（テスト時に小さい値を渡せるよう引数化）</param>
    public FullScanService(IConfiguration configuration, ILogger<FullScanService> logger, int totalRows = 1_000_000)
    {
        _configuration = configuration;
        _logger = logger;
        _totalRows = totalRows;
    }

    /// <summary>
    /// デモ用データ（デフォルト100万件）をセットアップする
    /// </summary>
    /// <returns>セットアップ結果（件数・処理時間）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong></para>
    /// <list type="number">
    /// <item><description>テーブル未作成の場合は作成（CREATE TABLE IF NOT EXISTS）</description></item>
    /// <item><description>既存データがある場合はスキップして早期リターン</description></item>
    /// <item><description>500件ずつバッチINSERTでデータを生成（トランザクションで一括コミット）</description></item>
    /// </list>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// CREATE TABLE IF NOT EXISTS LargeUsers (
    ///     Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ///     Email TEXT NOT NULL,
    ///     Name TEXT NOT NULL,
    ///     CreatedAt TEXT DEFAULT (datetime('now'))
    /// );
    /// INSERT INTO LargeUsers (Email, Name) VALUES (...), (...), ...;
    /// </code>
    /// <para><strong>期待実行時間:</strong> 約30〜60秒（100万件・初回のみ）</para>
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
        _logger.LogInformation("FullScan setup completed: {RowCount} rows, {ExecutionTimeMs}ms", _totalRows, sw.ElapsedMilliseconds);

        return new SetupResponse
        {
            Success = true,
            RowCount = _totalRows,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Message = $"セットアップ完了: {_totalRows:N0}件のデータを生成しました"
        };
    }

    /// <summary>
    /// インデックスなしでメールアドレスを検索する（フルスキャン）
    /// </summary>
    /// <param name="email">検索するメールアドレス</param>
    /// <returns>検索結果（件数・処理時間・HasIndex=false）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong> インデックスなしで全件スキャンしてEmailを比較する</para>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// SELECT Id, Email, Name FROM LargeUsers WHERE Email = @email
    /// </code>
    /// <para><strong>期待実行時間:</strong> 約200〜800ms（インデックスなしでフルスキャン）</para>
    /// </remarks>
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

    /// <summary>
    /// Email列にインデックスを作成する
    /// </summary>
    /// <returns>インデックス作成結果（処理時間）</returns>
    /// <remarks>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// CREATE INDEX IF NOT EXISTS IX_LargeUsers_Email ON LargeUsers(Email)
    /// </code>
    /// <para><strong>期待実行時間:</strong> 約5〜15秒（100万件に対してインデックス構築）</para>
    /// </remarks>
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

    /// <summary>
    /// インデックスありでメールアドレスを検索する
    /// </summary>
    /// <param name="email">検索するメールアドレス</param>
    /// <returns>検索結果（件数・処理時間・HasIndex=true）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong> IX_LargeUsers_Email インデックスを使ってB-Treeで高速検索する</para>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// SELECT Id, Email, Name FROM LargeUsers WHERE Email = @email
    /// </code>
    /// <para><strong>期待実行時間:</strong> 約1〜5ms（インデックスによりO(log n)で検索）</para>
    /// </remarks>
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

    /// <summary>接続文字列からSQLite接続を生成する</summary>
    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("FullScanDemo");
        return new SqliteConnection(connectionString);
    }

    /// <summary>LargeUsersテーブルが存在しない場合に作成する</summary>
    private static async Task EnsureTableCreatedAsync(SqliteConnection connection)
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

    /// <summary>LargeUsersテーブルの現在の行数を返す</summary>
    private static async Task<int> CountExistingRowsAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM LargeUsers";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    /// <summary>
    /// 日本語名のダミーデータをバッチINSERTで生成する
    /// </summary>
    /// <remarks>
    /// BatchSize件ずつINSERTしトランザクションで一括コミットすることで
    /// 単件INSERTに比べて大幅にパフォーマンスを改善している
    /// </remarks>
    private async Task InsertBatchDataAsync(SqliteConnection connection)
    {
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
                values.Append($"('user{rowNum:D7}@example.com', '{name}')");
            }
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO LargeUsers (Email, Name) VALUES {values}";
            await insertCmd.ExecuteNonQueryAsync();
        }
        transaction.Commit();
    }
}
