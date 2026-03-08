# ロールバック手順

## 概要
デプロイに失敗した場合や、本番環境で重大な問題が発生した場合に、前のバージョンに戻すための手順を説明します。

## ロールバックが必要なケース

### 緊急度：高（即座にロールバック）
- アプリケーションが起動しない
- 重大なセキュリティ脆弱性が発見された
- データ破損のリスクがある
- サービスが完全にダウンしている
- 致命的なバグで全ユーザーに影響

### 緊急度：中（状況を見てロールバック判断）
- 一部機能が動作しない
- パフォーマンスが著しく低下
- エラーログが大量に発生
- 特定のユーザーやケースで問題が発生

### 緊急度：低（ホットフィックスで対応可能）
- UI表示の軽微な問題
- ログレベルの不適切さ
- ドキュメントの誤り

## ロールバック方法

### 方法1: ECS Task Definition版数指定ロールバック（推奨）

最も安全で確実な方法です。

#### ステップ1: 現在のタスク定義確認

```bash
# 現在実行中のタスク定義を確認
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].taskDefinition' \
  --output text

# 結果例: arn:aws:ecs:ap-northeast-1:110221759530:task-definition/dotnet-task:15
# revision 15 が現在のバージョン
```

#### ステップ2: 利用可能なタスク定義の一覧確認

```bash
# 最新10個のタスク定義を表示
aws ecs list-task-definitions \
  --family-prefix dotnet-task \
  --region ap-northeast-1 \
  --sort DESC \
  --max-items 10 \
  --output table
```

#### ステップ3: 前のタスク定義の詳細確認

```bash
# 特定のリビジョンの詳細確認
aws ecs describe-task-definition \
  --task-definition dotnet-task:14 \
  --region ap-northeast-1 \
  --query 'taskDefinition.[family,revision,containerDefinitions[0].image]' \
  --output table
```

#### ステップ4: ロールバック実行

```bash
# 1つ前のバージョンにロールバック
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --task-definition dotnet-task:14 \
  --force-new-deployment \
  --region ap-northeast-1

# 成功メッセージを確認
# "service": "dotnet-service"
# "status": "ACTIVE"
```

#### ステップ5: ロールバック状況の監視

```bash
# デプロイ状況の監視
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].[runningCount,desiredCount,deployments]' \
  --output json

# 旧タスクの停止と新タスクの起動を確認
aws ecs list-tasks \
  --cluster app-cluster \
  --service-name dotnet-service \
  --region ap-northeast-1
```

#### ステップ6: ロールバック完了確認

```bash
# ヘルスチェック
curl -i https://rya234.com/dotnet/healthz

# ログ確認（エラーがないか）
aws logs tail /ecs/dotnet-app \
  --since 5m \
  --region ap-northeast-1 \
  --format short

# アプリケーション動作確認
# 主要機能をブラウザまたはAPIで確認
```

---

### 方法2: ECRイメージタグ指定ロールバック

特定のDockerイメージに戻したい場合に使用します。

#### ステップ1: 利用可能なECRイメージの確認

```bash
# 最新10個のイメージを表示
aws ecr describe-images \
  --repository-name dotnet-app \
  --region ap-northeast-1 \
  --query 'sort_by(imageDetails,& imagePushedAt)[-10:].[imageTags[0],imagePushedAt]' \
  --output table
```

#### ステップ2: ロールバック対象のイメージタグを特定

```bash
# 特定のコミットハッシュのイメージを確認
# 例: abc1234 というコミットのイメージ
aws ecr describe-images \
  --repository-name dotnet-app \
  --region ap-northeast-1 \
  --image-ids imageTag=abc1234
```

#### ステップ3: 新しいタスク定義を作成

```bash
# 現在のタスク定義を取得
aws ecs describe-task-definition \
  --task-definition dotnet-task \
  --region ap-northeast-1 \
  --query 'taskDefinition' > task-definition-rollback.json

# task-definition-rollback.json を編集
# - containerDefinitions[0].image を前のイメージに変更
# - 不要なフィールドを削除（taskDefinitionArn, revision, status, requiresAttributes, compatibilities, registeredAt, registeredBy）

# 編集例
# "image": "110221759530.dkr.ecr.ap-northeast-1.amazonaws.com/dotnet-app:abc1234"
```

