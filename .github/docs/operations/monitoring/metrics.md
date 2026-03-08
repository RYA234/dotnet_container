# メトリクス監視

## 概要
CloudWatch Metricsを使用したパフォーマンス監視とメトリクス分析の方法を説明します。

## 標準メトリクス（追加料金なし）

### ECS メトリクス

| メトリクス名 | 説明 | 単位 | 推奨閾値 |
|------------|------|------|---------|
| CPUUtilization | CPU使用率 | Percent | < 80% |
| MemoryUtilization | メモリ使用率 | Percent | < 80% |
| NetworkRxBytes | 受信バイト数 | Bytes | 監視のみ |
| NetworkTxBytes | 送信バイト数 | Bytes | 監視のみ |

### ALB メトリクス

| メトリクス名 | 説明 | 単位 | 推奨閾値 |
|------------|------|------|---------|
| RequestCount | リクエスト数 | Count | 監視のみ |
| TargetResponseTime | レスポンスタイム | Seconds | < 2秒 |
| HTTPCode_Target_2XX_Count | 成功レスポンス | Count | 監視のみ |
| HTTPCode_Target_4XX_Count | クライアントエラー | Count | < 5% |
| HTTPCode_Target_5XX_Count | サーバーエラー | Count | < 1% |
| HealthyHostCount | 正常なターゲット数 | Count | >= 1 |
| UnHealthyHostCount | 異常なターゲット数 | Count | = 0 |

### RDS/Supabase メトリクス

| メトリクス名 | 説明 | 単位 | 推奨閾値 |
|------------|------|------|---------|
| DatabaseConnections | データベース接続数 | Count | 監視のみ |
| CPUUtilization | CPU使用率 | Percent | < 80% |
| FreeableMemory | 空きメモリ | Bytes | 監視のみ |
| ReadLatency | 読み取りレイテンシ | Seconds | < 0.1秒 |
| WriteLatency | 書き込みレイテンシ | Seconds | < 0.1秒 |

## メトリクスの確認

### AWS CLI

#### ECS メトリクスの取得

```bash
# CPU使用率（過去1時間）
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average,Maximum \
  --region ap-northeast-1

# メモリ使用率
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name MemoryUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average,Maximum \
  --region ap-northeast-1
```

#### ALB メトリクスの取得

```bash
# リクエスト数
aws cloudwatch get-metric-statistics \
  --namespace AWS/ApplicationELB \
  --metric-name RequestCount \
  --dimensions Name=LoadBalancer,Value=<load-balancer-full-name> \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum \
  --region ap-northeast-1

# レスポンスタイム
aws cloudwatch get-metric-statistics \
  --namespace AWS/ApplicationELB \
  --metric-name TargetResponseTime \
  --dimensions Name=LoadBalancer,Value=<load-balancer-full-name> \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average,Maximum \
  --region ap-northeast-1

# エラー率
aws cloudwatch get-metric-statistics \
  --namespace AWS/ApplicationELB \
  --metric-name HTTPCode_Target_5XX_Count \
  --dimensions Name=LoadBalancer,Value=<load-balancer-full-name> \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum \
  --region ap-northeast-1
```

### AWS Management Console

1. CloudWatch Console を開く
2. 左メニューから「メトリクス」→「すべてのメトリクス」を選択
3. 名前空間を選択（AWS/ECS, AWS/ApplicationELB等）
4. メトリクスを選択してグラフ化

## カスタムメトリクス

### CloudWatch エージェントの使用

#### メトリクス送信の実装（C#）

```csharp
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

public class CloudWatchMetricsService
{
    private readonly IAmazonCloudWatch _cloudWatch;
    private readonly string _namespace = "DotNetApp/Custom";

    public CloudWatchMetricsService(IAmazonCloudWatch cloudWatch)
    {
        _cloudWatch = cloudWatch;
    }

    public async Task PutMetricAsync(
        string metricName,
        double value,
        StandardUnit unit = StandardUnit.None,
        Dictionary<string, string> dimensions = null)
    {
        var metricDatum = new MetricDatum
        {
            MetricName = metricName,
            Value = value,
            Timestamp = DateTime.UtcNow,
            Unit = unit
        };

        if (dimensions != null)
        {
            metricDatum.Dimensions = dimensions
                .Select(d => new Dimension { Name = d.Key, Value = d.Value })
                .ToList();
        }

        var request = new PutMetricDataRequest
        {
            Namespace = _namespace,
            MetricData = new List<MetricDatum> { metricDatum }
        };

        await _cloudWatch.PutMetricDataAsync(request);
    }

    public async Task PutCounterAsync(string metricName, int count = 1)
    {
        await PutMetricAsync(metricName, count, StandardUnit.Count);
    }

    public async Task PutTimerAsync(string metricName, TimeSpan duration)
    {
        await PutMetricAsync(
            metricName,
            duration.TotalMilliseconds,
            StandardUnit.Milliseconds);
    }
}
```

