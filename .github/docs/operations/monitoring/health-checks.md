# ヘルスチェック

## 概要
アプリケーションの稼働状態を確認するヘルスチェックの実装と運用方法を説明します。

## ヘルスチェックの種類

### 1. Liveness Check（生存確認）
アプリケーションが起動しているかを確認します。

**用途**: ECSタスクの再起動判断

### 2. Readiness Check（準備確認）
アプリケーションがリクエストを受け付ける準備ができているかを確認します。

**用途**: ALBのターゲットグループへの追加/削除判断

### 3. Dependency Check（依存関係確認）
外部サービス（データベース、API等）との接続状態を確認します。

**用途**: 詳細な診断、障害箇所の特定

## ヘルスチェックエンドポイント

### 基本エンドポイント

| エンドポイント | 種類 | レスポンス | 用途 |
|--------------|------|-----------|------|
| `/healthz` | Liveness | 200 OK / 503 Service Unavailable | ECS/ALBヘルスチェック |
| `/ready` | Readiness | 200 OK / 503 Service Unavailable | ALBターゲット登録 |
| `/health/dependencies` | Dependency | JSON詳細情報 | 診断・監視 |

### ASP.NET Core での実装

#### Program.cs での設定

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ヘルスチェックの登録
builder.Services.AddHealthChecks()
    // 基本的なヘルスチェック
    .AddCheck("self", () => HealthCheckResult.Healthy())

    // データベース接続チェック
    .AddNpgSql(
        builder.Configuration["Supabase:ConnectionString"],
        name: "database",
        tags: new[] { "db", "sql" })

    // HTTPエンドポイントチェック（外部API）
    .AddUrlGroup(
        new Uri("https://api.external-service.com/health"),
        name: "external-api",
        tags: new[] { "api" })

    // カスタムヘルスチェック
    .AddCheck<CustomHealthCheck>("custom");

var app = builder.Build();

// Liveness エンドポイント（基本的な生存確認のみ）
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("self"),
    AllowCachingResponses = false
});

// Readiness エンドポイント（依存関係を含む）
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = (check) => true,
    AllowCachingResponses = false
});

// 詳細診断エンドポイント（JSON形式）
app.MapHealthChecks("/health/dependencies", new HealthCheckOptions
{
    Predicate = (check) => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false
});

app.Run();
```

#### カスタムヘルスチェックの実装

```csharp
public class CustomHealthCheck : IHealthCheck
{
    private readonly ILogger<CustomHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public CustomHealthCheck(
        ILogger<CustomHealthCheck> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // カスタムチェックロジック
            // 例: キャッシュの確認、ファイルシステムのチェックなど

            var isHealthy = await PerformHealthCheckAsync(cancellationToken);

            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Custom check passed");
            }

            return HealthCheckResult.Degraded("Custom check returned degraded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom health check failed");
            return HealthCheckResult.Unhealthy(
                "Custom check failed",
                ex);
        }
    }

    private async Task<bool> PerformHealthCheckAsync(
        CancellationToken cancellationToken)
    {
        // 実際のチェックロジック
        await Task.Delay(10, cancellationToken); // シミュレーション
        return true;
    }
}
```

### レスポンス形式

#### シンプルレスポンス（/healthz, /ready）

```
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```

```
HTTP/1.1 503 Service Unavailable
Content-Type: text/plain

Unhealthy
```

#### 詳細レスポンス（/health/dependencies）

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0521234",
  "entries": {
    "self": {
      "status": "Healthy",
      "duration": "00:00:00.0001234",
      "data": {}
    },
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0234567",
      "data": {
        "connectionString": "Host=db.xxx.supabase.co"
      }
    },
    "external-api": {
      "status": "Healthy",
      "duration": "00:00:00.0285433",
      "data": {
        "uri": "https://api.external-service.com/health"
      }
    },
    "custom": {
      "status": "Healthy",
      "duration": "00:00:00.0100000",
      "description": "Custom check passed"
    }
  }
}
```

## ALB ヘルスチェック設定

### ターゲットグループのヘルスチェック設定

