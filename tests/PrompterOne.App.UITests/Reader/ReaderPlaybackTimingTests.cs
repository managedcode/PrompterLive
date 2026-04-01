using Microsoft.Playwright;
using PrompterOne.Core.Services.Rsvp;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class ReaderPlaybackTimingTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private const int LearnMinimumWordDurationMilliseconds = 60;
    private const string LearnWordSelector = "[data-testid='learn-word']";
    private const string ReaderTimingRecorderKey = "__prompterOneReaderTimingRecorder";
    private const string TeleprompterActiveWordSelector = ".rd-card-active .rd-w.rd-now";
    private const string TimingProbeScriptFileName = "test-reader-timing.tps";

    private static readonly IReadOnlyList<LearnTimingExpectation> LearnExpectations = BuildLearnExpectations();
    private static readonly IReadOnlyList<int> TeleprompterEffectiveWpmSequence =
    [
        BrowserTestConstants.ReaderTiming.BaseWpm,
        BrowserTestConstants.ReaderTiming.SlowWpm,
        BrowserTestConstants.ReaderTiming.BaseWpm,
        BrowserTestConstants.ReaderTiming.FastWpm,
        BrowserTestConstants.ReaderTiming.BaseWpm
    ];

    [Fact]
    public Task TeleprompterTimingProbe_PlaybackSequenceMatchesRenderedWordTimingMetadata() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterReaderTiming);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await InstallWordRecorderAsync(page, TeleprompterActiveWordSelector);
            await page.GetByTestId(UiTestIds.Teleprompter.PlayToggle).ClickAsync();

            var samples = await WaitForRecordedSamplesAsync(page, BrowserTestConstants.ReaderTiming.WordCount);

            Assert.Equal(BrowserTestConstants.ReaderTiming.ExpectedWords, samples.Select(sample => sample.Word).ToArray());
            Assert.Equal(TeleprompterEffectiveWpmSequence, samples.Select(sample => sample.EffectiveWpm).ToArray());

            for (var sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
            {
                var previousSample = samples[sampleIndex - 1];
                var currentSample = samples[sampleIndex];
                var observedDelay = currentSample.AtMs - previousSample.AtMs;
                var expectedDelay = previousSample.DurationMs + previousSample.PauseMs;

                Assert.InRange(
                    observedDelay,
                    expectedDelay - BrowserTestConstants.ReaderTiming.TeleprompterTimingToleranceMs,
                    expectedDelay + BrowserTestConstants.ReaderTiming.TeleprompterTimingToleranceMs);
            }
        });

    [Fact]
    public Task LearnTimingProbe_PlaybackSequenceMatchesExpectedWordByWordTiming() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.LearnReaderTiming);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Learn.Word))
                .ToContainTextAsync(BrowserTestConstants.ReaderTiming.FirstWord);
            await Expect(page.Locator($"#{UiDomIds.Learn.Speed}"))
                .ToHaveTextAsync(BrowserTestConstants.ReaderTiming.BaseWpm.ToString(System.Globalization.CultureInfo.InvariantCulture));

            await InstallWordRecorderAsync(page, LearnWordSelector);
            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();

            var samples = await WaitForRecordedSamplesAsync(page, BrowserTestConstants.ReaderTiming.WordCount);

            Assert.Equal(BrowserTestConstants.ReaderTiming.ExpectedWords, samples.Select(sample => sample.Word).ToArray());

            for (var sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
            {
                var previousSample = samples[sampleIndex - 1];
                var currentSample = samples[sampleIndex];
                var expected = LearnExpectations[sampleIndex - 1];
                var observedDelay = currentSample.AtMs - previousSample.AtMs;
                var expectedDelay = expected.DurationMs + expected.PauseMs;

                Assert.Equal(expected.Word, previousSample.Word);
                Assert.InRange(
                    observedDelay,
                    expectedDelay - BrowserTestConstants.ReaderTiming.LearnTimingToleranceMs,
                    expectedDelay + BrowserTestConstants.ReaderTiming.LearnTimingToleranceMs);
            }
        });

    private static IReadOnlyList<LearnTimingExpectation> BuildLearnExpectations()
    {
        var processor = new RsvpTextProcessor();
        var script = File.ReadAllText(GetTimingProbeScriptPath());
        var processed = processor.ParseScript(script);
        var playbackEngine = new RsvpPlaybackEngine
        {
            WordsPerMinute = BrowserTestConstants.ReaderTiming.BaseWpm
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

    private static string GetTimingProbeScriptPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../tests/TestData/Scripts",
            TimingProbeScriptFileName));

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

    private static Task InstallWordRecorderAsync(IPage page, string selector) =>
        page.EvaluateAsync(
            """
            config => {
                const recorder = {
                    lastWord: null,
                    pollIntervalMs: config.pollIntervalMs,
                    samples: [],
                    selector: config.selector,
                    startMs: performance.now(),
                    timer: 0
                };

                const readWord = () => {
                    const node = document.querySelector(recorder.selector);
                    if (!(node instanceof HTMLElement)) {
                        return;
                    }

                    const word = (node.textContent ?? '').trim();
                    if (!word || word === recorder.lastWord) {
                        return;
                    }

                    recorder.lastWord = word;
                    recorder.samples.push({
                        atMs: Math.round(performance.now() - recorder.startMs),
                        durationMs: Number(node.dataset.ms ?? 0),
                        effectiveWpm: Number(node.dataset.effectiveWpm ?? 0),
                        pauseMs: Number(node.dataset.pauseMs ?? 0),
                        word
                    });
                };

                readWord();
                recorder.timer = window.setInterval(readWord, recorder.pollIntervalMs);
                window[config.key] = recorder;
            }
            """,
            new
            {
                key = ReaderTimingRecorderKey,
                pollIntervalMs = BrowserTestConstants.ReaderTiming.CapturePollIntervalMs,
                selector
            });

    private static async Task<IReadOnlyList<RecordedWordSample>> WaitForRecordedSamplesAsync(IPage page, int expectedSampleCount)
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(BrowserTestConstants.ReaderTiming.SampleCaptureTimeoutMs))
        {
            var sampleCount = await ReadRecordedSampleCountAsync(page);
            if (sampleCount >= expectedSampleCount)
            {
                break;
            }

            await page.WaitForTimeoutAsync(BrowserTestConstants.ReaderTiming.CapturePollIntervalMs);
        }

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

        Assert.NotNull(samples);
        Assert.True(
            samples.Length >= expectedSampleCount,
            $"Expected at least {expectedSampleCount} recorded word samples, but captured {samples.Length}.");

        return samples.Take(expectedSampleCount).ToArray();
    }

    private static Task<int> ReadRecordedSampleCountAsync(IPage page) =>
        page.EvaluateAsync<int>(
            """
            key => window[key]?.samples?.length ?? 0
            """,
            ReaderTimingRecorderKey);

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