#### ステップ4: 新しいタスク定義を登録してデプロイ

```bash
# タスク定義を登録
aws ecs register-task-definition \
  --cli-input-json file://task-definition-rollback.json \
  --region ap-northeast-1

# 新しいタスク定義でサービスを更新
# 登録されたリビジョン番号を確認して使用
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --task-definition dotnet-task:16 \
  --force-new-deployment \
  --region ap-northeast-1
```

---

### 方法3: GitHub Actionsで以前のコミットを再デプロイ

#### ステップ1: ロールバック対象のコミットを特定

```bash
# 最近のコミット履歴を確認
git log --oneline -10

# 特定のコミットの詳細確認
git show <commit-hash>
```

#### ステップ2: ロールバック用のブランチを作成

```bash
# ロールバック対象のコミットにチェックアウト
git checkout <commit-hash>

# 一時的なロールバックブランチを作成
git checkout -b rollback/<commit-hash>

# mainブランチにマージ（force push）
git push origin rollback/<commit-hash>:main --force
```

**警告**: `--force` を使うと履歴が上書きされます。チーム全体に影響があるため、事前に通知が必要です。

#### ステップ3: より安全な方法（Revert）

```bash
# 問題のあるコミットをrevert
git revert <bad-commit-hash>

# revertコミットをプッシュ
git push origin main

# GitHub Actionsが自動的にデプロイ
```

---

## 緊急ロールバック（1分以内）

最速でロールバックする手順です。

```bash
# 1. 前のタスク定義にロールバック（ワンライナー）
aws ecs update-service --cluster app-cluster --service dotnet-service --task-definition dotnet-task:14 --force-new-deployment --region ap-northeast-1

# 2. 状態確認
aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1 --query 'services[0].[runningCount,desiredCount]'

# 3. ヘルスチェック
curl https://rya234.com/dotnet/healthz
```

---

## ロールバック後の対応

### 1. 関係者への通知

```
件名: [緊急] 本番環境ロールバック実施

本番環境でロールバックを実施しました。

- 実施日時: 2025-12-17 15:30 JST
- ロールバック理由: [理由を記載]
- ロールバック前のバージョン: dotnet-task:15
- ロールバック後のバージョン: dotnet-task:14
- 影響範囲: [影響範囲を記載]
- 現在の状況: サービス正常稼働中

詳細は以下のIssueで追跡します:
https://github.com/RYA234/dotnet_container/issues/XXX
```

### 2. 問題の調査

```bash
# ロールバック前のログを保存
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --start-time $(date -d '1 hour ago' +%s)000 \
  --end-time $(date +%s)000 \
  --region ap-northeast-1 > rollback-logs.json

# タスク停止理由の確認
aws ecs describe-tasks \
  --cluster app-cluster \
  --tasks <failed-task-arn> \
  --region ap-northeast-1 \
  --query 'tasks[0].[stoppedReason,containers[0].exitCode]'
```

### 3. GitHub Issueの作成

```bash
# GitHub CLIでIssueを作成
gh issue create \
  --title "ロールバック: [問題の簡潔な説明]" \
  --body "## 発生した問題
[問題の詳細]

## ロールバック実施内容
- ロールバック前: dotnet-task:15
- ロールバック後: dotnet-task:14
- 実施日時: 2025-12-17 15:30 JST

## 次のアクション
- [ ] 問題の根本原因調査
- [ ] 修正PRの作成
- [ ] 再デプロイ計画の策定

## 関連ログ
\`\`\`
[エラーログを添付]
\`\`\`
" \
  --label "bug,rollback,priority:high"
```

### 4. ポストモーテム（事後分析）

問題解決後、以下の項目を含むポストモーテムを作成します：

- **何が起きたか**: 問題の詳細な説明
- **根本原因**: なぜ問題が発生したか
- **影響範囲**: どのユーザー/機能に影響があったか
- **対応タイムライン**: 発見から解決までの時系列
- **対処法**: どのように問題を解決したか
- **再発防止策**: 今後同じ問題を防ぐための対策

