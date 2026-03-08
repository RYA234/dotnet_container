# データベースバックアップ

## 概要
Supabase PostgreSQLデータベースのバックアップとリストアの詳細手順を説明します。

## Supabase 自動バックアップ

### 自動バックアップ機能
- **頻度**: 日次（毎日自動実行）
- **保持期間**: 最新7日分
- **対象**: データベース全体（スキーマ + データ）
- **復旧ポイント**: 各バックアップ実行時点

### 自動バックアップの確認

1. Supabase Dashboardにログイン
2. プロジェクトを選択
3. Settings → Database → Backups タブ
4. バックアップ履歴を確認

### 自動バックアップからのリストア

1. Supabase Dashboard → Settings → Database → Backups
2. リストアしたいバックアップを選択
3. "Restore" ボタンをクリック
4. 確認ダイアログで承認
5. リストア完了まで待機（数分〜数十分）

**注意**: リストアすると現在のデータが上書きされます。

---

## 手動バックアップ（pg_dump）

### 前提条件

#### PostgreSQLクライアントのインストール

```bash
# Windows (公式インストーラー)
# https://www.postgresql.org/download/windows/

# macOS (Homebrew)
brew install postgresql

# Linux (Ubuntu/Debian)
sudo apt-get install postgresql-client

# バージョン確認
pg_dump --version
```

### 接続情報の取得

Supabase Dashboard → Settings → Database → Connection string

```
Host: db.[PROJECT-REF].supabase.co
Database: postgres
Port: 5432
User: postgres
Password: [YOUR-PASSWORD]
```

### 完全バックアップ

```bash
# 基本的なバックアップ
pg_dump "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > backup.sql

# 日付付きバックアップ
pg_dump "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > backup-$(date +%Y%m%d-%H%M%S).sql

# 圧縮バックアップ（推奨）
pg_dump "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" | gzip > backup-$(date +%Y%m%d-%H%M%S).sql.gz

# カスタム形式（並列リストア可能）
pg_dump -Fc "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > backup-$(date +%Y%m%d-%H%M%S).dump
```

### スキーマのみバックアップ

```bash
# スキーマのみ（データなし）
pg_dump --schema-only "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > schema-only.sql
```

### データのみバックアップ

```bash
# データのみ（スキーマなし）
pg_dump --data-only "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > data-only.sql
```

### 特定テーブルのみバックアップ

```bash
# 単一テーブル
pg_dump -t users "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > users-backup.sql

# 複数テーブル
pg_dump -t users -t orders "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > selected-tables-backup.sql

# スキーマ指定
pg_dump -n public "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" > public-schema-backup.sql
```

---

## バックアップの検証

### バックアップファイルの確認

```bash
# ファイルサイズ確認
ls -lh backup-20251217.sql.gz

# 圧縮ファイルの中身確認（最初の100行）
zcat backup-20251217.sql.gz | head -100

# SQLファイルの中身確認
head -100 backup-20251217.sql

# バックアップに含まれるテーブル一覧
grep "CREATE TABLE" backup-20251217.sql
```

### テストリストア（ローカル）

```bash
# ローカルのPostgreSQLにテストリストア
createdb test_restore
psql test_restore < backup-20251217.sql

# テーブル確認
psql test_restore -c "\dt"

# データ件数確認
psql test_restore -c "SELECT count(*) FROM users;"

# テストDB削除
dropdb test_restore
```

---

## リストア手順

### 完全リストア

**警告**: リストアすると既存のデータが削除されます。必ず事前にバックアップを取得してください。

#### 方法1: psql コマンド

```bash
# 圧縮ファイルからリストア
gunzip < backup-20251217.sql.gz | psql "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres"

# SQLファイルからリストア
psql "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" < backup-20251217.sql
```

#### 方法2: pg_restore（カスタム形式の場合）

```bash
# カスタム形式からリストア
pg_restore -d "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" backup-20251217.dump

# 並列リストア（高速）
pg_restore -j 4 -d "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" backup-20251217.dump

# クリーンリストア（既存オブジェクトを削除してからリストア）
pg_restore --clean -d "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" backup-20251217.dump
```

### 特定テーブルのみリストア

```bash
# 単一テーブルのリストア
pg_restore -t users -d "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" backup-20251217.dump

# スキーマ指定でリストア
pg_restore -n public -d "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" backup-20251217.dump
```

### データのみリストア（スキーマはそのまま）

```bash
# データのみリストア
pg_restore --data-only -d "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" backup-20251217.dump
```

---

## 自動化スクリプト

### 日次バックアップスクリプト

```bash
#!/bin/bash
# daily-backup.sh

# 設定
BACKUP_DIR="/backup/database"
DB_URL="postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres"
RETENTION_DAYS=30
DATE=$(date +%Y%m%d-%H%M%S)
BACKUP_FILE="$BACKUP_DIR/backup-$DATE.sql.gz"

# バックアップディレクトリ作成
mkdir -p $BACKUP_DIR

echo "=== Database Backup Started ==="
echo "Date: $(date)"

# バックアップ実行
pg_dump "$DB_URL" | gzip > "$BACKUP_FILE"

if [ $? -eq 0 ]; then
  echo "✓ Backup successful: $BACKUP_FILE"

  # ファイルサイズ確認
  SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
  echo "  File size: $SIZE"

  # 古いバックアップの削除
  find $BACKUP_DIR -name "backup-*.sql.gz" -mtime +$RETENTION_DAYS -delete
  echo "✓ Old backups deleted (older than $RETENTION_DAYS days)"

  # S3にアップロード（オプション）
  aws s3 cp "$BACKUP_FILE" s3://my-backup-bucket/database/
  echo "✓ Uploaded to S3"

else
  echo "✗ Backup failed"
  exit 1
fi

echo "=== Database Backup Completed ==="
```

