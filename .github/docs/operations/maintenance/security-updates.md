# セキュリティアップデート

## 概要
セキュリティ脆弱性への対応とアップデート手順を定義します。

## セキュリティアップデートポリシー

### 対応レベル

| 深刻度 | 対応期限 | 対応内容 |
|-------|---------|---------|
| Critical | 24時間以内 | 即座にパッチ適用 |
| High | 3日以内 | 計画的にパッチ適用 |
| Medium | 1週間以内 | 次回定期メンテナンスで対応 |
| Low | 1ヶ月以内 | 計画的に対応 |

---

## 依存関係の脆弱性チェック

### .NET パッケージの脆弱性確認

```bash
# プロジェクトディレクトリで実行
dotnet list package --vulnerable

# 出力例:
# The following sources were used:
#   https://api.nuget.org/v3/index.json
#
# Project `DotNetApp` has the following vulnerable packages
#    [net8.0]:
#    Top-level Package      Requested   Resolved   Severity   Advisory URL
#    > Newtonsoft.Json      12.0.1      12.0.1     High       https://...
```

### 脆弱性レポートの詳細確認

```bash
# 特定パッケージの詳細
dotnet list package --vulnerable --include-transitive

# JSON形式で出力
dotnet list package --vulnerable --format json > vulnerabilities.json
```

---

## アップデート手順

### ステップ1: 脆弱性の評価

```bash
# 1. 脆弱性一覧を取得
dotnet list package --vulnerable > vulnerabilities.txt

# 2. 各脆弱性のCVE情報を確認
# https://nvd.nist.gov/vuln/search

# 3. 影響範囲を評価
# - 使用している機能に影響があるか
# - 本番環境で exploitable か
# - 回避策があるか
```

### ステップ2: テスト環境でのアップデート

```bash
# ローカル環境で更新
dotnet add package <PackageName> --version <NewVersion>

# ビルド確認
dotnet build

# テスト実行
dotnet test

# ローカルで動作確認
dotnet run
```

### ステップ3: コードレビューとテスト

```bash
# 変更をコミット
git checkout -b security/update-package-name
git add .
git commit -m "security: Update <PackageName> to fix CVE-XXXX-XXXXX"
git push origin security/update-package-name

# Pull Request作成
gh pr create \
  --title "Security: Update <PackageName> to fix CVE-XXXX-XXXXX" \
  --body "## Security Update

### Vulnerability
- CVE: CVE-XXXX-XXXXX
- Severity: High
- Package: <PackageName> v1.0.0 → v1.0.1

### Changes
- Updated <PackageName> from v1.0.0 to v1.0.1
- All tests passing
- No breaking changes

### Testing
- [x] Unit tests passed
- [x] Integration tests passed
- [x] Local testing completed

### References
- https://nvd.nist.gov/vuln/detail/CVE-XXXX-XXXXX
"
```

### ステップ4: デプロイ

```bash
# PR承認後、mainにマージ
gh pr merge <PR-number> --squash

# 自動デプロイが実行される（GitHub Actions）
# または、手動デプロイ
```

### ステップ5: 動作確認

```bash
# デプロイ後の確認
curl -i https://rya234.com/dotnet/healthz

# ログ確認
aws logs tail /ecs/dotnet-app --since 10m --region ap-northeast-1 --format short

# エラーがないことを確認
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --start-time $(date -d '10 minutes ago' +%s)000 \
  --region ap-northeast-1
```

---

## .NET SDK のアップデート

### 現在のバージョン確認

```bash
dotnet --version
# 8.0.100
```

### アップデート手順

```bash
# 1. 新しい .NET SDK をダウンロード
# https://dotnet.microsoft.com/download

# 2. Dockerfileを更新
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# ↓
FROM mcr.microsoft.com/dotnet/sdk:8.0.x AS build  # 最新パッチバージョン

# 3. ローカルでビルドテスト
docker build -t dotnet-app:test .

# 4. テスト実行
docker run -d -p 8080:8080 --name test dotnet-app:test
curl http://localhost:8080/healthz
docker stop test && docker rm test

# 5. コミット＆デプロイ
git add Dockerfile
git commit -m "chore: Update .NET SDK to 8.0.x"
git push origin main
```

---

## Supabase のセキュリティ

### API キーのローテーション

```bash
# 1. Supabase Dashboardで新しいキーを生成
# Settings → API → Generate new key

# 2. Secrets Managerを更新
aws secretsmanager update-secret \
  --secret-id dotnet-app-secrets \
  --secret-string '{
    "SupabaseUrl": "https://xxx.supabase.co",
    "SupabaseAnonKey": "NEW-ANON-KEY",
    "SupabaseServiceKey": "NEW-SERVICE-KEY"
  }' \
  --region ap-northeast-1

# 3. ECSタスクを再起動（新しいシークレットを読み込むため）
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --force-new-deployment \
  --region ap-northeast-1

# 4. 動作確認
curl https://rya234.com/dotnet/healthz

# 5. 古いキーを無効化
# Supabase Dashboard → Settings → API → Revoke old key
```

### データベースパスワードの変更

```bash
# 1. Supabase Dashboardでパスワード変更
# Settings → Database → Reset database password

# 2. 接続文字列を更新
# 新しいパスワードで接続文字列を生成

# 3. Secrets Managerを更新
aws secretsmanager update-secret \
  --secret-id dotnet-app-secrets \
  --secret-string file://new-secrets.json \
  --region ap-northeast-1

# 4. ECSタスクを再起動
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --force-new-deployment \
  --region ap-northeast-1
```

