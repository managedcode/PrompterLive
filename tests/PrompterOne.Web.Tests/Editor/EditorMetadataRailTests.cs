using Bunit;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Tests;
using PrompterOne.Testing.Editor;

namespace PrompterOne.Web.Tests;

public sealed class EditorMetadataRailTests : BunitContext
{
    private const string SplitHint = "Create separate scripts from headings in the current draft.";
    private const string SplitSection = "Split Into New Scripts";
    private const string SplitTopLevelLabel = "New scripts from # headings";

    public EditorMetadataRailTests()
    {
        TestHarnessFactory.Create(this);
    }

    [Test]
    public void EditorMetadataRail_SplitActionsInvokeExpectedModes_AndRenderSplitFeedback()
    {
        var requestedModes = new List<TpsDocumentSplitMode>();
        var openLibraryRequests = 0;
        var cut = Render<EditorMetadataRail>(parameters => parameters
            .Add(component => component.Title, "Product Launch")
            .Add(component => component.Profile, "Actor")
            .Add(component => component.Status, new EditorStatusViewModel(2, 4, "Actor", 140, 0, 0, 256, "1:30", "1.0"))
            .Add(component => component.LocalHistory, [])
            .Add(component => component.SplitFeedback, BuildSplitFeedback())
            .Add(component => component.OpenLibraryRequested, () => openLibraryRequests++)
            .Add(component => component.SplitRequested, mode => requestedModes.Add(mode)));

        Assert.Contains(SplitSection, cut.Markup, StringComparison.Ordinal);
        Assert.Equal(SplitHint, cut.FindByTestId(UiTestIds.Editor.SplitHint).TextContent.Trim());
        Assert.Equal(SplitTopLevelLabel, cut.FindByTestId(UiTestIds.Editor.SplitTopLevel).TextContent.Trim());
        Assert.Equal(EditorSplitFeedbackTestData.SplitSegmentActionLabel, cut.FindByTestId(UiTestIds.Editor.SplitSegment).TextContent.Trim());

        cut.FindByTestId(UiTestIds.Editor.SplitTopLevel).Click();
        cut.FindByTestId(UiTestIds.Editor.SplitSegment).Click();
        cut.FindByTestId(UiTestIds.Editor.SplitResultOpenLibrary).Click();

        Assert.Equal(
            [TpsDocumentSplitMode.TopLevelHeading, TpsDocumentSplitMode.SegmentHeading],
            requestedModes);
        Assert.Equal(1, openLibraryRequests);
        Assert.Equal(EditorSplitFeedbackTestData.SplitFeedbackTitle, cut.FindByTestId(UiTestIds.Editor.SplitResultTitle).TextContent.Trim());
        Assert.Equal(EditorSplitFeedbackTestData.SplitFeedbackSummary, cut.FindByTestId(UiTestIds.Editor.SplitResultSummary).TextContent.Trim());
        Assert.Equal(EditorSplitFeedbackTestData.SplitFeedbackBadge, cut.FindByTestId(UiTestIds.Editor.SplitResultBadge).TextContent.Trim());
        Assert.Equal(EditorSplitFeedbackTestData.SplitFeedbackDestination, cut.FindByTestId(UiTestIds.Editor.SplitResultLibrary).TextContent.Trim());
        Assert.Equal(EditorSplitFeedbackTestData.SplitFeedbackDraftNote, cut.FindByTestId(UiTestIds.Editor.SplitResultCurrentDraft).TextContent.Trim());
        Assert.Equal(EditorSplitFeedbackTestData.EpisodeOneTitle, cut.FindByTestId(UiTestIds.Editor.SplitResultItem(0)).TextContent.Trim().Replace("01", string.Empty).Trim());
        Assert.Equal(EditorSplitFeedbackTestData.EpisodeTwoTitle, cut.FindByTestId(UiTestIds.Editor.SplitResultItem(1)).TextContent.Trim().Replace("02", string.Empty).Trim());
        Assert.Equal(EditorSplitFeedbackTestData.EpisodeThreeTitle, cut.FindByTestId(UiTestIds.Editor.SplitResultItem(2)).TextContent.Trim().Replace("03", string.Empty).Trim());
        Assert.Equal("+2 more in Library", cut.FindByTestId(UiTestIds.Editor.SplitResultMore).TextContent.Trim());
        Assert.Equal(EditorSplitFeedbackTestData.SplitActionLabel, cut.FindByTestId(UiTestIds.Editor.SplitResultOpenLibrary).TextContent.Trim());
    }

    private static EditorSplitFeedbackViewModel BuildSplitFeedback() =>
        new(
            Title: EditorSplitFeedbackTestData.SplitFeedbackTitle,
            Summary: EditorSplitFeedbackTestData.SplitFeedbackSummary,
            HeadingBadge: EditorSplitFeedbackTestData.SplitFeedbackBadge,
            DestinationNote: EditorSplitFeedbackTestData.SplitFeedbackDestination,
            DraftNote: EditorSplitFeedbackTestData.SplitFeedbackDraftNote,
            OpenLibraryLabel: EditorSplitFeedbackTestData.SplitActionLabel,
            CreatedTitles: EditorSplitFeedbackTestData.MetadataRailCreatedTitles,
            AdditionalCount: EditorSplitFeedbackTestData.MetadataRailAdditionalCount);
}
