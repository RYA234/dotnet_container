using System.Data;

namespace BlazorApp.Shared.Data;

/// <summary>
/// RDBMS非依存のDB接続ファクトリインターフェース。
/// Provider設定を変えるだけで SQLite / PostgreSQL / SQL Server を切り替えられる。
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>使用中のRDBMS種別（"SQLite", "PostgreSQL", "SqlServer"）</summary>
    string DatabaseType { get; }

    /// <summary>新しいDB接続を作成して返す（未オープン状態）</summary>
    IDbConnection CreateConnection();

    /// <summary>新しいDB接続を作成し、オープン状態で返す</summary>
    Task<IDbConnection> CreateOpenConnectionAsync();

    /// <summary>接続テスト（接続可能かどうかを確認）</summary>
    /// <returns>接続成功なら true、失敗なら false</returns>
    Task<bool> TestConnectionAsync();
}
