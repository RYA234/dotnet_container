# CloudWatch Logs 運用ガイド

## 概要
AWS CloudWatch Logsを使用したログ管理、検索、分析の詳細手順を説明します。

## ログ構成

### ロググループ
```
/ecs/dotnet-app          # .NETアプリケーションログ
/ecs/nodejs-app          # Node.jsアプリケーションログ (参考)
/aws/ecs/containerinsights/app-cluster/performance  # Container Insights
```

### ログストリーム
各ECSタスクごとに個別のログストリームが作成されます。

命名規則: `ecs/dotnet-app/<task-id>`

例: `ecs/dotnet-app/12345678-1234-1234-1234-123456789abc`

## ログ出力形式

### 推奨ログフォーマット（JSON構造化ログ）

```json
{
  "timestamp": "2025-12-17T15:30:45.123Z",
  "level": "INFO",
  "logger": "DotNetApp.Controllers.HomeController",
  "message": "Request received",
  "traceId": "abc123",
  "userId": "user_001",
  "method": "GET",
  "path": "/api/data",
  "duration": 125,
  "statusCode": 200
}
```

### ASP.NET Core でのログ設定

```csharp
// Program.cs
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.JsonWriterOptions = new JsonWriterOptions
    {
        Indented = false
    };
});
```

### ログレベル

| レベル | 使用場面 | 例 |
|-------|---------|-----|
| TRACE | 詳細なデバッグ情報 | 関数の入出力 |
| DEBUG | デバッグ情報 | 変数の値、実行フロー |
| INFO | 通常の動作情報 | リクエスト処理、初期化完了 |
| WARN | 警告（動作は継続） | 非推奨API使用、リトライ |
| ERROR | エラー（一部機能停止） | 例外発生、処理失敗 |
| FATAL | 致命的エラー（アプリ停止） | 起動失敗、致命的な設定エラー |

## ログ確認方法

### AWS CLI

#### 最新ログのリアルタイム表示

```bash
# 最新ログをリアルタイムで表示（tail -f 相当）
aws logs tail /ecs/dotnet-app \
  --follow \
  --region ap-northeast-1 \
  --format short

# 最新10分間のログを表示
aws logs tail /ecs/dotnet-app \
  --since 10m \
  --region ap-northeast-1 \
  --format short

# 特定の時刻以降のログを表示
aws logs tail /ecs/dotnet-app \
  --since "2025-12-17T15:00:00" \
  --region ap-northeast-1
```

#### ログのフィルタリング

```bash
# エラーログのみ表示
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --region ap-northeast-1 \
  --start-time $(date -d '1 hour ago' +%s)000

# 複数キーワードでフィルタ（AND条件）
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "[time, level=ERROR, logger, message=*database*]" \
  --region ap-northeast-1

# 特定のHTTPステータスコードでフィルタ
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "{ $.statusCode = 500 }" \
  --region ap-northeast-1

# 特定ユーザーのログを抽出
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "{ $.userId = \"user_001\" }" \
  --region ap-northeast-1
```

#### ログのエクスポート

```bash
# ログをファイルに保存
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --start-time $(date -d '1 day ago' +%s)000 \
  --end-time $(date +%s)000 \
  --region ap-northeast-1 > logs-export-$(date +%Y%m%d).json

# S3へのエクスポート（大量ログの場合）
aws logs create-export-task \
  --log-group-name /ecs/dotnet-app \
  --from $(date -d '7 days ago' +%s)000 \
  --to $(date +%s)000 \
  --destination my-log-bucket \
  --destination-prefix ecs-logs/ \
  --region ap-northeast-1
```

### AWS Management Console

1. CloudWatch Console を開く
2. 左メニューから「ログ」→「ロググループ」を選択
3. `/ecs/dotnet-app` をクリック
4. ログストリームを選択
5. フィルタ機能で検索

## ログ検索パターン

### フィルタパターン構文

#### シンプルなテキスト検索
```
ERROR                    # "ERROR"を含む行
"connection timeout"     # 完全一致
ERROR WARN               # "ERROR" OR "WARN"
```

#### フィールドベース検索（スペース区切り）
```
[time, request_id, level, message]

# レベルがERRORの行
[time, request_id, level=ERROR, message]

# メッセージに"database"を含む行
[time, request_id, level, message=*database*]
```

