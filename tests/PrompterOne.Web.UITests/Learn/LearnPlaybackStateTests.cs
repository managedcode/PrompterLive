using System.Globalization;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LearnPlaybackStateTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private sealed class ToggleIconState
    {
        public bool PlayHidden { get; init; }
        public bool PauseHidden { get; init; }
    }
    private readonly record struct ProgressState(int CurrentWordNumber, int TotalWordCount);

    [Test]
    public Task LearnScreen_PlayToggle_SwapsVisibleIconWhenPlaybackChanges() =>
        RunPageAsync(async page =>
        {
            await NavigateToLearnDemoAsync(page);

            var beforeToggle = await ReadToggleIconStateAsync(page);

            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();

            var afterToggle = await ReadToggleIconStateAsync(page);

            await Assert.That(beforeToggle.PlayHidden ^ beforeToggle.PauseHidden).IsTrue().Because("Expected exactly one learn playback icon to be visible before toggling.");
            await Assert.That(afterToggle.PlayHidden ^ afterToggle.PauseHidden).IsTrue().Because("Expected exactly one learn playback icon to be visible after toggling.");
            await Assert.That(afterToggle.PlayHidden).IsNotEqualTo(beforeToggle.PlayHidden);
            await Assert.That(afterToggle.PauseHidden).IsNotEqualTo(beforeToggle.PauseHidden);
        });
    [Test]
    public Task LearnScreen_PhraseBoundary_KeepsLeftContextVisibleAcrossPause() =>
        RunPageAsync(async page =>
        {
            await NavigateToLearnDemoAsync(page);
            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.PauseBoundaryProbeWord,
                BrowserTestConstants.Learn.PauseBoundaryProbeStepLimit);

            var leftWords = await ReadContextWordsAsync(page, UiTestIds.Learn.ContextLeft);
            var rightWords = await ReadContextWordsAsync(page, UiTestIds.Learn.ContextRight);

            await Assert.That(leftWords).IsEquivalentTo([
                    BrowserTestConstants.Learn.PauseBoundaryLeftContextFirstWord,
                    BrowserTestConstants.Learn.PauseBoundaryLeftContextSecondWord
                ], CollectionOrdering.Matching);
            await Assert.That(rightWords).IsEquivalentTo([
                    BrowserTestConstants.Learn.PauseBoundaryRightContextFirstWord,
                    BrowserTestConstants.Learn.PauseBoundaryRightContextSecondWord
                ], CollectionOrdering.Matching);
        });
    [Test]
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
            await Assert.That(finalState.CurrentWordNumber).IsEqualTo(finalState.TotalWordCount);
            await Assert.That(iconState.PlayHidden).IsFalse();
            await Assert.That(iconState.PauseHidden).IsTrue();
        });
    [Test]
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
            await Assert.That(wrappedState.CurrentWordNumber < wrappedState.TotalWordCount).IsTrue().Because($"Expected loop-enabled Learn playback to wrap after the final word, but the progress stayed at {wrappedState.CurrentWordNumber} / {wrappedState.TotalWordCount}.");
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
        Assert.Fail("Unexpected execution path.");
    }
    private static async Task<string> ReadFocusWordAsync(IPage page)
    {
        var rawWord = await page.GetByTestId(UiTestIds.Learn.Word).TextContentAsync();
        return string.Concat((rawWord ?? string.Empty).Where(character => !char.IsWhiteSpace(character)));
    }
    private static Task<string[]> ReadContextWordsAsync(IPage page, string contextTestId) =>
        page.EvaluateAsync<string[]>(
            """
            targetTestId => {
                const element = document.querySelector(`[data-test="${targetTestId}"]`);
                if (!element) {
                    return [];
                }
                return Array.from(element.children)
                    .map(child => child.textContent?.trim() ?? '')
                    .filter(text => text.length > 0);
            }
            """,
            contextTestId);
    private static async Task<ProgressState> ReadProgressStateAsync(IPage page)
    {
        var progressLabel = await page.GetByTestId(UiTestIds.Learn.ProgressLabel).TextContentAsync() ?? string.Empty;
        var match = BrowserTestConstants.Regexes.LearnProgressLabel.Match(progressLabel);
        await Assert.That(match.Success).IsTrue().Because($"Expected Learn progress label to match the current progress contract, but found '{progressLabel}'.");

        return new ProgressState(
            int.Parse(match.Groups["current"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["total"].Value, CultureInfo.InvariantCulture));
    }
    private static Task<ToggleIconState> ReadToggleIconStateAsync(IPage page) =>
        page.EvaluateAsync<ToggleIconState>(
            $$"""
            () => {
                const playIcon = document.querySelector('[data-test="{{UiTestIds.Learn.PlayIcon}}"]');
                const pauseIcon = document.querySelector('[data-test="{{UiTestIds.Learn.PauseIcon}}"]');
                return {
                    playHidden: !!playIcon?.hidden,
                    pauseHidden: !!pauseIcon?.hidden
                };
            }
            """);
}
