# SELECT * 無駄遣いデモ - 内部設計書

## 文書情報
- **作成日**: 2026-03-12
- **最終更新**: 2026-03-12
- **バージョン**: 1.0
- **ステータス**: 設計中

---

## 1. アーキテクチャ

### 1.1 レイヤー構成

```
[Razor View] SelectStar.cshtml
      ↓
[Controller] DemoController.cs
      ↓
[Service] ISelectStarService / SelectStarService.cs
      ↓
[ADO.NET] Microsoft.Data.Sqlite
      ↓
[SQLite] select_star_demo.db
```

### 1.2 ファイル構成

```
src/BlazorApp/
├── Features/
│   └── Demo/
│       ├── DTOs/
│       │   └── SelectStarResponse.cs       # レスポンス型定義
│       ├── Services/
│       │   ├── ISelectStarService.cs       # インターフェース
│       │   └── SelectStarService.cs        # 実装
│       └── Views/
│           └── SelectStar.cshtml           # Razor View
├── Data/
│   └── select_star_demo.db                 # SQLite（gitignore済み）
└── appsettings.json                        # 接続文字列追加

BlazorApp.Tests/
└── Features/
    └── Demo/
        └── SelectStarServiceTests.cs       # ユニットテスト
```

---

## 2. クラス設計

### 2.1 ISelectStarService

```csharp
public interface ISelectStarService
{
    Task<SetupResponse> SetupAsync();
    Task<SelectStarResponse> GetAllColumnsAsync();
    Task<SelectStarResponse> GetSpecificColumnsAsync();
}
```

### 2.2 SelectStarService

```csharp
public class SelectStarService : ISelectStarService
{
    private readonly string _connectionString;
    private readonly ILogger<SelectStarService> _logger;
    private readonly int _totalRows;

    // テスト時は 100件、本番は 10000件
    public SelectStarService(IConfiguration config, ILogger<SelectStarService> logger, int totalRows = 10000)
    {
        _connectionString = config.GetConnectionString("SelectStarDemo")
            ?? throw new InvalidOperationException("SelectStarDemo connection string is not configured.");
        _logger = logger;
        _totalRows = totalRows;
    }
}
```

### 2.3 DTOs

```csharp
// 共通レスポンス
public class SelectStarResponse
{
    public long ExecutionTimeMs { get; set; }
    public int RowCount { get; set; }
    public long DataSize { get; set; }
    public string DataSizeLabel { get; set; }
    public double AwsCostEstimate { get; set; }
    public string Sql { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
}

// SELECT * 用（全カラム）
public class ProfileFull
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Bio { get; set; }
    public string Preferences { get; set; }
    public string ActivityLog { get; set; }
    public string CreatedAt { get; set; }
}

// 必要カラムのみ
public class ProfileSummary
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

---

## 3. 処理フロー

### 3.1 SetupAsync

```
1. テーブル作成（CREATE TABLE IF NOT EXISTS Profiles）
2. COUNT(*) でデータ件数確認
3. 0件の場合のみデータ生成
4. バッチINSERT（500件 × バッチ数）+ トランザクション
5. 完了メッセージ返却
```

**大容量データの生成**:
```csharp
// Bio: 約10KB（100文字 × 100回繰り返し）
var bio = string.Concat(Enumerable.Repeat("私は東京都出身のソフトウェアエンジニアです。", 100));

// Preferences: 約5KB（80文字 × 62回繰り返し）
var preferences = string.Concat(Enumerable.Repeat("{\"theme\":\"dark\",\"language\":\"ja\",\"notifications\":{\"email\":true}}", 62));

