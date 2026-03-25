using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// LIKE検索デモページのE2Eテスト（説明動画用）
/// </summary>
[TestFixture]
public class LikeSearchDemoTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";
    private const string DemoUrl = $"{BaseUrl}/Demo/LikeSearch";

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
    public async Task LikeSearch_Demo_PrefixVsPartial()
    {
        await Page.GotoAsync(DemoUrl);
        await ShowCaptionAsync("LIKE検索デモページを開きました", 2000);

        // セットアップ
        await ShowCaptionAsync("①「デモデータをセットアップ」をクリックします（10万件生成）");
        await HighlightAndClickAsync("button[onclick='runSetup(this)']");
        await Page.WaitForSelectorAsync("#result-setup", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60000
        });
        var setupContent = await Page.Locator("#result-setup-content").TextContentAsync();
        Assert.That(setupContent, Does.Contain("message").Or.Contain("success").Or.Contain("セットアップ"), "セットアップ結果が表示されること");
        await ShowCaptionAsync("10万件のデモデータを生成しました", 2000);

        // キーワード入力
        await Page.FillAsync("#keywordInput", "田中");
        await ShowCaptionAsync("キーワード「田中」で検索します");
        await Page.WaitForTimeoutAsync(800);

        // 前方一致検索
        await ShowCaptionAsync("②「前方一致検索（インデックス使用）」をクリックします");
        await HighlightAndClickAsync("button[onclick='runPrefix(this)']");
        await Page.WaitForSelectorAsync("#result-prefix", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var prefixSummary = await Page.Locator("#result-prefix-summary").TextContentAsync();
        Assert.That(prefixSummary, Is.Not.Null.And.Not.Empty, "前方一致の実行結果サマリーが表示されること");
        var prefixSummaryText = await Page.Locator("#result-prefix-summary").TextContentAsync();
        Assert.That(prefixSummaryText, Does.Contain("ms"), "実行時間が表示されること");
        Assert.That(prefixSummaryText, Does.Contain("件"), "件数が表示されること");
        await ShowCaptionAsync($"前方一致: {prefixSummary?.Trim()} ✅ インデックスが使用されます", 2000);

        // 中間一致検索
        await ShowCaptionAsync("③「中間一致検索（フルスキャン）」をクリックします");
        await HighlightAndClickAsync("button[onclick='runPartial(this)']");
        await Page.WaitForSelectorAsync("#result-partial", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var partialSummary = await Page.Locator("#result-partial-summary").TextContentAsync();
        Assert.That(partialSummary, Is.Not.Null.And.Not.Empty, "中間一致の実行結果サマリーが表示されること");
        var partialSummaryText = await Page.Locator("#result-partial-summary").TextContentAsync();
        Assert.That(partialSummaryText, Does.Contain("ms"), "実行時間が表示されること");
        await ShowCaptionAsync($"中間一致: {partialSummary?.Trim()} ❌ フルスキャンになります", 2000);

        // 比較カード
        await Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
        await Page.WaitForSelectorAsync("#comparison-card", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var prefixIndex = await Page.Locator("#cmp-prefix-index").TextContentAsync();
        var partialIndex = await Page.Locator("#cmp-partial-index").TextContentAsync();
        Assert.That(prefixIndex, Does.Contain("✅"), "前方一致はインデックス使用と表示されること");
        Assert.That(partialIndex, Does.Contain("❌"), "中間一致はインデックス未使用と表示されること");
        await ShowCaptionAsync("前方一致はインデックスを活用、中間一致（%keyword%）はフルスキャンになります", 2000);

        await ShowCaptionAsync("✅ LIKE検索デモ完了", 2000);
    }
}
