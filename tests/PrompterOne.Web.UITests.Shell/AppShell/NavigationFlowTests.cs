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
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ShellRouteDriver.OpenLibraryAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.GoLive),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.GoLive);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await ShellRouteDriver.OpenSettingsAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.GoLive),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.GoLive);
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
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorRouteDriver.OpenReadyAsync(
                page,
                BrowserTestConstants.Routes.EditorQuantum,
                nameof(ScreenNavigation_UsesSpaRoutingWithoutReloadingBrowserContext));

            await page.EvaluateAsync("value => window.__prompterSpaNonce = value", nonce);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.EditorLearn),
                noWaitAfter: true);
            await PlaybackRouteDriver.WaitForLearnReadyAsync(page, BrowserTestConstants.Routes.LearnQuantum);
            await Assert.That(await page.EvaluateAsync<string>("() => window.__prompterSpaNonce")).IsEqualTo(nonce);

            await EditorRouteDriver.OpenReadyAsync(
                page,
                BrowserTestConstants.Routes.EditorQuantum,
                $"{nameof(ScreenNavigation_UsesSpaRoutingWithoutReloadingBrowserContext)}-return");
            await page.EvaluateAsync("value => window.__prompterSpaNonce = value", nonce);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.EditorRead),
                noWaitAfter: true);
            await PlaybackRouteDriver.WaitForTeleprompterReadyAsync(page, BrowserTestConstants.Routes.TeleprompterQuantum);
            await Assert.That(await page.EvaluateAsync<string>("() => window.__prompterSpaNonce")).IsEqualTo(nonce);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Teleprompter.Back),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
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
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ShellRouteDriver.OpenLibraryAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, GoLiveIndicatorStates.Idle);

            var libraryChrome = await ReadGoLiveChromeAsync(page);

            await ShellRouteDriver.OpenSettingsAsync(page);
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

    [Test]
    public async Task ShellHeaderChrome_KeepsActionsReachable_OnPhoneAndTabletViewports()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            foreach (var viewport in BrowserTestConstants.ResponsiveLayout.HeaderChromeViewports)
            {
                await page.SetViewportSizeAsync(viewport.Width, viewport.Height);

                await ShellRouteDriver.OpenLibraryAsync(page, viewport.Name);
                await AssertHeaderControlsReachableAsync(
                    page,
                    BrowserTestConstants.ResponsiveLayout.LibraryRouteName,
                    viewport,
                    UiTestIds.Header.Home,
                    UiTestIds.Header.Actions,
                    UiTestIds.Header.LibrarySearchSurface,
                    UiTestIds.Header.GoLive,
                    UiTestIds.Header.AiSpotlight,
                    UiTestIds.Header.LibraryOpenScriptButton,
                    UiTestIds.Header.LibraryNewScript);

                await EditorRouteDriver.OpenReadyAsync(
                    page,
                    BrowserTestConstants.Routes.EditorQuantum,
                    viewport.Name);
                await AssertHeaderControlsReachableAsync(
                    page,
                    BrowserTestConstants.ResponsiveLayout.EditorRouteName,
                    viewport,
                    UiTestIds.Header.Home,
                    UiTestIds.Header.Back,
                    UiTestIds.Header.Actions,
                    UiTestIds.Header.GoLive,
                    UiTestIds.Header.AiSpotlight,
                    UiTestIds.Header.EditorImportScriptButton,
                    UiTestIds.Header.EditorSaveFile,
                    UiTestIds.Header.EditorExportMarkdown,
                    UiTestIds.Header.EditorExportPlainText,
                    UiTestIds.Header.EditorLearn,
                    UiTestIds.Header.EditorRead);

                await ShellRouteDriver.OpenSettingsAsync(page, viewport.Name);
                await AssertHeaderControlsReachableAsync(
                    page,
                    BrowserTestConstants.ResponsiveLayout.SettingsRouteName,
                    viewport,
                    UiTestIds.Header.Home,
                    UiTestIds.Header.Back,
                    UiTestIds.Header.Actions,
                    UiTestIds.Header.GoLive,
                    UiTestIds.Header.AiSpotlight);
            }
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
            await ReadCssPropertyAsync(page, UiTestIds.Header.GoLive, BackgroundColorProperty),
            await ReadCssPropertyAsync(page, UiTestIds.Header.GoLiveIcon, ColorProperty),
            await ReadCssPropertyAsync(page, UiTestIds.Header.GoLiveDot, BackgroundColorProperty));
    }

    private static Task<string> ReadCssPropertyAsync(IPage page, string testId, string propertyName) =>
        page.EvaluateAsync<string>(
            """
            (args) => {
                const element = document.querySelector(`[data-test="${args.testId}"]`);
                if (!element) {
                    throw new Error(`Unable to resolve ${args.testId}.`);
                }

                return getComputedStyle(element)[args.propertyName] ?? "";
            }
            """,
            new { propertyName, testId });

    private static async Task AssertHeaderControlsReachableAsync(
        IPage page,
        string routeName,
        ResponsiveViewport viewport,
        params string[] controlTestIds)
    {
        foreach (var controlTestId in controlTestIds)
        {
            var locator = page.GetByTestId(controlTestId);
            await locator.ScrollIntoViewIfNeededAsync();
            await ResponsiveLayoutAssertions.AssertVisibleWithinViewportAsync(
                page,
                locator,
                controlTestId,
                routeName,
                viewport);
        }

        await UiScenarioArtifacts.CapturePageAsync(
            page,
            BuildHeaderChromeScenarioName(routeName, viewport),
            BrowserTestConstants.AppShellFlow.HeaderChromeReachableStep);
    }

    private static string BuildHeaderChromeScenarioName(string routeName, ResponsiveViewport viewport) =>
        string.Join(
            BrowserTestConstants.ScenarioArtifacts.Separator,
            BrowserTestConstants.AppShellFlow.HeaderChromeScenario,
            routeName,
            viewport.Name);

    private readonly record struct GoLiveChromeSnapshot(
        string ButtonBackground,
        string IconColor,
        string DotBackground);
}
