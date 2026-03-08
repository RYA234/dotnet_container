# アラート設定

## 概要
CloudWatch Alarmsを使用したアラート設定と通知の方法を説明します。

## アラート設計の原則

### 1. アラートのレベル分け

| レベル | 対応時間 | 通知方法 | 例 |
|-------|---------|---------|-----|
| Critical | 即座（5分以内） | メール + Slack + 電話 | サービスダウン |
| High | 30分以内 | メール + Slack | エラー率上昇 |
| Medium | 営業時間内 | Slack | リソース使用率高 |
| Low | 記録のみ | ログのみ | 軽微な警告 |

### 2. アラート疲れを防ぐ
- 適切な閾値設定
- 評価期間の調整
- ノイズの多いアラートは無効化または調整

### 3. アクション可能なアラート
- 対応方法が明確
- ドキュメントへのリンク
- ロールバック手順の明記

## 主要アラート設定

### 1. サービス可用性

#### ECSタスク数アラート

```bash
# タスクが0になったら即座にアラート
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-no-running-tasks \
  --alarm-description "Alert when no tasks are running" \
  --metric-name RunningCount \
  --namespace ECS/ContainerInsights \
  --statistic Average \
  --period 60 \
  --evaluation-periods 1 \
  --threshold 1 \
  --comparison-operator LessThanThreshold \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --alarm-actions arn:aws:sns:ap-northeast-1:110221759530:critical-alerts \
  --region ap-northeast-1
```

#### ALB Unhealthy Target アラート

```bash
# ターゲットが異常状態になったらアラート
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
  --alarm-actions arn:aws:sns:ap-northeast-1:110221759530:high-alerts \
  --region ap-northeast-1
```

### 2. パフォーマンス

#### CPU使用率アラート

```bash
# CPU使用率が80%を超えたらアラート
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-high-cpu \
  --alarm-description "Alert when CPU utilization is high" \
  --metric-name CPUUtilization \
  --namespace AWS/ECS \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --alarm-actions arn:aws:sns:ap-northeast-1:110221759530:medium-alerts \
  --region ap-northeast-1
```

#### メモリ使用率アラート

```bash
# メモリ使用率が80%を超えたらアラート
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-high-memory \
  --alarm-description "Alert when memory utilization is high" \
  --metric-name MemoryUtilization \
  --namespace AWS/ECS \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --alarm-actions arn:aws:sns:ap-northeast-1:110221759530:medium-alerts \
  --region ap-northeast-1
```

#### レスポンスタイムアラート

```bash
# レスポンスタイムが2秒を超えたらアラート
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-slow-response \
  --alarm-description "Alert when response time is slow" \
  --metric-name TargetResponseTime \
  --namespace AWS/ApplicationELB \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --threshold 2.0 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=LoadBalancer,Value=<load-balancer-full-name> \
  --alarm-actions arn:aws:sns:ap-northeast-1:110221759530:high-alerts \
  --region ap-northeast-1
```

### 3. エラー監視

#### HTTP 5xxエラーアラート

```bash
# 5xxエラーが発生したらアラート
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-5xx-errors \
  --alarm-description "Alert on HTTP 5xx errors" \
  --metric-name HTTPCode_Target_5XX_Count \
  --namespace AWS/ApplicationELB \
  --statistic Sum \
  --period 300 \
  --evaluation-periods 1 \
  --threshold 10 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=LoadBalancer,Value=<load-balancer-full-name> \
  --alarm-actions arn:aws:sns:ap-northeast-1:110221759530:high-alerts \
  --treat-missing-data notBreaching \
  --region ap-northeast-1
```

#### エラーログアラート（メトリクスフィルタから）

```bash
# まずメトリクスフィルタを作成（前提）
aws logs put-metric-filter \
  --log-group-name /ecs/dotnet-app \
  --filter-name ErrorCount \
  --filter-pattern "{ $.level = \"ERROR\" }" \
  --metric-transformations \
    metricName=ErrorCount,metricNamespace=DotNetApp/Logs,metricValue=1,defaultValue=0 \
  --region ap-northeast-1

# エラーログが5分間に5件以上でアラート
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-error-logs \
  --alarm-description "Alert on error logs" \
  --metric-name ErrorCount \
  --namespace DotNetApp/Logs \
  --statistic Sum \
  --period 300 \
  --evaluation-periods 1 \
  --threshold 5 \
  --comparison-operator GreaterThanThreshold \
  --alarm-actions arn:aws:sns:ap-northeast-1:110221759530:high-alerts \
  --treat-missing-data notBreaching \
  --region ap-northeast-1
```

## SNS トピックの設定

### SNS トピックの作成

```bash
# Critical アラート用トピック
aws sns create-topic \
  --name critical-alerts \
  --region ap-northeast-1

# High アラート用トピック
aws sns create-topic \
  --name high-alerts \
  --region ap-northeast-1

# Medium アラート用トピック
aws sns create-topic \
  --name medium-alerts \
  --region ap-northeast-1
```

### メール購読の追加