// ActivityLog: 約20KB（100文字 × 200回繰り返し）
var activityLog = string.Concat(Enumerable.Repeat("[{\"timestamp\":\"2024-01-01T00:00:00Z\",\"action\":\"login\"}]", 200));
```

### 3.2 GetAllColumnsAsync

```
1. Stopwatch 開始
2. SELECT * FROM Profiles を実行
3. List<ProfileFull> にマッピング
4. Stopwatch 停止
5. JSON シリアライズでデータサイズ計算
6. AWS転送料概算計算
7. SelectStarResponse 返却
```

### 3.3 GetSpecificColumnsAsync

```
1. Stopwatch 開始
2. SELECT Id, Name, Email FROM Profiles を実行
3. List<ProfileSummary> にマッピング
4. Stopwatch 停止
5. JSON シリアライズでデータサイズ計算
6. AWS転送料概算計算
7. SelectStarResponse 返却
```

---

## 4. データサイズ計算の実装

```csharp
private static (long bytes, string label, double awsCost) CalcDataSize(object data)
{
    var json = System.Text.Json.JsonSerializer.Serialize(data);
    var bytes = (long)System.Text.Encoding.UTF8.GetByteCount(json);

    string label = bytes switch
    {
        >= 1024 * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0 / 1024.0:F1} GB",
        >= 1024 * 1024        => $"{bytes / 1024.0 / 1024.0:F1} MB",
        >= 1024               => $"{bytes / 1024.0:F1} KB",
        _                     => $"{bytes} B"
    };

    // AWS Data Transfer: $0.01/GB（教育用簡略化）
    const double costPerGb = 0.01;
    var awsCost = (bytes / 1024.0 / 1024.0 / 1024.0) * costPerGb;

    return (bytes, label, awsCost);
}
```

---

## 5. データベース設計（物理）

### 5.1 DDL

```sql
CREATE TABLE IF NOT EXISTS Profiles (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT    NOT NULL,
    Email       TEXT    NOT NULL,
    Bio         TEXT,
    Preferences TEXT,
    ActivityLog TEXT,
    CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now'))
);
```

### 5.2 バッチINSERT

```csharp
// 500件ずつトランザクションで投入
const int batchSize = 500;
for (int batch = 0; batch < _totalRows / batchSize; batch++)
{
    using var transaction = connection.BeginTransaction();
    for (int i = 0; i < batchSize; i++)
    {
        var index = batch * batchSize + i + 1;
        // INSERT 実行
    }
    transaction.Commit();
}
```

---

## 6. DI 登録（Program.cs）

```csharp
// SELECT * デモ用 SQLite — ContentRootPath で絶対パスに変換
var selectStarDbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "select_star_demo.db");
builder.Configuration["ConnectionStrings:SelectStarDemo"] = $"Data Source={selectStarDbPath};";

builder.Services.AddScoped<ISelectStarService, SelectStarService>();
```

---

## 7. Controller 実装

```csharp
// DemoController.cs への追加

private readonly ISelectStarService _selectStarService;

// コンストラクタに追加
public DemoController(..., ISelectStarService selectStarService)
{
    _selectStarService = selectStarService;
}

// アクション
[HttpGet]
public IActionResult SelectStar() => View();

[HttpPost]
public async Task<IActionResult> SelectStarSetup()
    => Json(await _selectStarService.SetupAsync());

[HttpGet]
public async Task<IActionResult> SelectStarAllColumns()
    => Json(await _selectStarService.GetAllColumnsAsync());

[HttpGet]
public async Task<IActionResult> SelectStarSpecificColumns()
    => Json(await _selectStarService.GetSpecificColumnsAsync());
```

---

## 8. テスト設計方針

### 8.1 使用技術
- **xUnit** + **FluentAssertions** + **Moq**
- インメモリ SQLite（`Data Source=:memory:;Cache=Shared`）

### 8.2 テスト対象

| テスト | 検証内容 |
|--------|---------|
| Setup_CreatesTableAndData | テーブル作成・データ投入 |
| Setup_SkipsIfAlreadyExists | 既存データがあればスキップ |
| GetAllColumns_ReturnsAllFields | Bio/Preferences/ActivityLog を含む |
| GetAllColumns_DataSizeIsLarge | データサイズが指定カラムより大きい |
| GetSpecificColumns_ReturnsOnlyThreeFields | Id/Name/Email のみ |
| GetSpecificColumns_DataSizeIsSmall | データサイズが全カラムより小さい |
| DataSizeDifference_IsSignificant | 全カラム vs 必要カラムで大幅な差がある |
| AwsCostEstimate_IsCalculated | AWS転送料が 0 より大きい |
| ExecutionTime_IsRecorded | 実行時間が記録される |

---

## 9. 参考

- [要件定義書](requirements.md)
- [外部設計書](external-design.md)
- [FullScanService 参考実装](../../../src/BlazorApp/Features/Demo/Services/FullScanService.cs)
