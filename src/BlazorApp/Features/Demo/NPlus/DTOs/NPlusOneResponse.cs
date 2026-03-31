namespace BlazorApp.Features.Demo.DTOs;

/// <summary>N+1問題デモの検索結果DTO</summary>
public class NPlusOneResponse
{
    /// <summary>処理時間（ミリ秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>発行されたSQLクエリの総数（Bad: 1+N, Good: 1）</summary>
    public int SqlCount { get; set; }

    /// <summary>レスポンスのJSONバイト数</summary>
    public int DataSize { get; set; }

    /// <summary>取得したユーザー件数</summary>
    public int RowCount { get; set; }

    /// <summary>検索結果データ（ユーザー＋部署情報）</summary>
    public List<UserWithDepartment> Data { get; set; } = new();

    /// <summary>結果メッセージ（N+1の説明・クエリ数）</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>ユーザー情報と部署情報を結合したDTO</summary>
public class UserWithDepartment
{
    /// <summary>ユーザーID</summary>
    public int Id { get; set; }

    /// <summary>ユーザー名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>所属部署</summary>
    public DepartmentInfo Department { get; set; } = new();
}

/// <summary>部署情報DTO</summary>
public class DepartmentInfo
{
    /// <summary>部署ID</summary>
    public int Id { get; set; }

    /// <summary>部署名</summary>
    public string Name { get; set; } = string.Empty;
}
