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

# 設計書とコードの相互修復・一元管理

## 基本方針

**設計書とコードは常に同期させ、相互に修復する**ことで、ドキュメントとコードの乖離を防ぎます。

### 設計書の場所

すべての設計書は `docs/` ディレクトリに集約されています（このファイルから見た相対パス）。

```
docs/
├── README.md                    # ドキュメント一覧
├── requirements.md              # システム全体の要件定義
├── external-design/             # システム全体の外部設計
├── internal-design/             # システム全体の内部設計
├── operations.md                # 運用設計手順書
├── screen-transition.md         # 画面遷移図
├── features/                    # 機能別設計書 ⭐
│   ├── template/                # 新機能作成時のテンプレート
│   ├── n-plus-one-demo/         # 実装済みサンプル
│   │   ├── README.md
│   │   ├── requirements.md
│   │   ├── external-design.md
│   │   ├── internal-design.md
│   │   └── test-cases.md
│   └── [他の機能]/
└── adr/                         # Architecture Decision Records
    ├── template.md
    ├── 001-use-sqlite-for-education.md
    └── 002-avoid-orm-use-raw-sql.md
```

---

## 設計書 → コード（実装前）

### 新機能を実装する前の手順

1. **機能別設計書を作成**
   ```bash
   # テンプレートをコピー
   cp -r .github/docs/features/template/ .github/docs/features/[機能名]/

   # 各設計書を編集
   # - requirements.md: 要件定義
   # - external-design.md: 画面、API、DB論理設計
   # - internal-design.md: クラス、シーケンス、DB物理設計
   # - test-cases.md: テストケース
   ```

2. **設計書レビュー**
   - 設計書をレビューし、要件が正しいか確認
   - 曖昧な部分は明確にする

3. **実装開始**
   - 設計書に従ってコーディング
   - クラス名、メソッド名、SQL文は設計書と一致させる

4. **参考例**
   - [N+1問題デモ](docs/features/n-plus-one-demo/) を参考にする

---

## コード → 設計書（実装後）

### コードを修正した場合の手順

1. **該当する設計書を特定**
   - 機能別設計書: `docs/features/[機能名]/`
   - システム全体設計: `docs/external-design/` または `docs/internal-design/`

2. **設計書を更新**
   - クラス図、シーケンス図を修正
   - API仕様、テーブル定義を更新
   - 変更履歴を記録

3. **ADRを作成（技術判断が必要な場合）**
   ```bash
   # ADRテンプレートをコピー
   cp .github/docs/adr/template.md .github/docs/adr/003-[決定内容].md

   # 以下を記載
   # - なぜこの技術選定をしたか
   # - メリット・デメリット
   # - 代替案との比較
   ```

---

## 相互修復のチェックリスト

### コード実装時
- [ ] 設計書のクラス名、メソッド名と一致しているか
- [ ] 設計書のSQL文と一致しているか
- [ ] 設計書のシーケンス図通りの処理フローか
- [ ] 設計書のエラーハンドリング方針に従っているか

### コード修正時
- [ ] 設計書を更新したか（クラス図、シーケンス図、API仕様）
- [ ] 変更履歴を記録したか
- [ ] 技術判断が必要な変更は ADR を作成したか

### 設計書作成時
- [ ] テンプレートを使用したか
- [ ] すべてのセクション（要件、外部設計、内部設計、テスト）を記載したか
- [ ] 実装コードと整合性があるか

---

## 設計書とコードの対応表

| 設計書 | コード |
|-------|--------|
| `features/[機能名]/requirements.md` | 実装前に作成 |
| `features/[機能名]/external-design.md` | Controller, View, API, DBスキーマ |
| `features/[機能名]/internal-design.md` | Service, アルゴリズム, SQL文 |
| `features/[機能名]/test-cases.md` | xUnit テスト, Playwright E2E テスト |
| `external-design/api-specification.md` | Controller の API エンドポイント |
| `internal-design/class-design.md` | Service クラス、DTO |
| `internal-design/sequence-diagrams.md` | Controller → Service → DB の処理フロー |
| `internal-design/database-schema.md` | CREATE TABLE 文、初期データ |