---

## AWS リソースのセキュリティ

### IAM ポリシーの定期レビュー

```bash
# 1. 現在のポリシーを確認
aws iam list-attached-role-policies \
  --role-name ecsTaskExecutionRole \
  --region ap-northeast-1

# 2. 各ポリシーの内容を確認
aws iam get-role-policy \
  --role-name ecsTaskExecutionRole \
  --policy-name <PolicyName> \
  --region ap-northeast-1

# 3. 不要な権限を削除
# 最小権限の原則に従い、必要最小限の権限のみ付与
```

### セキュリティグループの監査

```bash
# セキュリティグループのルールを確認
aws ec2 describe-security-groups \
  --group-ids <security-group-id> \
  --region ap-northeast-1

# 不要なルールがないか確認:
# - 0.0.0.0/0 からのアクセス許可（必要最小限に）
# - 使われていないポート
# - 重複したルール
```

### CloudTrail ログの確認

```bash
# 最近のAPIコールを確認
aws cloudtrail lookup-events \
  --lookup-attributes AttributeKey=EventName,AttributeValue=UpdateService \
  --start-time $(date -d '7 days ago' +%Y-%m-%dT%H:%M:%S) \
  --region ap-northeast-1

# 異常なアクセスパターンを検出
# - 不正なIPからのアクセス
# - 権限エラー（AccessDenied）の頻発
# - 通常と異なる時間帯のアクセス
```

---

## セキュリティスキャン

### Docker イメージスキャン

```bash
# ECRの脆弱性スキャンを有効化
aws ecr put-image-scanning-configuration \
  --repository-name dotnet-app \
  --image-scanning-configuration scanOnPush=true \
  --region ap-northeast-1

# スキャン結果の確認
aws ecr describe-image-scan-findings \
  --repository-name dotnet-app \
  --image-id imageTag=latest \
  --region ap-northeast-1

# 脆弱性があれば修正
# 1. 基本イメージを最新に更新
# 2. 脆弱なパッケージをアップデート
# 3. 再ビルド・再デプロイ
```

### 静的コード解析

```bash
# Security Code Scan for .NET
# https://security-code-scan.github.io/

# NuGetパッケージとして追加
dotnet add package SecurityCodeScan.VS2019

# ビルド時に自動スキャン
dotnet build

# 警告を確認し、修正
```

---

## セキュリティインシデント対応

### インシデント発生時の対応フロー

```
1. インシデント検知
    ↓
2. 影響範囲の特定
    ↓
3. 即座の対応（サービス停止等）
    ↓
4. 調査開始
    ↓
5. 根本原因の特定
    ↓
6. 修正・パッチ適用
    ↓
7. サービス再開
    ↓
8. 事後報告書作成
    ↓
9. 再発防止策の策定
```

### インシデント対応チェックリスト

```markdown
## セキュリティインシデント対応

### 即座の対応
- [ ] サービスを停止（必要な場合）
- [ ] 影響範囲を特定
- [ ] 関係者に通知
- [ ] 証拠を保全（ログ、スナップショット）

### 調査
- [ ] アクセスログの解析
- [ ] 異常なトラフィックの確認
- [ ] データ漏洩の有無確認
- [ ] 攻撃手法の特定

### 対策
- [ ] 脆弱性の修正
- [ ] パスワード・キーの変更
- [ ] セキュリティグループの見直し
- [ ] WAFルールの追加

### 事後対応
- [ ] 事後報告書の作成
- [ ] ユーザーへの報告（必要な場合）
- [ ] 再発防止策の実施
- [ ] セキュリティポリシーの見直し
```

---

## 定期的なセキュリティレビュー

### 月次セキュリティチェック

```markdown
## 月次セキュリティチェックリスト

### 依存関係
- [ ] .NETパッケージの脆弱性確認
- [ ] Dockerベースイメージの更新確認
- [ ] OSパッケージの更新確認

### AWS リソース
- [ ] IAMポリシーのレビュー
- [ ] セキュリティグループのレビュー
- [ ] CloudTrailログの監査
- [ ] Secrets Managerのローテーション

### アプリケーション
- [ ] 静的コード解析の実施
- [ ] セキュリティスキャンの実施
- [ ] アクセスログの監査

### 外部サービス
- [ ] Supabase APIキーの確認
- [ ] SSL証明書の有効期限確認
- [ ] 外部API認証情報の確認
```

---

## セキュリティアップデートの自動化

### Dependabot の設定

`.github/dependabot.yml`:
```yaml
version: 2
updates:
  # .NET dependencies
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    labels:
      - "dependencies"
      - "security"
    open-pull-requests-limit: 10

  # Docker dependencies
  - package-ecosystem: "docker"
    directory: "/"
    schedule:
      interval: "weekly"
    labels:
      - "dependencies"
      - "docker"
```

### GitHub Security Alerts の有効化

1. GitHub Repository → Settings → Security → Enable Dependabot alerts
2. Enable Dependabot security updates
3. アラートが来たら速やかに対応

---

## 関連ドキュメント

- [定期メンテナンス](routine-maintenance.md)
- [トラブルシューティング](../troubleshooting/common-issues.md)
- [災害復旧手順](../backup-recovery/disaster-recovery.md)

---

**最終更新日**: 2025-12-17
