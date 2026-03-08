# 運用ドキュメント

## 概要
本番環境の運用に関する包括的なドキュメントです。デプロイ、監視、トラブルシューティング、メンテナンスの各領域をカバーしています。

---

## ドキュメント構成

```
operations/
├── README.md                    # このファイル（インデックス）
├── deployment/                  # デプロイメント
│   ├── automated-deployment.md
│   ├── manual-deployment.md
│   ├── rollback.md
│   └── deployment-checklist.md
├── monitoring/                  # 監視
│   ├── monitoring-overview.md
│   ├── cloudwatch-logs.md
│   ├── health-checks.md
│   ├── metrics.md
│   └── alerts.md
├── backup-recovery/            # バックアップ・復旧
│   ├── backup-strategy.md
│   ├── database-backup.md
│   └── disaster-recovery.md
├── troubleshooting/            # トラブルシューティング
│   ├── common-issues.md
│   ├── log-analysis.md
│   └── performance-tuning.md
└── maintenance/                # メンテナンス
    ├── routine-maintenance.md
    ├── security-updates.md
    └── cost-optimization.md
```

---

## クイックリンク

### デプロイメント

#### [自動デプロイメント](deployment/automated-deployment.md)
GitHub Actionsを使用した自動デプロイの手順

**よく使うコマンド**:
```bash
# デプロイ状況の確認
gh run list --workflow=deploy.yml

# デプロイ完了確認
curl -i https://rya234.com/dotnet/healthz
```

#### [手動デプロイメント](deployment/manual-deployment.md)
手動でのビルドとデプロイの手順

**よく使うコマンド**:
```bash
# Dockerビルド
docker build -t dotnet-app:latest .

# ECRにプッシュ
docker push 110221759530.dkr.ecr.ap-northeast-1.amazonaws.com/dotnet-app:latest

# ECSデプロイ
aws ecs update-service --cluster app-cluster --service dotnet-service --force-new-deployment --region ap-northeast-1
```

#### [ロールバック](deployment/rollback.md)
デプロイに失敗した場合の復旧手順

**緊急ロールバック**:
```bash
# 前のバージョンにロールバック
aws ecs update-service --cluster app-cluster --service dotnet-service --task-definition dotnet-task:14 --force-new-deployment --region ap-northeast-1
```

---

### 監視

#### [監視概要](monitoring/monitoring-overview.md)
監視戦略と監視項目の全体像

**主要監視項目**:
- ECSタスク実行数
- ヘルスチェック
- CPU/メモリ使用率
- エラーログ
- レスポンスタイム

#### [CloudWatch Logs](monitoring/cloudwatch-logs.md)
ログの確認・検索・分析方法

**よく使うコマンド**:
```bash
# 最新ログ確認
aws logs tail /ecs/dotnet-app --since 10m --region ap-northeast-1 --format short

# エラーログ検索
aws logs filter-log-events --log-group-name /ecs/dotnet-app --filter-pattern "ERROR" --region ap-northeast-1
```

#### [ヘルスチェック](monitoring/health-checks.md)
ヘルスチェックの設定と確認方法

**ヘルスチェックURL**:
```bash
curl -i https://rya234.com/dotnet/healthz
```

#### [メトリクス監視](monitoring/metrics.md)
CloudWatch Metricsでのパフォーマンス監視

#### [アラート設定](monitoring/alerts.md)
CloudWatch Alarmsの設定と管理

---

### バックアップ・復旧

#### [バックアップ戦略](backup-recovery/backup-strategy.md)
RPO/RTOの定義とバックアップ方針

**RPO/RTO**:
- ソースコード: RPO 0, RTO 15分
- データベース: RPO 24時間, RTO 1時間

#### [データベースバックアップ](backup-recovery/database-backup.md)
Supabaseデータベースのバックアップとリストア

**よく使うコマンド**:
```bash
# 手動バックアップ
pg_dump "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" | gzip > backup-$(date +%Y%m%d).sql.gz

# リストア
gunzip < backup-20251217.sql.gz | psql "postgresql://..."
```

#### [災害復旧](backup-recovery/disaster-recovery.md)
重大なインシデントからの復旧手順

**主な災害シナリオ**:
1. アプリケーション完全停止
2. データベース障害
3. AWSリージョン障害
4. セキュリティインシデント
5. 人為的ミス

---

### トラブルシューティング

#### [よくある問題と解決方法](troubleshooting/common-issues.md)
頻繁に発生する問題のトラブルシューティングガイド

**よくある問題**:
- ECSタスクが起動しない
- デプロイが完了しない
- データベース接続エラー
- レスポンスが遅い
- ログが出力されない

#### [ログ解析ガイド](troubleshooting/log-analysis.md)
ログを使った問題の特定と分析

**よく使うパターン**:
```bash
# エラーの時系列分析
fields @timestamp, level | filter level = "ERROR" | stats count() by bin(5m)

# エンドポイント別エラー率
fields @timestamp, path, statusCode | filter statusCode >= 500 | stats count() by path
```

#### [パフォーマンスチューニング](troubleshooting/performance-tuning.md)
アプリケーションのパフォーマンス最適化

**最適化のポイント**:
- データベースクエリの最適化（N+1問題）
- キャッシュの導入
- 非同期処理への変換
- リソースの最適化

---

### メンテナンス

#### [定期メンテナンス](maintenance/routine-maintenance.md)
日次・週次・月次のメンテナンス作業

**メンテナンススケジュール**:
- 日次: ログ確認、メトリクス確認
- 週次: バックアップ確認、セキュリティパッチ確認
- 月次: セキュリティ監査、パフォーマンスレビュー

#### [セキュリティアップデート](maintenance/security-updates.md)
脆弱性への対応とアップデート手順