```bash
# ヘルスチェック設定の確認
aws elbv2 describe-target-groups \
  --names dotnet-target-group \
  --region ap-northeast-1 \
  --query 'TargetGroups[0].HealthCheckEnabled,HealthCheckPath,HealthCheckIntervalSeconds,HealthyThresholdCount,UnhealthyThresholdCount'

# ヘルスチェック設定の変更
aws elbv2 modify-target-group \
  --target-group-arn <target-group-arn> \
  --health-check-path /healthz \
  --health-check-interval-seconds 30 \
  --health-check-timeout-seconds 5 \
  --healthy-threshold-count 2 \
  --unhealthy-threshold-count 3 \
  --matcher HttpCode=200 \
  --region ap-northeast-1
```

### 推奨設定

| パラメータ | 推奨値 | 説明 |
|-----------|--------|------|
| HealthCheckPath | `/healthz` | ヘルスチェックエンドポイント |
| HealthCheckIntervalSeconds | `30` | チェック間隔（秒） |
| HealthCheckTimeoutSeconds | `5` | タイムアウト（秒） |
| HealthyThresholdCount | `2` | 正常判定までのチェック回数 |
| UnhealthyThresholdCount | `3` | 異常判定までのチェック回数 |
| Matcher | `200` | 正常とみなすHTTPステータスコード |

## ECS ヘルスチェック設定

### タスク定義でのヘルスチェック

```json
{
  "family": "dotnet-task",
  "containerDefinitions": [
    {
      "name": "dotnet-app",
      "image": "110221759530.dkr.ecr.ap-northeast-1.amazonaws.com/dotnet-app:latest",
      "healthCheck": {
        "command": [
          "CMD-SHELL",
          "curl -f http://localhost:8080/healthz || exit 1"
        ],
        "interval": 30,
        "timeout": 5,
        "retries": 3,
        "startPeriod": 60
      }
    }
  ]
}
```

### ヘルスチェックパラメータ

| パラメータ | 説明 | 推奨値 |
|-----------|------|--------|
| interval | チェック間隔（秒） | 30 |
| timeout | タイムアウト（秒） | 5 |
| retries | リトライ回数 | 3 |
| startPeriod | 起動猶予期間（秒） | 60 |

## ヘルスチェックの確認

### 手動確認

```bash
# Liveness Check
curl -i https://rya234.com/dotnet/healthz

# Readiness Check
curl -i https://rya234.com/dotnet/ready

# 詳細診断
curl -s https://rya234.com/dotnet/health/dependencies | jq
```

### ALBターゲットヘルスの確認

```bash
# ターゲットグループのヘルス状態確認
aws elbv2 describe-target-health \
  --target-group-arn <target-group-arn> \
  --region ap-northeast-1

# 出力例
{
    "TargetHealthDescriptions": [
        {
            "Target": {
                "Id": "10.0.1.100",
                "Port": 8080
            },
            "HealthCheckPort": "8080",
            "TargetHealth": {
                "State": "healthy"
            }
        }
    ]
}
```

### ECSタスクヘルスの確認

```bash
# タスクのヘルス状態確認
aws ecs describe-tasks \
  --cluster app-cluster \
  --tasks <task-arn> \
  --region ap-northeast-1 \
  --query 'tasks[0].healthStatus'

# 出力: HEALTHY, UNHEALTHY, UNKNOWN
```

## ヘルスチェック監視

### CloudWatch Alarms

```bash
# ALBターゲットのUnhealthyアラーム作成
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-unhealthy-targets \
  --alarm-description "Alert when targets are unhealthy" \
  --metric-name UnHealthyHostCount \
  --namespace AWS/ApplicationELB \
  --statistic Average \
  --period 60 \
  --evaluation-periods 2 \
  --threshold 1 \
  --comparison-operator GreaterThanOrEqualToThreshold \
  --dimensions Name=TargetGroup,Value=<target-group-full-name> Name=LoadBalancer,Value=<load-balancer-full-name> \
  --region ap-northeast-1
```

### 定期的なヘルスチェック監視スクリプト

```bash
#!/bin/bash
# health-check-monitor.sh

HEALTH_URL="https://rya234.com/dotnet/healthz"
SLACK_WEBHOOK="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"

# ヘルスチェック実行
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ "$HTTP_STATUS" -ne 200 ]; then
  # Slack通知
  curl -X POST $SLACK_WEBHOOK \
    -H 'Content-Type: application/json' \
    -d "{\"text\":\"⚠️ Health check failed: HTTP $HTTP_STATUS\"}"

  exit 1
fi

echo "Health check passed: HTTP $HTTP_STATUS"
```