---

## ロールバックの検証

### 自動検証スクリプト

```bash
#!/bin/bash
# rollback-verify.sh

CLUSTER="app-cluster"
SERVICE="dotnet-service"
REGION="ap-northeast-1"
HEALTH_URL="https://rya234.com/dotnet/healthz"

echo "=== ロールバック検証開始 ==="

# 1. ECSサービス状態確認
echo "1. ECSサービス状態確認中..."
RUNNING_COUNT=$(aws ecs describe-services \
  --cluster $CLUSTER \
  --services $SERVICE \
  --region $REGION \
  --query 'services[0].runningCount' \
  --output text)

DESIRED_COUNT=$(aws ecs describe-services \
  --cluster $CLUSTER \
  --services $SERVICE \
  --region $REGION \
  --query 'services[0].desiredCount' \
  --output text)

if [ "$RUNNING_COUNT" -eq "$DESIRED_COUNT" ]; then
  echo "✓ ECSサービス正常 (Running: $RUNNING_COUNT, Desired: $DESIRED_COUNT)"
else
  echo "✗ ECSサービス異常 (Running: $RUNNING_COUNT, Desired: $DESIRED_COUNT)"
  exit 1
fi

# 2. ヘルスチェック
echo "2. ヘルスチェック実行中..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ "$HTTP_STATUS" -eq 200 ]; then
  echo "✓ ヘルスチェック成功 (HTTP $HTTP_STATUS)"
else
  echo "✗ ヘルスチェック失敗 (HTTP $HTTP_STATUS)"
  exit 1
fi

# 3. エラーログ確認
echo "3. エラーログ確認中（最近5分間）..."
ERROR_COUNT=$(aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --start-time $(date -d '5 minutes ago' +%s)000 \
  --region $REGION \
  --query 'length(events)' \
  --output text)

if [ "$ERROR_COUNT" -eq 0 ]; then
  echo "✓ エラーログなし"
else
  echo "⚠ エラーログ検出: $ERROR_COUNT 件"
fi

echo "=== ロールバック検証完了 ==="
```

---

## トラブルシューティング

### ロールバックが完了しない

```bash
# デプロイメント状況の確認
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].events[0:5]' \
  --output table

# タスクが起動しない場合、タスク定義を確認
aws ecs describe-task-definition \
  --task-definition dotnet-task:14 \
  --region ap-northeast-1

# ネットワーク設定やセキュリティグループを確認
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].networkConfiguration'
```

### ロールバック後もエラーが続く

前のバージョンにも問題がある場合：

```bash
# さらに古いバージョンにロールバック
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --task-definition dotnet-task:13 \
  --force-new-deployment \
  --region ap-northeast-1

# または、既知の安定バージョンにロールバック
# 例: 2週間前の安定バージョン
```

---

## ベストプラクティス

### 1. ロールバック前の確認
- [ ] 問題の原因を特定（可能な範囲で）
- [ ] ロールバック対象のバージョンが正常動作していたことを確認
- [ ] 関係者に通知
- [ ] データベースマイグレーションの有無を確認

### 2. ロールバック実施時
- [ ] 作業内容を記録（コマンド、結果、時刻）
- [ ] ログを保存
- [ ] 監視を強化

### 3. ロールバック後
- [ ] サービスが正常稼働していることを確認
- [ ] エラーログを監視
- [ ] 関係者に完了を通知
- [ ] GitHub Issueで問題を追跡
- [ ] ポストモーテムを作成

### 4. 再発防止
- テストカバレッジの向上
- ステージング環境での検証強化
- モニタリング・アラートの改善
- ロールバック手順の自動化

---

## 関連ドキュメント

- [自動デプロイ手順](automated-deployment.md)
- [手動デプロイ手順](manual-deployment.md)
- [デプロイチェックリスト](deployment-checklist.md)
- [トラブルシューティング](../troubleshooting/common-issues.md)

---

**最終更新日**: 2025-12-17