### cron設定（Linux/macOS）

```bash
# crontabを編集
crontab -e

# 毎日3時にバックアップ実行
0 3 * * * /path/to/daily-backup.sh >> /var/log/db-backup.log 2>&1
```

### タスクスケジューラ設定（Windows）

```powershell
# PowerShellスクリプト: daily-backup.ps1
$BackupDir = "C:\backup\database"
$Date = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupFile = "$BackupDir\backup-$Date.sql.gz"
$DbUrl = "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres"

# バックアップ実行
pg_dump $DbUrl | gzip > $BackupFile

# タスクスケジューラに登録
$Action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File C:\path\to\daily-backup.ps1"
$Trigger = New-ScheduledTaskTrigger -Daily -At 3am
Register-ScheduledTask -TaskName "DatabaseDailyBackup" -Action $Action -Trigger $Trigger
```

---

## 増分バックアップ（WAL アーカイブ）

Supabaseではプラン次第でWAL（Write-Ahead Logging）アーカイブが利用できます。
これにより、より細かい復旧ポイント（Point-in-Time Recovery）が可能になります。

### Point-in-Time Recovery (PITR)

Supabase Pro プラン以上で利用可能。

**メリット**:
- 任意の時点にリストア可能（秒単位）
- データ損失を最小化

**確認方法**:
Supabase Dashboard → Settings → Database → Point in Time Recovery

---

## バックアップのテスト手順

### 月次バックアップテスト

```bash
#!/bin/bash
# test-backup-restore.sh

echo "=== Backup/Restore Test Started ==="

# 1. 最新バックアップを取得
LATEST_BACKUP=$(ls -t /backup/database/backup-*.sql.gz | head -1)
echo "Testing backup: $LATEST_BACKUP"

# 2. テストDBを作成
TEST_DB="test_restore_$(date +%s)"
echo "Creating test database: $TEST_DB"

# ローカルPostgreSQLにリストア
createdb $TEST_DB
gunzip < $LATEST_BACKUP | psql $TEST_DB > /dev/null 2>&1

if [ $? -eq 0 ]; then
  echo "✓ Restore successful"

  # 3. データ整合性チェック
  TABLE_COUNT=$(psql $TEST_DB -t -c "SELECT count(*) FROM information_schema.tables WHERE table_schema='public';")
  echo "  Tables restored: $TABLE_COUNT"

  USER_COUNT=$(psql $TEST_DB -t -c "SELECT count(*) FROM users;")
  echo "  Users count: $USER_COUNT"

  # 4. テストDB削除
  dropdb $TEST_DB
  echo "✓ Test database cleaned up"

  echo "=== Backup Test PASSED ==="
  exit 0
else
  echo "✗ Restore failed"
  echo "=== Backup Test FAILED ==="
  exit 1
fi
```

---

## トラブルシューティング

### バックアップが失敗する

#### エラー: "connection refused"
```bash
# 接続情報を確認
psql "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" -c "SELECT version();"

# ファイアウォール確認
curl -I db.[PROJECT-REF].supabase.co:5432
```

#### エラー: "password authentication failed"
```bash
# パスワードを再取得
# Supabase Dashboard → Settings → Database → Database Password
```

### リストアが失敗する

#### エラー: "relation already exists"
```bash
# クリーンリストアを実行（既存オブジェクトを削除）
pg_restore --clean -d "postgresql://..." backup.dump
```

#### エラー: "out of memory"
```bash
# 大きなバックアップの場合、分割してリストア
# 1. スキーマのみリストア
pg_restore --schema-only -d "postgresql://..." backup.dump

# 2. データのみリストア（テーブル単位）
pg_restore -t table1 --data-only -d "postgresql://..." backup.dump
pg_restore -t table2 --data-only -d "postgresql://..." backup.dump
```

---

## ベストプラクティス

### 1. バックアップ前の確認
- [ ] ディスク容量が十分にある
- [ ] 接続情報が正しい
- [ ] バックアップ実行権限がある

### 2. バックアップの保存
- [ ] 複数の場所に保存（3-2-1ルール）
- [ ] 暗号化して保存（機密情報を含む場合）
- [ ] S3など冗長性のあるストレージを使用

### 3. 定期的なテスト
- 月次でリストアテストを実施
- リストア手順書を最新に保つ
- 担当者の訓練

### 4. ドキュメント化
- バックアップスケジュールを記録
- 復旧手順を文書化
- バックアップ実施履歴を保持

---

## 関連ドキュメント

- [バックアップ戦略](backup-strategy.md)
- [災害復旧手順](disaster-recovery.md)
- [トラブルシューティング](../troubleshooting/common-issues.md)

---

**最終更新日**: 2025-12-17
