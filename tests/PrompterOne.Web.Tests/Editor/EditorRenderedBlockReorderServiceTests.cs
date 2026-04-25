using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Web.Tests;

public sealed class EditorRenderedBlockReorderServiceTests
{
    private const int SegmentIndex = 0;
    private const int OpeningBlockIndex = 0;
    private const int MiddleBlockIndex = 1;
    private const int ClosingBlockIndex = 2;
    private const string SegmentName = "Scene";
    private const string SegmentEmotionKey = "neutral";
    private const string SegmentEmotionLabel = "Neutral";
    private const string SegmentAccentColor = "#888888";
    private const int TargetWpm = 140;
    private const string EmptyDuration = "";
    private const string OpeningHeading = "### [Opening|Speaker:Host|140WPM|warm]";
    private const string MiddleHeading = "### [Middle|Speaker:Host|140WPM|focused]";
    private const string ClosingHeading = "### [Closing|Speaker:Host|140WPM|calm]";
    private const string Source = """
        # Reorder Probe
        ## [Scene|140WPM|neutral]
        ### [Opening|Speaker:Host|140WPM|warm]
        Alpha opening block.

        ### [Middle|Speaker:Host|140WPM|focused]
        Bravo middle block.

        ### [Closing|Speaker:Host|140WPM|calm]
        Charlie closing block.
        """;

    [Test]
    public void Reorder_MovesBlockAfterTargetAndPreservesContent()
    {
        var result = EditorRenderedBlockReorderService.Reorder(
            Source,
            CreateSegments(Source),
            new EditorRenderedBlockReorderRequest(
                SegmentIndex,
                OpeningBlockIndex,
                SegmentIndex,
                MiddleBlockIndex,
                InsertAfterTarget: true));

        Assert.NotNull(result);
        Assert.True(IndexOf(result.Text, MiddleHeading) < IndexOf(result.Text, OpeningHeading));
        Assert.True(IndexOf(result.Text, OpeningHeading) < IndexOf(result.Text, ClosingHeading));
        Assert.Contains("Alpha opening block.", result.Text, StringComparison.Ordinal);
        Assert.Contains("Bravo middle block.", result.Text, StringComparison.Ordinal);
        Assert.Contains("Charlie closing block.", result.Text, StringComparison.Ordinal);
    }

    [Test]
    public void Reorder_MovesBlockBeforeTargetForDragFromBelow()
    {
        var result = EditorRenderedBlockReorderService.Reorder(
            Source,
            CreateSegments(Source),
            new EditorRenderedBlockReorderRequest(
                SegmentIndex,
                ClosingBlockIndex,
                SegmentIndex,
                OpeningBlockIndex,
                InsertAfterTarget: false));

        Assert.NotNull(result);
        Assert.True(IndexOf(result.Text, ClosingHeading) < IndexOf(result.Text, OpeningHeading));
        Assert.True(IndexOf(result.Text, OpeningHeading) < IndexOf(result.Text, MiddleHeading));
    }

    private static IReadOnlyList<EditorOutlineSegmentViewModel> CreateSegments(string source)
    {
        var openingStart = IndexOf(source, OpeningHeading);
        var middleStart = IndexOf(source, MiddleHeading);
        var closingStart = IndexOf(source, ClosingHeading);
        return
        [
            new EditorOutlineSegmentViewModel(
                SegmentIndex,
                SegmentName,
                SegmentEmotionKey,
                SegmentEmotionLabel,
                SegmentAccentColor,
                TargetWpm,
                EmptyDuration,
                IndexOf(source, "## [Scene"),
                source.Length - 1,
                [
                    new EditorOutlineBlockViewModel(
                        OpeningBlockIndex,
                        "Opening",
                        "Warm",
                        TargetWpm,
                        openingStart,
                        middleStart - 1),
                    new EditorOutlineBlockViewModel(
                        MiddleBlockIndex,
                        "Middle",
                        "Focused",
                        TargetWpm,
                        middleStart,
                        closingStart - 1),
                    new EditorOutlineBlockViewModel(
                        ClosingBlockIndex,
                        "Closing",
                        "Calm",
                        TargetWpm,
                        closingStart,
                        source.Length - 1)
                ])
        ];
    }

    private static int IndexOf(string source, string value) =>
        source.IndexOf(value, StringComparison.Ordinal);
}
