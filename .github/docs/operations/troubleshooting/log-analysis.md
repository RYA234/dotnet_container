# ログ解析ガイド

## 概要
ログを効果的に分析して問題を特定する方法を説明します。

## ログ解析の基本フロー

```
1. 問題の発生時刻を特定
    ↓
2. 該当時刻のログを抽出
    ↓
3. エラーログを検索
    ↓
4. エラー前後のコンテキストを確認
    ↓
5. パターンを特定
    ↓
6. 根本原因を特定
```

## よくあるエラーパターン

### パターン1: データベース接続エラー

#### ログ例
```
2025-12-17 15:30:45 ERROR Database connection failed: timeout after 30000ms
```

#### 分析手順
```bash
# エラーの頻度を確認
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "Database connection failed" \
  --start-time $(date -d '1 hour ago' +%s)000 \
  --region ap-northeast-1 \
  | jq '.events | length'

# 同じ時刻に発生した他のエラー
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --start-time $(date -d '15:25' +%s)000 \
  --end-time $(date -d '15:35' +%s)000 \
  --region ap-northeast-1
```

#### 根本原因の候補
1. データベースがダウン
2. ネットワーク問題
3. 接続数の上限
4. 認証情報の問題

---

### パターン2: メモリ不足 (OOM)

#### ログ例
```
System.OutOfMemoryException: Exception of type 'System.OutOfMemoryException' was thrown.
```

または、ECSタスクログ:
```
Task stopped: OutOfMemoryError: Container killed due to memory usage
```

#### 分析手順
```bash
# メモリ使用量の推移を確認
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name MemoryUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '2 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average,Maximum \
  --region ap-northeast-1

# OOMが発生した前後のログ
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "OutOfMemory" \
  --region ap-northeast-1
```

#### 根本原因の候補
1. メモリリーク
2. 大きなオブジェクトの処理
3. キャッシュの肥大化
4. タスク定義のメモリ設定が小さすぎる

---

### パターン3: タイムアウトエラー

#### ログ例
```
System.TimeoutException: The operation has timed out
```

#### 分析手順
```bash
# タイムアウトの頻度
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "TimeoutException" \
  --region ap-northeast-1 \
  | jq '.events | length'

# レスポンスタイムの分析
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "{ $.duration > 10000 }" \
  --region ap-northeast-1 \
  --query 'events[*].message' \
  --output text
```

#### 根本原因の候補
1. 外部APIの遅延
2. データベースクエリが遅い
3. ネットワーク遅延
4. リソース不足（CPU/メモリ）

---

## Logs Insights クエリ集

### エラーレートの時系列分析

```sql
fields @timestamp, level
| filter level = "ERROR"
| stats count() as error_count by bin(5m)
| sort @timestamp desc
```

### エンドポイント別のエラー集計

```sql
fields @timestamp, path, statusCode
| filter statusCode >= 500
| stats count() as error_count by path
| sort error_count desc
```

### レスポンスタイムの統計

```sql
fields @timestamp, path, duration
| filter duration > 0
| stats avg(duration) as avg_duration,
        max(duration) as max_duration,
        min(duration) as min_duration,
        count() as request_count
        by path
| sort avg_duration desc
```

### ユーザー別のエラー分析

```sql
fields @timestamp, userId, message
| filter level = "ERROR"
| stats count() as error_count by userId
| sort error_count desc
| limit 10
```

### 例外の種類別集計

```sql
fields @timestamp, exception
| filter exception like /Exception/
| parse exception /(?<exception_type>\\w+Exception)/
| stats count() as count by exception_type
| sort count desc
```

### 特定時間帯のスロークエリ

```sql
fields @timestamp, message, duration
| filter message like /query/ and duration > 1000
| sort duration desc
| limit 20
```

---

## 相関分析

### 複数のログソースを横断して分析

```bash
# 1. アプリケーションログでエラーを特定
ERROR_TIME=$(aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --max-items 1 \
  --region ap-northeast-1 \
  --query 'events[0].timestamp' \
  --output text)

# 2. 同じ時刻のALBログを確認
# ALBログをS3から取得して分析
aws s3 sync s3://alb-logs-bucket/ ./alb-logs/ --exclude "*" --include "*$(date -d @$((ERROR_TIME/1000)) +%Y%m%d)*"

# 3. 同じ時刻のメトリクスを確認
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster \
  --start-time $(date -u -d @$((ERROR_TIME/1000-300)) +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u -d @$((ERROR_TIME/1000+300)) +%Y-%m-%dT%H:%M:%S) \
  --period 60 \
  --statistics Average \
  --region ap-northeast-1
```

