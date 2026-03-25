using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// SELECT * デモページのE2Eテスト（説明動画用）
/// </summary>
[TestFixture]
public class SelectStarDemoTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";
    private const string DemoUrl = $"{BaseUrl}/Demo/SelectStar";

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
    public async Task SelectStar_Demo_AllColumnsVsSpecificColumns()
    {
        await Page.GotoAsync(DemoUrl);
        await ShowCaptionAsync("SELECT * デモページを開きました", 2000);

        await ShowCaptionAsync("①「デモデータをセットアップ」をクリックします（1万件生成）");
        await HighlightAndClickAsync("button[onclick='runSetup(this)']");
        await Page.WaitForSelectorAsync("#result-setup", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30000
        });
        var setupContent = await Page.Locator("#result-setup-content").TextContentAsync();
        Assert.That(setupContent, Does.Contain("message").Or.Contain("success").Or.Contain("セットアップ"), "セットアップ結果が表示されること");
        await ShowCaptionAsync("1万件のデモデータを生成しました", 2000);

        // SELECT * 実行
        await ShowCaptionAsync("②「SELECT * で取得」をクリックします（全カラム取得）");
        await HighlightAndClickAsync("button[onclick='runAllColumns(this)']");
        await Page.WaitForSelectorAsync("#result-all", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var allSummary = await Page.Locator("#result-all-summary").TextContentAsync();
        Assert.That(allSummary, Does.Contain("ms"), "実行時間が表示されること");
        Assert.That(allSummary, Does.Contain("KB").Or.Contain("MB").Or.Contain("B"), "データサイズが表示されること");
        await ShowCaptionAsync("全カラムのデータが取得されました（データサイズが大きい）", 2000);

        // 必要カラムのみ実行
        await ShowCaptionAsync("③「必要なカラムのみ取得」をクリックします");
        await HighlightAndClickAsync("button[onclick='runSpecificColumns(this)']");
        await Page.WaitForSelectorAsync("#result-specific", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var specificSummary = await Page.Locator("#result-specific-summary").TextContentAsync();
        Assert.That(specificSummary, Does.Contain("ms"), "実行時間が表示されること");
        Assert.That(specificSummary, Does.Contain("KB").Or.Contain("MB").Or.Contain("B"), "データサイズが表示されること");
        await ShowCaptionAsync("必要なカラムのみ取得されました（データサイズが小さい）", 2000);

        // 比較カード表示確認
        await Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
        await Page.WaitForSelectorAsync("#comparison-card", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var sizeReduction = await Page.Locator("#cmp-size-reduction").TextContentAsync();
        Assert.That(sizeReduction, Is.Not.Null.And.Not.Empty, "サイズ削減率が表示されること");
        await ShowCaptionAsync($"データサイズ削減率: {sizeReduction?.Trim()}", 2000);

        await ShowCaptionAsync("✅ SELECT * デモ完了", 2000);
    }
}