---

## 実例: N+1問題デモ

### 設計書
- [要件定義](docs/features/n-plus-one-demo/requirements.md)
- [外部設計](docs/features/n-plus-one-demo/external-design.md) - API仕様、ER図
- [内部設計](docs/features/n-plus-one-demo/internal-design.md) - クラス図、シーケンス図、SQL文
- [テストケース](docs/features/n-plus-one-demo/test-cases.md)

### コード
- `Features/Demo/DemoController.cs` - API エンドポイント
- `Features/Demo/Services/NPlusOneService.cs` - ビジネスロジック
- `Features/Demo/Models/NPlusOneResponse.cs` - DTO
- `Views/Demo/Performance.cshtml` - 画面

### 一致している点
- クラス名: `NPlusOneService` （設計書とコードで一致）
- メソッド名: `GetUsersBad()`, `GetUsersGood()` （設計書とコードで一致）
- SQL文: Bad版は101回クエリ、Good版は1回JOINクエリ （設計書通り）
- シーケンス図: Controller → Service → DB の流れが一致

---

## 禁止事項

1. **設計書なしでコーディング開始** - 必ず設計書を作成してから実装
2. **コード修正後に設計書を更新しない** - コードと設計書の乖離が発生
3. **ADRなしで技術選定** - 将来の判断根拠が不明になる
4. **テンプレートを使わない** - 一貫性が失われる

---

## Git コミット時のチェック

コミット前に以下を確認:

```bash
# 1. 設計書が更新されているか確認
git status .github/docs/

# 2. コードと設計書の両方がコミットに含まれているか確認
git diff --cached

# 3. コミットメッセージに設計書の更新を明記
git commit -m "Add [機能名] feature

- Implement [機能名]Controller and Service
- Update .github/docs/features/[機能名]/
- Add test cases

Refs: .github/docs/features/[機能名]/README.md
"
```

---

## 設計情報のコード埋め込み（XML ドキュメントコメント）

### 基本方針

**設計書の重要情報をコード内のXMLドキュメントコメントに埋め込む**ことで、GitHub Copilot や IDE が設計情報を参照できるようにします。

### XMLドキュメントコメントの書き方

#### クラスレベル

```csharp
/// <summary>
/// N+1問題のデモ実装
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/n-plus-one-demo/internal-design.md</para>
/// <para><strong>責務:</strong> N+1問題のBad版とGood版を実装し、実行時間とクエリ回数を測定する</para>
/// <para><strong>依存関係:</strong></para>
/// <list type="bullet">
/// <item><description>IConfiguration: 接続文字列取得</description></item>
/// <item><description>ILogger&lt;NPlusOneService&gt;: ログ出力</description></item>
/// </list>
/// </remarks>
public class NPlusOneService : INPlusOneService
{
    // ...
}
```

#### メソッドレベル

```csharp
/// <summary>
/// N+1問題版（非効率な実装）
/// </summary>
/// <returns>実行結果（実行時間、クエリ回数、データ）</returns>
/// <remarks>
/// <para><strong>アルゴリズム:</strong></para>
/// <list type="number">
/// <item><description>Stopwatch.Start()</description></item>
/// <item><description>Usersテーブルから全ユーザー取得（1回目のクエリ）</description></item>
/// <item><description>各ユーザーごとにループ: Departmentsテーブルから取得（N回のクエリ）</description></item>
/// <item><description>Stopwatch.Stop()</description></item>
/// <item><description>NPlusOneResponseを生成して返却</description></item>
/// </list>
/// <para><strong>SQL実行回数:</strong> 101回（1回のUsers取得 + 100回のDepartments取得）</para>
/// <para><strong>期待実行時間:</strong> 約45ms</para>
/// </remarks>
public async Task<NPlusOneResponse> GetUsersBad()
{
    // 実装
}
```

#### SQL文をコメントに記載

