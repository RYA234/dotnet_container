namespace BlazorApp.Features.Demo.DTOs;

/// <summary>フルスキャンデモの検索結果DTO</summary>
public class FullScanResponse
{
    /// <summary>クエリの実行時間（ミリ秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>ヒットした件数</summary>
    public int RowCount { get; set; }

    /// <summary>インデックスが使用されたかどうか</summary>
    public bool HasIndex { get; set; }

    /// <summary>結果メッセージ（実行計画・インデックス有無の説明）</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>検索結果データ（先頭数件）</summary>
    public List<LargeUserInfo> Data { get; set; } = new();
}

/// <summary>フルスキャンデモ用ユーザー情報DTO</summary>
public class LargeUserInfo
{
    /// <summary>ユーザーID</summary>
    public int Id { get; set; }

    /// <summary>メールアドレス（インデックス対象カラム）</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>ユーザー名</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>テストデータセットアップ結果DTO（全デモ共通）</summary>
public class SetupResponse
{
    /// <summary>セットアップが成功したかどうか</summary>
    public bool Success { get; set; }

    /// <summary>挿入または既存のデータ件数</summary>
    public int RowCount { get; set; }

    /// <summary>処理時間（ミリ秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>結果メッセージ</summary>
    public string Message { get; set; } = string.Empty;
}