```bash
# メールアドレスを購読
aws sns subscribe \
  --topic-arn arn:aws:sns:ap-northeast-1:110221759530:critical-alerts \
  --protocol email \
  --notification-endpoint your-email@example.com \
  --region ap-northeast-1

# 確認メールが送信されるので、リンクをクリックして承認
```

### Slack 通知の設定

#### Lambda関数の作成（Slack通知用）

```python
# slack_notifier.py
import json
import urllib3
import os

http = urllib3.PoolManager()

def lambda_handler(event, context):
    slack_webhook_url = os.environ['SLACK_WEBHOOK_URL']

    message = json.loads(event['Records'][0]['Sns']['Message'])

    alarm_name = message['AlarmName']
    new_state = message['NewStateValue']
    reason = message['NewStateReason']

    # Slack メッセージの作成
    slack_message = {
        'text': f"🚨 CloudWatch Alarm: {alarm_name}",
        'attachments': [{
            'color': 'danger' if new_state == 'ALARM' else 'good',
            'fields': [
                {'title': 'Alarm', 'value': alarm_name, 'short': True},
                {'title': 'State', 'value': new_state, 'short': True},
                {'title': 'Reason', 'value': reason, 'short': False}
            ]
        }]
    }

    encoded_msg = json.dumps(slack_message).encode('utf-8')
    resp = http.request('POST', slack_webhook_url, body=encoded_msg)

    return {
        'statusCode': 200,
        'body': json.dumps('Notification sent to Slack')
    }
```

#### Lambda関数のデプロイ

```bash
# Lambda関数の作成
aws lambda create-function \
  --function-name slack-notifier \
  --runtime python3.11 \
  --role arn:aws:iam::110221759530:role/lambda-execution-role \
  --handler slack_notifier.lambda_handler \
  --zip-file fileb://slack_notifier.zip \
  --environment Variables="{SLACK_WEBHOOK_URL=https://hooks.slack.com/services/YOUR/WEBHOOK/URL}" \
  --region ap-northeast-1

# SNSトピックとLambdaを連携
aws sns subscribe \
  --topic-arn arn:aws:sns:ap-northeast-1:110221759530:critical-alerts \
  --protocol lambda \
  --notification-endpoint arn:aws:lambda:ap-northeast-1:110221759530:function:slack-notifier \
  --region ap-northeast-1

# Lambdaに権限を付与
aws lambda add-permission \
  --function-name slack-notifier \
  --statement-id AllowSNSInvoke \
  --action lambda:InvokeFunction \
  --principal sns.amazonaws.com \
  --source-arn arn:aws:sns:ap-northeast-1:110221759530:critical-alerts \
  --region ap-northeast-1
```

## 複合アラート（Composite Alarms）

複数のアラート条件を組み合わせた高度なアラートを設定できます。

```bash
# サービスが完全にダウンしている判定
# (タスクが0 AND ターゲットが異常)
aws cloudwatch put-composite-alarm \
  --alarm-name dotnet-service-down \
  --alarm-description "Service is completely down" \
  --alarm-rule "ALARM(dotnet-no-running-tasks) AND ALARM(dotnet-unhealthy-targets)" \
  --actions-enabled \
  --alarm-actions arn:aws:sns:ap-northeast-1:110221759530:critical-alerts \
  --region ap-northeast-1
```

## アラート状態の確認

### 現在のアラート状態

```bash
# すべてのアラーム状態を確認
aws cloudwatch describe-alarms \
  --region ap-northeast-1 \
  --query 'MetricAlarms[*].[AlarmName,StateValue,StateReason]' \
  --output table

# ALARM状態のアラームのみ表示
aws cloudwatch describe-alarms \
  --state-value ALARM \
  --region ap-northeast-1 \
  --query 'MetricAlarms[*].[AlarmName,StateReason]' \
  --output table

# 特定のアラームの詳細
aws cloudwatch describe-alarms \
  --alarm-names dotnet-high-cpu dotnet-high-memory \
  --region ap-northeast-1
```

### アラーム履歴

```bash
# アラームの履歴（過去7日間）
aws cloudwatch describe-alarm-history \
  --alarm-name dotnet-high-cpu \
  --start-date $(date -u -d '7 days ago' +%Y-%m-%dT%H:%M:%S) \
  --history-item-type StateUpdate \
  --region ap-northeast-1 \
  --query 'AlarmHistoryItems[*].[Timestamp,HistorySummary]' \
  --output table
```

## アラートのテスト

### アラームを手動で発火

```bash
# アラーム状態に変更（テスト用）
aws cloudwatch set-alarm-state \
  --alarm-name dotnet-high-cpu \
  --state-value ALARM \
  --state-reason "Testing alarm notification" \
  --region ap-northeast-1

# OK状態に戻す
aws cloudwatch set-alarm-state \
  --alarm-name dotnet-high-cpu \
  --state-value OK \
  --state-reason "Test complete" \
  --region ap-northeast-1
```

### 通知のテスト

```bash
# SNSトピックに直接メッセージを送信
aws sns publish \
  --topic-arn arn:aws:sns:ap-northeast-1:110221759530:critical-alerts \
  --message "Test notification" \
  --subject "CloudWatch Alarm Test" \
  --region ap-northeast-1
```

