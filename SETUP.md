# .NET Blazor ECS デプロイメント設定ガイド (OIDC方式)

## 前提条件
- GitHubアカウント
- AWS CLI がインストールされていること
- AWS管理者権限またはCloudFormation実行権限

## 1. 新しいGitHubリポジトリの作成

1. GitHubで新しいリポジトリを作成
   - リポジトリ名: `dotnet_container` (または任意の名前)
   - Public または Private

## 2. AWS側のOIDC設定 (最初に実行)

### CloudFormationスタックのデプロイ

このステップでは、GitHub ActionsがAWSリソースにアクセスするための安全な認証方式（OIDC）を設定します。

```powershell
# CloudFormationスタックを作成
aws cloudformation create-stack `
  --stack-name github-oidc-setup `
  --template-body file://.aws/github-oidc-setup.yml `
  --parameters `
    ParameterKey=GitHubOrg,ParameterValue=RYA234 `
    ParameterKey=GitHubRepo,ParameterValue=dotnet_container `
  --capabilities CAPABILITY_NAMED_IAM `
  --region ap-northeast-1

# スタック作成の完了を待つ（2-3分）
aws cloudformation wait stack-create-complete `
  --stack-name github-oidc-setup `
  --region ap-northeast-1

# IAM RoleのARNを確認
aws cloudformation describe-stacks `
  --stack-name github-oidc-setup `
  --region ap-northeast-1 `
  --query "Stacks[0].Outputs[?OutputKey=='GitHubActionsRoleArn'].OutputValue" `
  --output text
```

**注意**: GitHubのユーザー名とリポジトリ名を自分のものに変更してください。

### スタック作成が完了したら

出力されたIAM RoleのARNをメモしてください。形式は以下のようになります:
```
arn:aws:iam::110221759530:role/GitHubActionsRole
```

## 3. GitHub Secretsの設定

GitHubリポジトリの **Settings > Secrets and variables > Actions** で以下のシークレットを追加:

### 必須シークレット:

| Secret名 | 値 | 説明 |
|---------|-----|------|
| `AWS_ACCOUNT_ID` | `110221759530` | あなたのAWSアカウントID |

**注意**: アクセスキーやシークレットキーは不要です！OIDC方式では一時的な認証情報が自動的に発行されます。

### AWS情報 (確認用):

```
AWS Account ID: 110221759530
AWS Region: ap-northeast-1
ECR Repository: dotnet-blazor-app
ECS Cluster: app-cluster
ECS Service: dotnet-service
ECS Task Definition: dotnet-app
Container Name: web
```

## 4. リポジトリのセットアップ

```powershell
# Gitリポジトリを初期化（まだの場合）
git init

# リモートリポジトリを追加
git remote add origin https://github.com/RYA234/dotnet_container.git

# ファイルを追加
git add .

# 初回コミット
git commit -m "Add OIDC setup for GitHub Actions"

# mainブランチにプッシュ
git branch -M main
git push -u origin main
```

## 5. 自動デプロイの確認

1. mainブランチにプッシュすると自動的にGitHub Actionsが起動
2. **Actions**タブでデプロイの進捗を確認
3. 成功すると、新しいイメージがECRにプッシュされ、ECSサービスが更新される

## 6. アクセス確認

デプロイ後、以下のURLでアクセス:
```
https://rya234.com/dotnet
```

## トラブルシューティング

### GitHub Actionsが失敗する場合

1. **OIDC認証エラー ("Not authorized to perform sts:AssumeRoleWithWebIdentity")**
   - CloudFormationスタックが正常に作成されているか確認
   ```powershell
   aws cloudformation describe-stacks --stack-name github-oidc-setup --region ap-northeast-1
   ```
   - GitHubリポジトリ名とユーザー名がCloudFormationのパラメータと一致しているか確認
   - GitHub Secretsに `AWS_ACCOUNT_ID` が設定されているか確認

2. **ECRへのプッシュエラー**
   - OIDC Providerが正しく作成されているか確認
   - IAM Roleに適切なECR権限があるか確認

3. **ECSデプロイエラー**
   - ECSサービス名、クラスター名が正しいか確認 (app-cluster, dotnet-service)
   - IAM RoleにPassRole権限があるか確認
   - タスク実行ロール (ecs-task-execution-role) が存在するか確認

4. **CloudFormationスタックの削除（再作成が必要な場合）**
   ```powershell
   aws cloudformation delete-stack --stack-name github-oidc-setup --region ap-northeast-1
   ```

## ローカル開発

```bash
# Docker Composeで起動
docker-compose up --build

# ブラウザでアクセス
http://localhost:5000/dotnet
```

## 手動デプロイ (緊急時)

```powershell
# ECRにログイン
aws ecr get-login-password --region ap-northeast-1 | docker login --username AWS --password-stdin 110221759530.dkr.ecr.ap-northeast-1.amazonaws.com

# イメージをビルド
docker build -t dotnet-blazor-app .

# タグ付け
docker tag dotnet-blazor-app:latest 110221759530.dkr.ecr.ap-northeast-1.amazonaws.com/dotnet-blazor-app:latest

# プッシュ
docker push 110221759530.dkr.ecr.ap-northeast-1.amazonaws.com/dotnet-blazor-app:latest

# ECSサービスを強制更新
aws ecs update-service --cluster app-cluster --service dotnet-service --force-new-deployment
```