```csharp
/// <summary>
/// N+1問題版（最適化済み）
/// </summary>
/// <returns>実行結果（実行時間、クエリ回数、データ）</returns>
/// <remarks>
/// <para><strong>アルゴリズム:</strong> JOINで一括取得</para>
/// <para><strong>SQL文:</strong></para>
/// <code>
/// SELECT
///     u.Id,
///     u.Name,
///     u.Email,
///     d.Id AS DeptId,
///     d.Name AS DeptName
/// FROM Users u
/// INNER JOIN Departments d ON u.DepartmentId = d.Id;
/// </code>
/// <para><strong>SQL実行回数:</strong> 1回</para>
/// <para><strong>期待実行時間:</strong> 約12ms</para>
/// </remarks>
public async Task<NPlusOneResponse> GetUsersGood()
{
    // 実装
}
```

---

### XMLドキュメントコメントのメリット

1. **IDE で設計情報が表示される**
   - Visual Studio: マウスホバーで設計情報を確認
   - VS Code: IntelliSense で設計情報を表示

2. **GitHub Copilot が参照できる**
   - コード補完時に設計情報を考慮
   - 設計書と一致したコードを生成

3. **DocFx で API ドキュメント生成**
   - XML コメントから自動的に API ドキュメントを生成
   - 設計書とコードが同期したドキュメント

4. **コードレビュー時に設計意図が明確**
   - PR レビュー時に設計書を開かなくても意図がわかる

---

### 必須項目

すべての public クラス・メソッドに以下を記載:

- [ ] `<summary>`: 簡潔な概要
- [ ] `<remarks>`: 詳細な設計情報
  - [ ] **設計書**: ファイルパス
  - [ ] **責務** または **アルゴリズム**: 処理の流れ
  - [ ] **SQL文**: DB アクセスがある場合
  - [ ] **期待実行時間** または **期待結果**: 性能要件

---

### テンプレート

#### Service クラス

```csharp
/// <summary>
/// [機能名]のビジネスロジック
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/[機能名]/internal-design.md</para>
/// <para><strong>責務:</strong> [責務の説明]</para>
/// <para><strong>依存関係:</strong></para>
/// <list type="bullet">
/// <item><description>IConfiguration: 設定取得</description></item>
/// <item><description>ILogger: ログ出力</description></item>
/// </list>
/// </remarks>
public class [Feature]Service : I[Feature]Service
{
}
```

#### メソッド

```csharp
/// <summary>
/// [メソッドの概要]
/// </summary>
/// <param name="request">リクエストパラメータ</param>
/// <returns>レスポンス</returns>
/// <remarks>
/// <para><strong>アルゴリズム:</strong></para>
/// <list type="number">
/// <item><description>入力検証</description></item>
/// <item><description>データベースアクセス</description></item>
/// <item><description>ビジネスロジック実行</description></item>
/// <item><description>レスポンス生成</description></item>
/// </list>
/// <para><strong>SQL文:</strong></para>
/// <code>
/// SELECT * FROM Table WHERE Id = @Id;
/// </code>
/// </remarks>
public async Task<Response> DoSomething(Request request)
{
}
```

---

### 禁止事項

1. **設計書へのリンクを省略しない** - 必ず `<strong>設計書:</strong>` を記載
2. **SQL文を省略しない** - DB アクセスがある場合は必ず `<code>` で記載
3. **アルゴリズムを省略しない** - 処理フローを必ず `<list>` で記載

---

## まとめ

- **設計書優先**: 実装前に必ず設計書を作成
- **常に同期**: コード修正時は設計書も更新
- **テンプレート活用**: 一貫性を保つためにテンプレートを使用
- **参考例を見る**: [N+1問題デモ](docs/features/n-plus-one-demo/) を参考に
- **ADR作成**: 技術判断は必ず記録
- **XML コメント埋め込み**: 設計情報をコード内に記載し、IDE や Copilot が参照できるようにする ⭐ NEW

この方針により、設計書とコードが常に一致し、将来のメンテナンスが容易になります。

**設計書の場所**: リポジトリルートから見ると `.github/docs/`、このファイル（copilot-instructions.md）から見ると `docs/`



