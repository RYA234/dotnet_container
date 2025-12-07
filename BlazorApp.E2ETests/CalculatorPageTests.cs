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

        // Wait for page to load
        await Page.WaitForSelectorAsync("h3:has-text('Calculator')");
        await Page.WaitForSelectorAsync("input[type=number]");

        var firstInput = Page.Locator("input[name=a]");
        var secondInput = Page.Locator("input[name=b]");
        var button = Page.Locator("button[type=submit]");

        await firstInput.FillAsync("2");
        await secondInput.FillAsync("3");
        await Page.SelectOptionAsync("select[name=op]", new[] { "+" });
        await button.ClickAsync();

        // Wait for page to reload after form submission
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("strong")).ToHaveTextAsync("5");
    }

    [Test]
    public async Task CalculatorPage_DivideByZero_ShowsMessage()
    {
        await Page.GotoAsync($"{BaseUrl}/calculator", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for page to load
        await Page.WaitForSelectorAsync("h3:has-text('Calculator')");
        await Page.WaitForSelectorAsync("input[type=number]");

        var firstInput = Page.Locator("input[name=a]");
        var secondInput = Page.Locator("input[name=b]");
        var button = Page.Locator("button[type=submit]");

        await firstInput.FillAsync("10");
        await secondInput.FillAsync("0");
        await Page.SelectOptionAsync("select[name=op]", new[] { "/" });
        await button.ClickAsync();

        // Wait for page to reload after form submission
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var msg = await Page.Locator("strong").TextContentAsync();
        Assert.That(msg, Does.Contain("ゼロで除算"));
    }
}
