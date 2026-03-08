# バックアップ戦略

## 概要
本番環境におけるバックアップ戦略と復旧方針（RPO/RTO）を定義します。

## RPO/RTO 定義

### RPO (Recovery Point Objective)
**データ損失の許容時間**

| データ種類 | RPO | 説明 |
|-----------|-----|------|
| ソースコード | 0 | Gitで管理、損失なし |
| データベース | 24時間 | 日次バックアップ |
| 設定情報 | 0 | Secrets Manager、コード管理 |
| ログ | 1時間 | CloudWatch Logs |

### RTO (Recovery Time Objective)
**復旧までの目標時間**

| システム | RTO | 説明 |
|---------|-----|------|
| アプリケーション | 15分 | ロールバックで復旧 |
| データベース | 1時間 | バックアップからリストア |
| 設定情報 | 5分 | Secrets Managerから取得 |
| インフラ | 30分 | Terraformで再構築 |

## バックアップ対象

### 1. ソースコード

#### バックアップ方法
- GitHub リポジトリで管理
- 自動バックアップ（Githubの機能）
- ローカルクローンも可

#### 確認方法
```bash
# リポジトリの確認
git remote -v

# 最新コミットの確認
git log -1

# ブランチ一覧
git branch -a
```

#### リストア方法
```bash
# 特定のコミットをチェックアウト
git checkout <commit-hash>

# 特定のタグをチェックアウト
git checkout tags/v1.0.0
```

---

### 2. データベース（Supabase PostgreSQL）

#### バックアップ方法
- **自動バックアップ**: Supabaseが日次で自動実行（最新7日分）
- **手動バックアップ**: 重要な変更前に手動実行
- **エクスポート**: pg_dumpで定期的にエクスポート

#### 自動バックアップの確認
```
Supabase Dashboard → Settings → Database → Backups
```

#### 手動バックアップ（pg_dump）
```bash
# PostgreSQLクライアントのインストール（Windows）
# https://www.postgresql.org/download/windows/

# バックアップ実行
pg_dump "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > backup-$(date +%Y%m%d).sql

# 圧縮してバックアップ
pg_dump "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" | gzip > backup-$(date +%Y%m%d).sql.gz
```

#### バックアップの保存先
- Supabase: 自動バックアップは Supabase 内に保存（7日間）
- ローカル: 重要なバックアップはローカルにも保存
- S3: 長期保存が必要な場合はS3にアップロード

```bash
# S3にアップロード
aws s3 cp backup-20251217.sql.gz s3://my-backup-bucket/database/backup-20251217.sql.gz
```

---

### 3. AWS Secrets Manager

#### バックアップ方法
Secrets Managerの値は自動的に保持されますが、念のため定期的にエクスポート。

```bash
# シークレット一覧の取得
aws secretsmanager list-secrets --region ap-northeast-1

# 特定のシークレット値を取得してバックアップ
aws secretsmanager get-secret-value \
  --secret-id dotnet-app-secrets \
  --region ap-northeast-1 \
  --query 'SecretString' \
  --output text > secrets-backup-$(date +%Y%m%d).json
```

#### リストア方法
```bash
# シークレットを更新
aws secretsmanager update-secret \
  --secret-id dotnet-app-secrets \
  --secret-string file://secrets-backup-20251217.json \
  --region ap-northeast-1
```

---

### 4. ECS タスク定義

#### バックアップ方法
```bash
# タスク定義をエクスポート
aws ecs describe-task-definition \
  --task-definition dotnet-task \
  --region ap-northeast-1 \
  --query 'taskDefinition' > task-definition-backup-$(date +%Y%m%d).json
```

#### リストア方法
```bash
# 不要なフィールドを削除してから登録
# taskDefinitionArn, revision, status, requiresAttributes, compatibilities, registeredAt, registeredBy を削除

# 新しいタスク定義として登録
aws ecs register-task-definition \
  --cli-input-json file://task-definition-backup-20251217.json \
  --region ap-northeast-1
```

---

### 5. Terraform 状態ファイル

#### バックアップ方法
Terraform Cloudまたはリモートバックエンド（S3）を使用している場合は自動的にバックアップされます。

```bash
# ローカルの場合、terraform.tfstate を定期的にバックアップ
cp terraform.tfstate terraform.tfstate.backup-$(date +%Y%m%d)

# S3にアップロード
aws s3 cp terraform.tfstate s3://terraform-state-bucket/backups/terraform.tfstate.$(date +%Y%m%d)
```

---

### 6. CloudWatch Logs

#### バックアップ方法
重要なログはS3にエクスポート。

```bash
# ログをS3にエクスポート
aws logs create-export-task \
  --log-group-name /ecs/dotnet-app \
  --from $(date -d '7 days ago' +%s)000 \
  --to $(date +%s)000 \
  --destination my-log-archive-bucket \
  --destination-prefix logs/dotnet-app/$(date +%Y%m%d)/ \
  --region ap-northeast-1

# エクスポートタスクの状態確認
aws logs describe-export-tasks \
  --region ap-northeast-1
```

---

## バックアップスケジュール

### 日次バックアップ

| 項目 | 実施時刻 | 自動/手動 | 保持期間 |
|------|---------|----------|---------|
| Supabase DB | 3:00 AM JST | 自動 | 7日 |
| CloudWatch Logs | - | 自動 | 7日 |
| Secrets | - | 常時保持 | 無期限 |

