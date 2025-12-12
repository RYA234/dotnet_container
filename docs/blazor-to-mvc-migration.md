# Blazor ServerからASP.NET Core MVCへの移行 - 実践的アーキテクチャ選択の記録

## はじめに

本記事では、AWS ECS Fargate上で稼働していたBlazor Serverアプリケーションを、ASP.NET Core MVCへ全面的に移行した経験を共有します。「なぜBlazorを選んだのか」から「なぜMVCに移行したのか」まで、技術的な判断の過程と実装の詳細を記録します。

## プロジェクト概要

- **環境**: AWS ECS Fargate
- **CI/CD**: GitHub Actions (OIDC認証)
- **外部サービス**: Supabase (AWS Secrets Manager経由)
- **移行前**: .NET 8 / Blazor Server
- **移行後**: .NET 8 / ASP.NET Core MVC (機能ベースアーキテクチャ)

## 目次

1. [Blazor Serverを選んだ理由](#blazor-serverを選んだ理由)
2. [運用中に直面した課題](#運用中に直面した課題)
3. [MVCへの移行を決断した理由](#mvcへの移行を決断した理由)
4. [移行の実装](#移行の実装)
5. [機能ベースアーキテクチャの採用](#機能ベースアーキテクチャの採用)
6. [ハマりポイントと解決策](#ハマりポイントと解決策)
7. [移行後の効果](#移行後の効果)
8. [まとめ](#まとめ)

---

## Blazor Serverを選んだ理由

当初、Blazor Serverを選択した理由は以下の通りでした：

### 1. C#の一貫性
```
フロントエンド: C# (Blazor)
バックエンド: C# (ASP.NET Core)
```

- JavaScriptを書かずに済む
- 型安全性がフロントエンドまで拡張される
- チーム全体がC#のみで開発可能

### 2. リアルタイム性
- SignalRによる自動的なリアルタイム通信
- 状態の自動同期
- イベント駆動のUI更新

### 3. 開発効率
- コンポーネントベースの開発
- 依存性注入(DI)がフロントエンドでも使える
- サーバーサイドロジックに直接アクセス

**初期段階では、これらのメリットが魅力的に見えました。**

---

## 運用中に直面した課題

しかし、実際に運用してみると、いくつかの重大な課題が浮き彫りになりました。

### 1. SignalRの複雑性

```csharp
// Blazor Serverは常にSignalR接続を維持
// ネットワーク断が発生すると...
[JSInvokable]
public async Task OnConnectionLost()
{
    // 再接続ロジックが必要
    // ユーザー体験に影響
}
```

**問題点:**
- WebSocket接続の管理が複雑
- ネットワーク切断時の再接続処理
- ロードバランサー（ALB）との相性問題
- 接続維持のためのリソース消費

### 2. アーキテクチャの特殊性

Blazor Serverは一般的なWebアプリケーションとは異なる動作をします：

```
通常のWeb: HTTP Request → Response (ステートレス)
Blazor Server: WebSocket接続維持 → SignalRでDOM更新 (ステートフル)
```

**これが意味すること:**
- React、Vueなどの標準的なJavaScriptライブラリが使えない
- 既存のWebエコシステムから孤立
- 学習リソースがMVCやReactに比べて少ない
- チームメンバーの習得コストが高い

### 3. WinFormsとの類似性への懸念

```csharp
// Blazorのイベントハンドリング
<button @onclick="OnButtonClick">Click</button>

@code {
    private void OnButtonClick()
    {
        // WinFormsのような書き方...
    }
}
```

これはWinFormsやWPFと似た構造であり、**「Webアプリケーションなのに、デスクトップアプリのように書く」** という違和感がありました。

WinFormsが徐々にレガシー化していく様子を見てきた経験から、Blazor Serverも同じ道を辿るのではないかという不安がありました。

---

## MVCへの移行を決断した理由

最終的に、以下の理由でASP.NET Core MVCへの移行を決断しました。

### 1. 標準的なWebアーキテクチャ

```
HTTP Request → Controller → Model → View → HTTP Response
```

この**単純明快なフロー**は：
- 広く理解されている
- ドキュメントが豊富
- トラブルシューティングが容易
- 新しいメンバーがすぐに理解できる

### 2. ライブラリエコシステムとの互換性

MVC/Razorベースなら：
- React、Vue.js、Alpineなどを自由に組み込める
- 既存のJavaScriptライブラリがそのまま使える
- CDNからの静的リソース読み込みが簡単

```html
<!-- MVCなら普通にJavaScriptライブラリを使える -->
<script src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
```

### 3. シンプルなステート管理

```csharp
// MVCはステートレス - シンプル！
public IActionResult Index()
{
    return View();
}

[HttpPost]
public IActionResult Calculate(int a, int b)
{
    ViewBag.Result = a + b;
    return View("Index");
}
```

- 接続の維持が不要
- サーバーリソースの節約
- スケーリングが容易

### 4. 将来性と保守性

- MVCは**ASP.NET Coreの基盤技術**
- 今後も長期サポートが期待できる
- Blazor Serverよりも成熟したフレームワーク

---

## 移行の実装

### ステップ1: Program.csの変更

**Before (Blazor Server):**
```csharp
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ...

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
```

**After (MVC):**
```csharp
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // 機能ベース構造対応（後述）
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Features/{1}/Views/{0}.cshtml");
        options.ViewLocationFormats.Add("/Features/Shared/Views/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    });

// ...

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

### ステップ2: Blazorコンポーネント → MVC Controller + View

**Before (Blazor Component):**
```razor
@page "/calculator"
@inject ICalculatorService Calculator

<h3>Calculator</h3>
<input @bind="a" type="number" />
<select @bind="op">
    <option value="+">+</option>
</select>
<input @bind="b" type="number" />
<button @onclick="Compute">=</button>
<strong>@resultText</strong>

@code {
    private int a, b;
    private string op = "+";
    private string resultText = string.Empty;

    private void Compute()
    {
        resultText = Calculator.Add(a, b).ToString();
    }
}
```

**After (MVC Controller + View):**

*Controller:*
```csharp
namespace BlazorApp.Features.Calculator;

public class CalculatorController : Controller
{
    private readonly ICalculatorService _calculatorService;

    public CalculatorController(ICalculatorService calculatorService)
    {
        _calculatorService = calculatorService;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public IActionResult Calculate(int a, int b, string op)
    {
        try
        {
            object result = op switch
            {
                "+" => _calculatorService.Add(a, b),
                "-" => _calculatorService.Subtract(a, b),
                "*" => _calculatorService.Multiply(a, b),
                "/" => _calculatorService.Divide(a, b),
                _ => throw new InvalidOperationException($"Unknown operator: {op}")
            };

            ViewBag.A = a;
            ViewBag.B = b;
            ViewBag.Op = op;
            ViewBag.Result = result;
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
        }

        return View("Index");
    }
}
```

*View:*
```cshtml
@{
    ViewData["Title"] = "Calculator";
}

<h3>Calculator</h3>

<form method="post" asp-action="Calculate">
    <input type="number" name="a" value="@ViewBag.A" required />
    <select name="op">
        <option value="+" selected="@(ViewBag.Op == "+")">+</option>
        <option value="-" selected="@(ViewBag.Op == "-")">-</option>
        <option value="*" selected="@(ViewBag.Op == "*")">*</option>
        <option value="/" selected="@(ViewBag.Op == "/")">/</option>
    </select>
    <input type="number" name="b" value="@ViewBag.B" required />
    <button type="submit">=</button>

    @if (ViewBag.Result != null)
    {
        <strong>@ViewBag.Result</strong>
    }

    @if (ViewBag.Error != null)
    {
        <strong style="color: red;">@ViewBag.Error</strong>
    }
</form>
```

### ステップ3: テストの更新

E2Eテストも更新が必要でした。

**Before (Blazor - SignalR前提):**
```csharp
await Page.WaitForSelectorAsync("h3:has-text('Calculator')");
await Page.WaitForSelectorAsync("input[type=number]");
await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded); // Blazor初期化待ち

var firstInput = Page.Locator("input[type=number]").Nth(0);
var secondInput = Page.Locator("input[type=number]").Nth(1);
```

**After (MVC - 標準フォーム):**
```csharp
await Page.WaitForSelectorAsync("h3:has-text('Calculator')");
await Page.WaitForSelectorAsync("input[type=number]");

var firstInput = Page.Locator("input[name=a]");  // name属性で特定
var secondInput = Page.Locator("input[name=b]");
var button = Page.Locator("button[type=submit]");

await button.ClickAsync();
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle); // フォーム送信後のページリロード待ち
```

---

## 機能ベースアーキテクチャの採用

移行と同時に、**機能ベース（Feature-Based）アーキテクチャ**を採用しました。

### 従来のレイヤーベース構造の問題点

```
Controllers/
  ├── HomeController.cs
  ├── CalculatorController.cs
  └── OrdersController.cs
Views/
  ├── Home/
  ├── Calculator/
  └── Orders/
Services/
  ├── CalculatorService.cs
  └── OrderService.cs
```

**問題:**
- Calculator機能を変更する際、3つのフォルダ（Controllers/, Views/, Services/）を行き来する必要がある
- 機能の全体像が把握しにくい
- ファイルが多くなるとナビゲーションが困難

### 機能ベース構造の採用

```
Features/
  ├── Home/
  │   ├── HomeController.cs
  │   └── Views/
  │       └── Index.cshtml
  ├── Calculator/
  │   ├── CalculatorController.cs
  │   ├── CalculatorService.cs      # Controller、Service、Viewが同じフォルダに！
  │   └── Views/
  │       └── Index.cshtml
  └── Orders/
      ├── OrdersController.cs
      ├── OrderService.cs
      ├── PricingService.cs
      └── Views/
          └── Index.cshtml
```

**メリット:**
1. **1機能 = 1フォルダ** で完結
2. 機能の追加・削除が容易
3. チーム開発で機能ごとに担当を分けやすい
4. コードレビューがしやすい

### ViewLocationの設定

MVCはデフォルトで`Views/{Controller}/{Action}.cshtml`を探すため、カスタム設定が必要でした：

```csharp
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Features/{1}/Views/{0}.cshtml");
        options.ViewLocationFormats.Add("/Features/Shared/Views/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    });
```

`{1}` = Controller名、`{0}` = Action名

---

## ハマりポイントと解決策

### 問題1: ECSデプロイで500エラー

**症状:**
```
System.InvalidOperationException: The view 'Index' was not found.
```

**原因:**
機能ベース構造（`Features/Home/Views/Index.cshtml`）にViewを配置したが、MVCがデフォルトの`Views/Home/Index.cshtml`を探していた。

**解決策:**
`AddRazorOptions`でViewLocationFormatsをカスタマイズ（上記参照）。

### 問題2: ヘルスチェック失敗でデプロイが進まない

**症状:**
- 新しいタスクが`unhealthy`
- 古いタスクが終了せず、デプロイが完了しない
- CloudWatch Logsに上記の500エラー

**デバッグ手順:**
```bash
# ECSサービス状態確認
aws ecs describe-services --cluster app-cluster --services dotnet-service

# ターゲットヘルス確認
aws elbv2 describe-target-health --target-group-arn <ARN>

# CloudWatchログ確認
aws logs tail /ecs/dotnet-app --since 20m --filter-pattern "Exception"
```

**結果:**
```json
{
    "TargetId": "10.0.4.212",
    "State": "unhealthy",
    "Reason": "Target.ResponseCodeMismatch",
    "Description": "Health checks failed with these codes: [500]"
}
```

**教訓:**
- デプロイ前にローカルで十分にテスト
- ヘルスチェックエンドポイント（`/healthz`）は必須
- CloudWatch Logsは最初に確認すべき

---

## 移行後の効果

### 1. コードの明確性

**Before (Blazor):**
```razor
@code {
    private void OnClick() { /* ... */ }
}
```
→ いつ実行されるのか？サーバー？クライアント？

**After (MVC):**
```csharp
[HttpPost]
public IActionResult Calculate() { /* ... */ }
```
→ 明らかにサーバーサイド実行

### 2. パフォーマンス

- SignalR接続維持のオーバーヘッド削減
- シンプルなHTTP Request/Responseのみ
- サーバーリソース消費減少

### 3. 開発速度

- 標準的なMVCパターン
- Stack Overflowで解決策がすぐ見つかる
- 新メンバーのオンボーディングが早い

### 4. デプロイの信頼性

- ステートレスなのでスケーリングが容易
- ロードバランサーとの相性が良い
- デプロイ時のダウンタイムなし

---

## まとめ

### Blazor Serverが向いているケース

- **社内ツール**で外部公開しない
- **リアルタイム性が必須**（ダッシュボード、チャットなど）
- チーム全員がC#に精通している
- ユーザー数が限定的

### ASP.NET Core MVCが向いているケース

- **一般的なWebアプリケーション**
- SEOが重要
- **標準的なWebエコシステム**を活用したい
- **React/Vueなどと組み合わせたい**
- **スケーラビリティが重要**

### 移行の教訓

1. **技術選択は初期だけでなく、運用フェーズも考慮する**
   - Blazorの初期開発は早かったが、運用で課題が顕在化

2. **「新しい技術」と「枯れた技術」のバランス**
   - MVCは地味だが、安定性と将来性がある

3. **アーキテクチャは進化させるもの**
   - レイヤーベース → 機能ベースへの進化
   - 状況に応じて最適な構造を選択

4. **ドキュメントとコミュニティの重要性**
   - トラブル時の情報量は技術選択の重要な要素

### 最後に

Blazor Serverは優れた技術ですが、**すべてのケースに最適ではありません**。

今回の移行を通じて、**「標準的で枯れた技術の価値」**を再認識しました。MVCは派手ではありませんが、確実に動作し、長期的にメンテナンスできるアーキテクチャです。

技術選択は常に**トレードオフ**です。プロジェクトの要件、チームのスキル、運用の複雑性を総合的に判断し、**最も適切な技術を選ぶことが重要**だと学びました。

---

## 参考リソース

- [ASP.NET Core MVC Documentation](https://docs.microsoft.com/aspnet/core/mvc/)
- [Feature Slices for ASP.NET Core MVC](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/september/asp-net-core-feature-slices-for-asp-net-core-mvc)
- [GitHub Repository](https://github.com/RYA234/dotnet_container)

---

**執筆日**: 2025年12月7日
**著者**: RYA234
**タグ**: #ASP.NET Core #Blazor #MVC #アーキテクチャ #リファクタリング #AWS ECS
