using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

/// <summary>
/// バリデーションデモページのE2Eテスト（説明動画用）
/// </summary>
[TestFixture]
public class ValidationDemoTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";
    private const string DemoUrl = $"{BaseUrl}/Demo/Validation";

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

    private async Task SubmitAndAssertAsync(string expectedStatus, string expectedCode)
    {
        // 前の結果を隠してから送信することで、新しいレスポンスを確実に待てる
        await Page.EvaluateAsync("document.getElementById('result-area').style.display = 'none'");
        await HighlightAndClickAsync("button[onclick='submitOrder()']");
        await Page.WaitForSelectorAsync("#result-area", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var status = await Page.Locator("#result-status").TextContentAsync();
        Assert.That(status?.Trim(), Is.EqualTo(expectedStatus), $"HTTP status が {expectedStatus} であること");
        var content = await Page.Locator("#result-content").TextContentAsync();
        Assert.That(content, Does.Contain(expectedCode), $"レスポンスに '{expectedCode}' が含まれること");
    }

    [Test]
    public async Task Validation_Demo_AllScenarios()
    {
        await Page.GotoAsync(DemoUrl);
        await ShowCaptionAsync("バリデーションデモページを開きました", 2000);
        await Page.ClickAsync("button[onclick='resetDemo()']");
        await Page.WaitForTimeoutAsync(500);

        // 正常系
        await ShowCaptionAsync("①「正常データ」シナリオで注文登録します");
        await HighlightAndClickAsync("button[onclick=\"setScenario('valid')\"]");
        await Page.WaitForTimeoutAsync(500);
        await SubmitAndAssertAsync("200", "注文が正常に登録されました");
        await ShowCaptionAsync("200 OK：注文が正常に登録されました", 2000);

        // 必須項目空
        await ShowCaptionAsync("②「必須フィールド空」シナリオ（ValidationException 400）");
        await HighlightAndClickAsync("button[onclick=\"setScenario('empty')\"]");
        await Page.WaitForTimeoutAsync(500);
        await SubmitAndAssertAsync("400", "VALIDATION_ERROR");
        await ShowCaptionAsync("400 Bad Request：複数フィールドのバリデーションエラーをまとめて返します", 2000);

        // メール形式不正
        await ShowCaptionAsync("③「メール形式不正」シナリオ（ValidationException 400）");
        await HighlightAndClickAsync("button[onclick=\"setScenario('invalid-email')\"]");
        await Page.WaitForTimeoutAsync(500);
        await SubmitAndAssertAsync("400", "VALIDATION_ERROR");
        await ShowCaptionAsync("400 Bad Request：メールアドレス形式のエラーが返ります", 2000);

        // 存在しない顧客
        await ShowCaptionAsync("④「存在しない顧客コード」シナリオ（NotFoundException 404）");
        await HighlightAndClickAsync("button[onclick=\"setScenario('unknown-customer')\"]");
        await Page.WaitForTimeoutAsync(500);
        await Page.EvaluateAsync("document.getElementById('result-area').style.display = 'none'");
        await HighlightAndClickAsync("button[onclick='submitOrder()']");
        await Page.WaitForSelectorAsync("#result-area", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var notFoundStatus = await Page.Locator("#result-status").TextContentAsync();
        Assert.That(notFoundStatus?.Trim(), Is.EqualTo("404").Or.EqualTo("400"), "存在しない顧客はエラーになること");
        await ShowCaptionAsync($"顧客コードが存在しない場合のエラーレスポンスです", 2000);

        // 与信超過
        await ShowCaptionAsync("⑤「与信超過」シナリオ（400 - 与信限度額超過）");
        await HighlightAndClickAsync("button[onclick=\"setScenario('credit-over')\"]");
        await Page.WaitForTimeoutAsync(500);
        await SubmitAndAssertAsync("400", "VALIDATION_ERROR");
        await ShowCaptionAsync("400 Bad Request：与信限度額超過はバリデーションエラーとして返ります", 2000);

        await ShowCaptionAsync("✅ バリデーションデモ完了", 2000);
    }
}