#### JSON検索
```
# statusCodeが500の行
{ $.statusCode = 500 }

# statusCodeが500以上の行
{ $.statusCode >= 500 }

# 特定のloggerを含む行
{ $.logger = "DotNetApp.Controllers.*" }

# 複数条件（AND）
{ ($.level = "ERROR") && ($.statusCode >= 500) }

# 複数条件（OR）
{ ($.statusCode = 500) || ($.statusCode = 503) }
```

### よく使う検索パターン集

#### エラー調査
```bash
# すべてのエラーログ
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "{ $.level = \"ERROR\" }" \
  --region ap-northeast-1

# 例外スタックトレース
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "Exception" \
  --region ap-northeast-1
```

#### パフォーマンス調査
```bash
# レスポンスタイムが2秒以上
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "{ $.duration >= 2000 }" \
  --region ap-northeast-1

# 遅いデータベースクエリ
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "{ ($.message = \"*query*\") && ($.duration >= 1000) }" \
  --region ap-northeast-1
```

#### 特定リクエストの追跡
```bash
# トレースIDで関連ログを抽出
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "{ $.traceId = \"abc123\" }" \
  --region ap-northeast-1
```

## Logs Insights クエリ

より高度な分析には CloudWatch Logs Insights を使用します。

### Logs Insights 基本クエリ

#### エラーログの集計
```sql
fields @timestamp, level, message
| filter level = "ERROR"
| sort @timestamp desc
| limit 100
```

#### HTTPステータスコード別集計
```sql
fields @timestamp, statusCode
| stats count() by statusCode
| sort statusCode
```

#### レスポンスタイムの統計
```sql
fields @timestamp, duration
| filter duration > 0
| stats avg(duration), max(duration), min(duration), count()
```

#### エンドポイント別エラー率
```sql
fields @timestamp, path, statusCode
| stats count() as total,
        count(statusCode >= 500) as errors
        by path
| eval error_rate = errors / total * 100
| sort error_rate desc
```

#### 時系列でのエラー件数
```sql
fields @timestamp, level
| filter level = "ERROR"
| stats count() by bin(5m)
```

### AWS CLI から Logs Insights を実行

```bash
# クエリの実行
QUERY_ID=$(aws logs start-query \
  --log-group-name /ecs/dotnet-app \
  --start-time $(date -d '1 hour ago' +%s) \
  --end-time $(date +%s) \
  --query-string 'fields @timestamp, level, message | filter level = "ERROR" | sort @timestamp desc | limit 100' \
  --region ap-northeast-1 \
  --query 'queryId' \
  --output text)

# クエリ結果の取得
aws logs get-query-results \
  --query-id $QUERY_ID \
  --region ap-northeast-1
```

## ログ保持とコスト管理

### ログ保持期間の設定

```bash
# 保持期間を7日に設定
aws logs put-retention-policy \
  --log-group-name /ecs/dotnet-app \
  --retention-in-days 7 \
  --region ap-northeast-1

# 保持期間の選択肢
# 1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1827, 3653 日
# または無期限（設定しない）

# 現在の設定確認
aws logs describe-log-groups \
  --log-group-name-prefix /ecs/dotnet-app \
  --region ap-northeast-1 \
  --query 'logGroups[*].[logGroupName,retentionInDays]' \
  --output table
```

### 古いログストリームの削除

```bash
# 30日以上前のログストリームを一覧表示
aws logs describe-log-streams \
  --log-group-name /ecs/dotnet-app \
  --order-by LastEventTime \
  --region ap-northeast-1 \
  --query "logStreams[?lastEventTimestamp<\`$(date -d '30 days ago' +%s)000\`].[logStreamName]" \
  --output text

# 古いログストリームを削除
aws logs delete-log-stream \
  --log-group-name /ecs/dotnet-app \
  --log-stream-name <log-stream-name> \
  --region ap-northeast-1
```

### コスト最適化のヒント

1. **適切な保持期間**: 本番環境は7-30日、開発環境は1-3日
2. **サンプリング**: デバッグログは本番では無効化
3. **S3アーカイブ**: 長期保存が必要な場合はS3へエクスポート
4. **ログレベル**: 本番環境ではINFO以上に設定

## メトリクスフィルタ

ログから自動的にメトリクスを生成します。