### 週次バックアップ

| 項目 | 実施曜日 | 保持期間 |
|------|---------|---------|
| データベース（手動） | 日曜 3:00 AM | 30日 |
| タスク定義 | 日曜 | 30日 |

### 重要イベント前のバックアップ

以下のイベント前には必ず手動バックアップを実施：
- [ ] 大規模なデプロイ前
- [ ] データベーススキーマ変更前
- [ ] インフラ変更前
- [ ] 本番環境の設定変更前

```bash
# 緊急バックアップスクリプト
#!/bin/bash
# emergency-backup.sh

DATE=$(date +%Y%m%d-%H%M%S)
BACKUP_DIR="./backups/$DATE"

mkdir -p $BACKUP_DIR

echo "=== Emergency Backup Started ==="

# 1. Database backup
echo "Backing up database..."
pg_dump "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" | gzip > $BACKUP_DIR/database.sql.gz

# 2. Secrets backup
echo "Backing up secrets..."
aws secretsmanager get-secret-value --secret-id dotnet-app-secrets --region ap-northeast-1 --query 'SecretString' --output text > $BACKUP_DIR/secrets.json

# 3. Task definition backup
echo "Backing up task definition..."
aws ecs describe-task-definition --task-definition dotnet-task --region ap-northeast-1 --query 'taskDefinition' > $BACKUP_DIR/task-definition.json

echo "=== Emergency Backup Completed ==="
echo "Backup location: $BACKUP_DIR"
```

---

## バックアップの検証

### 定期的な検証（月次）

#### 1. データベースバックアップの検証
```bash
# テスト環境にリストア
pg_restore -d test_database backup-20251217.sql.gz

# データ件数確認
psql -d test_database -c "SELECT count(*) FROM users;"
```

#### 2. アプリケーションのリストア検証
```bash
# 前のバージョンにロールバックできることを確認
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --task-definition dotnet-task:14 \
  --region ap-northeast-1
```

#### 3. 設定情報の検証
```bash
# Secrets Managerから値を取得できることを確認
aws secretsmanager get-secret-value \
  --secret-id dotnet-app-secrets \
  --region ap-northeast-1
```

---

## バックアップのモニタリング

### CloudWatch Alarms

```bash
# Supabaseバックアップ失敗アラート（カスタムメトリクスで実装）
# TODO: Supabase APIでバックアップ状態を確認し、メトリクスに送信
```

### 手動確認

#### バックアップ状況の確認スクリプト
```bash
#!/bin/bash
# check-backups.sh

echo "=== Backup Status Check ==="

# 1. Supabase バックアップ確認
echo "1. Supabase Backups:"
echo "   Check manually: https://app.supabase.com/project/[PROJECT-ID]/settings/database"

# 2. S3 バックアップ確認
echo "2. S3 Backups:"
aws s3 ls s3://my-backup-bucket/database/ --region ap-northeast-1

# 3. Secrets Manager 確認
echo "3. Secrets Manager:"
aws secretsmanager describe-secret --secret-id dotnet-app-secrets --region ap-northeast-1 --query '[Name,LastChangedDate]'

# 4. 最新のタスク定義リビジョン
echo "4. Task Definition:"
aws ecs list-task-definitions --family-prefix dotnet-task --region ap-northeast-1 --sort DESC --max-items 5

echo "=== Check Complete ==="
```

---

## バックアップのコスト最適化

### S3 ストレージクラス

古いバックアップは低コストなストレージクラスに移動：

```bash
# ライフサイクルポリシーの設定
aws s3api put-bucket-lifecycle-configuration \
  --bucket my-backup-bucket \
  --lifecycle-configuration file://lifecycle.json

# lifecycle.json
{
  "Rules": [
    {
      "Id": "ArchiveOldBackups",
      "Status": "Enabled",
      "Transitions": [
        {
          "Days": 30,
          "StorageClass": "STANDARD_IA"
        },
        {
          "Days": 90,
          "StorageClass": "GLACIER"
        }
      ],
      "Expiration": {
        "Days": 365
      }
    }
  ]
}
```

### CloudWatch Logs 保持期間

```bash
# 不要なログの保持期間を短縮
aws logs put-retention-policy \
  --log-group-name /ecs/dotnet-app \
  --retention-in-days 7 \
  --region ap-northeast-1
```

---

## ベストプラクティス

### 1. 3-2-1 ルール
- **3**: 3つのコピーを保持
- **2**: 2つの異なるメディアに保存
- **1**: 1つはオフサイトに保管

適用例：
- GitHub（オンライン）
- Supabase自動バックアップ（オンライン）
- S3バックアップ（オンライン、別リージョン可）

### 2. 定期的なリストアテスト
- 月次でバックアップからのリストアをテスト
- テスト環境で実施
- 手順書の更新

### 3. バックアップのドキュメント化
- バックアップ実施記録の保持
- リストア手順の文書化
- 担当者の明確化

### 4. 自動化
- 可能な限りバックアップを自動化
- バックアップ失敗時のアラート設定
- 定期的な検証の自動化

---

## 関連ドキュメント

- [データベースバックアップ](database-backup.md)
- [災害復旧手順](disaster-recovery.md)
- [ロールバック手順](../deployment/rollback.md)

---

**最終更新日**: 2025-12-17
