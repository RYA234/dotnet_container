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
        await Page.GotoAsync($"{BaseUrl}/orders", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for Blazor to initialize and inputs to be ready
        await Page.WaitForSelectorAsync("h3:has-text('Orders')");
        await Page.WaitForSelectorAsync("input[type=number]");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var quantityInput = Page.Locator("input[type=number]");
        var priceInput = Page.Locator("input:not([type])");
        var button = Page.Locator("button:has-text('計算')");

        await quantityInput.FillAsync("5");
        await priceInput.FillAsync("500");
        await button.ClickAsync();

        // 合計 2500, 割引は 0 （数量 5 < 10）
        // Wait for the calculation result to appear
        await Expect(Page.Locator("strong").Filter(new() { HasText = "2500" })).ToBeVisibleAsync();
        var page1 = await Page.ContentAsync();
        Assert.That(page1, Does.Contain("合計").And.Contain("2500"));
        Assert.That(page1, Does.Contain("割引後").And.Contain("2500"));
    }

    [Test]
    public async Task OrdersPage_BulkDiscount_Applies()
    {
        await Page.GotoAsync($"{BaseUrl}/orders", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for Blazor to initialize and inputs to be ready
        await Page.WaitForSelectorAsync("h3:has-text('Orders')");
        await Page.WaitForSelectorAsync("input[type=number]");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var quantityInput = Page.Locator("input[type=number]");
        var priceInput = Page.Locator("input:not([type])");
        var button = Page.Locator("button:has-text('計算')");

        await quantityInput.FillAsync("10");
        await priceInput.FillAsync("1000");
        await button.ClickAsync();

        // 合計 10000, 割引後は 9000（10% OFF）
        // Wait for the calculation result to appear
        await Expect(Page.Locator("strong").Filter(new() { HasText = "10000" })).ToBeVisibleAsync();
        await Expect(Page.Locator("strong").Filter(new() { HasText = "9000" })).ToBeVisibleAsync();
        var pageContent = await Page.ContentAsync();
        Assert.That(pageContent, Does.Contain("10000"));
        Assert.That(pageContent, Does.Contain("9000"));
    }
}
