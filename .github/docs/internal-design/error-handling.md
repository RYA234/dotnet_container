# エラーハンドリング設計

## 例外処理方針

### レイヤー別の責務

| レイヤー | 責務 | 例外処理 |
|---------|------|---------|
| Controller | HTTPリクエスト処理 | try-catchでラップ、500エラー返却 |
| Service | ビジネスロジック | 業務例外をスロー |
| DB層 | データアクセス | SqlException をそのままスロー |

---

## Controller層のエラーハンドリング

### DemoController

```csharp
[HttpGet("api/demo/n-plus-one/bad")]
public async Task<IActionResult> NPlusOneBad()
{
    try
    {
        var result = await _nPlusOneService.GetUsersBad();
        return Ok(result);
    }
    catch (SqliteException ex)
    {
        _logger.LogError(ex, "Database error in N+1 bad endpoint");
        return StatusCode(500, new
        {
            error = "Database connection failed",
            code = "DB_ERROR",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error in N+1 bad endpoint");
        return StatusCode(500, new
        {
            error = "Internal server error",
            code = "INTERNAL_ERROR",
            timestamp = DateTime.UtcNow
        });
    }
}
```

**ポイント**:
- **SqliteException**: DB接続エラー
- **Exception**: 予期しないエラー
- **ログ出力**: すべての例外を記録
- **エラーレスポンス**: JSON形式で返却

---

## Service層のエラーハンドリング

### NPlusOneService

```csharp
public async Task<NPlusOneResponse> GetUsersBad()
{
    // 例外はそのままスロー（Controller層でキャッチ）
    var connection = GetConnection();
    await connection.OpenAsync(); // SqliteException がスローされる可能性

    // ...
}

private SqliteConnection GetConnection()
{
    var connectionString = _configuration.GetConnectionString("DemoDatabase");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DemoDatabase' not found");
    }
    return new SqliteConnection(connectionString);
}
```

**ポイント**:
- **設定エラー**: `InvalidOperationException` をスロー
- **DB接続エラー**: `SqliteException` をそのままスロー
- **ログ不要**: Controller層でログ出力

---

## エラーレスポンス形式

### 共通エラー形式
```json
{
  "error": "エラーメッセージ",
  "code": "ERROR_CODE",
  "timestamp": "2025-12-10T12:00:00Z"
}
```

### エラーコード一覧

| コード | 意味 | HTTPステータス | 説明 |
|-------|------|---------------|------|
| DB_ERROR | データベースエラー | 500 | DB接続失敗、SQL実行エラー |
| CONFIG_ERROR | 設定エラー | 500 | 接続文字列未設定 |
| INTERNAL_ERROR | 内部エラー | 500 | 予期しないエラー |
| VALIDATION_ERROR | 入力エラー | 400 | パラメータ不正 |
| NOT_FOUND | リソース未検出 | 404 | データが見つからない |

---

## ログ設計

### ログレベル

| レベル | 用途 | 例 |
|-------|------|-----|
| Error | 例外発生時 | DB接続エラー、予期しないエラー |
| Warning | 想定外の状況 | データ0件、リトライ |
| Information | 正常処理 | API呼び出し、処理完了 |
| Debug | 開発時デバッグ | SQL文、クエリ回数 |

### ログ出力例

```csharp
// Error
_logger.LogError(ex, "Error in N+1 bad endpoint");

// Warning
_logger.LogWarning("No users found in database");

// Information
_logger.LogInformation("N+1 bad executed: {QueryCount} queries, {ExecutionTimeMs}ms",
    result.SqlCount, result.ExecutionTimeMs);

// Debug
_logger.LogDebug("Executing SQL: {Sql}", sql);
```

---

## 例外クラス設計

### 業務例外（将来実装）

```csharp
public class BusinessException : Exception
{
    public string ErrorCode { get; }

    public BusinessException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

// 使用例
if (inventory < 0)
{
    throw new BusinessException("在庫不足です", "INSUFFICIENT_INVENTORY");
}
```

---

## リトライ戦略（将来実装）

### Exponential Backoff

```csharp
public async Task<T> ExecuteWithRetry<T>(Func<Task<T>> func, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await func();
        }
        catch (SqliteException ex) when (i < maxRetries - 1)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, i)); // 1秒, 2秒, 4秒
            _logger.LogWarning(ex, "Retry {Attempt}/{MaxRetries} after {Delay}s",
                i + 1, maxRetries, delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }
    throw new Exception("Max retries exceeded");
}
```

---

## Circuit Breaker（将来実装）

### Polly を使用

```csharp
var circuitBreakerPolicy = Policy
    .Handle<SqliteException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    );

// 使用例
var result = await circuitBreakerPolicy.ExecuteAsync(async () =>
{
    return await _nPlusOneService.GetUsersBad();
});
```

**用途**: 外部API（Supabase）の障害時に遮断

---

## タイムアウト設計

### DB接続タイムアウト

```csharp
var connectionString = "Data Source=demo.db;Connection Timeout=30";
var connection = new SqliteConnection(connectionString);
```

### HTTP リクエストタイムアウト

```csharp
// Supabase 接続テスト
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};
```

---

## エラーハンドリングのテスト

### 単体テスト

```csharp
[Fact]
public async Task NPlusOneBad_DatabaseError_Returns500()
{
    // Arrange
    var mockService = new Mock<INPlusOneService>();
    mockService.Setup(s => s.GetUsersBad())
        .ThrowsAsync(new SqliteException("Connection failed", 1));

    var controller = new DemoController(mockService.Object, _logger);

    // Act
    var result = await controller.NPlusOneBad();

    // Assert
    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusCodeResult.StatusCode);
}
```

---

## 実例: 2025-12-10 Secrets Manager エラー

### 発生したエラー
```
ResourceInitializationError: unable to retrieve secret from asm:
service call has been retried 1 time(s):
retrieved secret from Secrets Manager did not contain json key anon_key
```

### 原因
- Secrets Manager のキー名: `anonKey` (camelCase)
- タスク定義の参照: `anon_key` (snake_case)

### 対応
```bash
aws secretsmanager update-secret \
  --secret-id ecs/typescript-container/supabase \
  --secret-string '{"url":"...","anon_key":"..."}' \
  --region ap-northeast-1
```

### 教訓
- **キー名の統一**: snake_case に統一
- **環境変数検証**: 起動時にチェック
- **ログ出力**: エラー内容を詳細に記録

詳細: [運用設計手順書 - インシデント対応](../operations.md#ケース1-ecs-タスクが起動しない)

---

## 参考

- [クラス設計](class-design.md)
- [シーケンス図](sequence-diagrams.md)
- [運用設計手順書](../operations.md)
