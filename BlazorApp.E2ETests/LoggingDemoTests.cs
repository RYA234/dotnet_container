using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// ログデモページのE2Eテスト（説明動画用）
/// </summary>
[TestFixture]
public class LoggingDemoTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";
    private const string DemoUrl = $"{BaseUrl}/Demo/Logging";

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
    public async Task Logging_Demo_AllFeatures()
    {
        await Page.GotoAsync(DemoUrl);
        await ShowCaptionAsync("ログデモページを開きました", 2000);

        // ログレベル出力
        await ShowCaptionAsync("①「全レベルのログを出力」をクリックします");
        await HighlightAndClickAsync("button[onclick=\"callApi('levels')\"]");
        await Page.WaitForSelectorAsync("#result-levels", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var levelsContent = await Page.Locator("#result-levels-content").TextContentAsync();
        Assert.That(levelsContent, Does.Contain("message"), "ログレベル出力のレスポンスに message が含まれること");
        await ShowCaptionAsync("Debug / Info / Warning / Error / Critical の5段階のログが出力されました", 2000);

        // パフォーマンスログ（高速）
        await Page.EvaluateAsync("window.scrollTo(0, 400)");
        await ShowCaptionAsync("②「300ms」を設定してパフォーマンスログを出力します（Info レベル）");
        await HighlightAndClickAsync("button[onclick='setElapsed(300)']");
        await HighlightAndClickAsync("button[onclick='callApiPerformance()']");
        await Page.WaitForSelectorAsync("#result-performance", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var fastContent = await Page.Locator("#result-performance-content").TextContentAsync();
        Assert.That(fastContent, Does.Contain("isSlowOperation"), "isSlowOperation が含まれること");
        Assert.That(fastContent, Does.Contain("false"), "300ms は低速操作ではないこと（isSlowOperation: false）");
        await ShowCaptionAsync("300ms は閾値以下なので Info レベルでログ出力されます", 2000);

        // パフォーマンスログ（低速）
        await ShowCaptionAsync("③「1100ms」を設定してパフォーマンスログを出力します（Warning レベル）");
        await HighlightAndClickAsync("button[onclick='setElapsed(1100)']");
        await HighlightAndClickAsync("button[onclick='callApiPerformance()']");
        await Page.WaitForTimeoutAsync(1500);
        await Page.WaitForSelectorAsync("#result-performance", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var slowContent = await Page.Locator("#result-performance-content").TextContentAsync();
        Assert.That(slowContent, Does.Contain("isSlowOperation"), "isSlowOperation が含まれること");
        Assert.That(slowContent, Does.Contain("true"), "1100ms は低速操作なので isSlowOperation: true");
        await ShowCaptionAsync("1100ms は閾値超過のため Warning レベルでログ出力されます", 2000);

        // マスキング
        await Page.EvaluateAsync("window.scrollTo(0, 800)");
        await ShowCaptionAsync("④「password=MySecret123」でマスキングをテストします");
        await HighlightAndClickAsync("button[onclick=\"setMaskInput('password=MySecret123')\"]");
        await HighlightAndClickAsync("button[onclick='callApiMask()']");
        await Page.WaitForSelectorAsync("#result-mask", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var maskContent = await Page.Locator("#result-mask-content").TextContentAsync();
        Assert.That(maskContent, Does.Contain("masked"), "masked フィールドが含まれること");
        Assert.That(maskContent, Does.Contain("***"), "マスク後に *** が含まれること");
        await ShowCaptionAsync("password の値が *** に置換されてログに記録されます", 2000);

        await ShowCaptionAsync("✅ ログデモ完了", 2000);
    }
}
