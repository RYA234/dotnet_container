using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BlazorApp.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class CalculatorPageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000/dotnet";

    [Test]
    public async Task CalculatorPage_Addition_Works()
    {
        await Page.GotoAsync($"{BaseUrl}/calculator", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for Blazor to initialize and inputs to be ready
        await Page.WaitForSelectorAsync("h3:has-text('Calculator')");
        await Page.WaitForSelectorAsync("input[type=number]");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var firstInput = Page.Locator("input[type=number]").Nth(0);
        var secondInput = Page.Locator("input[type=number]").Nth(1);
        var button = Page.Locator("button:has-text('=')");

        await firstInput.FillAsync("2");
        await secondInput.FillAsync("3");
        await Page.SelectOptionAsync("select", new[] { "+" });
        await button.ClickAsync();

        await Expect(Page.Locator("strong")).ToHaveTextAsync("5");
    }

    [Test]
    public async Task CalculatorPage_DivideByZero_ShowsMessage()
    {
        await Page.GotoAsync($"{BaseUrl}/calculator", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for Blazor to initialize and inputs to be ready
        await Page.WaitForSelectorAsync("h3:has-text('Calculator')");
        await Page.WaitForSelectorAsync("input[type=number]");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var firstInput = Page.Locator("input[type=number]").Nth(0);
        var secondInput = Page.Locator("input[type=number]").Nth(1);
        var button = Page.Locator("button:has-text('=')");

        await firstInput.FillAsync("10");
        await secondInput.FillAsync("0");
        await Page.SelectOptionAsync("select", new[] { "/" });
        await button.ClickAsync();

        var msg = await Page.Locator("strong").TextContentAsync();
        Assert.That(msg, Does.Contain("ゼロで除算"));
    }
}
