using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterKeyboardShortcutFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    [Test]
    public Task TeleprompterPage_KeyboardShortcuts_ToggleMirrorAndJustifyAlignment() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.Teleprompter.ShortcutScenarioName);

            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Teleprompter.Page).FocusAsync();
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Space);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.PlayToggle))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.State.ActiveAttribute,
                    BrowserTestConstants.Teleprompter.ActiveStateValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.PauseIcon)).ToBeVisibleAsync();

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Space);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.PlayToggle))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.State.ActiveAttribute,
                    BrowserTestConstants.Teleprompter.InactiveStateValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowRight);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowLeft);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderFirstBlockIndicator);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.O);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.TeleprompterFlow.ReaderOrientationAttribute,
                    BrowserTestConstants.TeleprompterFlow.OrientationPortraitValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.H);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.MirrorHorizontalToggle))
                .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.V);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.MirrorVerticalToggle))
                .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Digit2);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.AlignmentCenter))
                .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Digit3);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.AlignmentRight))
                .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Digit4);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.AlignmentJustify))
                .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute,
                    BrowserTestConstants.TeleprompterFlow.AlignmentJustifyValue);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.Teleprompter.ShortcutScenarioName,
                BrowserTestConstants.Teleprompter.ShortcutStep);
        });

    [Test]
    public Task TeleprompterPage_KeyboardShortcuts_KeepRangeInputFocusFromTriggeringPageShortcuts() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.Teleprompter.ShortcutInputFocusScenarioName);

            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderFirstBlockIndicator);

            await page.GetByTestId(UiTestIds.Teleprompter.WidthSlider).FocusAsync();
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.PageDown);

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderFirstBlockIndicator);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.O);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.TeleprompterFlow.ReaderOrientationAttribute,
                    BrowserTestConstants.TeleprompterFlow.OrientationLandscapeValue);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.Teleprompter.ShortcutInputFocusScenarioName,
                BrowserTestConstants.Teleprompter.ShortcutInputFocusStep);
        });
}
