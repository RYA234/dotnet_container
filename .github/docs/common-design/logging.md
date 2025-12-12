# ログ設計

## 文書情報
- **作成日**: 2025-12-12
- **最終更新**: 2025-12-12
- **バージョン**: 1.0
- **ステータス**: 実装中

---

## 1. ログ設計の基本方針

### 1.1 ログの目的

1. **デバッグ**: 開発中の問題解決
2. **監視**: 本番環境の異常検知
3. **分析**: パフォーマンス分析、ユーザー行動分析
4. **監査**: セキュリティイベント、データ変更の追跡
5. **コンプライアンス**: 法的要件の遵守

---

### 1.2 ログレベル

| レベル | 用途 | 例 | 本番出力 |
|--------|------|-----|---------|
| **Trace** | 詳細なデバッグ情報 | ループ内の変数値 | ❌ |
| **Debug** | デバッグ情報 | SQL実行、メソッド呼び出し | ❌ |
| **Information** | 重要な処理の記録 | API呼び出し成功、ユーザー作成 | ✅ |
| **Warning** | 警告事項 | リトライ実行、非推奨機能使用 | ✅ |
| **Error** | エラー | 例外発生、DB接続失敗 | ✅ |
| **Critical** | 致命的エラー | システム停止、データ破損 | ✅ |

**本番環境のログレベル**: `Information` 以上のみ出力

---

## 2. 構造化ログ

### 2.1 ILogger を使用した構造化ログ

```csharp
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public async Task<User> CreateUser(CreateUserRequest request)
    {
        // ❌ NG: 文字列連結
        _logger.LogInformation($"Creating user: {request.Name}, {request.Email}");

        // ✅ OK: 構造化ログ（パラメータ使用）
        _logger.LogInformation("Creating user: {UserName}, {Email}",
            request.Name, request.Email);

        try
        {
            var user = await CreateUserInDatabase(request);

            // ✅ OK: 成功ログ
            _logger.LogInformation("User created successfully: {UserId}, {Email}",
                user.Id, user.Email);

            return user;
        }
        catch (Exception ex)
        {
            // ✅ OK: エラーログ（例外オブジェクトを渡す）
            _logger.LogError(ex, "Failed to create user: {Email}", request.Email);
            throw;
        }
    }

    public async Task<User> GetUserById(int id)
    {
        _logger.LogDebug("Getting user by id: {UserId}", id);

        var user = await GetUserFromDatabase(id);

        if (user == null)
        {
            // ✅ OK: 警告ログ
            _logger.LogWarning("User not found: {UserId}", id);
            throw new NotFoundException("User", id.ToString());
        }

        return user;
    }
}
```

---

### 2.2 ログ出力例（JSON形式）

```json
{
  "timestamp": "2025-12-12T10:00:00.123Z",
  "level": "Information",
  "message": "User created successfully: {UserId}, {Email}",
  "properties": {
    "UserId": 123,
    "Email": "user@example.com",
    "SourceContext": "BlazorApp.Features.User.UserService",
    "RequestId": "0HMN8J9K7L6M5N4O3P2Q1R0S",
    "RequestPath": "/api/users",
    "Method": "POST"
  }
}
```

---

## 3. Serilog 設定

### 3.1 Serilog のインストール

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Seq
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
```

---

### 3.2 Serilog の設定（Program.cs）

```csharp
using Serilog;
using Serilog.Events;

// Serilog 設定
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .WriteTo.Seq("http://localhost:5341")  // Seq サーバー（ログ可視化ツール）
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog を使用
    builder.Host.UseSerilog();

    // ... その他の設定

    var app = builder.Build();

    // リクエストログ
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode > 499
                ? LogEventLevel.Error
                : LogEventLevel.Information;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
        };
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

---

### 3.3 appsettings.json での設定

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

---

## 4. ログのベストプラクティス

### 4.1 ログ出力の推奨パターン

```csharp
public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;

    public async Task<Order> CreateOrder(CreateOrderRequest request)
    {
        // ✅ OK: 処理開始ログ（Debug）
        _logger.LogDebug("Creating order: {UserId}, {ItemCount}",
            request.UserId, request.Items.Count);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // ビジネスロジック
            var order = await ProcessOrder(request);

            stopwatch.Stop();

            // ✅ OK: 成功ログ（Information）+ パフォーマンス測定
            _logger.LogInformation(
                "Order created successfully: {OrderId}, {UserId}, {TotalAmount}, {ElapsedMs}ms",
                order.Id, request.UserId, order.TotalAmount, stopwatch.ElapsedMilliseconds);

            return order;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // ✅ OK: エラーログ（Error）+ 例外オブジェクト
            _logger.LogError(ex,
                "Failed to create order: {UserId}, {ItemCount}, {ElapsedMs}ms",
                request.UserId, request.Items.Count, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private async Task<Order> ProcessOrder(CreateOrderRequest request)
    {
        // ✅ OK: 重要な処理ステップをログ出力
        _logger.LogInformation("Validating order items: {ItemCount}", request.Items.Count);

        await ValidateItems(request.Items);

        _logger.LogInformation("Calculating total amount: {UserId}", request.UserId);

        var totalAmount = CalculateTotalAmount(request.Items);

        _logger.LogInformation("Saving order to database: {UserId}, {TotalAmount}",
            request.UserId, totalAmount);

        var order = await SaveOrderToDatabase(request, totalAmount);

        return order;
    }
}
```

