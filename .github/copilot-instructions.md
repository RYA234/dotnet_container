# GitHub Copilot Custom Instructions

このリポジトリは .NET 8 / ASP.NET Core MVC アプリケーションです。コンテナ化され、必要に応じて AWS ECS Fargate へデプロイします。

## プロジェクト概要

- **言語**: C# (.NET 8)
- **フレームワーク**: ASP.NET Core MVC
- **デプロイ先**: Docker / AWS ECS Fargate
- **テスト**: xUnit (Unit), Playwright for .NET (E2E)
- **アーキテクチャ**: MVC (Model-View-Controller) + Services（DI）
- **DocFx**



## フォルダ構成

**機能ベース（Feature-Based）アーキテクチャを採用しています。**
各機能は独立したフォルダに、Controller/View/Serviceをまとめて配置します。

```
/
├── src/
│   └── BlazorApp/
│       ├── Features/              # 機能別フォルダ（機能ごとに完結）
│       │   ├── Home/
│       │   │   ├── HomeController.cs
│       │   │   └── Views/
│       │   │       └── Index.cshtml
│       │   ├── Calculator/
│       │   │   ├── CalculatorController.cs
│       │   │   ├── CalculatorService.cs      # ICalculatorServiceも含む
│       │   │   └── Views/
│       │   │       └── Index.cshtml
│       │   ├── Orders/
│       │   │   ├── OrdersController.cs
│       │   │   ├── OrderService.cs           # IOrderServiceも含む
│       │   │   ├── PricingService.cs
│       │   │   └── Views/
│       │   │       └── Index.cshtml
│       │   └── Supabase/
│       │       ├── SupabaseService.cs
│       │       └── ISupabaseService.cs
│       ├── Views/                 # 共有ビュー
│       │   ├── Shared/
│       │   │   └── _Layout.cshtml
│       │   ├── _ViewStart.cshtml
│       │   └── _ViewImports.cshtml
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

### ASP.NET Core MVC

1. **ルーティング**:
   - デフォルト: `{controller=Home}/{action=Index}/{id?}`
   - Controller アクションで `[HttpGet]`/`[HttpPost]` 属性を使用
2. **依存性注入**: Controller のコンストラクタでサービスを注入
3. **ビュー**:
   - Razor 構文を使用（`.cshtml` ファイル）
   - Tag Helpers を活用（`asp-controller`, `asp-action` 等）
4. **入力検証**: Model Binding + DataAnnotations を使用
5. **状態管理**:
   - セッション状態は Scoped サービスで管理
   - ViewBag/ViewData/TempData を適切に使い分ける

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

## 新機能の追加方法（機能ベースアーキテクチャ）

1. **機能フォルダ作成**: `Features/[Feature]/` フォルダを作成
2. **Controller 作成**: `Features/[Feature]/[Feature]Controller.cs` を作成
3. **Service 作成**: `Features/[Feature]/[Feature]Service.cs` と `I[Feature]Service.cs` を作成（必要に応じて）
4. **View 作成**: `Features/[Feature]/Views/` フォルダを作成し、必要なビューを追加
5. **DI 登録**: `Program.cs` に `AddScoped<I[Feature]Service, [Feature]Service>()`
6. **テスト**: 単体は `BlazorApp.Tests/Services/`、E2E は `BlazorApp.E2ETests/`
7. **ナビ**: 必要に応じて `Views/Shared/_Layout.cshtml` にリンクを追加

**重要**: 1つの機能に関連するすべてのファイル（Controller、Service、View）は同じフォルダにまとめます。

### 例: 新機能 "Orders"（機能ベース構成）

**フォルダ構造:**
```
Features/
  └── Orders/
      ├── OrdersController.cs       # Controller
      ├── OrderService.cs           # Service (IOrderServiceも含む)
      ├── PricingService.cs         # 関連Service
      └── Views/
          └── Index.cshtml          # View
```

**コード例:**

```csharp
// Features/Orders/OrderService.cs
namespace BlazorApp.Services;

public interface IOrderService
{
    decimal CalculateFinalPrice(Order order);
}

public class OrderService : IOrderService
{
    public decimal CalculateFinalPrice(Order order) { ... }
}
```

```csharp
// Features/Orders/OrdersController.cs
namespace BlazorApp.Features.Orders;

using Microsoft.AspNetCore.Mvc;
using BlazorApp.Services;

public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public IActionResult Calculate(string productName, int quantity, decimal price)
    {
        var order = new Order { ProductName = productName, Quantity = quantity, Price = price };
        ViewBag.FinalPrice = _orderService.CalculateFinalPrice(order);
        return View("Index");
    }
}
```

```cshtml
<!-- Features/Orders/Views/Index.cshtml -->
@{
    ViewData["Title"] = "Orders";
}
<h3>Order Calculator</h3>
<form method="post" asp-action="Calculate">
    <input type="text" name="productName" required />
    <input type="number" name="quantity" required />
    <input type="number" name="price" step="0.01" required />
    <button type="submit">Calculate</button>
</form>
```

```csharp
// Program.cs (DI登録)
builder.Services.AddScoped<IOrderService, OrderService>();
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
