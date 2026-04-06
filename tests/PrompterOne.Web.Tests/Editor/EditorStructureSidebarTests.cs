using Bunit;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorStructureSidebarTests : BunitContext
{
    private const string FocusedAccentColor = "#16A34A";
    private const string SplitActionLabel = "Open in Library";
    private const string SplitFeedbackBadge = "From ##";
    private const string SplitFeedbackDestination = "New scripts were saved to the library.";
    private const string SplitFeedbackDraftNote = "The current draft stayed open.";
    private const string SplitFeedbackSummary = "2 new scripts created.";
    private const string SplitFeedbackTitle = "Split complete";
    private const string WarmEmotion = "Warm";
    private const string EpisodeTitle = "Episode 1";
    private const string BlockTitle = "Opening";
    private const string CreatedEpisodeOne = "Episode 1 - How to Think About Systems";
    private const string CreatedEpisodeTwo = "Episode 2 - How Systems Talk to Each Other";
    private const string CreatedEpisodeThree = "Episode 3 - Event Sourcing and CQRS";

    public EditorStructureSidebarTests()
    {
        TestHarnessFactory.Create(this);
    }

    [Fact]
    public void EditorStructureSidebar_SplitButtonsInvokeExpectedModes_AndRenderSplitFeedback()
    {
        var requestedModes = new List<TpsDocumentSplitMode>();
        var openLibraryRequests = 0;
        var cut = Render<EditorStructureSidebar>(parameters => parameters
            .Add(component => component.ActiveSegmentIndex, 0)
            .Add(component => component.ActiveBlockIndex, 0)
            .Add(component => component.Segments, BuildSegments())
            .Add(component => component.SplitFeedback, BuildSplitFeedback())
            .Add(component => component.OnOpenLibraryRequested, () => openLibraryRequests++)
            .Add(component => component.OnSplitRequested, mode => requestedModes.Add(mode)));

        cut.FindByTestId(UiTestIds.Editor.SplitTopLevel).Click();
        cut.FindByTestId(UiTestIds.Editor.SplitSegment).Click();
        cut.FindByTestId(UiTestIds.Editor.SplitResultOpenLibrary).Click();

        Assert.Equal(
            [TpsDocumentSplitMode.TopLevelHeading, TpsDocumentSplitMode.SegmentHeading],
            requestedModes);
        Assert.Equal(1, openLibraryRequests);
        Assert.Equal(SplitFeedbackTitle, cut.FindByTestId(UiTestIds.Editor.SplitResultTitle).TextContent.Trim());
        Assert.Equal(SplitFeedbackSummary, cut.FindByTestId(UiTestIds.Editor.SplitResultSummary).TextContent.Trim());
        Assert.Equal(SplitFeedbackBadge, cut.FindByTestId(UiTestIds.Editor.SplitResultBadge).TextContent.Trim());
        Assert.Equal(SplitFeedbackDestination, cut.FindByTestId(UiTestIds.Editor.SplitResultLibrary).TextContent.Trim());
        Assert.Equal(SplitFeedbackDraftNote, cut.FindByTestId(UiTestIds.Editor.SplitResultCurrentDraft).TextContent.Trim());
        Assert.Equal(CreatedEpisodeOne, cut.FindByTestId(UiTestIds.Editor.SplitResultItem(0)).TextContent.Trim().Replace("01", string.Empty).Trim());
        Assert.Equal(CreatedEpisodeTwo, cut.FindByTestId(UiTestIds.Editor.SplitResultItem(1)).TextContent.Trim().Replace("02", string.Empty).Trim());
        Assert.Equal(CreatedEpisodeThree, cut.FindByTestId(UiTestIds.Editor.SplitResultItem(2)).TextContent.Trim().Replace("03", string.Empty).Trim());
        Assert.Equal("+2 more in Library", cut.FindByTestId(UiTestIds.Editor.SplitResultMore).TextContent.Trim());
        Assert.Equal(SplitActionLabel, cut.FindByTestId(UiTestIds.Editor.SplitResultOpenLibrary).TextContent.Trim());
    }

    private static IReadOnlyList<EditorOutlineSegmentViewModel> BuildSegments() =>
    [
        new(
            Index: 0,
            Name: EpisodeTitle,
            EmotionKey: "focused",
            EmotionLabel: WarmEmotion,
            AccentColor: FocusedAccentColor,
            TargetWpm: 140,
            DurationLabel: "0:30",
            StartIndex: 0,
            EndIndex: 20,
            Blocks:
            [
                new EditorOutlineBlockViewModel(
                    Index: 0,
                    Name: BlockTitle,
                    EmotionLabel: WarmEmotion,
                    TargetWpm: 140,
                    StartIndex: 0,
                    EndIndex: 20)
            ])
    ];

    private static EditorSplitFeedbackViewModel BuildSplitFeedback() =>
        new(
            Title: SplitFeedbackTitle,
            Summary: SplitFeedbackSummary,
            HeadingBadge: SplitFeedbackBadge,
            DestinationNote: SplitFeedbackDestination,
            DraftNote: SplitFeedbackDraftNote,
            OpenLibraryLabel: SplitActionLabel,
            CreatedTitles:
            [
                CreatedEpisodeOne,
                CreatedEpisodeTwo,
                CreatedEpisodeThree
            ],
            AdditionalCount: 2);
}
