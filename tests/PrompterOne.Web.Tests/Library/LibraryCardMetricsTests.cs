using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LibraryCardMetricsTests : BunitContext
{
    [Test]
    public async Task LibraryPage_UsesRealTpsMetricsOnCards_InsteadOfDisplayOverrides()
    {
        TestHarnessFactory.Create(this);

        var repository = Services.GetRequiredService<IScriptRepository>();
        var session = Services.GetRequiredService<IScriptSessionService>();
        var document = await repository.GetAsync(AppTestData.Scripts.QuantumId);
        Assert.NotNull(document);

        await session.OpenAsync(document!);
        var state = session.State;
        var expectedMetrics = BuildExpectedMetrics(state);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                expectedMetrics.WpmLabel,
                cut.FindByTestId(UiTestIds.Library.CardWpm(AppTestData.Scripts.QuantumId)).TextContent);
            Assert.Equal(
                expectedMetrics.WordCountLabel,
                cut.FindByTestId(UiTestIds.Library.CardWordCount(AppTestData.Scripts.QuantumId)).TextContent);
            Assert.Equal(
                expectedMetrics.SegmentCountLabel,
                cut.FindByTestId(UiTestIds.Library.CardSegmentCount(AppTestData.Scripts.QuantumId)).TextContent);
            Assert.Equal(
                expectedMetrics.DurationLabel,
                cut.FindByTestId(UiTestIds.Library.CardDuration(AppTestData.Scripts.QuantumId)).TextContent);
        });
    }

    private static ExpectedMetrics BuildExpectedMetrics(PrompterOne.Core.Models.Workspace.ScriptWorkspaceState state)
    {
        var averageWpm = state.PreviewSegments.Count > 0
            ? (int)Math.Round(state.PreviewSegments.Average(segment => segment.TargetWpm))
            : 140;
        var segmentCount = Math.Max(1, state.PreviewSegments.Count);
        var duration = state.EstimatedDuration;

        return new ExpectedMetrics(
            WpmLabel: $"{averageWpm} WPM",
            WordCountLabel: $"{state.WordCount:N0} words",
            SegmentCountLabel: $"{segmentCount} segment{(segmentCount == 1 ? string.Empty : "s")}",
            DurationLabel: $"{(int)Math.Max(duration.TotalMinutes, 0)}:{duration.Seconds:00}");
    }

    private readonly record struct ExpectedMetrics(
        string WpmLabel,
        string WordCountLabel,
        string SegmentCountLabel,
        string DurationLabel);
}