---

### 4.2 パフォーマンス測定ログ

```csharp
using System.Diagnostics;

public class PerformanceLoggingService
{
    private readonly ILogger<PerformanceLoggingService> _logger;

    public async Task<T> MeasureAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        Dictionary<string, object>? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Starting operation: {OperationName}, {Parameters}",
            operationName, parameters);

        try
        {
            var result = await operation();
            stopwatch.Stop();

            // ✅ OK: パフォーマンスログ
            _logger.LogInformation(
                "Operation completed: {OperationName}, {ElapsedMs}ms, {Parameters}",
                operationName, stopwatch.ElapsedMilliseconds, parameters);

            // 遅い処理を警告
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow operation detected: {OperationName}, {ElapsedMs}ms, {Parameters}",
                    operationName, stopwatch.ElapsedMilliseconds, parameters);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Operation failed: {OperationName}, {ElapsedMs}ms, {Parameters}",
                operationName, stopwatch.ElapsedMilliseconds, parameters);

            throw;
        }
    }
}

// 使用例
var result = await _performanceLogger.MeasureAsync(
    "GetUserOrders",
    async () => await _orderRepository.GetUserOrders(userId),
    new Dictionary<string, object> { ["UserId"] = userId });
```

---

### 4.3 SQL実行ログ

```csharp
public class DatabaseLogger
{
    private readonly ILogger<DatabaseLogger> _logger;

    public async Task<T> ExecuteQueryAsync<T>(
        string sql,
        Dictionary<string, object> parameters,
        Func<Task<T>> queryExecutor)
    {
        var stopwatch = Stopwatch.StartNew();

        // ✅ OK: SQL実行ログ（Debug）
        _logger.LogDebug("Executing SQL: {Sql}, {Parameters}", sql, parameters);

        try
        {
            var result = await queryExecutor();
            stopwatch.Stop();

            // ✅ OK: SQL実行時間ログ（Information）
            _logger.LogInformation("SQL executed successfully: {ElapsedMs}ms, {Sql}",
                stopwatch.ElapsedMilliseconds, sql);

            // 遅いクエリを警告
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning("Slow SQL detected: {ElapsedMs}ms, {Sql}, {Parameters}",
                    stopwatch.ElapsedMilliseconds, sql, parameters);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "SQL execution failed: {ElapsedMs}ms, {Sql}, {Parameters}",
                stopwatch.ElapsedMilliseconds, sql, parameters);

            throw;
        }
    }
}
```

---

## 5. セキュリティログ

### 5.1 認証・認可ログ

```csharp
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;

    public async Task<AuthResult> SignIn(string email, string password)
    {
        _logger.LogInformation("Sign-in attempt: {Email}", email);

        try
        {
            var result = await _authClient.SignIn(email, password);

            // ✅ OK: 認証成功ログ
            _logger.LogInformation("Sign-in successful: {Email}, {UserId}",
                email, result.User.Id);

            return result;
        }
        catch (UnauthorizedException ex)
        {
            // ✅ OK: 認証失敗ログ（Warning）
            _logger.LogWarning("Sign-in failed: {Email}, {Reason}",
                email, "Invalid credentials");

            throw;
        }
    }

    public async Task<bool> AuthorizeUser(int userId, string permission)
    {
        _logger.LogDebug("Authorization check: {UserId}, {Permission}",
            userId, permission);

        var hasPermission = await CheckPermission(userId, permission);

        if (!hasPermission)
        {
            // ✅ OK: 認可失敗ログ（Warning）
            _logger.LogWarning("Authorization denied: {UserId}, {Permission}",
                userId, permission);
        }

        return hasPermission;
    }
}
```

---

### 5.2 秘密情報のマスキング

```csharp
public class SecureLogger
{
    private readonly ILogger<SecureLogger> _logger;

    public void LogUserCreated(User user, string password)
    {
        // ❌ NG: パスワードをログ出力
        _logger.LogInformation("User created: {Email}, {Password}", user.Email, password);

        // ✅ OK: パスワードをマスキング
        _logger.LogInformation("User created: {Email}, Password: ***", user.Email);
    }

    public void LogDatabaseConnection(string connectionString)
    {
        // ❌ NG: 接続文字列をそのまま出力
        _logger.LogDebug("Connecting to database: {ConnectionString}", connectionString);

        // ✅ OK: 接続文字列をマスキング
        var maskedConnectionString = MaskConnectionString(connectionString);
        _logger.LogDebug("Connecting to database: {MaskedConnectionString}",
            maskedConnectionString);
    }

    private static string MaskConnectionString(string connectionString)
    {
        // "Password=abc123" → "Password=***"
        return Regex.Replace(connectionString,
            @"(Password|Pwd)=[^;]+",
            "$1=***",
            RegexOptions.IgnoreCase);
    }
}
```

