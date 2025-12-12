// このファイルは設計書とコードの同期のためのサンプルコードです
// 実際のコードは Features/[機能名]/ フォルダに配置してください

namespace BlazorApp.Features.[Feature];

using Microsoft.Data.Sqlite;
using System.Diagnostics;

/// <summary>
/// [機能名]のビジネスロジック
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/[機能名]/internal-design.md</para>
/// <para><strong>責務:</strong> [責務の説明]</para>
/// <para><strong>依存関係:</strong></para>
/// <list type="bullet">
/// <item><description>IConfiguration: 設定取得</description></item>
/// <item><description>ILogger&lt;FeatureService&gt;: ログ出力</description></item>
/// </list>
/// </remarks>
public class FeatureService : IFeatureService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeatureService> _logger;

    public FeatureService(IConfiguration configuration, ILogger<FeatureService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// [メソッドの概要]
    /// </summary>
    /// <param name="request">リクエストパラメータ</param>
    /// <returns>レスポンス</returns>
    /// <remarks>
    /// <para><strong>アルゴリズム:</strong></para>
    /// <list type="number">
    /// <item><description>入力検証</description></item>
    /// <item><description>データベースアクセス</description></item>
    /// <item><description>ビジネスロジック実行</description></item>
    /// <item><description>レスポンス生成</description></item>
    /// </list>
    /// <para><strong>SQL文:</strong></para>
    /// <code>
    /// SELECT * FROM Table WHERE Id = @Id;
    /// </code>
    /// <para><strong>期待結果:</strong> データが正常に取得される</para>
    /// </remarks>
    public async Task<Response> DoSomething(Request request)
    {
        var sw = Stopwatch.StartNew();

        // 入力検証
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // データベースアクセス
        using var connection = GetConnection();
        await connection.OpenAsync();

        var sql = "SELECT * FROM Table WHERE Id = @Id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", request.Id);

        // ビジネスロジック実行
        var result = await command.ExecuteReaderAsync();

        // レスポンス生成
        sw.Stop();
        _logger.LogInformation("DoSomething executed in {ElapsedMs}ms", sw.ElapsedMilliseconds);

        return new Response
        {
            Result = "success",
            ExecutionTimeMs = sw.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// データベース接続を取得
    /// </summary>
    /// <returns>SqliteConnection</returns>
    /// <remarks>
    /// <para><strong>接続文字列:</strong> appsettings.json の ConnectionStrings:DemoDatabase を使用</para>
    /// </remarks>
    private SqliteConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DemoDatabase");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DemoDatabase' not found");
        }
        return new SqliteConnection(connectionString);
    }
}

/// <summary>
/// [機能名]サービスのインターフェース
/// </summary>
public interface IFeatureService
{
    /// <summary>
    /// [メソッドの概要]
    /// </summary>
    /// <param name="request">リクエストパラメータ</param>
    /// <returns>レスポンス</returns>
    Task<Response> DoSomething(Request request);
}

/// <summary>
/// リクエストパラメータ
/// </summary>
public class Request
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// レスポンス
/// </summary>
public class Response
{
    /// <summary>
    /// 処理結果
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// 実行時間（ミリ秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}
