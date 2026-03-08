# 定期メンテナンス

## 概要
本番環境の定期的なメンテナンス作業を定義します。

## メンテナンススケジュール

### 日次メンテナンス

| 作業 | 時刻 | 所要時間 | 担当 |
|------|------|---------|------|
| ログ確認 | 毎朝9:00 | 15分 | 運用担当 |
| メトリクス確認 | 毎朝9:15 | 15分 | 運用担当 |
| アラート履歴確認 | 毎朝9:30 | 15分 | 運用担当 |

#### 日次チェックリスト
```bash
#!/bin/bash
# daily-check.sh

echo "=== Daily Maintenance Check $(date) ==="

# 1. サービス稼働確認
echo "1. Service Health Check"
curl -s https://rya234.com/dotnet/healthz && echo "✓ Service is healthy" || echo "✗ Service is down"

# 2. ECS状態確認
echo "2. ECS Service Status"
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].[runningCount,desiredCount,status]' \
  --output text

# 3. 過去24時間のエラーログ確認
echo "3. Error Logs (Last 24h)"
ERROR_COUNT=$(aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --start-time $(date -d '24 hours ago' +%s)000 \
  --region ap-northeast-1 \
  | jq '.events | length')
echo "Error count: $ERROR_COUNT"

# 4. アラート履歴
echo "4. CloudWatch Alarms"
aws cloudwatch describe-alarms \
  --state-value ALARM \
  --region ap-northeast-1 \
  --query 'MetricAlarms[*].[AlarmName,StateReason]' \
  --output table

# 5. リソース使用状況
echo "5. Resource Utilization"
echo "CPU (24h avg):"
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 86400 \
  --statistics Average \
  --region ap-northeast-1 \
  --query 'Datapoints[0].Average'

echo "Memory (24h avg):"
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name MemoryUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 86400 \
  --statistics Average \
  --region ap-northeast-1 \
  --query 'Datapoints[0].Average'

echo "=== Daily Check Complete ==="
```

---

### 週次メンテナンス

| 作業 | 曜日/時刻 | 所要時間 | 担当 |
|------|----------|---------|------|
| バックアップ確認 | 日曜 10:00 | 30分 | 運用担当 |
| セキュリティパッチ確認 | 月曜 10:00 | 30分 | 開発担当 |
| ログローテーション | 日曜 3:00 | 自動 | システム |
| パフォーマンスレポート | 金曜 15:00 | 1時間 | 運用担当 |

#### 週次チェックリスト

```markdown
## 週次メンテナンスチェックリスト

### 1. バックアップ確認
- [ ] Supabase 自動バックアップの確認
- [ ] 手動バックアップの実施
- [ ] バックアップファイルの整合性確認
- [ ] 古いバックアップの削除（30日以上前）

### 2. セキュリティパッチ
- [ ] .NET SDK の更新確認
- [ ] NuGetパッケージの脆弱性確認
- [ ] 依存ライブラリのアップデート確認
- [ ] AWSサービスのアップデート確認

### 3. パフォーマンスレビュー
- [ ] レスポンスタイムのトレンド分析
- [ ] リソース使用率の確認
- [ ] エラーログのパターン分析
- [ ] ボトルネックの特定

### 4. ログ管理
- [ ] CloudWatch Logs の保持期間確認
- [ ] 不要なログストリームの削除
- [ ] ログのS3エクスポート（必要な場合）
```

#### 週次パフォーマンスレポート

```bash
#!/bin/bash
# weekly-report.sh

WEEK_START=$(date -d '7 days ago' +%Y-%m-%d)
WEEK_END=$(date +%Y-%m-%d)

echo "=== Weekly Performance Report ==="
echo "Period: $WEEK_START to $WEEK_END"
echo ""

# 1. サービス稼働率
echo "1. Service Uptime"
# TODO: ヘルスチェック成功率を計算

# 2. エラー統計
echo "2. Error Statistics"
aws logs start-query \
  --log-group-name /ecs/dotnet-app \
  --start-time $(date -d '7 days ago' +%s) \
  --end-time $(date +%s) \
  --query-string 'fields @timestamp | filter level = "ERROR" | stats count() as error_count by bin(1d)' \
  --region ap-northeast-1

# 3. パフォーマンスメトリクス
echo "3. Performance Metrics"
echo "Average Response Time:"
# ALB metrics

echo "Average CPU Usage:"
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '7 days ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 604800 \
  --statistics Average \
  --region ap-northeast-1

# 4. コスト分析
echo "4. Cost Analysis"
# AWS Cost Explorer で確認

echo "=== Report Complete ==="
```