#### 使用例

```csharp
// Startup.cs / Program.cs
services.AddSingleton<IAmazonCloudWatch, AmazonCloudWatchClient>();
services.AddSingleton<CloudWatchMetricsService>();

// Controller
public class OrderController : ControllerBase
{
    private readonly CloudWatchMetricsService _metrics;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(OrderRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // ビジネスロジック
            var order = await _orderService.CreateAsync(request);

            // カスタムメトリクス送信
            await _metrics.PutCounterAsync("OrderCreated");
            await _metrics.PutTimerAsync("OrderCreationTime", stopwatch.Elapsed);

            return Ok(order);
        }
        catch (Exception ex)
        {
            await _metrics.PutCounterAsync("OrderCreationFailed");
            throw;
        }
    }
}
```

### ミドルウェアでのメトリクス自動収集

```csharp
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CloudWatchMetricsService _metrics;

    public MetricsMiddleware(
        RequestDelegate next,
        CloudWatchMetricsService metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);

            stopwatch.Stop();

            // メトリクス送信
            await _metrics.PutTimerAsync(
                "RequestDuration",
                stopwatch.Elapsed);

            await _metrics.PutMetricAsync(
                "StatusCode",
                context.Response.StatusCode,
                StandardUnit.None,
                new Dictionary<string, string>
                {
                    { "Path", context.Request.Path },
                    { "Method", context.Request.Method }
                });
        }
        catch (Exception)
        {
            await _metrics.PutCounterAsync("RequestFailed");
            throw;
        }
    }
}

// Program.cs
app.UseMiddleware<MetricsMiddleware>();
```

## メトリクスダッシュボード

### ダッシュボード定義（JSON）

```json
{
  "widgets": [
    {
      "type": "metric",
      "properties": {
        "metrics": [
          ["AWS/ECS", "CPUUtilization", {"stat": "Average"}],
          [".", "MemoryUtilization", {"stat": "Average"}]
        ],
        "period": 300,
        "stat": "Average",
        "region": "ap-northeast-1",
        "title": "ECS Resource Utilization",
        "yAxis": {
          "left": {
            "min": 0,
            "max": 100
          }
        }
      }
    },
    {
      "type": "metric",
      "properties": {
        "metrics": [
          ["AWS/ApplicationELB", "TargetResponseTime", {"stat": "Average"}]
        ],
        "period": 300,
        "stat": "Average",
        "region": "ap-northeast-1",
        "title": "Response Time",
        "yAxis": {
          "left": {
            "min": 0
          }
        }
      }
    },
    {
      "type": "metric",
      "properties": {
        "metrics": [
          ["AWS/ApplicationELB", "HTTPCode_Target_2XX_Count", {"stat": "Sum", "label": "2XX"}],
          [".", "HTTPCode_Target_4XX_Count", {"stat": "Sum", "label": "4XX"}],
          [".", "HTTPCode_Target_5XX_Count", {"stat": "Sum", "label": "5XX"}]
        ],
        "period": 300,
        "stat": "Sum",
        "region": "ap-northeast-1",
        "title": "HTTP Status Codes"
      }
    }
  ]
}
```

### ダッシュボードの作成

```bash
# ダッシュボードを作成
aws cloudwatch put-dashboard \
  --dashboard-name dotnet-app-dashboard \
  --dashboard-body file://dashboard.json \
  --region ap-northeast-1

# ダッシュボードの確認
aws cloudwatch get-dashboard \
  --dashboard-name dotnet-app-dashboard \
  --region ap-northeast-1

# ダッシュボードの削除
aws cloudwatch delete-dashboards \
  --dashboard-names dotnet-app-dashboard \
  --region ap-northeast-1
```

