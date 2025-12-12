# .NET ASP.NET Core MVC on AWS ECS Fargate

ASP.NET Core 8.0とMVCを使用したWebアプリケーション。AWS ECS Fargateで動作し、GitHub Actionsで自動デプロイされます。

## 🚀 機能

- **ASP.NET Core 8.0** + **MVC (Model-View-Controller)**
- パスベースルーティング対応 (`/dotnet`)
- AWS ECS Fargate上で動作
- GitHub Actionsによる自動デプロイ
- SSL/TLS対応 (ACM証明書)
- Supabase統合 (開発環境: .env、本番環境: AWS Secrets Manager)

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

### AWS Secrets Manager統合（本番環境）

本番環境（Production）では、環境変数の代わりにAWS Secrets Managerから自動的に認証情報を読み込みます。

**必要な設定:**
1. AWS Secrets Managerにシークレットを作成済み:
   - シークレット名: `ecs/dotnet-container/supabase`
   - 形式: `{"url":"https://...","anon_key":"..."}`

2. ECSタスク実行ロールに権限を付与済み:
   ```json
   {
     "Effect": "Allow",
     "Action": ["secretsmanager:GetSecretValue"],
     "Resource": "arn:aws:secretsmanager:ap-northeast-1:ACCOUNT_ID:secret:ecs/dotnet-container/*"
   }
   ```

アプリケーションは環境に応じて自動的に設定を切り替えます:
- **Development**: `.env`ファイルから読み込み
- **Production**: AWS Secrets Managerから読み込み

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

**機能ベース（Feature-Based）アーキテクチャを採用**
各機能は独立したフォルダに、Controller/View/Serviceをまとめて配置します。

```
/
├── src/
│   └── BlazorApp/
│       ├── Features/                     # 機能別フォルダ（機能ごとに完結）
│       │   ├── Home/
│       │   │   ├── HomeController.cs
│       │   │   └── Views/
│       │   │       └── Index.cshtml      # トップ（/dotnet）
│       │   ├── Calculator/
│       │   │   ├── CalculatorController.cs
│       │   │   ├── CalculatorService.cs
│       │   │   └── Views/
│       │   │       └── Index.cshtml      # /calculator
│       │   ├── Orders/
│       │   │   ├── OrdersController.cs
│       │   │   ├── OrderService.cs
│       │   │   ├── PricingService.cs
│       │   │   └── Views/
│       │   │       └── Index.cshtml      # /orders
│       │   └── Supabase/
│       │       ├── SupabaseService.cs
│       │       └── ISupabaseService.cs
│       ├── Views/                        # 共有ビュー
│       │   ├── Shared/
│       │   │   └── _Layout.cshtml        # 共有レイアウト
│       │   ├── _ViewStart.cshtml
│       │   └── _ViewImports.cshtml
│       ├── wwwroot/
│       │   └── css/site.css
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
├── .env.example
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

## 🎯 DB性能デモ

このアプリケーションには、データベースアクセスの性能問題を体感できるデモAPIが含まれています。

### N+1問題デモ

ORMでよく発生するN+1問題を実際に体験できるAPIエンドポイントです。

#### 📚 N+1問題とは？

N+1問題は、ORMを使用する際に発生する典型的なパフォーマンス問題です：

1. **1回目のクエリ**: 親エンティティ（ユーザー）を取得 → 100件取得
2. **N回のクエリ**: ループ内で各ユーザーの関連エンティティ（部署）を個別に取得 → 100回クエリ発行
3. **合計**: 1 + 100 = **101回のSQL発行** 🐌

この問題は、Eager Loadingで解決できます：
- **Bad**: `Users.ToListAsync()` → ループで `Departments.Find(id)`
- **Good**: `Users.Include(u => u.Department).ToListAsync()` → 1回のJOINクエリ ⚡

#### 🔌 エンドポイント

| エンドポイント | 説明 | クエリ数 |
|---|---|---|
| `GET /api/demo/n-plus-one/bad` | N+1問題あり（非効率） | 101回 |
| `GET /api/demo/n-plus-one/good` | 最適化済み（効率的） | 1回 |

#### 💻 使用方法

```bash
# N+1問題あり（非効率）
curl https://rya234.com/dotnet/api/demo/n-plus-one/bad | jq

