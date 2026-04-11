using System.Text.Json;
using Microsoft.Playwright;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Rsvp;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class ReaderPlaybackTimingTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const int LearnMinimumWordDurationMilliseconds = 60;
    private const string ReaderTimingRecorderKey = "__prompterOneReaderTimingRecorder";
    private const string TimingProbeScriptFileName = "test-reader-timing.tps";

    private static readonly IReadOnlyList<LearnTimingExpectation> LearnExpectations =
        BuildLearnExpectations(TimingProbeScriptFileName, BrowserTestConstants.ReaderTiming.BaseWpm);
    private static readonly IReadOnlyList<LearnTimingExpectation> LearnSlowExpectations =
        BuildLearnExpectations(TimingProbeScriptFileName, BrowserTestConstants.ReaderTiming.LearnSlowWpm);
    private static readonly IReadOnlyList<LearnTimingExpectation> LearnFastExpectations =
        BuildLearnExpectations(TimingProbeScriptFileName, BrowserTestConstants.ReaderTiming.LearnFastWpm);
    private static readonly IReadOnlyList<int> TeleprompterEffectiveWpmSequence =
    [
        BrowserTestConstants.ReaderTiming.BaseWpm,
        BrowserTestConstants.ReaderTiming.SlowWpm,
        BrowserTestConstants.ReaderTiming.BaseWpm,
        BrowserTestConstants.ReaderTiming.FastWpm,
        BrowserTestConstants.ReaderTiming.BaseWpm
    ];

    [Test]
    public Task TeleprompterTimingProbe_PlaybackSequenceMatchesRenderedWordTimingMetadata() =>
        RunPageAsync(async page =>
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterReaderTiming);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await InstallWordRecorderAsync(page, UiTestIds.Teleprompter.ActiveWord);
            await StartWordRecorderAsync(page);
            var playToggle = page.GetByTestId(UiTestIds.Teleprompter.PlayToggle);
            await UiInteractionDriver.ClickAndContinueAsync(playToggle);
            await Expect(playToggle)
                .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);

            var samples = (await WaitForRecordedSamplesAsync(page, BrowserTestConstants.ReaderTiming.WordCount)).ToArray();

            await Assert.That(samples.Select(sample => sample.Word).ToArray()).IsEquivalentTo(BrowserTestConstants.ReaderTiming.ExpectedWords, CollectionOrdering.Matching);
            await Assert.That(samples.Select(sample => sample.EffectiveWpm).ToArray()).IsEquivalentTo(TeleprompterEffectiveWpmSequence, CollectionOrdering.Matching);

            var timingTolerance = BrowserTestConstants.ReaderTiming.TeleprompterTimingToleranceMs
                                  + BrowserTestConstants.ReaderTiming.CapturePollIntervalMs;

            for (var sampleIndex = 1; sampleIndex < samples.Length; sampleIndex++)
            {
                var previousSample = samples[sampleIndex - 1];
                var currentSample = samples[sampleIndex];
                var observedDelay = currentSample.AtMs - previousSample.AtMs;
                var expectedDelay = previousSample.DurationMs + previousSample.PauseMs;

                await Assert.That(observedDelay)
                    .IsBetween(expectedDelay - timingTolerance, expectedDelay + timingTolerance);
            }
        });

    [Test]
    public Task LearnTimingProbe_PlaybackSequenceMatchesExpectedWordByWordTiming() =>
        RunPageAsync(async page =>
        {
            var samples = await CaptureLearnSamplesAsync(
                page,
                BrowserTestConstants.Routes.LearnReaderTiming,
                BrowserTestConstants.ReaderTiming.BaseWpm,
                BrowserTestConstants.ReaderTiming.WordCount);

            await Assert.That(samples.Select(sample => sample.Word).ToArray()).IsEquivalentTo(BrowserTestConstants.ReaderTiming.ExpectedWords, CollectionOrdering.Matching);
            await AssertLearnTimingMatches(samples, LearnExpectations);
        });

    [Test]
    public Task LearnTimingProbe_UserSpeedChange_ChangesWordByWordTiming() =>
        RunPageAsync(async page =>
        {
            var slowSamples = await CaptureLearnSamplesAsync(
                page,
                BrowserTestConstants.Routes.LearnReaderTiming,
                BrowserTestConstants.ReaderTiming.LearnSlowWpm,
                BrowserTestConstants.ReaderTiming.WordCount);
            var fastSamples = await CaptureLearnSamplesAsync(
                page,
                BrowserTestConstants.Routes.LearnReaderTiming,
                BrowserTestConstants.ReaderTiming.LearnFastWpm,
                BrowserTestConstants.ReaderTiming.WordCount);

            await Assert.That(slowSamples.Select(sample => sample.Word).ToArray()).IsEquivalentTo(BrowserTestConstants.ReaderTiming.ExpectedWords, CollectionOrdering.Matching);
            await Assert.That(fastSamples.Select(sample => sample.Word).ToArray()).IsEquivalentTo(BrowserTestConstants.ReaderTiming.ExpectedWords, CollectionOrdering.Matching);

            await AssertLearnTimingMatches(slowSamples, LearnSlowExpectations);
            await AssertLearnTimingMatches(fastSamples, LearnFastExpectations);

            var slowPlaybackSpanMs = ReadPlaybackSpanMilliseconds(slowSamples);
            var fastPlaybackSpanMs = ReadPlaybackSpanMilliseconds(fastSamples);
            await Assert.That(fastPlaybackSpanMs < slowPlaybackSpanMs).IsTrue().Because($"Expected {BrowserTestConstants.ReaderTiming.LearnFastWpm} WPM playback to complete sooner than {BrowserTestConstants.ReaderTiming.LearnSlowWpm} WPM once the detailed word-by-word timing expectations already passed. Slow span: {slowPlaybackSpanMs} ms. Fast span: {fastPlaybackSpanMs} ms.");
        });

    private static IReadOnlyList<LearnTimingExpectation> BuildLearnExpectations(string scriptFileName, int targetWpm)
    {
        var processor = new RsvpTextProcessor();
        var script = File.ReadAllText(GetTimingProbeScriptPath(scriptFileName));
        var processed = processor.ParseScript(script);
        var playbackEngine = new RsvpPlaybackEngine
        {
            WordsPerMinute = targetWpm
        };

        playbackEngine.LoadTimeline(processed);

        var expectations = new List<LearnTimingExpectation>();
        for (var wordIndex = 0; wordIndex < processed.AllWords.Count; wordIndex++)
        {
            var word = processed.AllWords[wordIndex];
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            expectations.Add(new LearnTimingExpectation(
                NormalizeLearnDisplayWord(word),
                Math.Max(
                    LearnMinimumWordDurationMilliseconds,
                    (int)Math.Round(playbackEngine.GetWordDisplayTime(wordIndex, word).TotalMilliseconds)),
                playbackEngine.GetPauseAfterMilliseconds(wordIndex) ?? 0));
        }

        return expectations;
    }

    private static string GetTimingProbeScriptPath(string scriptFileName) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../tests/TestData/Scripts",
            scriptFileName));

    private static string NormalizeLearnDisplayWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        var startIndex = 0;
        var endIndex = word.Length - 1;

        while (startIndex <= endIndex && IsDisplayBoundaryPunctuation(word[startIndex]))
        {
            startIndex++;
        }

        while (endIndex >= startIndex && IsDisplayBoundaryPunctuation(word[endIndex]))
        {
            endIndex--;
        }

        return startIndex > endIndex
            ? string.Empty
            : word[startIndex..(endIndex + 1)];
    }

    private static bool IsDisplayBoundaryPunctuation(char character) =>
        char.IsPunctuation(character) && character is not '\'' and not '’';

    private static Task InstallWordRecorderAsync(IPage page, string testId) =>
        page.EvaluateAsync(
            """
            config => {
                const recorder = {
                    active: false,
                    durationAttributeName: config.durationAttributeName,
                    effectiveWpmAttributeName: config.effectiveWpmAttributeName,
                    lastWord: null,
                    pauseAttributeName: config.pauseAttributeName,
                    pollIntervalMs: config.pollIntervalMs,
                    samples: [],
                    testId: config.testId,
                    startMs: 0,
                    timer: 0
                };

                const readWord = () => {
                    if (!recorder.active) {
                        return;
                    }

                    const node = document.querySelector(`[data-test="${recorder.testId}"]`);
                    if (!(node instanceof HTMLElement)) {
                        return;
                    }

                    const word = (node.textContent ?? '').replace(/\s+/g, '');
                    if (!word || word === recorder.lastWord) {
                        return;
                    }

                    recorder.lastWord = word;
                    recorder.samples.push({
                        atMs: Math.round(performance.now() - recorder.startMs),
                        durationMs: Number(node.getAttribute(recorder.durationAttributeName) ?? 0),
                        effectiveWpm: Number(node.getAttribute(recorder.effectiveWpmAttributeName) ?? 0),
                        pauseMs: Number(node.getAttribute(recorder.pauseAttributeName) ?? 0),
                        word
                    });
                };

                recorder.start = () => {
                    recorder.active = true;
                    recorder.lastWord = null;
                    recorder.samples = [];
                    recorder.startMs = performance.now();
                    readWord();
                };

                recorder.markPlaybackStarted = () => {
                    if (recorder.samples.length > 0) {
                        const startedAtMs = Math.round(performance.now() - recorder.startMs);
                        const nextSample = recorder.samples[1];
                        recorder.samples[0].atMs = nextSample
                            ? Math.min(startedAtMs, nextSample.atMs)
                            : startedAtMs;
                    }
                };

                recorder.timer = window.setInterval(readWord, recorder.pollIntervalMs);
                window[config.key] = recorder;
            }
            """,
            new
            {
                key = ReaderTimingRecorderKey,
                durationAttributeName = UiDataAttributes.Teleprompter.DurationMilliseconds,
                effectiveWpmAttributeName = UiDataAttributes.Teleprompter.EffectiveWordsPerMinute,
                pauseAttributeName = UiDataAttributes.Teleprompter.PauseMilliseconds,
                pollIntervalMs = BrowserTestConstants.ReaderTiming.CapturePollIntervalMs,
                testId
            });

    private static Task StartWordRecorderAsync(IPage page) =>
        page.EvaluateAsync(
            """
            key => {
                const recorder = window[key];
                if (!recorder?.start) {
                    throw new Error(`Reader timing recorder '${key}' is not installed.`);
                }

                recorder.start();
            }
            """,
            ReaderTimingRecorderKey);

    private static Task MarkWordRecorderPlaybackStartedAsync(IPage page) =>
        page.EvaluateAsync(
            """
            key => {
                const recorder = window[key];
                if (!recorder?.markPlaybackStarted) {
                    throw new Error(`Reader timing recorder '${key}' is not installed.`);
                }

                recorder.markPlaybackStarted();
            }
            """,
            ReaderTimingRecorderKey);

    private static async Task<IReadOnlyList<RecordedWordSample>> CaptureLearnSamplesAsync(
        IPage page,
        string route,
        int targetWpm,
        int expectedSampleCount)
    {
        await SeedLearnSpeedAsync(page, targetWpm);
        await page.GotoAsync(UiTestHostConstants.BlankPagePath, new() { WaitUntil = WaitUntilState.Load });
        await ReaderRouteDriver.OpenLearnAsync(page, route);
        await Expect(page.GetByTestId(UiTestIds.Learn.Word)).ToBeVisibleAsync();
        await Assert.That(await ReadNormalizedLearnWordAsync(page)).IsEqualTo(BrowserTestConstants.ReaderTiming.FirstWord);
        await Expect(page.GetByTestId(UiTestIds.Learn.SpeedValue))
            .ToHaveTextAsync(targetWpm.ToString(System.Globalization.CultureInfo.InvariantCulture));

        await InstallWordRecorderAsync(page, UiTestIds.Learn.Word);
        await StartWordRecorderAsync(page);
        var playToggle = page.GetByTestId(UiTestIds.Learn.PlayToggle);
        await StartLearnPlaybackAsync(page, playToggle);

        var samples = await WaitForRecordedSamplesAsync(page, expectedSampleCount);
        await Expect(page.GetByTestId(UiTestIds.Learn.PlayToggle))
            .ToHaveAttributeAsync("aria-pressed", bool.FalseString.ToLowerInvariant());

        return samples;
    }

    private static async Task StartLearnPlaybackAsync(IPage page, ILocator playToggle)
    {
        Exception? lastFailure = null;

        for (var attempt = 1; attempt <= BrowserTestConstants.Timing.InteractionRetryCount; attempt++)
        {
            try
            {
                await UiInteractionDriver.ClickAndContinueAsync(playToggle, noWaitAfter: true);
                await WaitForLearnPlaybackStartedAsync(page);
                await MarkWordRecorderPlaybackStartedAsync(page);
                return;
            }
            catch (Exception exception) when (
                attempt < BrowserTestConstants.Timing.InteractionRetryCount &&
                exception is TimeoutException or PlaywrightException)
            {
                lastFailure = exception;
            }
        }

        throw lastFailure ?? new InvalidOperationException("Learn playback did not start after the interaction retries completed.");
    }

    private static Task WaitForLearnPlaybackStartedAsync(IPage page) =>
        page.WaitForFunctionAsync(
            """
            ([key, attributeName, testAttributeName, testId, trueValue]) => {
                const selector = `[${testAttributeName}="${testId}"]`;
                const playToggle = document.querySelector(selector);
                const hasAdvanced = (window[key]?.samples?.length ?? 0) > 1;
                return playToggle?.getAttribute(attributeName) === trueValue || hasAdvanced;
            }
            """,
            new object[]
            {
                ReaderTimingRecorderKey,
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.Html.DataTestAttribute,
                UiTestIds.Learn.PlayToggle,
                bool.TrueString.ToLowerInvariant()
            },
            new()
            {
                Timeout = BrowserTestConstants.Timing.ReaderPlaybackStartTimeoutMs
            });

    private static Task SeedLearnSpeedAsync(IPage page, int targetWpm) =>
        page.EvaluateAsync(
            """
            storage => {
                window.localStorage.setItem(storage.key, storage.json);
            }
            """,
            new
            {
                key = string.Concat(BrowserStorageKeys.SettingsPrefix, BrowserAppSettingsKeys.LearnSettings),
                json = JsonSerializer.Serialize(new LearnSettings(
                    HasCustomizedWordsPerMinute: true,
                    WordsPerMinute: targetWpm))
            });

    private static async Task AssertLearnTimingMatches(
        IReadOnlyList<RecordedWordSample> samples,
        IReadOnlyList<LearnTimingExpectation> expectations)
    {
        for (var sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
        {
            var previousSample = samples[sampleIndex - 1];
            var expected = expectations[sampleIndex - 1];
            var observedDelay = ReadObservedDelayMilliseconds(samples, sampleIndex);
            var expectedDelay = expected.DurationMs + expected.PauseMs;
            var toleranceMilliseconds = sampleIndex == 1
                ? BrowserTestConstants.ReaderTiming.LearnStartupTimingToleranceMs
                : Math.Max(
                    BrowserTestConstants.ReaderTiming.LearnTimingToleranceMs,
                    (int)Math.Ceiling(expectedDelay * BrowserTestConstants.ReaderTiming.LearnTimingToleranceRatio));

            await Assert.That(previousSample.Word).IsEqualTo(expected.Word);
            if (sampleIndex == 1)
            {
                await Assert.That(observedDelay).IsGreaterThanOrEqualTo(0);
                continue;
            }

            await Assert.That(observedDelay).IsBetween(expectedDelay - toleranceMilliseconds, expectedDelay + toleranceMilliseconds);
        }
    }

    private static int ReadObservedDelayMilliseconds(IReadOnlyList<RecordedWordSample> samples, int sampleIndex)
    {
        var previousSample = samples[sampleIndex - 1];
        var currentSample = samples[sampleIndex];
        return currentSample.AtMs - previousSample.AtMs;
    }

    private static int ReadPlaybackSpanMilliseconds(IReadOnlyList<RecordedWordSample> samples)
    {
        var firstSample = samples[0];
        var lastSample = samples[^1];
        return lastSample.AtMs - firstSample.AtMs;
    }

    private static async Task<string> ReadNormalizedLearnWordAsync(IPage page)
    {
        var rawWord = await page.GetByTestId(UiTestIds.Learn.Word).TextContentAsync() ?? string.Empty;
        return string.Concat(rawWord.Where(character => !char.IsWhiteSpace(character)));
    }

    private static async Task<IReadOnlyList<RecordedWordSample>> WaitForRecordedSamplesAsync(IPage page, int expectedSampleCount)
    {
        await page.WaitForFunctionAsync(
            """
            ([key, expectedCount]) => (window[key]?.samples?.length ?? 0) >= expectedCount
            """,
            new object[] { ReaderTimingRecorderKey, expectedSampleCount },
            new() { Timeout = BrowserTestConstants.ReaderTiming.SampleCaptureTimeoutMs });

        var samples = await page.EvaluateAsync<RecordedWordSample[]>(
            """
            key => {
                const recorder = window[key];
                if (recorder?.timer) {
                    window.clearInterval(recorder.timer);
                    recorder.timer = 0;
                }

                return recorder?.samples ?? [];
            }
            """,
            ReaderTimingRecorderKey);

        await Assert.That(samples).IsNotNull();
        await Assert.That(samples.Length >= expectedSampleCount).IsTrue().Because($"Expected at least {expectedSampleCount} recorded word samples, but captured {samples.Length}.");

        return samples.Take(expectedSampleCount).ToArray();
    }

    private sealed record LearnTimingExpectation(string Word, int DurationMs, int PauseMs);

    private sealed class RecordedWordSample
    {
        public int AtMs { get; set; }

        public int DurationMs { get; set; }

        public int EffectiveWpm { get; set; }

        public int PauseMs { get; set; }

        public string Word { get; set; } = string.Empty;
    }
}
