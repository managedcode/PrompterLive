using System.Globalization;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed class LearnPlaybackStateTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private sealed class ToggleIconState
    {
        public bool PlayHidden { get; init; }
        public bool PauseHidden { get; init; }
    }
    private readonly record struct ProgressState(int CurrentWordNumber, int TotalWordCount);

    [Fact]
    public Task LearnScreen_PlayToggle_SwapsVisibleIconWhenPlaybackChanges() =>
        RunPageAsync(async page =>
        {
            await NavigateToLearnDemoAsync(page);

            var beforeToggle = await ReadToggleIconStateAsync(page);

            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();

            var afterToggle = await ReadToggleIconStateAsync(page);

            Assert.True(beforeToggle.PlayHidden ^ beforeToggle.PauseHidden, "Expected exactly one learn playback icon to be visible before toggling.");
            Assert.True(afterToggle.PlayHidden ^ afterToggle.PauseHidden, "Expected exactly one learn playback icon to be visible after toggling.");
            Assert.NotEqual(beforeToggle.PlayHidden, afterToggle.PlayHidden);
            Assert.NotEqual(beforeToggle.PauseHidden, afterToggle.PauseHidden);
        });
    [Fact]
    public Task LearnScreen_PhraseBoundary_KeepsLeftContextVisibleAcrossPause() =>
        RunPageAsync(async page =>
        {
            await NavigateToLearnDemoAsync(page);
            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.PauseBoundaryProbeWord,
                BrowserTestConstants.Learn.PauseBoundaryProbeStepLimit);

            var leftWords = await ReadContextWordsAsync(page, UiDomIds.Learn.ContextLeft);
            var rightWords = await ReadContextWordsAsync(page, UiDomIds.Learn.ContextRight);

            Assert.Equal(
                [
                    BrowserTestConstants.Learn.PauseBoundaryLeftContextFirstWord,
                    BrowserTestConstants.Learn.PauseBoundaryLeftContextSecondWord
                ],
                leftWords);
            Assert.Equal(
                [
                    BrowserTestConstants.Learn.PauseBoundaryRightContextFirstWord,
                    BrowserTestConstants.Learn.PauseBoundaryRightContextSecondWord
                ],
                rightWords);
        });
    [Fact]
    public Task LearnScreen_PlaybackStopsOnFinalWordWhenLoopIsOff() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.Learn.StopAtEndScenarioName);
            await NavigateToLearnDemoAsync(page);
            await MoveToPenultimateWordAsync(page);
            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.LearnPlaybackProbeWindowMs);
            var finalState = await ReadProgressStateAsync(page);
            var iconState = await ReadToggleIconStateAsync(page);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.Learn.StopAtEndScenarioName,
                BrowserTestConstants.Learn.StopAtEndStep);
            Assert.Equal(finalState.TotalWordCount, finalState.CurrentWordNumber);
            Assert.False(iconState.PlayHidden);
            Assert.True(iconState.PauseHidden);
        });
    [Fact]
    public Task LearnScreen_LoopToggle_AllowsPlaybackToWrapFromTheFinalWord() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.Learn.LoopWrapScenarioName);
            await NavigateToLearnDemoAsync(page);
            await MoveToFinalWordAsync(page);
            await page.GetByTestId(UiTestIds.Learn.LoopToggle).ClickAsync();
            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.LearnPlaybackProbeWindowMs);
            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
            var wrappedState = await ReadProgressStateAsync(page);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.Learn.LoopWrapScenarioName,
                BrowserTestConstants.Learn.LoopWrapStep);
            Assert.True(
                wrappedState.CurrentWordNumber < wrappedState.TotalWordCount,
                $"Expected loop-enabled Learn playback to wrap after the final word, but the progress stayed at {wrappedState.CurrentWordNumber} / {wrappedState.TotalWordCount}.");
        });
    private static async Task NavigateToLearnDemoAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.LearnDemo);
        await Expect(page.GetByTestId(UiTestIds.Learn.Page))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
    }

    private static async Task MoveToFinalWordAsync(IPage page)
    {
        var progress = await ReadProgressStateAsync(page);
        await MoveToWordNumberAsync(page, progress.TotalWordCount);
    }

    private static async Task MoveToPenultimateWordAsync(IPage page)
    {
        var progress = await ReadProgressStateAsync(page);
        var penultimateWordNumber = Math.Max(1, progress.TotalWordCount - 1);
        await MoveToWordNumberAsync(page, penultimateWordNumber);
    }

    private static async Task MoveToWordNumberAsync(IPage page, int targetWordNumber)
    {
        var progress = await ReadProgressStateAsync(page);
        while (progress.CurrentWordNumber + BrowserTestConstants.Learn.StepForwardLargeWordCount <= targetWordNumber)
        {
            await page.GetByTestId(UiTestIds.Learn.StepForwardLarge).ClickAsync();
            progress = await ReadProgressStateAsync(page);
        }

        while (progress.CurrentWordNumber < targetWordNumber)
        {
            await page.GetByTestId(UiTestIds.Learn.StepForward).ClickAsync();
            progress = await ReadProgressStateAsync(page);
        }
    }
    private static async Task StepUntilWordAsync(IPage page, string targetWord, int stepLimit)
    {
        for (var stepIndex = 0; stepIndex < stepLimit; stepIndex++)
        {
            var currentWord = await ReadFocusWordAsync(page);
            if (string.Equals(currentWord, targetWord, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            await page.GetByTestId(UiTestIds.Learn.StepForward).ClickAsync();
        }
        Assert.Fail($"Did not reach the learn probe word '{targetWord}' within {stepLimit} steps.");
    }
    private static async Task<string> ReadFocusWordAsync(IPage page)
    {
        var rawWord = await page.GetByTestId(UiTestIds.Learn.Word).TextContentAsync();
        return string.Concat((rawWord ?? string.Empty).Where(character => !char.IsWhiteSpace(character)));
    }
    private static Task<string[]> ReadContextWordsAsync(IPage page, string elementId) =>
        page.EvaluateAsync<string[]>(
            """
            targetId => {
                const element = document.getElementById(targetId);
                if (!element) {
                    return [];
                }
                return Array.from(element.children)
                    .map(child => child.textContent?.trim() ?? '')
                    .filter(text => text.length > 0);
            }
            """,
            elementId);
    private static async Task<ProgressState> ReadProgressStateAsync(IPage page)
    {
        var progressLabel = await page.GetByTestId(UiTestIds.Learn.ProgressLabel).TextContentAsync() ?? string.Empty;
        var match = BrowserTestConstants.Regexes.LearnProgressLabel.Match(progressLabel);
        Assert.True(
            match.Success,
            $"Expected Learn progress label to match the current progress contract, but found '{progressLabel}'.");

        return new ProgressState(
            int.Parse(match.Groups["current"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["total"].Value, CultureInfo.InvariantCulture));
    }
    private static Task<ToggleIconState> ReadToggleIconStateAsync(IPage page) =>
        page.EvaluateAsync<ToggleIconState>(
            $$"""
            () => {
                const button = document.querySelector('[data-testid="{{UiTestIds.Learn.PlayToggle}}"]');
                const playIcon = button?.querySelector('[data-toggle-icon="play"]');
                const pauseIcon = button?.querySelector('[data-toggle-icon="pause"]');
                return {
                    playHidden: !!playIcon?.hidden,
                    pauseHidden: !!pauseIcon?.hidden
                };
            }
            """);
}