**対応レベル**:
- Critical: 24時間以内
- High: 3日以内
- Medium: 1週間以内
- Low: 1ヶ月以内

#### [コスト最適化](maintenance/cost-optimization.md)
AWSコストの削減と効率化

**最適化のポイント**:
- ECS Fargateのリソース最適化
- CloudWatch Logsの保持期間設定
- 古いECRイメージの削除
- NAT Gatewayの削減

---

## 緊急時の対応フロー

### 1. サービスダウン（Critical）

```
1. アラート受信 → 即座に対応開始（5分以内）
2. 状況確認
   aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1
3. ロールバック
   aws ecs update-service --cluster app-cluster --service dotnet-service --task-definition dotnet-task:14 --force-new-deployment --region ap-northeast-1
4. 動作確認
   curl https://rya234.com/dotnet/healthz
5. 関係者に通知
```

参考: [災害復旧手順](backup-recovery/disaster-recovery.md)

### 2. パフォーマンス劣化（High）

```
1. メトリクス確認
   - CPU/メモリ使用率
   - レスポンスタイム
   - エラーログ
2. ボトルネック特定
   - データベースクエリ
   - 外部API
   - リソース不足
3. 対策実施
   - クエリ最適化
   - リソース増強
   - キャッシュ導入
```

参考: [パフォーマンスチューニング](troubleshooting/performance-tuning.md)

### 3. エラー率上昇（High）

```
1. エラーログ確認
   aws logs filter-log-events --log-group-name /ecs/dotnet-app --filter-pattern "ERROR" --region ap-northeast-1
2. エラーパターン分析
   - データベース接続エラー
   - タイムアウト
   - 例外
3. 根本原因の特定と修正
```

参考: [よくある問題](troubleshooting/common-issues.md)

---

## オンコール対応

### オンコール担当の役割

1. **アラート監視**: CloudWatchアラートに24時間対応
2. **初動対応**: インシデント発生時の初期対応（15分以内）
3. **エスカレーション**: 必要に応じて上位者にエスカレーション
4. **記録**: 対応内容の記録とドキュメント更新

### オンコール時の連絡先

| 役割 | 連絡方法 | 対応時間 |
|-----|---------|---------|
| オンコール担当 | Slack + メール | 24/7 |
| チームリーダー | 電話 + Slack | 24/7 |
| プロジェクトオーナー | 電話 | 平日9-18時 |

### 対応優先度

| レベル | 対応時間 | 例 |
|-------|---------|-----|
| P0 (Critical) | 即座（5分以内） | サービスダウン |
| P1 (High) | 30分以内 | エラー率上昇 |
| P2 (Medium) | 営業時間内 | パフォーマンス低下 |
| P3 (Low) | 計画的に対応 | 軽微な警告 |

---

## 運用ベストプラクティス

### デプロイメント

- [ ] 業務時間外のデプロイ
- [ ] デプロイ前のチェックリスト確認
- [ ] ロールバック手順の確認
- [ ] デプロイ後の動作確認

### 監視

- [ ] 適切な閾値設定
- [ ] アラート疲れの防止
- [ ] 定期的なメトリクスレビュー
- [ ] ログの構造化

### バックアップ

- [ ] 日次バックアップの確認
- [ ] 月次バックアップテスト
- [ ] バックアップの保存場所の分散
- [ ] リストア手順の文書化

### セキュリティ

- [ ] 定期的な脆弱性スキャン
- [ ] Secrets の定期的なローテーション
- [ ] セキュリティパッチの迅速な適用
- [ ] アクセスログの監査

---

## よく使うコマンド集

### サービス状態確認

```bash
# ECS サービス状態
aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1 --query 'services[0].[runningCount,desiredCount,status]'

# ヘルスチェック
curl -i https://rya234.com/dotnet/healthz

# 最新ログ
aws logs tail /ecs/dotnet-app --since 10m --region ap-northeast-1 --format short
```

### デプロイ関連

```bash
# 強制デプロイ
aws ecs update-service --cluster app-cluster --service dotnet-service --force-new-deployment --region ap-northeast-1

# タスク定義のリビジョン指定デプロイ
aws ecs update-service --cluster app-cluster --service dotnet-service --task-definition dotnet-task:15 --region ap-northeast-1

# GitHub Actions デプロイ状況
gh run list --workflow=deploy.yml --limit 5
```

### トラブルシューティング

```bash
# タスク停止理由
aws ecs describe-tasks --cluster app-cluster --tasks <task-arn> --region ap-northeast-1 --query 'tasks[0].stoppedReason'

# エラーログ検索
aws logs filter-log-events --log-group-name /ecs/dotnet-app --filter-pattern "ERROR" --start-time $(date -d '1 hour ago' +%s)000 --region ap-northeast-1

# CPU使用率確認
aws cloudwatch get-metric-statistics --namespace AWS/ECS --metric-name CPUUtilization --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) --end-time $(date -u +%Y-%m-%dT%H:%M:%S) --period 300 --statistics Average --region ap-northeast-1
```

---

## 関連ドキュメント

### 設計ドキュメント
- [要件定義](../requirements.md)
- [外部設計](../external-design.md)
- [内部設計](../internal-design.md)

### 開発ドキュメント
- [開発ガイド](../README.md)
- [プロジェクトマネジメント](../project-management.md)

---

## ドキュメント更新履歴

| 日付 | 更新内容 | 更新者 |
|------|---------|--------|
| 2025-12-17 | 初版作成 | Claude |

---

## フィードバック

運用ドキュメントの改善提案や誤りの報告は、GitHubのIssueで受け付けています。

```bash
# Issue作成
gh issue create --title "[運用ドキュメント] 改善提案" --label documentation
```

---

**最終更新日**: 2025-12-17