---

### 月次メンテナンス

| 作業 | 実施日 | 所要時間 | 担当 |
|------|--------|---------|------|
| セキュリティ監査 | 第1週 | 2時間 | セキュリティ担当 |
| パフォーマンスレビュー | 第2週 | 2時間 | 開発リーダー |
| バックアップテスト | 第3週 | 2時間 | 運用担当 |
| コスト最適化 | 第4週 | 2時間 | インフラ担当 |
| ドキュメント更新 | 月末 | 1時間 | 全員 |

#### 月次チェックリスト

```markdown
## 月次メンテナンスチェックリスト

### 1. セキュリティ監査
- [ ] IAMロールと権限の確認
- [ ] Secrets Manager のローテーション
- [ ] セキュリティグループの見直し
- [ ] SSL証明書の有効期限確認
- [ ] アクセスログの監査

### 2. パフォーマンスレビュー
- [ ] 月次パフォーマンスレポートの作成
- [ ] ボトルネックの特定と対策
- [ ] キャパシティプランニング
- [ ] SLO/SLI の達成状況確認

### 3. バックアップテスト
- [ ] データベースバックアップからのリストア
- [ ] リストア時間の計測
- [ ] データ整合性の確認
- [ ] リストア手順書の更新

### 4. コスト最適化
- [ ] AWS コスト分析
- [ ] 不要なリソースの削除
- [ ] リソースの適正サイズ化
- [ ] 予約インスタンスの検討

### 5. ドキュメント更新
- [ ] 運用ドキュメントの見直し
- [ ] トラブルシューティングガイドの更新
- [ ] 新しい問題と解決方法の追記
- [ ] 変更履歴の記録
```

---

### 四半期メンテナンス

| 作業 | 実施月 | 所要時間 | 担当 |
|------|--------|---------|------|
| DR訓練 | 四半期初月 | 半日 | 全員 |
| アーキテクチャレビュー | 四半期2月目 | 半日 | 開発リーダー |
| 監視戦略の見直し | 四半期3月目 | 2時間 | 運用担当 |

---

## メンテナンスウィンドウ

### 定期メンテナンス時間

**日時**: 毎週日曜日 3:00-5:00 AM JST

**作業内容**:
- システムアップデート
- データベースメンテナンス
- パフォーマンスチューニング

### メンテナンス通知

#### ユーザーへの事前通知（1週間前）

```
件名: [予定] 定期メンテナンスのお知らせ

いつもご利用いただきありがとうございます。

下記日程にて定期メンテナンスを実施いたします。

【日時】
2025年12月21日（日）3:00-5:00 AM JST

【影響】
メンテナンス中はサービスをご利用いただけません。

【作業内容】
- システムアップデート
- パフォーマンス改善

ご不便をおかけいたしますが、何卒ご理解のほどお願い申し上げます。
```

### メンテナンスモードの設定

```bash
# 1. タスク数を0にしてサービス停止
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --desired-count 0 \
  --region ap-northeast-1

# 2. メンテナンス作業実施
# ...

# 3. サービス再開
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --desired-count 1 \
  --region ap-northeast-1
```

---

## メンテナンス記録

### メンテナンスログの保持

```markdown
# メンテナンスログ

## 2025-12-17 定期メンテナンス

### 実施内容
- データベースバックアップの確認
- ログのクリーンアップ
- パフォーマンスメトリクスの確認

### 実施者
山田太郎

### 実施時刻
2025-12-17 10:00-10:30 JST

### 結果
すべての項目で問題なし

### 備考
CPU使用率が先週より10%上昇。要監視。

---
```

---

## 緊急メンテナンス

### 緊急メンテナンスの判断基準

以下の場合は緊急メンテナンスを実施：
- 重大なセキュリティ脆弱性の発見
- データ損失のリスク
- サービス停止の危険性
- 重大なバグの発見

### 緊急メンテナンスフロー

```
1. 問題の発見・報告
    ↓
2. 影響度の評価
    ↓
3. 緊急メンテナンスの決定
    ↓
4. ユーザーへの緊急通知
    ↓
5. サービス停止
    ↓
6. メンテナンス作業
    ↓
7. 動作確認
    ↓
8. サービス再開
    ↓
9. ユーザーへの完了通知
    ↓
10. 事後報告書の作成
```

---

## 関連ドキュメント

- [セキュリティアップデート](security-updates.md)
- [コスト最適化](cost-optimization.md)
- [バックアップ戦略](../backup-recovery/backup-strategy.md)

---

**最終更新日**: 2025-12-17
