# ログ・テスト設計

## ログ設計

### ログレベル

| レベル | 用途 | 例 |
|-------|------|-----|
| Error | 例外発生時 | DB接続エラー、予期しないエラー |
| Warning | 想定外の状況 | データ0件、リトライ |
| Information | 正常処理 | API呼び出し、処理完了 |
| Debug | 開発時デバッグ | SQL文、クエリ回数 |
| Trace | 詳細デバッグ | ループ内の変数 |

---

### ログ出力例

#### Error
```csharp
_logger.LogError(ex, "Error in N+1 bad endpoint");
```

**出力**:
```
2025-12-10T12:00:00Z [ERR] Error in N+1 bad endpoint
System.Data.SqliteException: unable to open database file
   at Microsoft.Data.Sqlite.SqliteConnection.Open()
   ...
```

#### Warning
```csharp
_logger.LogWarning("No users found in database");
```

**出力**:
```
2025-12-10T12:00:00Z [WRN] No users found in database
```

#### Information
```csharp
_logger.LogInformation("N+1 bad executed: {QueryCount} queries, {ExecutionTimeMs}ms",
    result.SqlCount, result.ExecutionTimeMs);
```

**出力**:
```
2025-12-10T12:00:00Z [INF] N+1 bad executed: 101 queries, 45ms
```

#### Debug
```csharp
_logger.LogDebug("Executing SQL: {Sql}", sql);
```

**出力**:
```
2025-12-10T12:00:00Z [DBG] Executing SQL: SELECT Id, Name, DepartmentId, Email FROM Users
```

---

### 構造化ログ

#### 推奨: プレースホルダ
```csharp
_logger.LogInformation("User {UserId} accessed endpoint {Endpoint}", userId, endpoint);
```

**理由**:
- 構造化されたログ（JSON）
- CloudWatch Logs Insights でクエリ可能

#### ❌ Bad: 文字列連結
```csharp
_logger.LogInformation($"User {userId} accessed endpoint {endpoint}");
```

**問題点**:
- 文字列として記録される
- クエリが難しい

---

### ログ出力先

#### 開発環境
- **Console**: 標準出力
- **File**: logs/app.log

#### 本番環境（ECS）
- **CloudWatch Logs**: `/ecs/dotnet-app`
- **保持期間**: 1日（コスト削減）

---

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

---

## テスト設計

### 単体テスト

#### テストケース（NPlusOneService）

| No | テストケース名 | 入力 | 期待結果 |
|----|-------------|------|---------|
| T-01 | GetUsersBad_正常系 | なし | SqlCount = 101 |
| T-02 | GetUsersGood_正常系 | なし | SqlCount = 1 |
| T-03 | GetUsersBad_性能比較 | なし | ExecutionTimeMs > GetUsersGood |
| T-04 | GetUsersBad_データ件数 | なし | RowCount = 100 |
| T-05 | GetUsersGood_データ件数 | なし | RowCount = 100 |

#### 実装例
```csharp
public class NPlusOneServiceTests
{
    private readonly NPlusOneService _service;
    private readonly ILogger<NPlusOneService> _logger;
    private readonly IConfiguration _configuration;

    public NPlusOneServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"ConnectionStrings:DemoDatabase", "Data Source=demo_test.db"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _logger = new Mock<ILogger<NPlusOneService>>().Object;
        _service = new NPlusOneService(_configuration, _logger);
    }

    [Fact]
    public async Task GetUsersBad_正常系_SqlCountが101である()
    {
        // Act
        var result = await _service.GetUsersBad();

        // Assert
        Assert.Equal(101, result.SqlCount);
    }

    [Fact]
    public async Task GetUsersGood_正常系_SqlCountが1である()
    {
        // Act
        var result = await _service.GetUsersGood();

        // Assert
        Assert.Equal(1, result.SqlCount);
    }

    [Fact]
    public async Task GetUsersBad_性能比較_BadがGoodより遅い()
    {
        // Act
        var badResult = await _service.GetUsersBad();
        var goodResult = await _service.GetUsersGood();

        // Assert
        Assert.True(badResult.ExecutionTimeMs > goodResult.ExecutionTimeMs);
    }

    [Fact]
    public async Task GetUsersBad_データ件数_100件である()
    {
        // Act
        var result = await _service.GetUsersBad();

        // Assert
        Assert.Equal(100, result.RowCount);
    }
}
```

---

### 統合テスト（未実装）

#### テストケース（DemoController）

| No | テストケース名 | 入力 | 期待結果 |
|----|-------------|------|---------|
| T-10 | NPlusOneBad_正常系 | GET /api/demo/n-plus-one/bad | 200 OK |
| T-11 | NPlusOneGood_正常系 | GET /api/demo/n-plus-one/good | 200 OK |
| T-12 | NPlusOneBad_レスポンス形式 | GET /api/demo/n-plus-one/bad | JSON形式 |

---

### E2Eテスト（未実装）

#### Playwright

```typescript
test('N+1問題デモ - Bad版実行', async ({ page }) => {
  await page.goto('https://rya234.com/dotnet/Demo/Performance');

  await page.click('#run-bad-button');

  await page.waitForSelector('#result-bad');
  const result = await page.textContent('#result-bad');
  expect(result).toContain('"sqlCount":101');
});
```

---

### テストデータ

#### demo_test.db
- **Users**: 100件
- **Departments**: 5件
- **初期化**: テストメソッド実行前に毎回作成

```csharp
[Fact]
public async Task SetupTestDatabase()
{
    using (var connection = new SqliteConnection("Data Source=demo_test.db"))
    {
        await connection.OpenAsync();

        // テーブル作成
        var createDepartmentsTable = @"
            CREATE TABLE IF NOT EXISTS Departments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            )";
        new SqliteCommand(createDepartmentsTable, connection).ExecuteNonQuery();

        // 初期データ投入
        // ...
    }
}
```

---

### テストカバレッジ

#### 目標
- **行カバレッジ**: 80%以上
- **分岐カバレッジ**: 70%以上

#### 測定
```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage
```

---

### CI/CDでのテスト実行

#### GitHub Actions
```yaml
- name: Run tests
  run: dotnet test --no-build --verbosity normal
```

---

### パフォーマンステスト（未実装）

#### BenchmarkDotNet

```csharp
[MemoryDiagnoser]
public class NPlusOneBenchmark
{
    private NPlusOneService _service;

    [GlobalSetup]
    public void Setup()
    {
        // ...
    }

    [Benchmark]
    public async Task GetUsersBad()
    {
        await _service.GetUsersBad();
    }

    [Benchmark]
    public async Task GetUsersGood()
    {
        await _service.GetUsersGood();
    }
}
```

**実行**:
```bash
dotnet run -c Release --project Benchmarks
```

**出力例**:
```
| Method        | Mean      | Error    | StdDev   | Gen 0   | Allocated |
|-------------- |----------:|---------:|---------:|--------:|----------:|
| GetUsersBad   | 45.23 ms  | 0.85 ms  | 0.75 ms  | 125.00  | 512 KB    |
| GetUsersGood  | 12.45 ms  | 0.23 ms  | 0.20 ms  | 62.50   | 256 KB    |
```

---

## 参考

- [エラー処理設計](error-handling.md)
- [クラス設計](class-design.md)
- [運用設計手順書](../operations.md)
