using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class NavigationFlowTests(StandaloneAppFixture fixture)
{
    private const string BackgroundColorProperty = "backgroundColor";
    private const string ColorProperty = "color";
    private const string LiveDangerIconColor = "rgb(255, 138, 138)";
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task ShellHeader_OpensGoLive_FromLibraryAndSettings()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Header.GoLive).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.GoLive));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Header.GoLive).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.GoLive));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task ScreenNavigation_UsesSpaRoutingWithoutReloadingBrowserContext()
    {
        const string nonce = "spa-nav-stable";
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.EvaluateAsync("value => window.__prompterSpaNonce = value", nonce);

            await page.GetByTestId(UiTestIds.Header.EditorLearn).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnQuantum));
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync();
            await Assert.That(await page.EvaluateAsync<string>("() => window.__prompterSpaNonce")).IsEqualTo(nonce);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await page.EvaluateAsync("value => window.__prompterSpaNonce = value", nonce);

            await page.GetByTestId(UiTestIds.Header.EditorRead).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.TeleprompterQuantum));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
            await Assert.That(await page.EvaluateAsync<string>("() => window.__prompterSpaNonce")).IsEqualTo(nonce);

            await page.GetByTestId(UiTestIds.Teleprompter.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.EditorQuantum));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await Assert.That(await page.EvaluateAsync<string>("() => window.__prompterSpaNonce")).IsEqualTo(nonce);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task ShellHeader_UsesConsistentNeutralGoLiveChrome_OnLibraryAndSettings()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, GoLiveIndicatorStates.Idle);

            var libraryChrome = await ReadGoLiveChromeAsync(page);

            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, GoLiveIndicatorStates.Idle);

            var settingsChrome = await ReadGoLiveChromeAsync(page);

            await Assert.That(settingsChrome.ButtonBackground).IsEqualTo(libraryChrome.ButtonBackground);
            await Assert.That(settingsChrome.IconColor).IsEqualTo(libraryChrome.IconColor);
            await Assert.That(settingsChrome.DotBackground).IsEqualTo(libraryChrome.DotBackground);
            await Assert.That(libraryChrome.IconColor).IsNotEqualTo(LiveDangerIconColor);
            await Assert.That(settingsChrome.IconColor).IsNotEqualTo(LiveDangerIconColor);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task<GoLiveChromeSnapshot> ReadGoLiveChromeAsync(IPage page)
    {
        var button = page.GetByTestId(UiTestIds.Header.GoLive);
        var dot = page.GetByTestId(UiTestIds.Header.GoLiveDot);
        var icon = page.GetByTestId(UiTestIds.Header.GoLiveIcon);

        await Expect(button).ToBeVisibleAsync();
        await Expect(dot).ToBeVisibleAsync();
        await Expect(icon).ToBeVisibleAsync();

        return new(
            await ReadCssPropertyAsync(button, BackgroundColorProperty),
            await ReadCssPropertyAsync(icon, ColorProperty),
            await ReadCssPropertyAsync(dot, BackgroundColorProperty));
    }

    private static Task<string> ReadCssPropertyAsync(ILocator locator, string propertyName) =>
        locator.EvaluateAsync<string>(
            "(element, propertyName) => getComputedStyle(element)[propertyName]",
            propertyName);

    private readonly record struct GoLiveChromeSnapshot(
        string ButtonBackground,
        string IconColor,
        string DotBackground);
}
