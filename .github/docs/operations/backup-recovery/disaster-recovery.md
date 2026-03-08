# 災害復旧手順 (Disaster Recovery)

## 概要
重大なインシデントや災害が発生した際の復旧手順を定義します。

## 災害シナリオ

### シナリオ1: アプリケーション完全停止

**症状**:
- ECSタスクが起動しない
- ヘルスチェックが全て失敗
- サービスが完全にダウン

**影響度**: Critical
**RTO**: 15分

#### 復旧手順

##### ステップ1: 状況確認（1分）
```bash
# ECSサービス状態確認
aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1

# タスク状態確認
aws ecs list-tasks --cluster app-cluster --service-name dotnet-service --region ap-northeast-1

# 最近のタスク停止理由
aws ecs describe-tasks --cluster app-cluster --tasks <task-arn> --region ap-northeast-1 --query 'tasks[0].stoppedReason'
```

##### ステップ2: ロールバック（5分）
```bash
# 前の安定バージョンにロールバック
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --task-definition dotnet-task:14 \
  --force-new-deployment \
  --region ap-northeast-1

# デプロイ状況監視
watch -n 10 "aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1 --query 'services[0].[runningCount,desiredCount]'"
```

##### ステップ3: 動作確認（2分）
```bash
# ヘルスチェック
curl -i https://rya234.com/dotnet/healthz

# ログ確認
aws logs tail /ecs/dotnet-app --since 5m --region ap-northeast-1 --format short
```

##### ステップ4: 通知（1分）
関係者にインシデントとロールバック実施を通知。

詳細は [../deployment/rollback.md](../deployment/rollback.md) を参照。

---

### シナリオ2: データベース障害

**症状**:
- データベース接続エラー
- アプリケーションはエラーを返す
- Supabaseダッシュボードにアクセスできない

**影響度**: Critical
**RTO**: 1時間

#### 復旧手順

##### ステップ1: 状況確認（5分）
```bash
# Supabase 接続テスト
psql "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" -c "SELECT 1"

# Supabase ステータスページ確認
# https://status.supabase.com/

# アプリケーションログでエラー確認
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "database" \
  --start-time $(date -d '10 minutes ago' +%s)000 \
  --region ap-northeast-1
```

##### ステップ2: Supabase側の問題かを判断（5分）

**Supabase側の障害の場合**:
- Supabaseサポートに連絡
- ステータスページで復旧状況を監視
- ユーザーにメンテナンス通知

**アプリケーション側の問題の場合**:
- 接続プール設定を確認
- Secrets Managerの認証情報を確認
- ネットワーク設定を確認

##### ステップ3: バックアップからの復旧（30分）

最悪の場合、バックアップからリストア：

```bash
# 1. 最新バックアップを確認
# Supabase Dashboard → Settings → Database → Backups

# 2. バックアップからリストア
# Supabase Dashboard でリストアを実行

# 3. 手動バックアップからのリストア（Supabaseが使えない場合）
# 新しいSupabaseプロジェクトを作成
# 手動バックアップからリストア
gunzip < backup-latest.sql.gz | psql "postgresql://postgres:[NEW-PASSWORD]@db.[NEW-PROJECT-REF].supabase.co:5432/postgres"

# 4. 接続情報を更新
aws secretsmanager update-secret \
  --secret-id dotnet-app-secrets \
  --secret-string '{"SupabaseUrl":"https://[NEW-PROJECT-REF].supabase.co","SupabaseAnonKey":"[NEW-ANON-KEY]","ConnectionString":"postgresql://..."}' \
  --region ap-northeast-1

# 5. ECSタスクを再起動（新しい接続情報を読み込むため）
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --force-new-deployment \
  --region ap-northeast-1
```

##### ステップ4: 動作確認（10分）
```bash
# ヘルスチェック
curl -i https://rya234.com/dotnet/healthz

# データベース接続確認
psql "postgresql://..." -c "SELECT count(*) FROM users;"

# アプリケーション動作確認
# 主要機能をテスト
```

詳細は [database-backup.md](database-backup.md) を参照。

---

### シナリオ3: AWSリージョン障害

**症状**:
- ap-northeast-1リージョン全体が利用不可
- ECS、ALB、RDSすべてアクセス不可

**影響度**: Critical
**RTO**: 数時間〜1日

#### 復旧手順

**現状**: 単一リージョン構成のため、リージョン障害時は完全停止

**短期対応**:
1. AWSステータスページで復旧予定を確認
2. ユーザーにメンテナンス通知
3. リージョン復旧後、サービスが自動復旧することを確認

**長期対策（マルチリージョン対応）**:
- 別リージョンにスタンバイ環境を構築
- Route 53でフェイルオーバー設定
- データベースのレプリケーション設定

---

### シナリオ4: セキュリティインシデント

**症状**:
- 不正アクセスの検知
- データ漏洩の疑い
- 異常なトラフィック

**影響度**: Critical
**RTO**: 即座

#### 復旧手順

##### ステップ1: 即座の対応（5分以内）
```bash
# 1. サービスを即座に停止
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --desired-count 0 \
  --region ap-northeast-1

# 2. WAF設定を更新（該当するIPをブロック）
# AWS WAF Console で対応

# 3. Secrets Managerの認証情報をローテーション
aws secretsmanager rotate-secret \
  --secret-id dotnet-app-secrets \
  --region ap-northeast-1
```

##### ステップ2: 調査（1時間）
```bash
# アクセスログの確認
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --start-time $(date -d '24 hours ago' +%s)000 \
  --region ap-northeast-1 > incident-logs.json

# 不正アクセスのパターンを分析
# - アクセス元IP
# - アクセスパターン
# - 影響範囲
```

