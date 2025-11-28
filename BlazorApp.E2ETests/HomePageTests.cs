using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// ホームページのE2Eテスト
/// Playwrightを使用したブラウザ自動操作テストの例
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class HomePageTests : PageTest
{
    // テスト対象のベースURL（ローカル開発用）
    private const string BaseUrl = "http://localhost:5000/dotnet";

    [Test]
    public async Task HomePage_Loads_Successfully()
    {
        // ホームページにアクセス
        await Page.GotoAsync(BaseUrl);

        // ページタイトルを確認
        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(".*Blazor.*"));
    }

    [Test]
    public async Task HomePage_DisplaysExpectedContent()
    {
        // ホームページにアクセス
        await Page.GotoAsync(BaseUrl);

        // 見出しが表示されていることを確認
        var heading = Page.Locator("h1").First;
        await Expect(heading).ToBeVisibleAsync();

        // 見出しテキストを確認
        var headingText = await heading.TextContentAsync();
        Assert.That(headingText, Does.Contain("基幹システムサンプル").Or.Contain("Blazor"));
    }

    [Test]
    public async Task HomePage_ContainsProjectDescription()
    {
        // ホームページにアクセス
        await Page.GotoAsync(BaseUrl);

        // プロジェクトの目的セクションを確認
        var content = await Page.ContentAsync();
        Assert.That(content, Does.Contain("プロジェクトの目的").Or.Contain("在庫").Or.Contain("物流"));
    }

    [Test]
    public async Task HomePage_DisplaysCards()
    {
        // ホームページにアクセス
        await Page.GotoAsync(BaseUrl);

        // カードコンポーネントが表示されていることを確認
        var cards = Page.Locator(".card");
        var count = await cards.CountAsync();

        // 少なくとも1つのカードが表示されていることを確認
        Assert.That(count, Is.GreaterThan(0), "ページに少なくとも1つのカードが表示されている必要があります");
    }

    [Test]
    public async Task HomePage_IsResponsive()
    {
        // モバイルビューポートでテスト
        await Page.SetViewportSizeAsync(375, 667); // iPhone SE サイズ
        await Page.GotoAsync(BaseUrl);

        // ページが正常に読み込まれることを確認
        await Expect(Page.Locator("h1").First).ToBeVisibleAsync();

        // デスクトップビューポートでテスト
        await Page.SetViewportSizeAsync(1920, 1080);
        await Page.ReloadAsync();

        // ページが正常に読み込まれることを確認
        await Expect(Page.Locator("h1").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task HomePage_ContainsTechnologyStack()
    {
        // ホームページにアクセス
        await Page.GotoAsync(BaseUrl);

        // 技術スタックセクションを確認
        var content = await Page.ContentAsync();
        Assert.That(content, Does.Contain("技術スタック").Or.Contain("Blazor").Or.Contain(".NET"));
    }
}
