namespace BlazorApp.Features.Demo.DTOs;

/// <summary>LIKE検索デモの検索結果DTO</summary>
public class LikeSearchResponse
{
    /// <summary>クエリの実行時間（ミリ秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>ヒットした件数</summary>
    public int RowCount { get; set; }

    /// <summary>インデックスが使用されたかどうか（前方一致: true / 部分一致: false）</summary>
    public bool UsesIndex { get; set; }

    /// <summary>検索種別（"prefix" / "partial"）</summary>
    public string SearchType { get; set; } = "";

    /// <summary>実行されたSQL文</summary>
    public string Sql { get; set; } = "";

    /// <summary>検索キーワード</summary>
    public string Keyword { get; set; } = "";

    /// <summary>結果メッセージ（インデックス使用有無の説明）</summary>
    public string Message { get; set; } = "";

    /// <summary>検索結果データ（先頭数件）</summary>
    public List<SearchUserInfo> Data { get; set; } = new();
}

/// <summary>LIKE検索デモ用ユーザー情報DTO</summary>
public class SearchUserInfo
{
    /// <summary>ユーザーID</summary>
    public int Id { get; set; }

    /// <summary>ユーザー名</summary>
    public string Name { get; set; } = "";

    /// <summary>メールアドレス</summary>
    public string Email { get; set; } = "";
}
