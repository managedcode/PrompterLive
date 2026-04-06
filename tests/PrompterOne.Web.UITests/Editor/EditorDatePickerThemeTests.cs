using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorDatePickerThemeTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture){
    [Test]
    public Task EditorScreen_CreatedDateField_UsesThemeAwarePickerChrome_WithoutClipping() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.DatePickerScenario);

            var darkMetrics = await OpenEditorAndReadDatePickerMetricsAsync(page, BrowserTestConstants.EditorFlow.DatePickerDarkStep);
            await AssertDatePickerMetrics(darkMetrics, BrowserTestConstants.SettingsFlow.DarkTheme);

            await SwitchThemeAsync(page, BrowserTestConstants.SettingsFlow.LightTheme);
            var lightMetrics = await OpenEditorAndReadDatePickerMetricsAsync(page, BrowserTestConstants.EditorFlow.DatePickerLightStep);
            await AssertDatePickerMetrics(lightMetrics, BrowserTestConstants.SettingsFlow.LightTheme);
        });

    private static async Task AssertDatePickerMetrics(DatePickerMetrics metrics, string expectedTheme)
    {
        await Assert.That(metrics.Theme).IsEqualTo(expectedTheme);
        await Assert.That(metrics.ColorScheme).IsEqualTo(expectedTheme);
        await Assert.That(metrics.InputWidth >= BrowserTestConstants.EditorFlow.MinimumDateFieldWidthPx).IsTrue().Because($"Expected the editor Created date field to stay wide enough for the full value in theme '{expectedTheme}', but its width was {metrics.InputWidth:0.##}.");
    }

    private static async Task<DatePickerMetrics> OpenEditorAndReadDatePickerMetricsAsync(IPage page, string screenshotStep)
    {
        await page.GotoAsync(
            BrowserTestConstants.Routes.EditorDemo,
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        var createdInput = page.GetByTestId(UiTestIds.Editor.Created);
        await Expect(createdInput).ToBeVisibleAsync(
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await Expect(page.GetByTestId(UiTestIds.Editor.CreatedIcon)).ToBeVisibleAsync();

        await UiScenarioArtifacts.CapturePageAsync(
            page,
            BrowserTestConstants.EditorFlow.DatePickerScenario,
            screenshotStep);

        return await createdInput.EvaluateAsync<DatePickerMetrics>(
            """
            element => {
                const inputStyle = getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                return {
                    theme: document.documentElement.getAttribute('data-theme') ?? '',
                    colorScheme: inputStyle.colorScheme,
                    inputWidth: rect.width
                };
            }
            """);
    }

    private static async Task SwitchThemeAsync(IPage page, string theme)
    {
        await page.GotoAsync(
            BrowserTestConstants.Routes.Settings,
            new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync(
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await page.GetByTestId(UiTestIds.Settings.NavAppearance).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AppearancePanel)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.ThemeOption(theme)).ClickAsync();
        await Expect(page.Locator("html")).ToHaveAttributeAsync(
            BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
            theme);
    }

    private readonly record struct DatePickerMetrics(
        string Theme,
        string ColorScheme,
        double InputWidth);
}
