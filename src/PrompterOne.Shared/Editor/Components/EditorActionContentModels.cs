namespace PrompterOne.Shared.Components.Editor;

public enum EditorActionIconKind
{
    None,
    ChevronDown,
    Spark,
    PauseClock,
    PauseBars,
    Lightning,
    SemanticDotVoice,
    SemanticDotEmotion,
    EditPoint,
    Highlight,
    Phonetic,
    SpeakerHigh,
    Undo,
    Redo
}

public enum EditorActionIconTone
{
    Default,
    Gold,
    Red,
    Orange,
    Purple,
    Voice,
    Emotion
}

public enum EditorActionContentTone
{
    Default,
    Emphasis,
    Highlight,
    Loud,
    Soft,
    Whisper,
    Legato,
    Staccato,
    Energy,
    Melody,
    Stress,
    SpeedXslow,
    SpeedSlow,
    SpeedNormal,
    SpeedFast,
    SpeedXfast,
    Structure,
    Pronunciation,
    Reset,
    DeliverySarcasm,
    DeliveryAside,
    DeliveryRhetorical,
    DeliveryBuilding,
    EmotionNeutral,
    EmotionWarm,
    EmotionProfessional,
    EmotionFocused,
    EmotionConcerned,
    EmotionUrgent,
    EmotionMotivational,
    EmotionExcited,
    EmotionHappy,
    EmotionSad,
    EmotionCalm,
    EmotionEnergetic
}

public sealed record EditorActionContentDescriptor(
    EditorActionIconKind LeadingIcon = EditorActionIconKind.None,
    EditorActionIconTone LeadingIconTone = EditorActionIconTone.Default,
    string LeadingText = "",
    EditorActionContentTone LeadingTone = EditorActionContentTone.Default,
    string Label = "",
    string MetaText = "",
    EditorActionIconKind TrailingIcon = EditorActionIconKind.None,
    EditorActionIconTone TrailingIconTone = EditorActionIconTone.Default,
    bool LeadingBold = false,
    bool LeadingItalic = false,
    bool LeadingMono = false)
{
    public bool HasLabel => !string.IsNullOrWhiteSpace(Label);
    public bool HasLeadingIcon => LeadingIcon is not EditorActionIconKind.None;
    public bool HasLeadingText => !string.IsNullOrWhiteSpace(LeadingText);
    public bool HasMetaText => !string.IsNullOrWhiteSpace(MetaText);
    public bool HasTrailingIcon => TrailingIcon is not EditorActionIconKind.None;
}

internal static class EditorActionContents
{
    public static EditorActionContentDescriptor Glyph(
        string text,
        EditorActionContentTone tone = EditorActionContentTone.Default,
        bool bold = false,
        bool italic = false,
        bool mono = false) =>
        new(
            LeadingText: text,
            LeadingTone: tone,
            LeadingBold: bold,
            LeadingItalic: italic,
            LeadingMono: mono);

    public static EditorActionContentDescriptor Icon(
        EditorActionIconKind icon,
        EditorActionIconTone tone = EditorActionIconTone.Default) =>
        new(
            LeadingIcon: icon,
            LeadingIconTone: tone);

    public static EditorActionContentDescriptor IconLabel(
        EditorActionIconKind icon,
        string label,
        string metaText = "",
        EditorActionIconTone tone = EditorActionIconTone.Default) =>
        new(
            LeadingIcon: icon,
            LeadingIconTone: tone,
            Label: label,
            MetaText: metaText);

    public static EditorActionContentDescriptor Label(
        string leadingText,
        string label,
        string metaText = "",
        EditorActionContentTone tone = EditorActionContentTone.Default,
        bool mono = false) =>
        new(
            LeadingText: leadingText,
            LeadingTone: tone,
            Label: label,
            MetaText: metaText,
            LeadingBold: true,
            LeadingMono: mono);

    public static EditorActionContentDescriptor Emotion(
        string label,
        string metaText,
        EditorActionContentTone tone) =>
        new(
            LeadingText: metaText,
            LeadingTone: tone,
            Label: label,
            LeadingBold: true);

    public static EditorActionContentDescriptor LabelTrigger(
        string leadingText,
        EditorActionContentTone tone = EditorActionContentTone.Default,
        bool mono = false) =>
        new(
            LeadingText: leadingText,
            LeadingTone: tone,
            TrailingIcon: EditorActionIconKind.ChevronDown,
            LeadingBold: true,
            LeadingMono: mono);

    public static EditorActionContentDescriptor Trigger(
        EditorActionIconKind leadingIcon,
        EditorActionIconTone tone = EditorActionIconTone.Default) =>
        new(
            LeadingIcon: leadingIcon,
            LeadingIconTone: tone,
            TrailingIcon: EditorActionIconKind.ChevronDown);

    public static EditorActionContentDescriptor IconTrigger(
        EditorActionIconKind icon,
        string label,
        EditorActionIconTone tone = EditorActionIconTone.Default) =>
        new(
            LeadingIcon: icon,
            LeadingIconTone: tone,
            Label: label,
            TrailingIcon: EditorActionIconKind.ChevronDown);
}
