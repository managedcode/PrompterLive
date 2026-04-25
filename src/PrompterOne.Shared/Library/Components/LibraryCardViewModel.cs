namespace PrompterOne.Shared.Components.Library;

public sealed record LibraryCardViewModel(
    string Id,
    string Title,
    string Emotion,
    string CoverClass,
    string AccentColor,
    int AverageWpm,
    int WordCount,
    int SegmentCount,
    string Author,
    DateTimeOffset UpdatedAt,
    string UpdatedLabel,
    string ModeLabel,
    TimeSpan Duration,
    string DurationLabel,
    string DocumentName,
    string SearchText,
    string? FolderId,
    bool IsFavorite,
    int DisplayOrder,
    string TestId)
{
    public string WpmLabel => $"{AverageWpm} WPM";

    public string WordCountLabel => $"{WordCount:N0} words";

    public string SegmentCountLabel => $"{SegmentCount} segment{(SegmentCount == 1 ? string.Empty : "s")}";
}