### エラーカウントメトリクスの作成

```bash
# エラーログをカウントするメトリクスフィルタを作成
aws logs put-metric-filter \
  --log-group-name /ecs/dotnet-app \
  --filter-name ErrorCount \
  --filter-pattern "{ $.level = \"ERROR\" }" \
  --metric-transformations \
    metricName=ErrorCount,metricNamespace=DotNetApp/Logs,metricValue=1,defaultValue=0 \
  --region ap-northeast-1

# HTTP 5xxエラーをカウント
aws logs put-metric-filter \
  --log-group-name /ecs/dotnet-app \
  --filter-name Http5xxCount \
  --filter-pattern "{ $.statusCode >= 500 }" \
  --metric-transformations \
    metricName=Http5xxCount,metricNamespace=DotNetApp/Logs,metricValue=1,defaultValue=0 \
  --region ap-northeast-1

# レスポンスタイムの平均
aws logs put-metric-filter \
  --log-group-name /ecs/dotnet-app \
  --filter-name AverageResponseTime \
  --filter-pattern "{ $.duration > 0 }" \
  --metric-transformations \
    metricName=ResponseTime,metricNamespace=DotNetApp/Logs,metricValue=$.duration,unit=Milliseconds \
  --region ap-northeast-1
```

### メトリクスフィルタの確認

```bash
# 設定済みメトリクスフィルタの一覧
aws logs describe-metric-filters \
  --log-group-name /ecs/dotnet-app \
  --region ap-northeast-1

# メトリクスフィルタの削除
aws logs delete-metric-filter \
  --log-group-name /ecs/dotnet-app \
  --filter-name ErrorCount \
  --region ap-northeast-1
```

## トラブルシューティング

### ログが出力されない場合

#### 1. ECSタスク定義の確認
```bash
# ログ設定を確認
aws ecs describe-task-definition \
  --task-definition dotnet-task \
  --region ap-northeast-1 \
  --query 'taskDefinition.containerDefinitions[0].logConfiguration'
```

期待される出力:
```json
{
    "logDriver": "awslogs",
    "options": {
        "awslogs-group": "/ecs/dotnet-app",
        "awslogs-region": "ap-northeast-1",
        "awslogs-stream-prefix": "ecs"
    }
}
```

#### 2. IAMロールの確認
```bash
# タスク実行ロールに必要な権限があるか確認
aws iam get-role-policy \
  --role-name ecsTaskExecutionRole \
  --policy-name CloudWatchLogsPolicy \
  --region ap-northeast-1
```

必要な権限:
```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "logs:CreateLogGroup",
                "logs:CreateLogStream",
                "logs:PutLogEvents"
            ],
            "Resource": "*"
        }
    ]
}
```

#### 3. ログドライバーの確認
アプリケーションが標準出力(stdout)にログを出力しているか確認。

```csharp
// Program.cs
// コンソールにログ出力
builder.Logging.AddConsole();
```

### ログが文字化けする場合

UTF-8エンコーディングを確認:

```csharp
// Program.cs
Console.OutputEncoding = System.Text.Encoding.UTF8;
```

## ログ分析のベストプラクティス

### 1. 構造化ログの活用
- JSON形式でログを出力
- 検索しやすいフィールド名を使用
- トレースIDで関連ログを紐付け

### 2. 適切なログレベル
- 本番環境: INFO以上
- 開発環境: DEBUG以上
- トラブル時のみ: TRACE

### 3. 機密情報の除外
- パスワード、APIキーをログに出力しない
- 個人情報（PII）のマスキング

```csharp
// 悪い例
_logger.LogInformation($"User logged in: {user.Email}, Password: {password}");

// 良い例
_logger.LogInformation($"User logged in: {user.Email.Mask()}");
```

### 4. ログの集約と相関
- トレースIDを全てのログに付与
- リクエスト単位でログをグループ化

### 5. 定期的なレビュー
- 週次: エラーログのレビュー
- 月次: ログパターンの見直し
- 不要なログの削減

## 関連ドキュメント

- [監視概要](monitoring-overview.md)
- [メトリクス監視](metrics.md)
- [アラート設定](alerts.md)
- [トラブルシューティング](../troubleshooting/log-analysis.md)

---

**最終更新日**: 2025-12-17