### ダッシュボードURL
```
https://console.aws.amazon.com/cloudwatch/home?region=ap-northeast-1#dashboards:name=dotnet-app-dashboard
```

## Container Insights

### Container Insights の有効化

```bash
# クラスタ設定を確認
aws ecs describe-clusters \
  --clusters app-cluster \
  --region ap-northeast-1 \
  --query 'clusters[0].settings'

# Container Insights を有効化
aws ecs update-cluster-settings \
  --cluster app-cluster \
  --settings name=containerInsights,value=enabled \
  --region ap-northeast-1
```

### Container Insights メトリクス

Container Insightsを有効化すると、以下の追加メトリクスが利用可能になります：

- タスクレベルのCPU/メモリ使用率
- コンテナレベルのリソース使用率
- ネットワークトラフィック詳細
- ディスクI/O

**注意**: Container Insightsは追加料金が発生します。

### Container Insights の無効化（コスト削減）

```bash
aws ecs update-cluster-settings \
  --cluster app-cluster \
  --settings name=containerInsights,value=disabled \
  --region ap-northeast-1

# 関連ロググループの削除
aws logs delete-log-group \
  --log-group-name /aws/ecs/containerinsights/app-cluster/performance \
  --region ap-northeast-1
```

## メトリクスの分析

### AWS CLI でのクエリ

```bash
# 過去24時間の平均CPU使用率
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 3600 \
  --statistics Average \
  --region ap-northeast-1 \
  --query 'Datapoints[*].[Timestamp,Average]' \
  --output table

# ピーク時のメモリ使用率
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name MemoryUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '7 days ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 86400 \
  --statistics Maximum \
  --region ap-northeast-1
```

### スクリプトでの自動分析

```bash
#!/bin/bash
# metrics-report.sh

CLUSTER="app-cluster"
SERVICE="dotnet-service"
REGION="ap-northeast-1"

echo "=== ECS Metrics Report ==="
echo "Cluster: $CLUSTER"
echo "Service: $SERVICE"
echo "Date: $(date)"
echo ""

# CPU使用率
CPU_AVG=$(aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=$CLUSTER Name=ServiceName,Value=$SERVICE \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 86400 \
  --statistics Average \
  --region $REGION \
  --query 'Datapoints[0].Average' \
  --output text)

echo "CPU Utilization (24h avg): $CPU_AVG%"

# メモリ使用率
MEM_AVG=$(aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name MemoryUtilization \
  --dimensions Name=ClusterName,Value=$CLUSTER Name=ServiceName,Value=$SERVICE \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 86400 \
  --statistics Average \
  --region $REGION \
  --query 'Datapoints[0].Average' \
  --output text)

echo "Memory Utilization (24h avg): $MEM_AVG%"
```

## パフォーマンスチューニング

### リソース使用率の最適化

#### CPU使用率が高い場合
```bash
# タスク定義でCPUを増やす
{
  "cpu": "512",  // 256 → 512 に増加
  "memory": "1024"
}

# または、タスク数を増やす（水平スケーリング）
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --desired-count 2 \
  --region ap-northeast-1
```

#### メモリ使用率が高い場合
```bash
# タスク定義でメモリを増やす
{
  "cpu": "256",
  "memory": "1024"  // 512 → 1024 に増加
}
```

### レスポンスタイムの改善

1. **データベースクエリの最適化**
2. **キャッシュの導入**
3. **非同期処理の活用**
4. **接続プーリングの設定**

## ベストプラクティス

### 1. 重要メトリクスの定義
- ビジネスに影響するメトリクスを特定
- SLI（Service Level Indicator）を設定
- SLO（Service Level Objective）を定義

### 2. メトリクスの粒度
- 標準メトリクス: 5分間隔
- カスタムメトリクス: 必要に応じて1分間隔
- 長期保存: 1時間または1日間隔に集約

### 3. コスト最適化
- 標準メトリクスを優先使用
- カスタムメトリクスは必要最小限に
- Container Insightsは必要に応じて有効化

### 4. トレンド分析
- 週次: メトリクスのトレンド確認
- 月次: キャパシティプランニング
- 異常値の検出とアラート設定

## 関連ドキュメント

- [監視概要](monitoring-overview.md)
- [CloudWatch Logs](cloudwatch-logs.md)
- [ヘルスチェック](health-checks.md)
- [アラート設定](alerts.md)

---

**最終更新日**: 2025-12-17
