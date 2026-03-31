using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// LIKE検索デモのサービス実装
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/LikeSearch/internal-design.md</para>
/// <para><strong>責務:</strong> 前方一致と中間一致のLIKE検索パフォーマンス差をSQLiteで実測する</para>
/// <para><strong>依存関係:</strong></para>
/// <list type="bullet">
/// <item><description>IConfiguration: 接続文字列（LikeSearchDemo）取得</description></item>
/// <item><description>ILogger: 実行時間・件数のログ出力</description></item>
/// </list>
/// </remarks>
public class LikeSearchService : ILikeSearchService
{
    /// <summary>SQLiteの1回のINSERTで扱う行数。SQLiteのパラメータ上限(999)を考慮した値</summary>
    private const int BatchSize = 500;

    private readonly IConfiguration _configuration;
    private readonly ILogger<LikeSearchService> _logger;

    /// <summary>セットアップ時に生成するデータ件数。デフォルトは10万件</summary>
    private readonly int _totalRows;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="configuration">接続文字列を含む設定</param>
    /// <param name="logger">ロガー</param>
    /// <param name="totalRows">生成するデータ件数（テスト時に小さい値を渡せるよう引数化）</param>
    public LikeSearchService(IConfiguration configuration, ILogger<LikeSearchService> logger, int totalRows = 100_000)
    {
        _configuration = configuration;
        _logger = logger;
        _totalRows = totalRows;
    }

    /// <summary>
    /// デモ用データ（デフォルト10万件）をセットアップする
    /// </summary>
    /// <returns>セットアップ結果（件数・処理時間）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong></para>
    /// <list type="number">
    /// <item><description>テーブルとName列インデックスを作成（CREATE TABLE / CREATE INDEX IF NOT EXISTS）</description></item>
    /// <item><description>既存データがある場合はスキップして早期リターン</description></item>
    /// <item><description>500件ずつバッチINSERTでデータを生成（トランザクションで一括コミット）</description></item>
    /// </list>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// CREATE TABLE IF NOT EXISTS SearchUsers (
    ///     Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ///     Name TEXT NOT NULL,
    ///     Email TEXT NOT NULL,
    ///     CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
    /// );
    /// CREATE INDEX IF NOT EXISTS IX_SearchUsers_Name ON SearchUsers(Name);
    /// </code>
    /// <para><strong>期待実行時間:</strong> 約10〜20秒（10万件・初回のみ）</para>
    /// </remarks>
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

        await InsertBatchDataAsync(connection);

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

    /// <summary>
    /// 前方一致（LIKE 'keyword%'）で検索する
    /// </summary>
    /// <param name="keyword">検索キーワード</param>
    /// <returns>検索結果（件数・処理時間・UsesIndex=true）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong> IX_SearchUsers_Name インデックスを使ってB-Treeで前方一致検索する</para>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// SELECT Id, Name, Email FROM SearchUsers WHERE Name LIKE @keyword  -- @keyword = 'keyword%'
    /// </code>
    /// <para><strong>期待実行時間:</strong> 約1〜10ms（インデックス有効）</para>
    /// </remarks>
    public async Task<LikeSearchResponse> SearchPrefixAsync(string keyword)
    {
        var sw = Stopwatch.StartNew();
        var result = new List<SearchUserInfo>();

        // 前方一致パターン: インデックスが有効になる
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

    /// <summary>
    /// 中間一致（LIKE '%keyword%'）で検索する
    /// </summary>
    /// <param name="keyword">検索キーワード</param>
    /// <returns>検索結果（件数・処理時間・UsesIndex=false）</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong> 先頭が'%'のためインデックスが無効になり全件スキャンする</para>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// SELECT Id, Name, Email FROM SearchUsers WHERE Name LIKE @keyword  -- @keyword = '%keyword%'
    /// </code>
    /// <para><strong>期待実行時間:</strong> 約50〜200ms（インデックス無効・フルスキャン）</para>
    /// </remarks>
    public async Task<LikeSearchResponse> SearchPartialAsync(string keyword)
    {
        var sw = Stopwatch.StartNew();
        var result = new List<SearchUserInfo>();

        // 中間一致パターン: 先頭が'%'のためインデックスが使えずフルスキャンになる
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

    /// <summary>接続文字列からSQLite接続を生成する</summary>
    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("LikeSearchDemo");
        return new SqliteConnection(connectionString);
    }

    /// <summary>SearchUsersテーブルとName列インデックスが存在しない場合に作成する</summary>
    private static async Task EnsureTableCreatedAsync(SqliteConnection connection)
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

    /// <summary>
    /// 日本語名のダミーデータをバッチINSERTで生成する
    /// </summary>
    /// <remarks>
    /// BatchSize件ずつINSERTしトランザクションで一括コミットすることで
    /// 単件INSERTに比べて大幅にパフォーマンスを改善している
    /// </remarks>
    private async Task InsertBatchDataAsync(SqliteConnection connection)
    {
        var lastNames = new[] { "山田", "鈴木", "佐藤", "高橋", "伊藤", "渡辺", "山本", "中村", "小林", "加藤",
                                 "山口", "山崎", "山下", "川田", "田中", "斎藤", "松本", "井上", "木村", "林" };
        var firstNames = new[] { "太郎", "花子", "次郎", "美咲", "健一", "恵子", "大輔", "裕子", "隆", "由美",
                                  "一郎", "幸子", "浩二", "洋子", "明", "直子", "誠", "智子", "豊", "節子" };

        using var transaction = connection.BeginTransaction();
        for (int batch = 0; batch < _totalRows / BatchSize; batch++)
        {
            var values = new StringBuilder();
            for (int i = 0; i < BatchSize; i++)
            {
                var rowNum = batch * BatchSize + i + 1;
                var name = lastNames[rowNum % lastNames.Length] + firstNames[rowNum % firstNames.Length];
                if (i > 0) values.Append(',');
                values.Append($"('{name}', 'user{rowNum}@example.com')");
            }
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO SearchUsers (Name, Email) VALUES {values}";
            await insertCmd.ExecuteNonQueryAsync();
        }
        transaction.Commit();
    }
}
