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

### 前提条件

- AWS インフラが構築済み（[インフラリポジトリ](https://github.com/RYA234/my_web_infra)参照）
  - ECSクラスター、サービス
  - ECRリポジトリ
  - ALB、ターゲットグループ
  - OIDC認証済みのGitHubActionsRole
- GitHub Secretsに `AWS_ACCOUNT_ID` が設定済み

### デプロイ手順

このリポジトリは**アプリケーションコード専用**です。インフラ構築は[インフラリポジトリ](https://github.com/RYA234/my_web_infra)で管理されています。

1. このリポジトリをフォークまたはクローン
2. アプリケーションコードをカスタマイズ
3. `main`ブランチにプッシュすると自動デプロイ

```bash
git clone https://github.com/YOUR_USERNAME/dotnet_container.git
cd dotnet_container
# コードを編集
git add .
git commit -m "Update application"
git push origin main
```

GitHub Actionsが自動的にビルド→ECRプッシュ→ECSデプロイを実行します。

## 💻 ローカル開発

### 環境変数の設定

1. `.env.example`をコピーして`.env`を作成:

```powershell
Copy-Item .env.example .env
```

2. `.env`ファイルを編集してSupabaseの設定を追加:

```ini
Supabase__Url=https://your-project.supabase.co
Supabase__AnonKey=your-anon-key-here
```

### Docker Composeで起動

```powershell
docker compose up -d --build
```

ブラウザ: http://localhost:5000/dotnet

停止:

```powershell
docker compose down
```

### .NET SDKで起動

```powershell
dotnet run --project "src\BlazorApp\BlazorApp.csproj"
```

### Supabase接続テスト

アプリケーション起動後、以下のエンドポイントでSupabase接続を確認できます:

http://localhost:5000/dotnet/supabase/test

## 🧪 テスト

このプロジェクトは単体テストとE2Eテストの両方を含んでいます。

### 単体テストの実行

```bash
# すべての単体テストを実行
dotnet test BlazorApp.Tests/

# カバレッジレポートを生成
dotnet test BlazorApp.Tests/ --collect:"XPlat Code Coverage"

# 詳細な出力でテストを実行
dotnet test BlazorApp.Tests/ --verbosity detailed
```

### E2Eテストの実行

```bash
# アプリケーションを起動
docker-compose up -d

# E2Eテストを実行
dotnet test BlazorApp.E2ETests/

# アプリケーションを停止
docker-compose down
```

### テストプロジェクト

#### 単体テスト (BlazorApp.Tests)
- **フレームワーク**: xUnit、Moq、FluentAssertions
- **サンプルテスト**:
  - `CalculatorServiceTests`: xUnitとFluentAssertionsの使用例
  - `OrderServiceTests`: Moqを使ったモッキングの例

#### E2Eテスト (BlazorApp.E2ETests)
- **フレームワーク**: Playwright for .NET、NUnit
- **サンプルテスト**:
  - `HomePageTests`: ページの読み込み、コンテンツ表示、レスポンシブデザインのテスト
  - `AccessibilityTests`: アクセシビリティ、パフォーマンス、ブラウザ互換性のテスト

### CI/CD統合

- プルリクエスト作成時に自動テスト実行
  - `.github/workflows/test.yml`: 単体テスト
  - `.github/workflows/e2e-test.yml`: E2Eテスト
- mainブランチへのプッシュ前にテスト実行 (`.github/workflows/deploy.yml`)
- テストカバレッジレポートの自動生成

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

**本番環境サンプル**: https://rya234.com/dotnet

（あなたの環境では、ALBのDNS名または独自ドメインでアクセスできます）

## 📁 プロジェクト構造

```
/
├── src/
│   └── BlazorApp/
│       ├── Features/
│       │   ├── Calculator/
│       │   │   ├── CalculatorService.cs
│       │   │   └── Pages/
│       │   │       └── Index.razor       # /calculator
│       │   └── Orders/
│       │       ├── OrderService.cs
│       │       ├── PricingService.cs
│       │       └── Pages/
│       │           └── Index.razor       # /orders
│       ├── Pages/
│       │   ├── Index.razor               # トップ（/dotnet）
│       │   ├── _Host.cshtml
│       │   └── _Imports.razor
│       ├── wwwroot/
│       │   └── css/site.css
│       ├── App.razor
│       ├── Program.cs
│       └── BlazorApp.csproj
├── BlazorApp.Tests/
│   └── Services/
│       ├── CalculatorServiceTests.cs
│       └── OrderServiceTests.cs
├── BlazorApp.E2ETests/
│   ├── HomePageTests.cs
│   ├── AccessibilityTests.cs
│   ├── CalculatorPageTests.cs
│   └── OrdersPageTests.cs
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── dotnet_container.sln
└── README.md
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

### ヘルスチェック（ALB向け）

ターゲットグループのヘルスチェックパスに `/dotnet/healthz` を設定してください。

```csharp
// Program.cs
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
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
- GitHub SecretでAWS_ACCOUNT_IDが正しく設定されているか確認
- GitHubActionsRoleの信頼ポリシーにリポジトリが含まれているか確認
- ECS/ECRリソースが正しくセットアップされているか確認（[インフラリポジトリ](https://github.com/RYA234/my_web_infra)参照）

## 🔐 セキュリティ

このプロジェクトでは以下のセキュリティベストプラクティスを採用しています：

- **OIDC認証**: AWSアクセスキーを保存せず、一時的な認証情報を使用
- **最小権限の原則**: IAMロールは必要最小限の権限のみを付与
- **機密情報の保護**: `.gitignore`で機密ファイルを除外
- **HTTPS通信**: ACM証明書によるSSL/TLS暗号化

## 🎯 技術スタック

- **フロントエンド**: Blazor Server (C#)
- **バックエンド**: ASP.NET Core 8.0
- **単体テスト**: xUnit, Moq, FluentAssertions, Coverlet
- **E2Eテスト**: Playwright for .NET, NUnit
- **コンテナ**: Docker + Docker Compose
- **インフラ**: AWS ECS Fargate
- **CI/CD**: GitHub Actions (OIDC認証)
- **レジストリ**: Amazon ECR
- **ロードバランサー**: Application Load Balancer
- **証明書**: AWS Certificate Manager
- **ログ**: CloudWatch Logs

## 📝 ライセンス

MIT License

## 👤 作成者

RYA234

## 🔗 関連リンク

- [インフラリポジトリ](https://github.com/RYA234/my_web_infra)
- [AWS ECS Documentation](https://docs.aws.amazon.com/ecs/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [GitHub Actions OIDC](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-amazon-web-services)
