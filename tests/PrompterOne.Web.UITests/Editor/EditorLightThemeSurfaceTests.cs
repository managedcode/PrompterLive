using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorLightThemeSurfaceTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture){
    private const string BackgroundColorProperty = "backgroundColor";
    private const string ColorProperty = "color";
    private const string ScenarioName = "editor-light-theme-surface";
    private const string FullEditorStep = "01-full-editor-surface";
    private const string SourceStageStep = "02-source-stage-surface";
    private const double MinimumToolbarSurfaceChannel = 220;
    private const double MinimumMetadataSurfaceChannel = 210;
    private const double MaximumReadableTextChannel = 160;

    private readonly record struct CssColor(double R, double G, double B, double A);

    [Test]
    public Task EditorScreen_LightTheme_UsesReadableMainPanelsAndMonacoSurface() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(ScenarioName);

            await SwitchThemeAsync(page);
            await page.GotoAsync(
                BrowserTestConstants.Routes.EditorDemo,
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync(
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.Locator("html")).ToHaveAttributeAsync(
                BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
                BrowserTestConstants.SettingsFlow.LightTheme);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var toolbar = page.GetByTestId(UiTestIds.Editor.Toolbar);
            var metadata = page.GetByTestId(UiTestIds.Editor.MetadataRail);
            var scrollHost = page.GetByTestId(UiTestIds.Editor.SourceScrollHost);
            var stage = page.GetByTestId(UiTestIds.Editor.SourceStage);
            var minimap = page.GetByTestId(UiTestIds.Editor.SourceMinimap);
            var title = page.GetByTestId(UiTestIds.Editor.Title);

            await Expect(toolbar).ToBeVisibleAsync();
            await Expect(metadata).ToBeVisibleAsync();
            await Expect(scrollHost).ToBeVisibleAsync();
            await Expect(stage).ToBeVisibleAsync();
            await Expect(minimap).ToBeVisibleAsync();
            await Expect(title).ToBeVisibleAsync();

            var toolbarBackground = await ReadCssColorAsync(toolbar, BackgroundColorProperty);
            var metadataBackground = await ReadCssColorAsync(metadata, BackgroundColorProperty);
            var renderedLineColor = await ReadRenderedLineColorAsync(stage);
            var titleColor = await ReadCssColorAsync(title, ColorProperty);
            var state = await EditorMonacoDriver.GetStateAsync(page);

            await Assert.That(state.Text).Contains(BrowserTestConstants.Editor.BodyHeading);
            await Assert.That(HasMinimumChannels(toolbarBackground, MinimumToolbarSurfaceChannel)).IsTrue().Because($"Expected the light-theme editor toolbar surface to stay light, but got rgba({toolbarBackground.R:0.##}, {toolbarBackground.G:0.##}, {toolbarBackground.B:0.##}, {toolbarBackground.A:0.##}).");
            await Assert.That(HasMinimumChannels(metadataBackground, MinimumMetadataSurfaceChannel)).IsTrue().Because($"Expected the light-theme editor metadata surface to stay light, but got rgba({metadataBackground.R:0.##}, {metadataBackground.G:0.##}, {metadataBackground.B:0.##}, {metadataBackground.A:0.##}).");
            await Assert.That(HasMaximumChannels(titleColor, MaximumReadableTextChannel)).IsTrue().Because($"Expected the light-theme editor title text to stay readable, but got rgba({titleColor.R:0.##}, {titleColor.G:0.##}, {titleColor.B:0.##}, {titleColor.A:0.##}).");
            await Assert.That(HasMaximumChannels(renderedLineColor, MaximumReadableTextChannel)).IsTrue().Because($"Expected the light-theme Monaco text to stay readable, but got rgba({renderedLineColor.R:0.##}, {renderedLineColor.G:0.##}, {renderedLineColor.B:0.##}, {renderedLineColor.A:0.##}).");
            await Assert.That(state.Layout.MinimapWidth >= BrowserTestConstants.Editor.MinimapMinimumWidthPx).IsTrue().Because($"Expected the Monaco minimap to stay visible in light theme, but its width was {state.Layout.MinimapWidth:0.##}px.");

            await UiScenarioArtifacts.CapturePageAsync(page, ScenarioName, FullEditorStep);
            await UiScenarioArtifacts.CaptureLocatorAsync(stage, ScenarioName, SourceStageStep);
        });

    private static async Task SwitchThemeAsync(IPage page)
    {
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
    }

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

    private static async Task<CssColor> ReadRenderedLineColorAsync(ILocator locator) =>
        await locator.EvaluateAsync<CssColor>(
            """
            element => {
                const line = element.querySelector('.view-line');
                const sample = Array.from(line?.querySelectorAll('span') ?? [])
                    .find(node => node.textContent?.trim()?.length > 0) ?? line;
                if (!(sample instanceof HTMLElement)) {
                    return { r: 0, g: 0, b: 0, a: 0 };
                }

                const value = getComputedStyle(sample).color;
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
            """);
}