using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// フルテーブルスキャンデモページのE2Eテスト（説明動画用）
/// </summary>
[TestFixture]
public class FullScanDemoTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";
    private const string DemoUrl = $"{BaseUrl}/Demo/FullScan";

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
    public async Task FullScan_Demo_WithoutIndexVsWithIndex()
    {
        await Page.GotoAsync(DemoUrl);
        await ShowCaptionAsync("フルスキャンデモページを開きました", 2000);

        // セットアップ（100万件）
        await ShowCaptionAsync("①「デモデータをセットアップ」をクリックします（100万件生成・時間がかかります）");
        await HighlightAndClickAsync("button[onclick='runSetup()']");
        await Page.WaitForSelectorAsync("#result-setup", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120000
        });
        var setupContent = await Page.Locator("#result-setup-content").TextContentAsync();
        Assert.That(setupContent, Does.Contain("message").Or.Contain("success").Or.Contain("セットアップ"), "セットアップ結果が表示されること");
        await ShowCaptionAsync("100万件のデモデータを生成しました", 2000);

        // メールアドレス入力
        await Page.FillAsync("#emailInput", "user500000@example.com");
        await ShowCaptionAsync("メールアドレスで検索します");
        await Page.WaitForTimeoutAsync(800);

        // インデックスなし検索
        await ShowCaptionAsync("②「インデックスなしで検索」をクリックします（フルスキャン）");
        await HighlightAndClickAsync("button[onclick='runWithoutIndex()']");
        await Page.WaitForSelectorAsync("#result-without-index", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30000
        });
        var withoutContent = await Page.Locator("#result-without-index-content").TextContentAsync();
        Assert.That(withoutContent, Does.Contain("executionTimeMs"), "実行時間が返ること");
        Assert.That(withoutContent, Does.Contain("rowCount"), "rowCount フィールドが返ること");
        Assert.That(withoutContent, Does.Contain("hasIndex"), "hasIndex フィールドが返ること");
        await ShowCaptionAsync("100万件をフルスキャン：時間がかかります", 2000);

        // インデックス作成
        await ShowCaptionAsync("③「インデックスを作成」をクリックします");
        await HighlightAndClickAsync("button[onclick='runCreateIndex()']");
        await Page.WaitForSelectorAsync("#result-create-index", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30000
        });
        var createIndexContent = await Page.Locator("#result-create-index-content").TextContentAsync();
        Assert.That(createIndexContent, Does.Contain("message").Or.Contain("success").Or.Contain("インデックス"), "インデックス作成結果が表示されること");
        await ShowCaptionAsync("メールアドレスカラムにインデックスを作成しました", 2000);

        // インデックスあり検索
        await ShowCaptionAsync("④「インデックスありで検索」をクリックします（高速）");
        await HighlightAndClickAsync("button[onclick='runWithIndex()']");
        await Page.WaitForSelectorAsync("#result-with-index", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30000
        });
        var withContent = await Page.Locator("#result-with-index-content").TextContentAsync();
        Assert.That(withContent, Does.Contain("executionTimeMs"), "実行時間が返ること");
        Assert.That(withContent, Does.Contain("rowCount"), "rowCount フィールドが返ること");
        Assert.That(withContent, Does.Contain("hasIndex"), "hasIndex フィールドが返ること");
        await ShowCaptionAsync("インデックスを使った検索：大幅に高速化されます", 2000);

        await ShowCaptionAsync("✅ フルスキャンデモ完了", 2000);
    }
}
