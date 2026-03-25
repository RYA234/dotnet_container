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

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
        };
    }

    [TearDown]
    public async Task RenameVideoAsync()
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        await Page.CloseAsync();
        var originalPath = await Page.Video!.PathAsync();
        await Page.Video!.SaveAsAsync(Path.Combine("videos", $"{testName}_{timestamp}.webm"));
        if (File.Exists(originalPath))
            File.Delete(originalPath);
    }

    /// <summary>
    /// 動画用字幕を画面左上に表示する
    /// </summary>
    private async Task ShowCaptionAsync(string text, int waitMs = 1500)
    {
        await Page.EvaluateAsync(@"(text) => {
            let div = document.getElementById('e2e-caption');
            if (!div) {
                div = document.createElement('div');
                div.id = 'e2e-caption';
                div.style.cssText = 'position:fixed;top:12px;left:12px;background:rgba(0,0,0,0.75);color:#fff;padding:6px 14px;z-index:99999;font-size:16px;border-radius:4px;font-family:sans-serif;pointer-events:none;';
                document.body.appendChild(div);
            }
            div.textContent = text;
        }", text);
        await Page.WaitForTimeoutAsync(waitMs);
    }

    [Test]
    public async Task HomePage_Loads_Successfully()
    {
        await Page.GotoAsync(BaseUrl);
        await ShowCaptionAsync("ホームページにアクセス中...");

        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(".*Home.*|.*Blazor.*|.*ホーム.*|.*Container.*"));
        await ShowCaptionAsync("ページタイトルを確認中...");

        await ShowCaptionAsync("✅ ページタイトル確認OK", 2000);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = $"screenshots/home-page-loads.png",
            FullPage = true
        });
    }

    [Test]
    public async Task HomePage_DisplaysExpectedContent()
    {
        await Page.GotoAsync(BaseUrl);
        await ShowCaptionAsync("ホームページにアクセス中...");

        var heading = Page.Locator("h1").First;
        await Expect(heading).ToBeVisibleAsync();
        await ShowCaptionAsync("見出し（h1）の表示を確認中...");

        var headingText = await heading.TextContentAsync();
        Assert.That(headingText, Does.Contain("ASP.NET Core MVC").Or.Contain("Welcome").Or.Contain(".NET Container"));
        await ShowCaptionAsync("見出しテキストを確認中...");

        await ShowCaptionAsync("✅ コンテンツ表示確認OK", 2000);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = $"screenshots/home-page-content.png",
            FullPage = true
        });
    }

    [Test]
    public async Task HomePage_ContainsProjectDescription()
    {
        await Page.GotoAsync(BaseUrl);
        await ShowCaptionAsync("ホームページにアクセス中...");

        var content = await Page.ContentAsync();
        Assert.That(content, Does.Contain("migrated").Or.Contain("Calculator").Or.Contain("Orders").Or.Contain("Container"));
        await ShowCaptionAsync("プロジェクト説明セクションを確認中...");

        await ShowCaptionAsync("✅ プロジェクト説明確認OK", 2000);
    }

    [Test]
    public async Task HomePage_DisplaysLinks()
    {
        await Page.GotoAsync(BaseUrl);
        await ShowCaptionAsync("ホームページにアクセス中...");

        var links = Page.Locator("a");
        var count = await links.CountAsync();
        Assert.That(count, Is.GreaterThan(0), "ページに少なくとも1つのリンクが表示されている必要があります");
        await ShowCaptionAsync("リンクの存在を確認中...");

        await ShowCaptionAsync($"✅ リンク確認OK（{count}件）", 2000);
    }

    [Test]
    public async Task HomePage_IsResponsive()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await Page.GotoAsync(BaseUrl);
        await ShowCaptionAsync("モバイルビュー（375x667）でアクセス中...");

        await Expect(Page.Locator("h1").First).ToBeVisibleAsync();
        await ShowCaptionAsync("✅ モバイルビュー表示確認OK", 2000);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = $"screenshots/home-page-mobile.png",
            FullPage = true
        });

        await Page.SetViewportSizeAsync(1920, 1080);
        await Page.ReloadAsync();
        await ShowCaptionAsync("デスクトップビュー（1920x1080）に切り替え中...");

        await Expect(Page.Locator("h1").First).ToBeVisibleAsync();
        await ShowCaptionAsync("✅ デスクトップビュー表示確認OK", 2000);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = $"screenshots/home-page-desktop.png",
            FullPage = true
        });
    }

    [Test]
    public async Task HomePage_ContainsTechnologyStack()
    {
        await Page.GotoAsync(BaseUrl);
        await ShowCaptionAsync("ホームページにアクセス中...");

        var content = await Page.ContentAsync();
        Assert.That(content, Does.Contain("ASP.NET Core MVC").Or.Contain("MVC").Or.Contain(".NET"));
        await ShowCaptionAsync("技術スタックセクションを確認中...");

        await ShowCaptionAsync("✅ 技術スタック確認OK", 2000);
    }
}