##### ステップ3: 対策実施（数時間）
1. 脆弱性の修正
2. セキュリティパッチの適用
3. 認証情報の全変更
4. アクセス制御の強化

##### ステップ4: サービス再開（30分）
```bash
# 修正版をデプロイ
# 詳細な監視を実施
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --desired-count 1 \
  --region ap-northeast-1
```

##### ステップ5: 事後対応
1. インシデントレポートの作成
2. 再発防止策の策定
3. 関係者への報告
4. 必要に応じて公表

---

### シナリオ5: 人為的ミス（誤削除等）

**症状**:
- 重要なリソースが削除された
- 設定が誤って変更された
- データが誤って削除された

**影響度**: High〜Critical
**RTO**: 30分〜2時間

#### 復旧手順

##### 誤ってECSサービスを削除した場合

```bash
# Terraformから再作成
cd Terraform/aws
terraform plan
terraform apply

# 手動で作成する場合
aws ecs create-service \
  --cluster app-cluster \
  --service-name dotnet-service \
  --task-definition dotnet-task:latest \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "..." \
  --region ap-northeast-1
```

##### 誤ってSecretsを削除した場合

```bash
# Secretsはデフォルトで7日間の猶予期間あり
# 削除を取り消す
aws secretsmanager restore-secret \
  --secret-id dotnet-app-secrets \
  --region ap-northeast-1

# 完全に削除されている場合、バックアップから復元
aws secretsmanager create-secret \
  --name dotnet-app-secrets \
  --secret-string file://secrets-backup-latest.json \
  --region ap-northeast-1
```

##### 誤ってデータを削除した場合

```bash
# 最新バックアップからリストア
# 詳細は database-backup.md を参照

# PITRが有効な場合、削除直前の状態にリストア
# Supabase Dashboard → Settings → Database → Point in Time Recovery
```

---

## 災害復旧テスト

### 年次DR訓練

年に1回、災害復旧訓練を実施することを推奨。

#### DR訓練手順

```bash
#!/bin/bash
# dr-drill.sh

echo "=== Disaster Recovery Drill Started ==="
echo "Date: $(date)"

# 1. バックアップの取得
echo "1. Taking backup..."
./daily-backup.sh

# 2. サービスを停止（本番では実施しない、ステージング環境で実施）
echo "2. Simulating service failure..."
# aws ecs update-service --cluster app-cluster --service dotnet-service --desired-count 0

# 3. バックアップからのリストア
echo "3. Restoring from backup..."
# gunzip < backup-latest.sql.gz | psql "postgresql://..."

# 4. サービスを再起動
echo "4. Restarting service..."
# aws ecs update-service --cluster app-cluster --service dotnet-service --desired-count 1

# 5. 動作確認
echo "5. Verifying service..."
# curl https://rya234.com/dotnet/healthz

echo "=== Disaster Recovery Drill Completed ==="
echo "Review the results and update documentation as needed."
```

---

## エスカレーションフロー

```
インシデント発生
    ↓
[5分以内] 初動対応者が対応開始
    ↓
状況判断
    ↓
├─ [軽微] → 通常対応
├─ [中程度] → チームリーダーに報告
└─ [重大] → 即座にエスカレーション
           ↓
       プロジェクトオーナーに報告
           ↓
       [必要に応じて] 外部サポートに連絡
```

### 連絡先リスト

| 役割 | 連絡方法 | 対応時間 |
|-----|---------|---------|
| オンコール担当 | Slack + メール | 24/7 |
| チームリーダー | 電話 + Slack | 24/7 |
| プロジェクトオーナー | 電話 | 平日9-18時 |
| AWSサポート | AWS Console | 24/7 |
| Supabaseサポート | https://supabase.com/dashboard/support | 24/7（有料プラン） |

---

## ポストモーテム（事後分析）

インシデント解決後、必ずポストモーテムを作成します。

### ポストモーテムテンプレート

```markdown
# インシデントポストモーテム

## 概要
- **インシデントID**: INC-2025-001
- **発生日時**: 2025-12-17 15:30 JST
- **復旧日時**: 2025-12-17 16:00 JST
- **影響時間**: 30分
- **影響範囲**: 全ユーザー

## タイムライン
- 15:30: アラート発火（ECSタスク停止）
- 15:32: オンコール担当が対応開始
- 15:35: 原因特定（メモリ不足）
- 15:40: ロールバック実施
- 15:50: サービス復旧確認
- 16:00: 正常稼働確認、インシデントクローズ

## 根本原因
メモリリークにより、タスクがOOM Killerによって強制終了された。

## 対応内容
1. 前の安定バージョンにロールバック
2. メモリ使用量を監視
3. メモリリークの原因を調査

## 良かった点
- アラートが即座に発火
- ロールバック手順が明確で迅速に対応
- 30分以内に復旧

## 改善点
- メモリ使用量のアラートが未設定だった
- メモリリークを事前に検知できなかった

## 再発防止策
- [ ] メモリ使用量のアラート設定（閾値: 80%）
- [ ] コードレビューでメモリリークチェックを強化
- [ ] 負荷テストの実施
- [ ] デプロイ前のメモリプロファイリング

## 学んだ教訓
- モニタリングの重要性
- 迅速なロールバックの価値
- ドキュメントの整備が対応時間短縮に寄与
```

---

## 関連ドキュメント

- [バックアップ戦略](backup-strategy.md)
- [データベースバックアップ](database-backup.md)
- [ロールバック手順](../deployment/rollback.md)
- [トラブルシューティング](../troubleshooting/common-issues.md)

---

**最終更新日**: 2025-12-17
