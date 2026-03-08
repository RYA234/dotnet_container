# 手動デプロイメント

## 概要
GitHub Actionsを使わずに、手動でDockerイメージをビルドし、AWS ECSにデプロイする手順を説明します。

## いつ手動デプロイを行うか

### 適切なケース
- GitHub Actionsが利用できない場合
- デバッグやテスト目的でのデプロイ
- 緊急対応が必要な場合
- CI/CDパイプラインのトラブルシューティング
- 特定のバージョンを指定してデプロイしたい場合

### 推奨されないケース
- 通常の本番デプロイ（自動デプロイを推奨）
- 頻繁なデプロイ作業（自動化すべき）

## 前提条件

### 必須ツール
```bash
# Docker
docker --version
# Docker version 24.0.0 以上

# AWS CLI
aws --version
# aws-cli/2.0.0 以上

# Git
git --version
# git version 2.0.0 以上
```

### AWS認証情報の設定
```bash
# AWS認証情報の確認
aws sts get-caller-identity

# 認証情報が未設定の場合
aws configure
# AWS Access Key ID: <your-access-key>
# AWS Secret Access Key: <your-secret-key>
# Default region name: ap-northeast-1
# Default output format: json
```

### 必要な権限
- ECR: `PutImage`, `InitiateLayerUpload`, `UploadLayerPart`, `CompleteLayerUpload`
- ECS: `UpdateService`, `DescribeServices`, `DescribeTasks`
- IAM: `PassRole` (ECSタスク実行ロール用)

## 手動デプロイ手順

### ステップ1: 最新コードの取得

```bash
# リポジトリのクローン（初回のみ）
cd C:\Users\mryua\Desktop\github
git clone https://github.com/RYA234/dotnet_container.git
cd dotnet_container

# 既存リポジトリの場合は更新
git checkout main
git pull origin main

# 特定のコミットやタグをデプロイする場合
git checkout <commit-hash-or-tag>
```

### ステップ2: 環境変数の設定

```bash
# Windows (PowerShell)
$env:AWS_REGION = "ap-northeast-1"
$env:ECR_REGISTRY = "110221759530.dkr.ecr.ap-northeast-1.amazonaws.com"
$env:ECR_REPOSITORY = "dotnet-app"
$env:IMAGE_TAG = (git rev-parse --short HEAD)

# Linux/Mac
export AWS_REGION=ap-northeast-1
export ECR_REGISTRY=110221759530.dkr.ecr.ap-northeast-1.amazonaws.com
export ECR_REPOSITORY=dotnet-app
export IMAGE_TAG=$(git rev-parse --short HEAD)
```

### ステップ3: Dockerイメージのビルド

```bash
# ビルドディレクトリに移動
cd C:\Users\mryua\Desktop\github\dotnet_container

# Dockerイメージのビルド
docker build -t dotnet-app:$IMAGE_TAG .

# ビルド成功の確認
docker images | grep dotnet-app

# ローカルでの動作確認（オプション）
docker run -d -p 8080:8080 --name dotnet-test dotnet-app:$IMAGE_TAG

# ヘルスチェック
curl http://localhost:8080/healthz

# テストコンテナの停止と削除
docker stop dotnet-test
docker rm dotnet-test
```

#### ビルドオプション

```bash
# キャッシュを使わずにビルド（クリーンビルド）
docker build --no-cache -t dotnet-app:$IMAGE_TAG .

# マルチステージビルドの特定ステージのみビルド
docker build --target builder -t dotnet-app-builder:$IMAGE_TAG .

# ビルド引数を指定
docker build --build-arg ASPNETCORE_ENVIRONMENT=Production -t dotnet-app:$IMAGE_TAG .
```

### ステップ4: ECRへのログイン

```bash
# ECRへのログイン認証情報を取得
aws ecr get-login-password --region ap-northeast-1 | docker login --username AWS --password-stdin 110221759530.dkr.ecr.ap-northeast-1.amazonaws.com

# ログイン成功確認
# "Login Succeeded" と表示されることを確認
```

#### トラブルシューティング: ECRログイン失敗

```bash
# 認証情報の確認
aws sts get-caller-identity

# ECRリポジトリの存在確認
aws ecr describe-repositories --repository-names dotnet-app --region ap-northeast-1

# IAMポリシーの確認
aws iam list-attached-user-policies --user-name <your-user-name>
```

### ステップ5: イメージのタグ付け

```bash
# タグ付け（SHAハッシュ版）
docker tag dotnet-app:$IMAGE_TAG $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG

# タグ付け（latest版）
docker tag dotnet-app:$IMAGE_TAG $ECR_REGISTRY/$ECR_REPOSITORY:latest

# タグ付け結果の確認
docker images | grep dotnet-app
```

### ステップ6: ECRへのプッシュ

```bash
# SHAハッシュ版をプッシュ
docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG

# latest版をプッシュ
docker push $ECR_REGISTRY/$ECR_REPOSITORY:latest

# プッシュ完了確認
aws ecr describe-images --repository-name dotnet-app --region ap-northeast-1 --query 'sort_by(imageDetails,& imagePushedAt)[-5:]' --output table
```

#### プッシュの進行状況確認

```bash
# プッシュ中のレイヤー確認
docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG | tee push.log

# プッシュ完了後、イメージサイズ確認
aws ecr describe-images \
  --repository-name dotnet-app \
  --image-ids imageTag=$IMAGE_TAG \
  --region ap-northeast-1 \
  --query 'imageDetails[0].imageSizeInBytes' \
  --output text
```

### ステップ7: ECSタスク定義の更新（オプション）