---

## 自動化スクリプト

### エラーサマリーレポート

```bash
#!/bin/bash
# error-summary.sh

HOURS=${1:-1}
START_TIME=$(date -d "$HOURS hours ago" +%s)000

echo "=== Error Summary Report ==="
echo "Time Range: Last $HOURS hour(s)"
echo ""

# エラー件数
ERROR_COUNT=$(aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --start-time $START_TIME \
  --region ap-northeast-1 \
  | jq '.events | length')

echo "Total Errors: $ERROR_COUNT"
echo ""

# エラーの種類別集計
echo "Errors by Type:"
aws logs start-query \
  --log-group-name /ecs/dotnet-app \
  --start-time $((START_TIME/1000)) \
  --end-time $(date +%s) \
  --query-string 'fields @timestamp, exception | filter level = "ERROR" | stats count() by exception' \
  --region ap-northeast-1

echo ""

# 最新のエラー5件
echo "Latest 5 Errors:"
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --start-time $START_TIME \
  --max-items 5 \
  --region ap-northeast-1 \
  --query 'events[*].message' \
  --output table
```

### パフォーマンス分析レポート

```bash
#!/bin/bash
# performance-analysis.sh

HOURS=${1:-24}

echo "=== Performance Analysis Report ==="
echo "Time Range: Last $HOURS hour(s)"
echo ""

# Logs Insights クエリを実行
QUERY_ID=$(aws logs start-query \
  --log-group-name /ecs/dotnet-app \
  --start-time $(date -d "$HOURS hours ago" +%s) \
  --end-time $(date +%s) \
  --query-string 'fields @timestamp, path, duration | filter duration > 0 | stats avg(duration) as avg, max(duration) as max, count() as count by path | sort avg desc' \
  --region ap-northeast-1 \
  --query 'queryId' \
  --output text)

# 結果を待機
sleep 5

# 結果を取得
aws logs get-query-results \
  --query-id $QUERY_ID \
  --region ap-northeast-1
```

---

## ログパターン認識

### 異常パターンの検出

```bash
# 1時間ごとのエラー件数を取得
for hour in {0..23}; do
  START=$(($(date +%s) - 3600 * ($hour + 1)))000
  END=$(($(date +%s) - 3600 * $hour))000

  COUNT=$(aws logs filter-log-events \
    --log-group-name /ecs/dotnet-app \
    --filter-pattern "ERROR" \
    --start-time $START \
    --end-time $END \
    --region ap-northeast-1 \
    | jq '.events | length')

  echo "Hour -$hour: $COUNT errors"
done

# 平均と標準偏差を計算し、異常値を検出
```

---

## ベストプラクティス

### 1. 構造化ログの活用
```csharp
// 良い例: JSON形式の構造化ログ
_logger.LogInformation(
    "Request processed: {Method} {Path} {StatusCode} {Duration}ms",
    request.Method,
    request.Path,
    response.StatusCode,
    duration);

// 悪い例: 文字列のみ
_logger.LogInformation($"Request processed: {request.Method} {request.Path}");
```

### 2. コンテキスト情報の追加
```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["TraceId"] = traceId,
    ["UserId"] = userId,
    ["SessionId"] = sessionId
}))
{
    _logger.LogInformation("Processing request");
}
```

### 3. ログレベルの適切な使用
- **TRACE**: 詳細なデバッグ情報（本番では無効）
- **DEBUG**: デバッグ情報（本番では無効）
- **INFO**: 通常の動作情報
- **WARN**: 警告（動作は継続）
- **ERROR**: エラー（一部機能停止）
- **FATAL**: 致命的エラー（アプリ停止）

### 4. 機密情報のマスキング
```csharp
// 悪い例
_logger.LogInformation($"User login: {email}, Password: {password}");

// 良い例
_logger.LogInformation($"User login: {email.Mask()}");
```

---

## 関連ドキュメント

- [CloudWatch Logs運用](../monitoring/cloudwatch-logs.md)
- [よくある問題](common-issues.md)
- [パフォーマンスチューニング](performance-tuning.md)

---

**最終更新日**: 2025-12-17
