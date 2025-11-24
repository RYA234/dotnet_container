# .NET Blazor Server on AWS ECS Fargate

ASP.NET Core 8.0とBlazor Serverを使用したWebアプリケーション。AWS ECS Fargateで動作し、GitHub Actionsで自動デプロイされます。

## 🚀 機能

- **ASP.NET Core 8.0** + **Blazor Server**
- パスベースルーティング対応 (`/dotnet`)
- AWS ECS Fargate上で動作
- GitHub Actionsによる自動デプロイ
- SSL/TLS対応 (ACM証明書)

## 📋 アーキテクチャ

```
GitHub (main push)
  → GitHub Actions
    → Docker Build
      → Amazon ECR
        → Amazon ECS Fargate
          → Application Load Balancer
            → https://rya234.com/dotnet
```

### インフラ構成

- **ECS Cluster**: app-cluster
- **ECS Service**: dotnet-service (Fargate)
- **Target Group**: dotnet-tg (ポート5000)
- **ECR Repository**: dotnet-blazor-app
- **リソース**: CPU 256, メモリ 512MB
- **ネットワーク**: プライベートサブネット
- **ログ**: CloudWatch Logs (`/ecs/dotnet-app`)

## 🛠️ セットアップ

詳細なセットアップ手順は [SETUP.md](SETUP.md) を参照してください。

### クイックスタート

1. GitHubで新しいリポジトリを作成
2. GitHub Secretsを設定:
   - `AWS_ACCESS_KEY_ID`
   - `AWS_SECRET_ACCESS_KEY`
3. このフォルダをプッシュ:
```bash
git init
git remote add origin https://github.com/YOUR_USERNAME/dotnet-blazor-ecs.git
git add .
git commit -m "Initial commit"
git branch -M main
git push -u origin main
```

## 💻 ローカル開発

### Docker Composeで起動

```bash
docker-compose up --build
```

ブラウザで http://localhost:5000/dotnet にアクセス

### .NET SDKで起動

```bash
dotnet run
```

## 🔄 デプロイ

mainブランチにプッシュすると自動的にデプロイされます:

```bash
git add .
git commit -m "Update application"
git push origin main
```

GitHub Actionsのワークフローが:
1. Dockerイメージをビルド
2. ECRにプッシュ
3. ECSタスク定義を更新
4. ECSサービスを再デプロイ

## 🌐 アクセス

**本番環境**: https://rya234.com/dotnet

## 📁 プロジェクト構造

```
dotnet/
├── .github/
│   └── workflows/
│       └── deploy.yml          # GitHub Actionsワークフロー
├── Pages/
│   ├── Index.razor             # メインページ
│   ├── _Host.cshtml            # ホストページ
│   └── _Imports.razor          # インポート設定
├── wwwroot/
│   └── css/
│       └── site.css            # スタイルシート
├── App.razor                   # ルーター設定
├── Program.cs                  # エントリーポイント
├── BlazorApp.csproj            # プロジェクトファイル
├── Dockerfile                  # Dockerビルド設定
├── docker-compose.yml          # ローカル開発用
├── push-to-ecr.ps1             # 手動デプロイスクリプト
├── SETUP.md                    # セットアップガイド
└── README.md                   # このファイル
```

## 🔧 設定

### パスベースルーティング

アプリケーションは `/dotnet` パスで動作します:

```csharp
// Program.cs
app.UsePathBase("/dotnet");
```

### ポート設定

```csharp
// Program.cs (環境変数で設定)
ENV ASPNETCORE_URLS=http://+:5000
```

## 📊 監視

- **CloudWatch Logs**: `/ecs/dotnet-app`
- **ECSサービスメトリクス**: CloudWatchで確認可能
- **ALBターゲットヘルス**: ALBコンソールで確認

## 🐛 トラブルシューティング

### アプリケーションが起動しない

1. CloudWatch Logsを確認:
```bash
aws logs tail /ecs/dotnet-app --follow
```

2. ECSタスクの状態を確認:
```bash
aws ecs describe-services --cluster app-cluster --services dotnet-service
```

3. ターゲットグループのヘルスチェック:
```bash
aws elbv2 describe-target-health --target-group-arn <TARGET_GROUP_ARN>
```

### GitHub Actionsが失敗する

- **Actions**タブでログを確認
- AWS認証情報が正しく設定されているか確認
- IAMユーザーに必要な権限があるか確認 (詳細はSETUP.md参照)

## 📝 ライセンス

MIT License

## 👤 作成者

RYA234

## 🔗 関連リンク

- [インフラリポジトリ](https://github.com/RYA234/my_web_infra)
- [AWS ECS Documentation](https://docs.aws.amazon.com/ecs/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
