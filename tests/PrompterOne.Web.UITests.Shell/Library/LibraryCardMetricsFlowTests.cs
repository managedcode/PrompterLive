using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LibraryCardMetricsFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const int DefaultAverageWpm = 140;
    private const string QuantumDocumentName = "test-quantum-computing.tps";

    [Test]
    public Task LibraryScreen_QuantumCardShowsRealTpsMetrics() =>
        RunPageAsync(async page =>
        {
            var expectedMetrics = await BuildExpectedMetricsAsync();

            await ShellRouteDriver.OpenLibraryAsync(page);

            var quantumCard = page.GetByTestId(BrowserTestConstants.Elements.QuantumCard);
            var wpm = page.GetByTestId(UiTestIds.Library.CardWpm(BrowserTestConstants.Scripts.QuantumId));
            var words = page.GetByTestId(UiTestIds.Library.CardWordCount(BrowserTestConstants.Scripts.QuantumId));
            var segments = page.GetByTestId(UiTestIds.Library.CardSegmentCount(BrowserTestConstants.Scripts.QuantumId));
            var duration = page.GetByTestId(UiTestIds.Library.CardDuration(BrowserTestConstants.Scripts.QuantumId));
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.LibraryFlow.MetricsScenario);
            await UiScenarioArtifacts.CaptureLocatorAsync(
                quantumCard,
                BrowserTestConstants.LibraryFlow.MetricsScenario,
                BrowserTestConstants.LibraryFlow.MetricsStep);

            await Expect(wpm).ToHaveTextAsync(expectedMetrics.WpmLabel);
            await Expect(words).ToHaveTextAsync(expectedMetrics.WordCountLabel);
            await Expect(segments).ToHaveTextAsync(expectedMetrics.SegmentCountLabel);
            await Expect(duration).ToHaveTextAsync(expectedMetrics.DurationLabel);
        });

    private static async Task<ExpectedMetrics> BuildExpectedMetricsAsync()
    {
        var path = ResolveScriptPath(QuantumDocumentName);
        var text = await File.ReadAllTextAsync(path);
        var documentReader = new TpsDocumentReader();
        var compiler = new ScriptCompiler();
        var previewService = new ScriptPreviewService(documentReader, compiler);
        var document = await documentReader.ReadAsync(text);
        var compiledScript = await compiler.CompileAsync(document);
        var previewSegments = await previewService.BuildPreviewAsync(text);
        var wordCount = compiledScript.Segments
            .SelectMany(segment => segment.Blocks)
            .SelectMany(block => block.Words)
            .Count(word => word.Metadata?.IsPause != true && !string.IsNullOrWhiteSpace(word.CleanText));
        var totalDuration = compiledScript.Segments
            .SelectMany(segment => segment.Blocks)
            .SelectMany(block => block.Words)
            .Aggregate(TimeSpan.Zero, (current, word) => current + word.DisplayDuration);
        var averageWpm = previewSegments.Count > 0
            ? (int)Math.Round(previewSegments.Average(segment => segment.TargetWpm))
            : DefaultAverageWpm;
        var segmentCount = Math.Max(1, previewSegments.Count);

        return new ExpectedMetrics(
            $"{averageWpm} WPM",
            $"{wordCount:N0} words",
            $"{segmentCount} segment{(segmentCount == 1 ? string.Empty : "s")}",
            $"{(int)Math.Max(totalDuration.TotalMinutes, 0)}:{totalDuration.Seconds:00}");
    }

    private static string ResolveScriptPath(string fileName) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../tests/TestData/Scripts",
            fileName));

    private readonly record struct ExpectedMetrics(
        string WpmLabel,
        string WordCountLabel,
        string SegmentCountLabel,
        string DurationLabel);
}