タスク定義を更新する必要がある場合（環境変数、リソース制限の変更など）：

```bash
# 現在のタスク定義を取得
aws ecs describe-task-definition \
  --task-definition dotnet-task \
  --region ap-northeast-1 \
  --query 'taskDefinition' > task-definition.json

# task-definition.json を編集
# - 新しいイメージURIに更新
# - 不要なフィールド（taskDefinitionArn, revision, status等）を削除

# 新しいタスク定義を登録
aws ecs register-task-definition \
  --cli-input-json file://task-definition.json \
  --region ap-northeast-1
```

### ステップ8: ECSサービスの更新

```bash
# 強制的に新しいデプロイを実行
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --force-new-deployment \
  --region ap-northeast-1

# タスク定義のリビジョンを指定してデプロイ
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --task-definition dotnet-task:10 \
  --force-new-deployment \
  --region ap-northeast-1

# デザイアドカウントを変更する場合
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --desired-count 2 \
  --region ap-northeast-1
```

### ステップ9: デプロイ状況の監視

```bash
# サービスの状態確認
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].[serviceName,status,runningCount,desiredCount,deployments]' \
  --output json

# 実行中のタスク一覧
aws ecs list-tasks \
  --cluster app-cluster \
  --service-name dotnet-service \
  --desired-status RUNNING \
  --region ap-northeast-1

# 特定タスクの詳細確認
aws ecs describe-tasks \
  --cluster app-cluster \
  --tasks <task-arn> \
  --region ap-northeast-1

# デプロイ進行状況をリアルタイム監視（1分ごと更新）
watch -n 60 "aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1 --query 'services[0].[runningCount,desiredCount]'"
```

### ステップ10: デプロイ完了確認

```bash
# ヘルスチェック
curl -i https://rya234.com/dotnet/healthz

# 期待されるレスポンス
# HTTP/1.1 200 OK
# Healthy

# ログ確認（最新10分間）
aws logs tail /ecs/dotnet-app --since 10m --region ap-northeast-1 --format short

# アプリケーションバージョン確認（バージョンエンドポイントがある場合）
curl https://rya234.com/dotnet/version
```

## デプロイ完了後のクリーンアップ

### ローカルのDockerイメージ削除

```bash
# 使用していないイメージの削除
docker image prune -a

# 特定のイメージのみ削除
docker rmi dotnet-app:$IMAGE_TAG
docker rmi $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
```

### 古いECRイメージの削除

```bash
# 古いイメージの一覧表示（最新10個以外）
aws ecr describe-images \
  --repository-name dotnet-app \
  --region ap-northeast-1 \
  --query 'sort_by(imageDetails,& imagePushedAt)[:-10].[imageDigest]' \
  --output text

# 古いイメージを削除（手動確認後）
# 注意: latestタグは削除しないこと
aws ecr batch-delete-image \
  --repository-name dotnet-app \
  --region ap-northeast-1 \
  --image-ids imageDigest=<digest>
```

## トラブルシューティング

### ビルドエラー

```bash
# Dockerビルドログの詳細表示
docker build --progress=plain --no-cache -t dotnet-app:$IMAGE_TAG .

# ディスク容量確認
docker system df

# ビルドキャッシュのクリア
docker builder prune -a
```

### プッシュエラー

```bash
# ECR認証の再実行
aws ecr get-login-password --region ap-northeast-1 | docker login --username AWS --password-stdin 110221759530.dkr.ecr.ap-northeast-1.amazonaws.com

# ネットワーク接続確認
curl -I https://110221759530.dkr.ecr.ap-northeast-1.amazonaws.com

# ECRリポジトリのポリシー確認
aws ecr get-repository-policy --repository-name dotnet-app --region ap-northeast-1
```

### デプロイエラー

```bash
# ECSイベントログ確認
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].events[0:10]' \
  --output table

# タスクが停止した理由を確認
aws ecs describe-tasks \
  --cluster app-cluster \
  --tasks <task-arn> \
  --region ap-northeast-1 \
  --query 'tasks[0].stoppedReason'

# CloudWatch Logsでエラー確認
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --region ap-northeast-1 \
  --start-time $(date -d '30 minutes ago' +%s)000
```

## 緊急ロールバック

デプロイに失敗した場合は、すぐに前のバージョンにロールバックしてください。

```bash
# 前のタスク定義リビジョンに戻す
aws ecs update-service \
  --cluster app-cluster \
  --service dotnet-service \
  --task-definition dotnet-task:<previous-revision> \
  --force-new-deployment \
  --region ap-northeast-1
```

詳細は [rollback.md](rollback.md) を参照してください。

## ベストプラクティス

### 1. デプロイ前チェックリスト
- [ ] ローカルでDockerイメージのビルドとテストが成功
- [ ] AWS認証情報が正しく設定されている
- [ ] ECRリポジトリへのアクセス権限がある
- [ ] デプロイ対象のGitコミットが正しい
- [ ] ロールバック手順を確認済み

### 2. セキュリティ
- AWS認証情報をコードにハードコーディングしない
- `.env` ファイルや機密情報をDockerイメージに含めない
- ECRイメージスキャンを有効化

### 3. ドキュメント化
- デプロイ実施日時を記録
- 使用したイメージタグを記録
- 問題が発生した場合は原因と対処法を記録

## 関連ドキュメント

- [自動デプロイ手順](automated-deployment.md)
- [ロールバック手順](rollback.md)
- [デプロイチェックリスト](deployment-checklist.md)
- [トラブルシューティング](../troubleshooting/common-issues.md)

---

**最終更新日**: 2025-12-17
