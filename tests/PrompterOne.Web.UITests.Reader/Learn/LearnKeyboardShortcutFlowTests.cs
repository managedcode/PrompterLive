using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LearnKeyboardShortcutFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    [Test]
    public Task LearnPage_KeyboardShortcuts_ToggleLoopPlaybackAndSpeed() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.Learn.ShortcutScenarioName);

            await page.GotoAsync(BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Learn.Page).FocusAsync();
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Space);
            await Expect(page.GetByTestId(UiTestIds.Learn.PlayToggle))
                .ToHaveAttributeAsync("aria-pressed", BrowserTestConstants.Learn.LoopPressedValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.L);
            await Expect(page.GetByTestId(UiTestIds.Learn.LoopToggle))
                .ToHaveAttributeAsync("aria-pressed", BrowserTestConstants.Learn.LoopPressedValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowUp);
            await Expect(page.GetByTestId(UiTestIds.Learn.SpeedValue))
                .ToHaveTextAsync(BrowserTestConstants.Learn.SpeedAfterIncreaseText);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.Learn.ShortcutScenarioName,
                BrowserTestConstants.Learn.ShortcutStep);
        });
}
