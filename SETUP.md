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
    ParameterKey=GitHubOrg,ParameterValue=YOUR_GITHUB_USERNAME `
    ParameterKey=GitHubRepo,ParameterValue=YOUR_REPO_NAME `
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
arn:aws:iam::YOUR_AWS_ACCOUNT_ID:role/GitHubActionsRole
```

## 3. GitHub Secretsの設定

GitHubリポジトリの **Settings > Secrets and variables > Actions** で以下のシークレットを追加:

### 必須シークレット:

| Secret名 | 値 | 説明 |
|---------|-----|------|
| `AWS_ACCOUNT_ID` | `123456789012` | あなたのAWSアカウントID（12桁の数字） |

**注意**: アクセスキーやシークレットキーは不要です！OIDC方式では一時的な認証情報が自動的に発行されます。

### AWS情報 (デフォルト設定):

これらの値は [.github/workflows/deploy.yml](.github/workflows/deploy.yml) で設定されています。必要に応じて変更してください。

```
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
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git

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

デプロイ後、Application Load BalancerのDNS名でアクセスできます:
```
http://your-alb-dns-name.ap-northeast-1.elb.amazonaws.com/dotnet
```

独自ドメインを設定している場合は、Route 53でAレコードを作成してください。

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
# 環境変数を設定
$AWS_ACCOUNT_ID = "YOUR_AWS_ACCOUNT_ID"
$AWS_REGION = "ap-northeast-1"
$ECR_REPOSITORY = "dotnet-blazor-app"

# ECRにログイン
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com"

# イメージをビルド
docker build -t $ECR_REPOSITORY .

# タグ付け
docker tag "${ECR_REPOSITORY}:latest" "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/${ECR_REPOSITORY}:latest"

# プッシュ
docker push "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/${ECR_REPOSITORY}:latest"

# ECSサービスを強制更新
aws ecs update-service --cluster app-cluster --service dotnet-service --force-new-deployment --region $AWS_REGION
```
