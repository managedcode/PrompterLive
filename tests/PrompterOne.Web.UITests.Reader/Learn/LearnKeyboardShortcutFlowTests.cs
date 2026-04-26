using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LearnKeyboardShortcutFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private readonly record struct ProgressState(int CurrentWordNumber, int TotalWordCount);

    [Test]
    public Task LearnPage_KeyboardShortcuts_ToggleLoopPlaybackAndSpeed() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.Learn.ShortcutScenarioName);

            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnDemo);
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

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowDown);
            await Expect(page.GetByTestId(UiTestIds.Learn.SpeedValue))
                .ToHaveTextAsync(BrowserTestConstants.Learn.SpeedAfterDecreaseText);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.Learn.ShortcutScenarioName,
                BrowserTestConstants.Learn.ShortcutStep);
        });

    [Test]
    public Task LearnPage_KeyboardShortcuts_MoveByWordAndPhraseWithoutPlayback() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.Learn.ShortcutNavigationScenarioName);

            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Learn.Page).FocusAsync();
            var initialProgress = await ReadProgressStateAsync(page);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowRight);
            var afterForward = await ReadProgressStateAsync(page);

            await Assert.That(afterForward.CurrentWordNumber).IsEqualTo(initialProgress.CurrentWordNumber + 1);
            await Assert.That(afterForward.TotalWordCount).IsEqualTo(initialProgress.TotalWordCount);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowLeft);
            var afterBackward = await ReadProgressStateAsync(page);

            await Assert.That(afterBackward.CurrentWordNumber).IsEqualTo(initialProgress.CurrentWordNumber);
            await Assert.That(afterBackward.TotalWordCount).IsEqualTo(initialProgress.TotalWordCount);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.PageDown);
            var afterLargeForward = await ReadProgressStateAsync(page);

            await Assert.That(afterLargeForward.CurrentWordNumber)
                .IsEqualTo(initialProgress.CurrentWordNumber + BrowserTestConstants.Learn.StepForwardLargeWordCount);
            await Assert.That(afterLargeForward.TotalWordCount).IsEqualTo(initialProgress.TotalWordCount);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.PageUp);
            var afterLargeBackward = await ReadProgressStateAsync(page);

            await Assert.That(afterLargeBackward.CurrentWordNumber).IsEqualTo(initialProgress.CurrentWordNumber);
            await Assert.That(afterLargeBackward.TotalWordCount).IsEqualTo(initialProgress.TotalWordCount);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.Learn.ShortcutNavigationScenarioName,
                BrowserTestConstants.Learn.ShortcutNavigationStep);
        });

    private static async Task<ProgressState> ReadProgressStateAsync(Microsoft.Playwright.IPage page)
    {
        var progressLabel = await page.GetByTestId(UiTestIds.Learn.ProgressLabel).TextContentAsync() ?? string.Empty;
        var match = BrowserTestConstants.Regexes.LearnProgressLabel.Match(progressLabel);
        await Assert.That(match.Success).IsTrue().Because($"Expected Learn progress label to match the current progress contract, but found '{progressLabel}'.");

        return new ProgressState(
            int.Parse(match.Groups["current"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["total"].Value, CultureInfo.InvariantCulture));
    }
}