# 最適化済み（効率的）
curl https://rya234.com/dotnet/api/demo/n-plus-one/good | jq
```

#### 📊 レスポンス例

**Badエンドポイント**（N+1問題あり）:
```json
{
  "executionTimeMs": 45,
  "sqlCount": 101,
  "dataSize": 8524,
  "rowCount": 100,
  "data": [
    {
      "id": 1,
      "name": "山田太郎0",
      "department": {
        "id": 1,
        "name": "開発部"
      }
    }
  ],
  "message": "N+1問題あり: ループ内で部署情報を100回個別に取得しています（合計101クエリ）"
}
```

**Goodエンドポイント**（最適化済み）:
```json
{
  "executionTimeMs": 12,
  "sqlCount": 1,
  "dataSize": 8524,
  "rowCount": 100,
  "message": "最適化済み: 1回のJOINクエリで全データを取得しています"
}
```

#### 🔍 性能比較

| メトリクス | Bad (N+1) | Good (Eager Loading) | 改善率 |
|---|---|---|---|
| SQL発行回数 | 101回 | 1回 | **99%削減** ✨ |
| 実行時間 | ~45ms | ~12ms | **73%高速化** ⚡ |

**注意**: InMemoryデータベースのため実行時間差は小さいですが、SQL Serverでは数十倍の差が出ることもあります。

#### 🎓 学習ポイント

このデモでは以下を学べます：

1. **N+1問題の発生原因**: ループ内での個別クエリ発行
2. **解決方法**: Entity Framework CoreのInclude()によるEager Loading
3. **性能測定**: SQL発行回数と実行時間の比較
4. **実装例**:
   ```csharp
   // Bad: N+1問題あり
   var users = await context.Users.ToListAsync();
   foreach (var user in users) {
       var dept = await context.Departments.FindAsync(user.DepartmentId);
   }

   // Good: 最適化済み
   var users = await context.Users
       .Include(u => u.Department)
       .ToListAsync();
   ```

#### ⚙️ SQL Server向けセットアップ（オプション）

本番環境でSQL Serverを使用する場合は、以下のSQLファイルを実行してください:

```bash
# 1. テーブル作成
sqlcmd -S your-server -d your-database -i sql/demo/n-plus-one/01_ddl.sql

# 2. ダミーデータ投入（100ユーザー、10部署）
sqlcmd -S your-server -d your-database -i sql/demo/n-plus-one/02_insert.sql
```

**デフォルト設定**: InMemoryデータベースを使用しているため、SQLファイルの実行は不要です。アプリケーション起動時に自動的にダミーデータが生成されます。

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

- **フロントエンド**: Razor Views (MVC)
- **バックエンド**: ASP.NET Core 8.0 MVC
- **単体テスト**: xUnit, Moq, FluentAssertions, Coverlet
- **E2Eテスト**: Playwright for .NET, NUnit
- **コンテナ**: Docker + Docker Compose
- **インフラ**: AWS ECS Fargate
- **CI/CD**: GitHub Actions (OIDC認証)
- **レジストリ**: Amazon ECR
- **ロードバランサー**: Application Load Balancer
- **証明書**: AWS Certificate Manager
- **ログ**: CloudWatch Logs
- **外部サービス**: Supabase (開発: .env、本番: AWS Secrets Manager)

## 📝 ライセンス

MIT License

## 👤 作成者

RYA234

## 🔗 関連リンク

- [インフラリポジトリ](https://github.com/RYA234/my_web_infra)
- [AWS ECS Documentation](https://docs.aws.amazon.com/ecs/)
- [ASP.NET Core MVC Documentation](https://docs.microsoft.com/aspnet/core/mvc/)
- [GitHub Actions OIDC](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-amazon-web-services)
