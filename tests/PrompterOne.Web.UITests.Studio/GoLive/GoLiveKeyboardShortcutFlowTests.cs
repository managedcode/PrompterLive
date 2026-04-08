using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class GoLiveKeyboardShortcutFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    [Test]
    public Task GoLivePage_KeyboardShortcuts_ToggleModeLayoutAndRecording() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.GoLive.ShortcutScenario);

            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await GoLiveFlowTests.SeedGoLiveOperationalSettingsAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.Page).FocusAsync();

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Digit2);
            await Expect(page.GetByTestId(UiTestIds.GoLive.ModeStudio))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.State.ActiveAttribute,
                    BrowserTestConstants.State.ActiveValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.BracketLeft);
            await Expect(page.GetByTestId(UiTestIds.GoLive.SourceRail)).ToHaveCountAsync(0);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.F);
            await Expect(page.GetByTestId(UiTestIds.GoLive.FullProgramToggle))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.State.ActiveAttribute,
                    BrowserTestConstants.State.ActiveValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Enter);
            await Expect(page.GetByTestId(UiTestIds.GoLive.ActiveSourceLabel))
                .ToHaveTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.R);
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeActiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.GoLive.ShortcutScenario,
                BrowserTestConstants.GoLive.ShortcutStep);
        });
}
