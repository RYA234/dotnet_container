namespace BlazorApp.Features.Demo.DTOs;

/// <summary>SELECT *デモの検索結果DTO</summary>
public class SelectStarResponse
{
    /// <summary>クエリの実行時間（ミリ秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>取得した件数</summary>
    public int RowCount { get; set; }

    /// <summary>転送データのバイト数</summary>
    public long DataSize { get; set; }

    /// <summary>転送量の表示用ラベル（例: "350.2 MB"）</summary>
    public string DataSizeLabel { get; set; } = "";

    /// <summary>AWS転送費用の推計（$0.01/GB 基準）</summary>
    public double AwsCostEstimate { get; set; }

    /// <summary>実行されたSQL文</summary>
    public string Sql { get; set; } = "";

    /// <summary>結果メッセージ（転送量・AWS費用の説明）</summary>
    public string Message { get; set; } = "";

    /// <summary>検索結果データ（先頭3件。全件返却はデータ量が大きすぎるため省略）</summary>
    public object Data { get; set; } = new();
}

/// <summary>SELECT *（全カラム）で取得したプロフィールDTO</summary>
public class ProfileFull
{
    /// <summary>プロフィールID</summary>
    public int Id { get; set; }

    /// <summary>ユーザー名</summary>
    public string Name { get; set; } = "";

    /// <summary>メールアドレス</summary>
    public string Email { get; set; } = "";

    /// <summary>自己紹介文（約10KB）</summary>
    public string Bio { get; set; } = "";

    /// <summary>設定情報JSON（約5KB）</summary>
    public string Preferences { get; set; } = "";

    /// <summary>アクティビティログJSON（約20KB）</summary>
    public string ActivityLog { get; set; } = "";

    /// <summary>作成日時</summary>
    public string CreatedAt { get; set; } = "";
}

/// <summary>必要カラムのみ（Id, Name, Email）取得したプロフィールDTO</summary>
public class ProfileSummary
{
    /// <summary>プロフィールID</summary>
    public int Id { get; set; }

    /// <summary>ユーザー名</summary>
    public string Name { get; set; } = "";

    /// <summary>メールアドレス</summary>
    public string Email { get; set; } = "";
}
