using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LibraryThemeFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture){
    private readonly record struct CssColor(double R, double G, double B, double A);

    [Test]
    public Task LibraryScreen_LightTheme_UsesReadableSidebarCardsAndCreateTiles() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.LibraryFlow.LightThemeScenario);

            await page.GotoAsync(
                BrowserTestConstants.Routes.Settings,
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync(
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Settings.NavAppearance).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.AppearancePanel)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.ThemeOption(BrowserTestConstants.SettingsFlow.LightTheme)).ClickAsync();
            await Expect(page.Locator("html")).ToHaveAttributeAsync(
                BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
                BrowserTestConstants.SettingsFlow.LightTheme);

            await page.GotoAsync(
                BrowserTestConstants.Routes.Library,
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync(
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.Locator("html")).ToHaveAttributeAsync(
                BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
                BrowserTestConstants.SettingsFlow.LightTheme);

            var sidebar = page.GetByTestId(UiTestIds.Library.Sidebar);
            var folderAll = page.GetByTestId(UiTestIds.Library.FolderAll);
            var searchSurface = page.GetByTestId(UiTestIds.Header.LibrarySearchSurface);
            var createButton = page.GetByTestId(UiTestIds.Header.LibraryNewScript);
            var demoCardSurface = page.GetByTestId(UiTestIds.Library.CardSurface(BrowserTestConstants.Scripts.DemoId));
            var createScriptSurface = page.GetByTestId(UiTestIds.Library.CreateScriptSurface);
            var createFolderSurface = page.GetByTestId(UiTestIds.Library.FolderCreateTileSurface);

            await Expect(sidebar).ToBeVisibleAsync();
            await Expect(searchSurface).ToBeVisibleAsync();
            await Expect(createButton).ToBeVisibleAsync();
            await Expect(demoCardSurface).ToBeVisibleAsync();
            await Expect(createScriptSurface).ToBeVisibleAsync();
            await Expect(createFolderSurface).ToBeVisibleAsync();

            var sidebarBackground = await ReadCssColorAsync(sidebar, BrowserTestConstants.LibraryFlow.BackgroundColorProperty);
            var searchBackground = await ReadCssColorAsync(searchSurface, BrowserTestConstants.LibraryFlow.BackgroundColorProperty);
            var cardBackground = await ReadCssColorAsync(demoCardSurface, BrowserTestConstants.LibraryFlow.BackgroundColorProperty);
            var createScriptBackground = await ReadCssColorAsync(createScriptSurface, BrowserTestConstants.LibraryFlow.BackgroundColorProperty);
            var createFolderBackground = await ReadCssColorAsync(createFolderSurface, BrowserTestConstants.LibraryFlow.BackgroundColorProperty);
            var createButtonColor = await ReadCssColorAsync(createButton, BrowserTestConstants.LibraryFlow.ColorProperty);
            var folderAllColor = await ReadCssColorAsync(folderAll, BrowserTestConstants.LibraryFlow.ColorProperty);

            await Assert.That(HasMinimumChannels(sidebarBackground, BrowserTestConstants.LibraryFlow.MinimumLightSidebarSurfaceChannel)).IsTrue().Because($"Expected the light-theme library sidebar surface to stay light, but got rgba({sidebarBackground.R:0.##}, {sidebarBackground.G:0.##}, {sidebarBackground.B:0.##}, {sidebarBackground.A:0.##}).");
            await Assert.That(HasMinimumChannels(searchBackground, BrowserTestConstants.LibraryFlow.MinimumLightSearchSurfaceChannel)).IsTrue().Because($"Expected the light-theme library search surface to stay light, but got rgba({searchBackground.R:0.##}, {searchBackground.G:0.##}, {searchBackground.B:0.##}, {searchBackground.A:0.##}).");
            await Assert.That(HasMinimumChannels(cardBackground, BrowserTestConstants.LibraryFlow.MinimumLightCardSurfaceChannel)).IsTrue().Because($"Expected the light-theme library card surface to stay light, but got rgba({cardBackground.R:0.##}, {cardBackground.G:0.##}, {cardBackground.B:0.##}, {cardBackground.A:0.##}).");
            await Assert.That(HasMinimumChannels(createScriptBackground, BrowserTestConstants.LibraryFlow.MinimumLightCreateTileSurfaceChannel)).IsTrue().Because($"Expected the light-theme New Script tile surface to stay light, but got rgba({createScriptBackground.R:0.##}, {createScriptBackground.G:0.##}, {createScriptBackground.B:0.##}, {createScriptBackground.A:0.##}).");
            await Assert.That(HasMinimumChannels(createFolderBackground, BrowserTestConstants.LibraryFlow.MinimumLightCreateTileSurfaceChannel)).IsTrue().Because($"Expected the light-theme New Folder tile surface to stay light, but got rgba({createFolderBackground.R:0.##}, {createFolderBackground.G:0.##}, {createFolderBackground.B:0.##}, {createFolderBackground.A:0.##}).");
            await Assert.That(HasMaximumChannels(createButtonColor, BrowserTestConstants.LibraryFlow.MaximumReadableTextChannel)).IsTrue().Because($"Expected the light-theme header create button text to stay readable, but got rgba({createButtonColor.R:0.##}, {createButtonColor.G:0.##}, {createButtonColor.B:0.##}, {createButtonColor.A:0.##}).");
            await Assert.That(HasMaximumChannels(folderAllColor, BrowserTestConstants.LibraryFlow.MaximumReadableSecondaryTextChannel)).IsTrue().Because($"Expected the light-theme library sidebar text to stay readable, but got rgba({folderAllColor.R:0.##}, {folderAllColor.G:0.##}, {folderAllColor.B:0.##}, {folderAllColor.A:0.##}).");

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.LibraryFlow.LightThemeScenario,
                BrowserTestConstants.LibraryFlow.LightThemeStep);
        });

    private static bool HasMaximumChannels(CssColor color, double maximum) =>
        color.R <= maximum && color.G <= maximum && color.B <= maximum;

    private static bool HasMinimumChannels(CssColor color, double minimum) =>
        color.R >= minimum && color.G >= minimum && color.B >= minimum;

    private static async Task<CssColor> ReadCssColorAsync(ILocator locator, string propertyName) =>
        await locator.EvaluateAsync<CssColor>(
            """
            (element, propertyName) => {
                const value = getComputedStyle(element)[propertyName];
                const match = value.match(/rgba?\(([^)]+)\)/);
                if (!match) {
                    return { r: 0, g: 0, b: 0, a: 0 };
                }

                const parts = match[1].split(',').map(part => Number.parseFloat(part.trim()));
                return {
                    r: parts[0] ?? 0,
                    g: parts[1] ?? 0,
                    b: parts[2] ?? 0,
                    a: parts[3] ?? 1
                };
            }
            """,
            propertyName);
}