using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorStructureAuthoringTests : BunitContext
{
    private static readonly TimeSpan StructureRefreshTimeout = TimeSpan.FromSeconds(10);
    private readonly AppHarness _harness;

    public EditorStructureAuthoringTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public void EditorPage_ChangingSourceHeadersRefreshesStructureTreeWithoutLegacyInspector()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorQuantum);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(EditorStructureAuthoringTestSource.InitialSegmentHeading, source.GetAttribute("value"));
            Assert.Contains("Introduction", cut.Markup);
            Assert.DoesNotContain("ACTIVE SEGMENT", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("ACTIVE BLOCK", cut.Markup, StringComparison.Ordinal);
        });

        var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
        var updatedSource = (source.GetAttribute("value") ?? string.Empty)
            .Replace("## [Introduction|280WPM|neutral|0:00-1:10]", "## [Launch Angle|205WPM|focused|1:00-2:00]", StringComparison.Ordinal)
            .Replace("### [Overview Block|280WPM|neutral]", "### [Signal Block|205WPM|professional]", StringComparison.Ordinal);

        source.Input(updatedSource);

        cut.WaitForAssertion(() =>
            {
                var currentSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value");
                Assert.Contains("## [Launch Angle|205WPM|focused|1:00-2:00]", currentSource);
                Assert.Contains("### [Signal Block|205WPM|professional]", currentSource);
                Assert.Contains("data-nav=\"seg-0\"", cut.Markup, StringComparison.Ordinal);
                Assert.Contains("Launch Angle", cut.Markup, StringComparison.Ordinal);
                Assert.Contains("Signal Block", cut.Markup, StringComparison.Ordinal);
                Assert.DoesNotContain("ACTIVE SEGMENT", cut.Markup, StringComparison.Ordinal);
            },
            StructureRefreshTimeout);
    }

    [Test]
    public void EditorPage_ChangingSpeedOffsetsRewritesFrontMatter()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
            Assert.Equal(EditorStructureAuthoringTestSource.DefaultXslowOffset, cut.FindByTestId(UiTestIds.Editor.SpeedXslow).GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.SpeedXslow).Change(EditorStructureAuthoringTestSource.UpdatedXslowOffset);
        cut.FindByTestId(UiTestIds.Editor.SpeedSlow).Change(EditorStructureAuthoringTestSource.UpdatedSlowOffset);
        cut.FindByTestId(UiTestIds.Editor.SpeedFast).Change(EditorStructureAuthoringTestSource.UpdatedFastOffset);
        cut.FindByTestId(UiTestIds.Editor.SpeedXfast).Change(EditorStructureAuthoringTestSource.UpdatedXfastOffset);

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.DoesNotContain(EditorStructureAuthoringTestSource.SpeedOffsetsField, visibleSource, StringComparison.Ordinal);
            Assert.Contains(EditorStructureAuthoringTestSource.SpeedOffsetsField, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorStructureAuthoringTestSource.UpdatedXslowPersistence, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorStructureAuthoringTestSource.UpdatedSlowPersistence, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorStructureAuthoringTestSource.UpdatedFastPersistence, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorStructureAuthoringTestSource.UpdatedXfastPersistence, persistedText, StringComparison.Ordinal);
        });
    }

    private static class EditorStructureAuthoringTestSource
    {
        public const string DefaultXslowOffset = "-40";
        public const string InitialSegmentHeading = "## [Introduction|280WPM|neutral|0:00-1:10]";
        public const string UpdatedFastOffset = "30";
        public const string SpeedOffsetsField = "speed_offsets:";
        public const string UpdatedFastPersistence = "  fast: 30";
        public const string UpdatedSlowOffset = "-15";
        public const string UpdatedSlowPersistence = "  slow: -15";
        public const string UpdatedXfastOffset = "55";
        public const string UpdatedXfastPersistence = "  xfast: 55";
        public const string UpdatedXslowOffset = "-45";
        public const string UpdatedXslowPersistence = "  xslow: -45";
    }
}
