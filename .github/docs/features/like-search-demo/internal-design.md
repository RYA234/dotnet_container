# LIKE検索デモ - 内部設計書

## 文書情報
- **作成日**: 2026-03-12
- **最終更新**: 2026-03-12
- **バージョン**: 1.0
- **ステータス**: 設計中

---

## 1. アーキテクチャ

### 1.1 レイヤー構成

```
[Razor View] LikeSearch.cshtml
      ↓
[Controller] DemoController.cs
      ↓
[Service] ILikeSearchService / LikeSearchService.cs
      ↓
[ADO.NET] Microsoft.Data.Sqlite
      ↓
[SQLite] like_search_demo.db
```

### 1.2 ファイル構成

```
src/BlazorApp/
├── Features/
│   └── Demo/
│       ├── DTOs/
│       │   └── LikeSearchResponse.cs       # レスポンス型定義
│       ├── Services/
│       │   ├── ILikeSearchService.cs       # インターフェース
│       │   └── LikeSearchService.cs        # 実装
│       └── Views/
│           └── LikeSearch.cshtml           # Razor View
├── Data/
│   └── like_search_demo.db                 # SQLite（gitignore済み）
└── appsettings.json                        # 接続文字列追加

BlazorApp.Tests/
└── Features/
    └── Demo/
        └── LikeSearchServiceTests.cs       # ユニットテスト
```

---

## 2. クラス設計

### 2.1 ILikeSearchService

```csharp
public interface ILikeSearchService
{
    Task<SetupResponse> SetupAsync();
    Task<LikeSearchResponse> SearchPrefixAsync(string keyword);
    Task<LikeSearchResponse> SearchPartialAsync(string keyword);
}
```

### 2.2 LikeSearchService

```csharp
public class LikeSearchService : ILikeSearchService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LikeSearchService> _logger;
    private readonly int _totalRows;

    // テスト時は 1000件、本番は 100000件
    public LikeSearchService(IConfiguration config, ILogger<LikeSearchService> logger, int totalRows = 100_000)
}
```

---

## 3. 処理フロー

### 3.1 SetupAsync

```
1. テーブル作成（CREATE TABLE IF NOT EXISTS SearchUsers）
2. インデックス作成（CREATE INDEX IF NOT EXISTS IX_SearchUsers_Name）
3. COUNT(*) でデータ件数確認
4. 0件の場合のみデータ生成
5. バッチINSERT（500件ずつ）+ トランザクション
```

**日本人名の生成**:
```csharp
var lastNames = new[] { "山田", "鈴木", "佐藤", "高橋", "伊藤", "渡辺", "山本", "中村", "小林", "加藤",
                         "山口", "山崎", "山下", "川田", "田中", "斎藤", "松本", "井上", "木村", "林" };
var firstNames = new[] { "太郎", "花子", "次郎", "美咲", "健一", "恵子", "大輔", "裕子", "隆", "由美",
                          "一郎", "幸子", "浩二", "洋子", "明", "直子", "誠", "智子", "豊", "節子" };
```

### 3.2 SearchPrefixAsync

```sql
-- 前方一致: インデックス使用可能
SELECT Id, Name, Email FROM SearchUsers WHERE Name LIKE @keyword
-- @keyword = '山%'
```

### 3.3 SearchPartialAsync

```sql
-- 中間一致: インデックス無効（フルスキャン）
SELECT Id, Name, Email FROM SearchUsers WHERE Name LIKE @keyword
-- @keyword = '%山%'
```

---

## 4. データベース設計（物理）

### 4.1 DDL

```sql
CREATE TABLE IF NOT EXISTS SearchUsers (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    Name      TEXT    NOT NULL,
    Email     TEXT    NOT NULL,
    CreatedAt TEXT    NOT NULL DEFAULT (datetime('now'))
);

-- 前方一致で有効。中間一致では無効（SQLiteの仕様）
CREATE INDEX IF NOT EXISTS IX_SearchUsers_Name ON SearchUsers(Name);
```

---

## 5. DI 登録（Program.cs）

```csharp
var likeSearchDbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "like_search_demo.db");
builder.Configuration["ConnectionStrings:LikeSearchDemo"] = $"Data Source={likeSearchDbPath};";

builder.Services.AddScoped<ILikeSearchService, LikeSearchService>();
```

---

## 6. テスト設計方針

### 6.1 使用技術
- xUnit + FluentAssertions
- インメモリ SQLite（`Data Source=:memory:;Cache=Shared`）
- テスト時は 1000件（totalRows パラメータ）

### 6.2 テスト観点

| テスト | 検証内容 |
|--------|---------|
| Setup_CreatesTableAndIndex | テーブル・インデックス作成 |
| Setup_SkipsIfAlreadyExists | 既存データがあればスキップ |
| SearchPrefix_ReturnsMatchingUsers | 前方一致で正しく検索 |
| SearchPrefix_UsesIndex | usesIndex = true |
| SearchPrefix_KeywordPatternIsCorrect | キーワードが 'xxx%' 形式 |
| SearchPartial_ReturnsMatchingUsers | 中間一致で正しく検索 |
| SearchPartial_NotUsesIndex | usesIndex = false |
| SearchPartial_KeywordPatternIsCorrect | キーワードが '%xxx%' 形式 |
| SearchPartial_FindsMoreThanPrefix | 中間一致の方が件数が多い |

---

## 7. 参考

- [要件定義書](requirements.md)
- [外部設計書](external-design.md)
- [SelectStarService 参考実装](../../../src/BlazorApp/Features/Demo/Services/SelectStarService.cs)