## アラートの一時停止

メンテナンス中などアラートを一時的に無効化する場合：

```bash
# アラームアクションを無効化
aws cloudwatch disable-alarm-actions \
  --alarm-names dotnet-high-cpu dotnet-high-memory dotnet-5xx-errors \
  --region ap-northeast-1

# メンテナンス完了後、再度有効化
aws cloudwatch enable-alarm-actions \
  --alarm-names dotnet-high-cpu dotnet-high-memory dotnet-5xx-errors \
  --region ap-northeast-1
```

## アラート管理スクリプト

### すべてのアラートを一括作成

```bash
#!/bin/bash
# create-alarms.sh

CLUSTER="app-cluster"
SERVICE="dotnet-service"
REGION="ap-northeast-1"
SNS_CRITICAL="arn:aws:sns:ap-northeast-1:110221759530:critical-alerts"
SNS_HIGH="arn:aws:sns:ap-northeast-1:110221759530:high-alerts"
SNS_MEDIUM="arn:aws:sns:ap-northeast-1:110221759530:medium-alerts"

echo "Creating alarms for $SERVICE..."

# Critical: No running tasks
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-no-running-tasks \
  --alarm-description "No running tasks" \
  --metric-name RunningCount \
  --namespace ECS/ContainerInsights \
  --statistic Average \
  --period 60 \
  --evaluation-periods 1 \
  --threshold 1 \
  --comparison-operator LessThanThreshold \
  --dimensions Name=ClusterName,Value=$CLUSTER Name=ServiceName,Value=$SERVICE \
  --alarm-actions $SNS_CRITICAL \
  --region $REGION

echo "✓ Created: dotnet-no-running-tasks"

# High: CPU utilization
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-high-cpu \
  --alarm-description "High CPU utilization" \
  --metric-name CPUUtilization \
  --namespace AWS/ECS \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=ClusterName,Value=$CLUSTER Name=ServiceName,Value=$SERVICE \
  --alarm-actions $SNS_HIGH \
  --region $REGION

echo "✓ Created: dotnet-high-cpu"

# Medium: Memory utilization
aws cloudwatch put-metric-alarm \
  --alarm-name dotnet-high-memory \
  --alarm-description "High memory utilization" \
  --metric-name MemoryUtilization \
  --namespace AWS/ECS \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=ClusterName,Value=$CLUSTER Name=ServiceName,Value=$SERVICE \
  --alarm-actions $SNS_MEDIUM \
  --region $REGION

echo "✓ Created: dotnet-high-memory"

echo "All alarms created successfully!"
```

## アラート通知のカスタマイズ

### メール通知のフォーマット

デフォルトのメール通知:
```
You are receiving this email because your Amazon CloudWatch Alarm "dotnet-high-cpu" in the AP-NORTHEAST-1 region has entered the ALARM state...
```

カスタマイズするには、Lambda関数でSNSメッセージを加工してSESで送信します。

### Slack通知のカスタマイズ

```python
def format_slack_message(alarm_data):
    """Slackメッセージを整形"""
    alarm_name = alarm_data['AlarmName']
    new_state = alarm_data['NewStateValue']
    reason = alarm_data['NewStateReason']
    timestamp = alarm_data['StateChangeTime']

    # アラームレベルに応じて色を変更
    color = {
        'ALARM': 'danger',
        'OK': 'good',
        'INSUFFICIENT_DATA': 'warning'
    }.get(new_state, 'warning')

    # アイコンを追加
    icon = {
        'ALARM': '🚨',
        'OK': '✅',
        'INSUFFICIENT_DATA': '⚠️'
    }.get(new_state, '❓')

    message = {
        'text': f"{icon} CloudWatch Alarm: {alarm_name}",
        'attachments': [{
            'color': color,
            'fields': [
                {'title': 'Alarm Name', 'value': alarm_name, 'short': True},
                {'title': 'State', 'value': new_state, 'short': True},
                {'title': 'Reason', 'value': reason, 'short': False},
                {'title': 'Time', 'value': timestamp, 'short': True}
            ],
            'footer': 'CloudWatch Alarms',
            'ts': int(time.time())
        }]
    }

    return message
```

## ベストプラクティス

### 1. 適切な閾値設定
- 過去のメトリクスデータから閾値を決定
- 誤検知を減らすため評価期間を調整
- ビジネス影響を考慮

### 2. アラート疲れの防止
- 頻繁に発火するアラートは見直し
- 重要度に応じて通知方法を変える
- メンテナンス時は一時停止

### 3. ドキュメント化
- 各アラートの対応手順を明記
- エスカレーションパスを定義
- ロールバック手順へのリンク

### 4. 定期的な見直し
- 月次: アラート履歴のレビュー
- 四半期: 閾値の見直し
- 年次: アラート戦略の見直し

## 関連ドキュメント

- [監視概要](monitoring-overview.md)
- [CloudWatch Logs](cloudwatch-logs.md)
- [メトリクス監視](metrics.md)
- [トラブルシューティング](../troubleshooting/common-issues.md)

---

**最終更新日**: 2025-12-17
