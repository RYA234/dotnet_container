namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// DB接続デモ用サービスのインターフェース。
/// IDbConnectionFactory 経由でDB接続・クエリ発行のデモを行う。
/// </summary>
public interface IDatabaseConnectionDemoService
{
    /// <summary>DB接続テスト</summary>
    Task<DbConnectionTestResult> TestConnectionAsync();

    /// <summary>テーブル一覧を取得（SQLiteのsqlite_masterを使用）</summary>
    Task<IEnumerable<string>> GetTablesAsync();

    /// <summary>指定テーブルの件数を取得</summary>
    Task<int> GetRowCountAsync(string tableName);
}

/// <summary>DB接続テスト結果</summary>
public record DbConnectionTestResult(
    bool IsConnected,
    string DatabaseType,
    string ConnectionString,
    long ElapsedMs
);