---

## 6. AWS CloudWatch Logs 統合

### 6.1 CloudWatch Logs Sink 設定

```bash
dotnet add package AWS.Logger.SeriLog
```

```csharp
using AWS.Logger.SeriLog;

// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.AWSSeriLog(
        configuration: builder.Configuration,
        textFormatter: new Serilog.Formatting.Json.JsonFormatter())
    .CreateLogger();
```

**appsettings.json**:
```json
{
  "AWS": {
    "Region": "ap-northeast-1"
  },
  "AWS.Logging": {
    "LogGroup": "/ecs/dotnet-app",
    "Region": "ap-northeast-1"
  }
}
```

---

### 6.2 CloudWatch Logs クエリ例

```
# エラーログを検索
fields @timestamp, level, message, exception
| filter level = "Error"
| sort @timestamp desc
| limit 100

# 遅いSQL検索
fields @timestamp, ElapsedMs, Sql
| filter ElapsedMs > 100
| sort ElapsedMs desc
| limit 50

# 認証失敗を検索
fields @timestamp, Email, Reason
| filter message like "Sign-in failed"
| stats count() by Email
```

---

## 7. ログ監視とアラート

### 7.1 CloudWatch Alarms 設定

```yaml
# Terraform 例
resource "aws_cloudwatch_log_metric_filter" "error_count" {
  name           = "ErrorCount"
  log_group_name = "/ecs/dotnet-app"
  pattern        = "[timestamp, level=Error, ...]"

  metric_transformation {
    name      = "ErrorCount"
    namespace = "DotnetApp"
    value     = "1"
  }
}

resource "aws_cloudwatch_metric_alarm" "high_error_rate" {
  alarm_name          = "HighErrorRate"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "ErrorCount"
  namespace           = "DotnetApp"
  period              = "300"  # 5分間
  statistic           = "Sum"
  threshold           = "10"    # 5分間に10件以上エラー

  alarm_actions = [aws_sns_topic.alerts.arn]
}
```

---

### 7.2 Seq を使用したログ可視化

```bash
# Seq を Docker で起動
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

**ブラウザで Seq にアクセス**: `http://localhost:5341`

**Seq のクエリ例**:
- `Level = "Error"` - エラーログのみ表示
- `ElapsedMs > 1000` - 1秒以上かかった処理を表示
- `UserId = 123` - 特定ユーザーのログを表示

---

## 8. ログローテーション

### 8.1 ファイルログのローテーション

```csharp
// Serilog 設定
.WriteTo.File(
    path: "logs/app-.log",
    rollingInterval: RollingInterval.Day,        // 日次ローテーション
    retainedFileCountLimit: 30,                  // 30日分保持
    fileSizeLimitBytes: 100_000_000,             // 100MB制限
    rollOnFileSizeLimit: true,                   // サイズ超過時にローテーション
    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
```

**生成されるログファイル例**:
```
logs/app-20251212.log
logs/app-20251211.log
logs/app-20251210.log
...
```

---

## 9. ログのベストプラクティス まとめ

### 9.1 やるべきこと ✅

1. **構造化ログを使用**
   - `_logger.LogInformation("User created: {UserId}", userId)`

2. **適切なログレベルを選択**
   - Debug: デバッグ情報
   - Information: 重要な処理
   - Warning: 警告事項
   - Error: エラー

3. **例外オブジェクトを渡す**
   - `_logger.LogError(ex, "Failed to create user")`

4. **パフォーマンスを測定**
   - `_logger.LogInformation("Query executed in {ElapsedMs}ms", sw.ElapsedMilliseconds)`

5. **セキュリティイベントを記録**
   - 認証成功/失敗、認可失敗

6. **秘密情報をマスキング**
   - パスワード、接続文字列、API キー

---

### 9.2 やってはいけないこと ❌

1. **文字列連結を使用**
   - ❌ `_logger.LogInformation($"User: {userId}")`

2. **ループ内で大量のログ出力**
   - ❌ `foreach (var item in items) { _logger.LogDebug("Item: {Id}", item.Id); }`

3. **秘密情報をそのまま出力**
   - ❌ `_logger.LogInformation("Password: {Password}", password)`

4. **本番環境で Debug ログを有効化**
   - ❌ `"MinimumLevel": "Debug"` （本番は `"Information"` 以上）

5. **例外を文字列で出力**
   - ❌ `_logger.LogError($"Error: {ex.Message}")`

---

## 10. 参考

- [エラーハンドリング設計](error-handling.md)
- [セキュリティ設計](security.md)
- [API設計規約](api-design.md)
- [Serilog Documentation](https://serilog.net/)
- [AWS CloudWatch Logs](https://docs.aws.amazon.com/AmazonCloudWatch/latest/logs/)
