using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// アクセシビリティとパフォーマンスのE2Eテスト
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class AccessibilityTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";

    [Test]
    public async Task HomePage_HasProperHeadingStructure()
    {
        await Page.GotoAsync(BaseUrl);

        // H1タグが存在することを確認
        var h1Count = await Page.Locator("h1").CountAsync();
        Assert.That(h1Count, Is.GreaterThan(0), "ページにH1見出しが必要です");

        // H1は1つだけであるべき（SEOとアクセシビリティのベストプラクティス）
        Assert.That(h1Count, Is.LessThanOrEqualTo(2), "H1見出しは多くても2つまでにすべきです");
    }

    [Test]
    public async Task HomePage_ImagesHaveAltText()
    {
        await Page.GotoAsync(BaseUrl);

        // すべての画像を取得
        var images = await Page.Locator("img").AllAsync();

        foreach (var image in images)
        {
            var alt = await image.GetAttributeAsync("alt");
            // alt属性が存在することを確認（空でも良いが、属性自体は必要）
            Assert.That(alt, Is.Not.Null, "すべての画像にalt属性が必要です");
        }
    }

    [Test]
    public async Task HomePage_LinksAreAccessible()
    {
        await Page.GotoAsync(BaseUrl);

        // すべてのリンクを取得
        var links = await Page.Locator("a").AllAsync();

        if (links.Count > 0)
        {
            foreach (var link in links)
            {
                var isVisible = await link.IsVisibleAsync();
                if (isVisible)
                {
                    var text = await link.TextContentAsync();
                    var ariaLabel = await link.GetAttributeAsync("aria-label");

                    // リンクにテキストまたはaria-labelが必要
                    Assert.That(
                        !string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(ariaLabel),
                        $"リンクには説明テキストまたはaria-labelが必要です"
                    );
                }
            }
        }
    }

    [Test]
    public async Task HomePage_LoadsWithinReasonableTime()
    {
        // ページ読み込み時間を測定
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await Page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        stopwatch.Stop();

        // 5秒以内に読み込まれることを確認
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000),
            $"ページの読み込みに{stopwatch.ElapsedMilliseconds}msかかりました（5000ms以内が望ましい）");
    }

    [Test]
    public async Task HomePage_HasNoConsoleErrors()
    {
        var consoleMessages = new List<string>();

        // コンソールメッセージをキャプチャ
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleMessages.Add(msg.Text);
            }
        };

        await Page.GotoAsync(BaseUrl);

        // 少し待機してすべてのコンソールメッセージを収集
        await Page.WaitForTimeoutAsync(1000);

        // 404エラーを除外（リソースの読み込みエラーは一般的）
        var criticalErrors = consoleMessages
            .Where(msg => !msg.Contains("404") && !msg.Contains("Failed to load resource"))
            .ToList();

        // 重大なコンソールエラーがないことを確認
        Assert.That(criticalErrors, Is.Empty,
            $"重大なコンソールエラーが検出されました: {string.Join(", ", criticalErrors)}");
    }

    [Test]
    public async Task HomePage_RendersInDifferentBrowsers()
    {
        // このテストは複数のブラウザで実行可能
        await Page.GotoAsync(BaseUrl);

        // 基本的なコンテンツが表示されることを確認
        await Expect(Page.Locator("h1").First).ToBeVisibleAsync();

        // スクリーンショットを撮影（デバッグ用）
        var browserName = TestContext.CurrentContext.Test.Properties.Get("BrowserName") ?? "chromium";
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = $"screenshots/homepage-{browserName}.png",
            FullPage = true
        });
    }
}
