namespace PrompterOne.Testing.Editor;

public static class EditorSplitFeedbackTestData
{
    public const int MetadataRailAdditionalCount = 2;
    public const string EpisodeOneTitle = "Episode 1 - How to Think About Systems";
    public const string EpisodeThreeTitle = "Episode 3 - Event Sourcing and CQRS";
    public const string EpisodeTwoTitle = "Episode 2 - How Systems Talk to Each Other";
    public const string JordanTitle = "Jordan";
    public const string SplitActionLabel = "Open in Library";
    public const string SplitFeedbackBadge = "From ##";
    public const string SplitFeedbackDestination = "New scripts were saved to the library.";
    public const string SplitFeedbackSiblingDestination = "New scripts were saved next to this script.";
    public const string SplitFeedbackDraftNote = "The current draft stayed open.";
    public const string SplitSpeakerActionLabel = "New scripts by speaker";
    public const string SplitSpeakerBadge = "By speaker";
    public const string SplitFeedbackSummary = "2 new scripts created.";
    public const string SplitFeedbackTitle = "Split complete";
    public const string SplitSegmentActionLabel = "New scripts from ## headings";
    public const string SplitSource =
        """
        ## [Episode 1 - How to Think About Systems|140WPM|Professional]
        Before you write code, / you need to think about the system. //

        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional]
        APIs, events, and retries matter. //
        """;

    public const string SplitBySpeakerSource =
        """
        ## [Opening|Speaker:Alex|140WPM|warm]
        Alex intro line. //

        ### [Question|Speaker:Jordan|130WPM|focused]
        Jordan asks the question. //

        ### [Answer|135WPM|professional]
        Alex answers with inherited speaker context. //

        ## [Wrap Up|150WPM|motivational]
        ### [Signoff|Speaker:Jordan|150WPM|focused]
        Jordan closes the interview. //
        """;

    public const string EditedSplitSource =
        """
        ## [Episode 1 - How to Think About Systems|140WPM|Professional]
        Before you write code, / you need to think about the system. //

        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional]
        APIs, events, and retries matter. //

        Notes: keep the current draft open while reviewing the split.
        """;

    public static IReadOnlyList<string> MetadataRailCreatedTitles { get; } =
    [
        EpisodeOneTitle,
        EpisodeTwoTitle,
        EpisodeThreeTitle
    ];

    public static IReadOnlyList<string> SpeakerCreatedTitles { get; } =
    [
        "Alex",
        JordanTitle
    ];
}
