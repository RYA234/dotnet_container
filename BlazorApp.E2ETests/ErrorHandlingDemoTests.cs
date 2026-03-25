using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// エラーハンドリングデモページのE2Eテスト（説明動画用）
/// </summary>
[TestFixture]
public class ErrorHandlingDemoTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";
    private const string DemoUrl = $"{BaseUrl}/Demo/ErrorHandling";

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
    public async Task ErrorHandling_Demo_AllErrorTypes()
    {
        await Page.GotoAsync(DemoUrl);
        await ShowCaptionAsync("エラーハンドリングデモページを開きました", 2000);

        await Page.EvaluateAsync("window.scrollTo(0, 300)");
        await ShowCaptionAsync("5種類のカスタム例外クラスを定義しています", 2000);

        await Page.EvaluateAsync("window.scrollTo(0, 600)");

        // ValidationException
        await ShowCaptionAsync("①「400を発火」ボタンをクリックします（ValidationException）");
        await HighlightAndClickAsync("button[onclick=\"callApi('validation')\"]");
        await Page.WaitForSelectorAsync("#result-area", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        await ShowCaptionAsync("複数フィールドのバリデーションエラーをまとめて返します", 2000);

        // NotFoundException
        await ShowCaptionAsync("②「404を発火」ボタンをクリックします（NotFoundException）");
        await HighlightAndClickAsync("button[onclick=\"callApi('not-found')\"]");
        await Page.WaitForTimeoutAsync(500);
        await ShowCaptionAsync("resourceType・resourceId を含むエラーレスポンスを返します", 2000);

        // BusinessRuleException
        await ShowCaptionAsync("③「400を発火」ボタンをクリックします（BusinessRuleException）");
        await HighlightAndClickAsync("button[onclick=\"callApi('business-rule')\"]");
        await Page.WaitForTimeoutAsync(500);
        await ShowCaptionAsync("与信限度額超過など、技術的には正常でもルール違反の場合です", 2000);

        // InfrastructureException
        await ShowCaptionAsync("④「500を発火」ボタンをクリックします（InfrastructureException）");
        await HighlightAndClickAsync("button[onclick=\"callApi('infrastructure')\"]");
        await Page.WaitForTimeoutAsync(500);
        await ShowCaptionAsync("DB・外部API障害など、システム起因のエラーです", 2000);

        // 予期しない例外
        await ShowCaptionAsync("⑤「500を発火」ボタンをクリックします（予期しない例外）");
        await HighlightAndClickAsync("button[onclick=\"callApi('unexpected')\"]");
        await Page.WaitForTimeoutAsync(500);
        await ShowCaptionAsync("NullReferenceException 等の予期しないエラーも一括ハンドリングします", 2000);

        await Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight / 2)");
        await ShowCaptionAsync("ExceptionHandlingMiddleware が全例外を一括でキャッチします", 2000);

        await ShowCaptionAsync("✅ エラーハンドリングデモ完了", 2000);
    }
}
