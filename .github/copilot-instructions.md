# GitHub Copilot Custom Instructions

このリポジトリは .NET 8 / ASP.NET Core Blazor Server アプリケーションです。コンテナ化され、必要に応じて AWS ECS Fargate へデプロイします。

## プロジェクト概要

- **言語**: C# (.NET 8)
- **フレームワーク**: ASP.NET Core Blazor Server
- **デプロイ先**: Docker / AWS ECS Fargate
- **テスト**: xUnit (Unit), Playwright for .NET (E2E)
- **アーキテクチャ**: Razor Pages/Components + Services（DI）

## フォルダ構成

```
/
├── src/
│   └── BlazorApp/
│       ├── Pages/                 # Razor ページ/コンポーネント（ルーティング対象）
│       │   ├── Index.razor
│       │   └── _Host.cshtml
│       ├── Services/              # アプリケーションサービス（ビジネスロジック）
│       │   ├── CalculatorService.cs
│       │   └── OrderService.cs
│       ├── wwwroot/               # 静的アセット
│       ├── Program.cs             # エントリポイント（Middleware/DI）
│       └── BlazorApp.csproj
├── BlazorApp.Tests/               # xUnit 単体テスト
├── BlazorApp.E2ETests/            # Playwright E2E テスト
├── dotnet_container.sln           # ソリューション
├── Dockerfile                     # コンテナビルド（src/BlazorApp を対象）
└── docker-compose.yml             # ローカル起動（5000→5000, BasePath=/dotnet）
```

## コーディング規約

### C# / .NET

1. **Nullable**: `<Nullable>enable</Nullable>` を有効化し、null 安全を徹底
2. **非同期**: 非同期 API を優先し、メソッド名は `Async` サフィックス
3. **DI**: サービスは `I*` インターフェース + 実装を用意し、`Program.cs` で登録
4. **ロギング**: `ILogger<T>` を注入して使用。構造化ログを推奨
5. **命名規則**:
   - クラス/インターフェース: PascalCase（例: `IOrderService`, `CalculatorService`）
   - メソッド/プロパティ: PascalCase（例: `GetTotal`, `PlaceOrderAsync`）
   - 変数/引数: camelCase（例: `orderId`, `options`）
   - private フィールド: `_camelCase`
   - 定数/readonly: `public const`/`static readonly` は PascalCase
6. **例外処理**: サービス層で適切にスローし、UI ではユーザー向けメッセージに変換

### Blazor

1. **ルーティング**: コンポーネントに `@page "/path"` を定義
2. **依存性注入**: `@inject` または `[Inject]` を使用
3. **UI 応答性**: 長時間処理は `await` + 非同期で UI ブロックを避ける
4. **入力検証**: `EditForm` + DataAnnotations を使用
5. **状態管理**: セッション状態は Scoped サービスで管理。`static` の乱用禁止

## 設定 / 環境変数

- **設定ファイル**: `appsettings.json` / `appsettings.Development.json`
- **主要環境変数**:
  - `ASPNETCORE_ENVIRONMENT`（Development/Staging/Production）
  - `ASPNETCORE_URLS`（例: `http://+:8080`。コンテナでの待受に推奨）
- **シークレット管理**:
  - 開発: `dotnet user-secrets` を使用（プロジェクト直下で設定）
  - 本番: AWS Secrets Manager / Parameter Store を使用（リポジトリに含めない）
- **起動時検証**: 重要設定は起動時に検証し、欠落時は明示的に失敗させる

## テスト

1. **Unit Tests (xUnit)**
   - 置き場所: `BlazorApp.Tests/`
   - ファイル名: `*Tests.cs`
   - サービスは DI に過度に依存しない設計で単体テスト可能に

2. **E2E Tests (Playwright for .NET)**
   - 置き場所: `BlazorApp.E2ETests/`
   - 実サーバーを起動して UI の主要シナリオを検証
   - 例: `HomePageTests.cs`, `AccessibilityTests.cs`

## セキュリティ

1. **シークレット**: コード/リポジトリに直書きしない。User Secrets / Secrets Manager を利用
2. **CSP**: 外部スクリプトを追加する場合は CSP の導入を検討
3. **エラーハンドリング**: 例外詳細をユーザーに露出しない（ログにのみ詳細）
4. **入力検証**: フォーム入力は DataAnnotations などで検証

## コンテナ / デプロイ

1. **ローカル**: `Dockerfile` / `docker-compose.yml` で起動
2. **ECS Fargate**:
   - `ASPNETCORE_URLS=http://+:8080` で待受（ALB → タスクへ 8080）
   - ヘルスチェック: `/` または実装した `/healthz`
   - Secrets/Env: タスク定義の `secrets` / `environment` で注入
3. **ログ**: 標準出力へ出力し、CloudWatch Logs に集約

## 新機能の追加方法

1. **サービス追加**: `Services/I[Feature]Service.cs` と `Services/[Feature]Service.cs` を作成
2. **DI 登録**: `Program.cs` に `AddScoped<I[Feature]Service, [Feature]Service>()`
3. **UI 作成**: `Pages/[feature].razor` を追加し `@page` でルートを付与
4. **テスト**: 単体は `BlazorApp.Tests/Services/`、E2E は `BlazorApp.E2ETests/`
5. **ナビ**: 必要に応じて `Pages/Index.razor` へリンクを追加

### 例: 新機能 "Orders"（抜粋）

```csharp
// Services/IOrderService.cs
public interface IOrderService
{
    Task<int> CreateAsync(OrderDto dto, CancellationToken ct = default);
}
```

```csharp
// Program.cs
builder.Services.AddScoped<IOrderService, OrderService>();
```

```razor
@page "/orders"
@inject IOrderService OrderService
<h3>Orders</h3>
```

## 禁止事項

1. `static` な可変グローバル状態の使用
2. 同期的で重い I/O や `Task.Result` / `Wait()` の使用
3. 環境依存値のハードコード（設定/環境変数を使用）
4. 機密情報のコミット
5. UI スレッドをブロックする待機や無限ループ

## Git コミットメッセージ

- フォーマット: `[動詞] [対象] - [詳細]`
- 例:
  - `Add orders page and service`
  - `Fix DI registration for OrderService`
  - `Refactor calculator to async API`
- PR には必ず `Closes #[issue-number]` を含める

## その他

- 当面はモノリシック構成を維持
- UI はシンプルに保つ（不要な複雑化を避ける）
- 機能はサービスとページで疎結合に構成