## トラブルシューティング

### ヘルスチェックが失敗する場合

#### 1. アプリケーションログを確認

```bash
# 最新のエラーログを確認
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --start-time $(date -d '10 minutes ago' +%s)000 \
  --region ap-northeast-1
```

#### 2. エンドポイントに直接アクセス

```bash
# タスクのプライベートIPを取得
TASK_ARN=$(aws ecs list-tasks --cluster app-cluster --service-name dotnet-service --region ap-northeast-1 --query 'taskArns[0]' --output text)

PRIVATE_IP=$(aws ecs describe-tasks \
  --cluster app-cluster \
  --tasks $TASK_ARN \
  --region ap-northeast-1 \
  --query 'tasks[0].attachments[0].details[?name==`privateIPv4Address`].value' \
  --output text)

# 直接アクセス（VPC内から）
curl http://$PRIVATE_IP:8080/healthz
```

#### 3. セキュリティグループの確認

```bash
# セキュリティグループのインバウンドルール確認
aws ec2 describe-security-groups \
  --group-ids <security-group-id> \
  --region ap-northeast-1 \
  --query 'SecurityGroups[0].IpPermissions'
```

#### 4. タイムアウト設定の見直し

ヘルスチェックのタイムアウトがアプリケーションの起動時間より短い場合、startPeriodを延長します。

```json
{
  "healthCheck": {
    "startPeriod": 120  // 60秒 → 120秒に延長
  }
}
```

### よくある問題と対処法

| 問題 | 原因 | 対処法 |
|------|------|--------|
| ヘルスチェックが常に失敗 | エンドポイントが実装されていない | `/healthz` エンドポイントを実装 |
| 間欠的に失敗 | タイムアウト設定が短すぎる | timeout, startPeriodを延長 |
| データベース接続エラー | Supabase接続情報が不正 | Secrets Managerの設定確認 |
| 403/404エラー | パスが間違っている | HealthCheckPath設定を確認 |

## ベストプラクティス

### 1. ヘルスチェックは軽量に
- データベースへの複雑なクエリを避ける
- 外部APIへの呼び出しを最小限に
- キャッシュの活用

```csharp
// 悪い例: 重い処理
.AddCheck("heavy", () =>
{
    // 大量のデータをクエリ
    var count = dbContext.Users.Count();
    return HealthCheckResult.Healthy();
});

// 良い例: 軽量な処理
.AddCheck("light", () =>
{
    // 単純な接続確認のみ
    var canConnect = dbContext.Database.CanConnect();
    return canConnect
        ? HealthCheckResult.Healthy()
        : HealthCheckResult.Unhealthy();
});
```

### 2. タイムアウトの適切な設定
- ヘルスチェックは5秒以内に応答
- 起動時間を考慮してstartPeriodを設定

### 3. 段階的なヘルスチェック
- Liveness: 最も軽量（自己チェックのみ）
- Readiness: 中程度（依存関係の簡易チェック）
- Dependencies: 詳細（全ての依存関係）

### 4. キャッシュの活用
```csharp
private static DateTime _lastDatabaseCheck = DateTime.MinValue;
private static bool _lastDatabaseStatus = false;
private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30);

public Task<HealthCheckResult> CheckHealthAsync(...)
{
    if (DateTime.UtcNow - _lastDatabaseCheck < CacheExpiration)
    {
        return Task.FromResult(_lastDatabaseStatus
            ? HealthCheckResult.Healthy("Cached")
            : HealthCheckResult.Unhealthy("Cached"));
    }

    // 実際のチェック
    // ...
}
```

### 5. 監視とアラート
- ヘルスチェック失敗時のアラート設定
- ヘルスチェック応答時間の監視
- ヘルスチェック失敗率のトレンド分析

## 関連ドキュメント

- [監視概要](monitoring-overview.md)
- [CloudWatch Logs](cloudwatch-logs.md)
- [メトリクス監視](metrics.md)
- [アラート設定](alerts.md)
- [トラブルシューティング](../troubleshooting/common-issues.md)

---

**最終更新日**: 2025-12-17
