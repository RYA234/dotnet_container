using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class OrdersPageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";

    [Test]
    public async Task OrdersPage_PriceCalculation_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/orders");

        await Page.FillAsync("input[type=number]", "5");
        await Page.FillAsync("input:not([type])", "500");
        await Page.ClickAsync("button:has-text('計算')");

        // 合計 2500, 割引は 0 （数量 5 < 10）
        await Expect(Page.Locator("text=合計:")).ToBeVisibleAsync();
        var page1 = await Page.ContentAsync();
        Assert.That(page1, Does.Contain("合計").And.Contain("2500"));
        Assert.That(page1, Does.Contain("割引後").And.Contain("2500"));
    }

    [Test]
    public async Task OrdersPage_BulkDiscount_Applies()
    {
        await Page.GotoAsync($"{BaseUrl}/orders");

        await Page.FillAsync("input[type=number]", "10");
        await Page.FillAsync("input:not([type])", "1000");
        await Page.ClickAsync("button:has-text('計算')");

        // 合計 10000, 割引後は 9000（10% OFF）
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("10000"));
        Assert.That(pageContent, Does.Contain("9000"));
    }
}
