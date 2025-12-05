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
        await Page.GotoAsync($"{BaseUrl}/calculator");

        // Wait for Blazor to initialize
        await Page.WaitForSelectorAsync("h3:has-text('Calculator')");

        await Page.FillAsync("input[type=number] >> nth=0", "2");
        await Page.FillAsync("input[type=number] >> nth=1", "3");
        await Page.SelectOptionAsync("select", new[] { "+" });
        await Page.ClickAsync("button:has-text('=')");

        await Expect(Page.Locator("strong")).ToHaveTextAsync("5");
    }

    [Test]
    public async Task CalculatorPage_DivideByZero_ShowsMessage()
    {
        await Page.GotoAsync($"{BaseUrl}/calculator");

        // Wait for Blazor to initialize
        await Page.WaitForSelectorAsync("h3:has-text('Calculator')");

        await Page.FillAsync("input[type=number] >> nth=0", "10");
        await Page.FillAsync("input[type=number] >> nth=1", "0");
        await Page.SelectOptionAsync("select", new[] { "/" });
        await Page.ClickAsync("button:has-text('=')");

        var msg = await Page.Locator("strong").TextContentAsync();
        Assert.That(msg, Does.Contain("ゼロで除算"));
    }
}
