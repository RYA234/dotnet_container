using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// N+1問題デモページのE2Eテスト（説明動画用）
/// </summary>
[TestFixture]
public class NPlusOneDemoTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";
    private const string DemoUrl = $"{BaseUrl}/Demo/Performance";

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

    private async Task HighlightAndClickAsync(string selector)
    {
        await Page.EvaluateAsync(@"(selector) => {
            const el = document.querySelector(selector);
            if (el) {
                el.style.outline = '3px solid red';
                el.style.outlineOffset = '2px';
            }
        }", selector);
        await Page.WaitForTimeoutAsync(800);
        await Page.ClickAsync(selector);
    }

    [Test]
    public async Task NPlusOne_Demo_BadVsGood()
    {
        await Page.GotoAsync(DemoUrl);
        await ShowCaptionAsync("N+1問題デモページを開きました", 2000);

        await Page.EvaluateAsync("window.scrollTo(0, 300)");
        await ShowCaptionAsync("N+1問題：ユーザー100件取得後、各ユーザーの注文を1件ずつ取得します", 2000);

        // Bad パターン（N+1問題あり）
        await ShowCaptionAsync("①「N+1問題テスト」をクリックします（101クエリ発行）");
        await HighlightAndClickAsync("button[onclick='testNPlusOneBad()']");
        await Page.WaitForSelectorAsync("#result-bad", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        await Page.WaitForSelectorAsync("#result-bad-content", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        var badContent = await Page.Locator("#result-bad-content").TextContentAsync();
        Assert.That(badContent, Does.Contain("sqlCount"), "レスポンスに sqlCount が含まれること");
        Assert.That(badContent, Does.Contain("101"), "N+1問題でクエリ数が 101 であること");
        Assert.That(badContent, Does.Contain("N+1問題あり"), "バッドパターンのメッセージが含まれること");
        await ShowCaptionAsync("101回のSQLクエリが発行されました（1 + 100）", 2000);

        // Good パターン（N+1解消済み）
        await ShowCaptionAsync("②「最適化済みテスト」をクリックします（1クエリ）");
        await HighlightAndClickAsync("button[onclick='testNPlusOneGood()']");
        await Page.WaitForSelectorAsync("#result-good", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        await Page.WaitForSelectorAsync("#result-good-content", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        var goodContent = await Page.Locator("#result-good-content").TextContentAsync();
        Assert.That(goodContent, Does.Contain("sqlCount"), "レスポンスに sqlCount が含まれること");
        Assert.That(goodContent, Does.Contain("最適化済み"), "グッドパターンのメッセージが含まれること");
        await ShowCaptionAsync("JOINを使って1回のSQLクエリに最適化されました", 2000);

        await ShowCaptionAsync("✅ N+1問題デモ完了", 2000);
    }
}
