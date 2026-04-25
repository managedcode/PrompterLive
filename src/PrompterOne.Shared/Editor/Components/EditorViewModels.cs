using PrompterOne.Core.Models.Editor;

namespace PrompterOne.Shared.Components.Editor;

public sealed record EditorOutlineSegmentViewModel(
    int Index,
    string Name,
    string EmotionKey,
    string EmotionLabel,
    string AccentColor,
    int TargetWpm,
    string DurationLabel,
    int StartIndex,
    int EndIndex,
    IReadOnlyList<EditorOutlineBlockViewModel> Blocks)
{
    public string TargetWpmLabel => $"{TargetWpm}WPM";
}

public sealed record EditorOutlineBlockViewModel(
    int Index,
    string Name,
    string EmotionLabel,
    int TargetWpm,
    int StartIndex,
    int EndIndex)
{
    public string TargetWpmLabel => $"{TargetWpm}WPM";
}

public sealed record EditorStatusViewModel(
    int Line,
    int Column,
    string Profile,
    int BaseWpm,
    int SegmentCount,
    int BlockCount,
    int WordCount,
    string Duration,
    string Version);

public sealed record EditorLocalRevisionViewModel(
    string Id,
    string SavedAtLabel,
    string Title,
    string DocumentName);

public sealed record EditorRenderedSegmentViewModel(
    int Index,
    string Number,
    string Name,
    string EmotionLabel,
    string TargetWpmLabel,
    string DurationLabel,
    IReadOnlyList<EditorRenderedBlockViewModel> Blocks);

public sealed record EditorRenderedBlockViewModel(
    int SegmentIndex,
    int BlockIndex,
    string Number,
    string Name,
    string EmotionLabel,
    string TargetWpmLabel,
    string Text);

public sealed record EditorRenderedBlockTextChange(
    int SegmentIndex,
    int BlockIndex,
    string Text);

public sealed record EditorNavigationTarget(
    int SegmentIndex,
    int? BlockIndex,
    int StartIndex,
    int EndIndex);

public sealed record EditorStructureHeaderEditorViewModel(
    string Label,
    int StartIndex,
    string Name,
    int? TargetWpm,
    string EmotionLabel,
    string Speaker,
    string Timing,
    bool SupportsTiming)
{
    public static EditorStructureHeaderEditorViewModel Empty(string label, bool supportsTiming) =>
        new(label, 0, string.Empty, null, string.Empty, string.Empty, string.Empty, supportsTiming);
}

public sealed record EditorSplitFeedbackViewModel(
    string Title,
    string Summary,
    string HeadingBadge,
    string DestinationNote,
    string DraftNote,
    string OpenLibraryLabel,
    IReadOnlyList<string> CreatedTitles,
    int AdditionalCount);

public sealed record EditorSelectionViewModel(
    EditorSelectionRange Range,
    int Line,
    int Column,
    double ToolbarTop,
    double ToolbarLeft)
{
    public static EditorSelectionViewModel Empty { get; } = new(
        EditorSelectionRange.Empty,
        1,
        1,
        0,
        0);

    public bool HasSelection => Range.HasSelection;
}

public enum EditorCommandKind
{
    Wrap,
    Insert,
    ClearColor
}

public sealed record EditorCommandRequest(
    EditorCommandKind Kind,
    string PrimaryToken,
    string? SecondaryToken = null,
    string PlaceholderText = "",
    int? CaretOffset = null);

public enum EditorHistoryCommand
{
    Undo,
    Redo
}
