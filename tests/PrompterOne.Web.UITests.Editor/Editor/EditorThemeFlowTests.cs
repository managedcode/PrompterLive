using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorThemeFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const string BackgroundColorProperty = "backgroundColor";
    private const string ColorProperty = "color";

    private readonly record struct CssColor(double R, double G, double B, double A);

    [Test]
    public Task EditorScreen_LightTheme_EmotionMenu_UsesReadableDropdownAndCustomTooltipOnly() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.LightThemeScenario);
            var browserErrors = BrowserErrorCollector.Attach(page);
            var bootstrapOverlay = page.GetByTestId(UiTestIds.Diagnostics.Bootstrap);

            try
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

                await page.GotoAsync(
                    BrowserTestConstants.Routes.EditorDemo,
                    new() { WaitUntil = WaitUntilState.NetworkIdle });
                await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync(
                    new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
                await EditorMonacoDriver.WaitUntilReadyAsync(page);
                await Expect(page.Locator("html")).ToHaveAttributeAsync(
                    BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
                    BrowserTestConstants.SettingsFlow.LightTheme);
                await Expect(bootstrapOverlay).ToBeHiddenAsync();

                var emotionTrigger = page.GetByTestId(UiTestIds.Editor.EmotionTrigger);
                var emotionMenu = page.GetByTestId(UiTestIds.Editor.MenuEmotion);
                var motivationalEmotion = page.GetByTestId(UiTestIds.Editor.EmotionMotivational);
                var tooltip = page.GetByTestId(UiTestIds.Editor.ToolbarTooltip)
                    .Filter(new() { HasTextString = BrowserTestConstants.EditorFlow.MotivationalEmotionTooltipText });

                await emotionTrigger.ClickAsync();
                await Expect(emotionMenu).ToBeVisibleAsync();
                await Expect(motivationalEmotion).ToBeVisibleAsync();

                await motivationalEmotion.HoverAsync();
                await page.WaitForTimeoutAsync(BrowserTestConstants.EditorFlow.TooltipSettleDelayMs);
                await Expect(tooltip).ToBeVisibleAsync();

                var menuBackground = await ReadCssColorAsync(emotionMenu, BackgroundColorProperty);
                var menuItemColor = await ReadCssColorAsync(motivationalEmotion, ColorProperty);
                var tooltipBackground = await ReadCssColorAsync(tooltip, BackgroundColorProperty);
                var tooltipColor = await ReadCssColorAsync(tooltip, ColorProperty);
                var tooltipOpacity = await ReadOpacityAsync(tooltip);

                await Assert.That(await motivationalEmotion.GetAttributeAsync("title")).IsNull();
                await Assert.That(await motivationalEmotion.GetAttributeAsync("aria-label")).IsEqualTo(BrowserTestConstants.EditorFlow.MotivationalEmotionTooltipText);
                await Assert.That(await tooltip.InnerTextAsync()).IsEqualTo(BrowserTestConstants.EditorFlow.MotivationalEmotionTooltipText);

                await Assert.That(HasMinimumChannels(menuBackground, BrowserTestConstants.EditorFlow.MinimumLightMenuSurfaceChannel)).IsTrue().Because($"Expected the light-theme editor emotion menu surface to stay light, but got rgba({menuBackground.R:0.##}, {menuBackground.G:0.##}, {menuBackground.B:0.##}, {menuBackground.A:0.##}).");
                await Assert.That(HasMaximumChannels(menuItemColor, BrowserTestConstants.EditorFlow.MaximumReadableTextChannel)).IsTrue().Because($"Expected the light-theme editor emotion text to stay readable, but got rgba({menuItemColor.R:0.##}, {menuItemColor.G:0.##}, {menuItemColor.B:0.##}, {menuItemColor.A:0.##}).");
                await Assert.That(HasMinimumChannels(tooltipBackground, BrowserTestConstants.EditorFlow.MinimumLightTooltipSurfaceChannel)).IsTrue().Because($"Expected the light-theme editor tooltip surface to stay light, but got rgba({tooltipBackground.R:0.##}, {tooltipBackground.G:0.##}, {tooltipBackground.B:0.##}, {tooltipBackground.A:0.##}).");
                await Assert.That(HasMaximumChannels(tooltipColor, BrowserTestConstants.EditorFlow.MaximumReadableTextChannel)).IsTrue().Because($"Expected the light-theme editor tooltip text to stay readable, but got rgba({tooltipColor.R:0.##}, {tooltipColor.G:0.##}, {tooltipColor.B:0.##}, {tooltipColor.A:0.##}).");
                await Assert.That(tooltipOpacity >= BrowserTestConstants.EditorFlow.MinimumVisibleTooltipOpacity).IsTrue().Because($"Expected the custom editor tooltip to be visibly rendered on hover, but its opacity was {tooltipOpacity:0.##}.");

                await UiScenarioArtifacts.CapturePageAsync(
                    page,
                    BrowserTestConstants.EditorFlow.LightThemeScenario,
                    BrowserTestConstants.EditorFlow.LightThemeStep);
            }
            catch (Exception exception)
            {
                var bootstrapText = await bootstrapOverlay.TextContentAsync() ?? string.Empty;
                throw new InvalidOperationException(
                    $"Editor light-theme dropdown flow failed. BootstrapOverlay='{bootstrapText}'. BrowserErrors:{Environment.NewLine}{browserErrors.Describe()}{Environment.NewLine}{exception}");
            }
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

    private static async Task<double> ReadOpacityAsync(ILocator locator) =>
        await locator.EvaluateAsync<double>(
            """
            element => Number.parseFloat(getComputedStyle(element).opacity)
            """);
}
