using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Components.Editor;

public enum EditorToolbarActionType
{
    Command,
    History,
    Ai,
    ToggleMenu
}

public sealed record EditorToolbarActionDescriptor(
    string Key,
    EditorToolbarActionType ActionType,
    string CssClass,
    string ContentHtml,
    string Tooltip,
    string? TestId = null,
    string? Style = null,
    string? MenuId = null,
    EditorCommandRequest? Command = null,
    EditorHistoryCommand? HistoryCommand = null,
    EditorAiAssistAction? AiAction = null,
    bool PreventMouseDown = false);

public sealed record EditorToolbarDropdownGroupDescriptor(
    string Label,
    IReadOnlyList<EditorToolbarActionDescriptor> Actions,
    bool HasSeparatorBefore = false);

public sealed record EditorToolbarSectionDescriptor(
    string Key,
    string Label,
    string? MenuId,
    string? DropdownTestId,
    string? DropdownStyle,
    IReadOnlyList<EditorToolbarActionDescriptor> MainActions,
    IReadOnlyList<EditorToolbarDropdownGroupDescriptor> DropdownGroups)
{
    public bool HasDropdown => !string.IsNullOrWhiteSpace(MenuId);
}

public static class EditorToolbarMenuIds
{
    public const string Format = "format";
    public const string Color = "color";
    public const string Emotion = "emotion";
    public const string FloatingEmotion = "floating-emotion";
    public const string Pause = "pause";
    public const string Speed = "speed";
    public const string Insert = "insert";
}

public static class EditorToolbarCatalog
{
    private const string ChevronDownIcon = """<svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="6,9 12,15 18,9" /></svg>""";
    private const string SparkIcon = """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2L9 9 2 12l7 3 3 7 3-7 7-3-7-3z" /></svg>""";
    private const string PauseClockIcon = """<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10" /><polyline points="12,6 12,12 16,14" /></svg>""";
    private const string PauseFloatIcon = """<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="6" y="4" width="4" height="16"></rect><rect x="14" y="4" width="4" height="16"></rect></svg>""";
    private const string LightningIcon = """<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="13,2 3,14 12,14 11,22 21,10 12,10" /></svg>""";
    private const string EditPointIconGold = """<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#FFD060" stroke-width="2"><path d="M12 2L2 7l10 5 10-5-10-5z" /><path d="M2 17l10 5 10-5" /><path d="M2 12l10 5 10-5" /></svg>""";
    private const string EditPointIconRed = """<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#FF6060" stroke-width="2" style="flex-shrink:0"><path d="M12 2L2 7l10 5 10-5-10-5z" /><path d="M2 17l10 5 10-5" /><path d="M2 12l10 5 10-5" /></svg>""";
    private const string EditPointIconOrange = """<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#FFB86A" stroke-width="2" style="flex-shrink:0"><path d="M12 2L2 7l10 5 10-5-10-5z" /><path d="M2 17l10 5 10-5" /><path d="M2 12l10 5 10-5" /></svg>""";
    private const string PhoneticIcon = """<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#D88AFF" stroke-width="2"><path d="M12 1a3 3 0 0 0-3 3v8a3 3 0 0 0 6 0V4a3 3 0 0 0-3-3z" /><path d="M19 10v2a7 7 0 0 1-14 0v-2" /></svg>""";
    private const string HighlightIcon = """<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="#FFE066" stroke-width="2"><rect x="3" y="14" width="18" height="6" rx="2"></rect><path d="M8 14V8a4 4 0 0 1 8 0v6"></path></svg>""";
    private const string UndoIcon = """<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="1,4 1,10 7,10" /><path d="M3.51 15a9 9 0 1 0 2.13-9.36L1 10" /></svg>""";
    private const string RedoIcon = """<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23,4 23,10 17,10" /><path d="M20.49 15a9 9 0 1 1-2.13-9.36L23 10" /></svg>""";

    public static IReadOnlyList<EditorToolbarSectionDescriptor> Sections { get; } =
    [
        new(
            "history",
            "History",
            null,
            null,
            null,
            [
                History("undo", UndoIcon, "Undo last action (Ctrl+Z)", "editor-undo", EditorHistoryCommand.Undo),
                History("redo", RedoIcon, "Redo last undone action (Ctrl+Y)", "editor-redo", EditorHistoryCommand.Redo)
            ],
            []),
        new(
            "format",
            "Format",
            EditorToolbarMenuIds.Format,
            "editor-menu-format",
            null,
            [
                Wrap("bold", "<b>B</b>", "Bold — strong emphasis level 2. Markdown: **text**", "editor-bold", "**", "**"),
                Wrap("italic", "<i>I</i>", "Italic — emphasis level 1. Markdown: *text*", "editor-italic", "*", "*"),
                Wrap("emphasis", "Em", "Emphasis — standard emphasis level 1. TPS: [emphasis]text[/emphasis]", "editor-emphasis", "[emphasis]", "[/emphasis]"),
                Toggle(EditorToolbarMenuIds.Format, "editor-format-trigger", $"More formatting options{string.Empty}", ChevronDownIcon)
            ],
            [
                new("Formatting", [
                    Wrap("format-bold", "<b style=\"color:var(--text-1)\">B</b> Bold <small>**text**</small>", "Bold — strong emphasis level 2. Markdown: **text**", "editor-format-bold", "**", "**", cssClass: "tb-emo-item"),
                    Wrap("format-italic", "<i style=\"color:var(--text-1)\">I</i> Italic <small>*text*</small>", "Italic — emphasis level 1. Markdown: *text*", "editor-format-italic", "*", "*", cssClass: "tb-emo-item"),
                    Wrap("format-emphasis", "<span style=\"color:var(--gold-text);font-weight:600\">Em</span> Emphasis <small>[emphasis]</small>", "Emphasis — standard emphasis level 1. TPS: [emphasis]text[/emphasis]", "editor-format-emphasis", "[emphasis]", "[/emphasis]", cssClass: "tb-emo-item"),
                    Wrap("format-highlight", "<span style=\"background:rgba(255,232,122,.18);padding:1px 4px;border-radius:3px;color:#FFE87A\">H</span> Highlight <small>[highlight]</small>", "Highlight [highlight]", "editor-format-highlight", "[highlight]", "[/highlight]", cssClass: "tb-emo-item"),
                    Wrap("format-stress", "<span style=\"color:#FFD060;font-weight:700\">S</span> Stress <small>[stress]</small>", "Stress wrap — mark a word or phrase as stressed. [stress]text[/stress]", "editor-format-stress", "[stress]", "[/stress]", cssClass: "tb-emo-item")
                ])
            ]),
        new(
            "voice",
            "Voice",
            EditorToolbarMenuIds.Color,
            "editor-menu-color",
            null,
            [
                Toggle(EditorToolbarMenuIds.Color, "editor-color-trigger", "Voice cues — volume and stress tags for the current selection", "<span class=\"cdot\" style=\"background:#FFD060\"></span>" + ChevronDownIcon)
            ],
            [
                new("Volume", [
                    Wrap("voice-loud", "<span style=\"color:#FFB86A;font-weight:700;width:44px;text-align:right\">LOUD</span> Loud <small>[loud]</small>", "Raise vocal force without changing emotion. [loud]text[/loud]", "editor-color-green", "[loud]", "[/loud]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("voice-soft", "<span style=\"color:#B7D9FF;font-weight:700;width:44px;text-align:right\">SOFT</span> Soft <small>[soft]</small>", "Soften delivery and keep the phrase gentle. [soft]text[/soft]", "editor-color-soft", "[soft]", "[/soft]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("voice-whisper", "<span style=\"color:#CFCBD7;font-weight:700;width:44px;text-align:right\">LOW</span> Whisper <small>[whisper]</small>", "Very quiet, intimate delivery. [whisper]text[/whisper]", "editor-color-whisper", "[whisper]", "[/whisper]", cssClass: "tb-emo-item tb-tip")
                ]),
                new("Stress", [
                    Wrap("voice-stress", "<span style=\"color:#FFD060;font-weight:700;width:44px;text-align:right\">MARK</span> Stress <small>[stress]</small>", "Stress a word or phrase inline. [stress]text[/stress]", "editor-color-stress", "[stress]", "[/stress]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("voice-stress-guide", "<span style=\"color:#FFD060;font-weight:700;width:44px;text-align:right\">GUIDE</span> Stress guide <small>[stress:...]</small>", "Add a spoken stress cue. [stress:rising]text[/stress]", "editor-color-guide", "[stress:rising]", "[/stress]", cssClass: "tb-emo-item tb-tip", placeholder: "text")
                ], HasSeparatorBefore: true),
                new("Reset", [
                    ClearColor("voice-clear", "Remove supported inline TPS wrappers from the selected text", "editor-color-clear")
                ], HasSeparatorBefore: true)
            ]),
        new(
            "emotion",
            "Emotion",
            EditorToolbarMenuIds.Emotion,
            "editor-menu-emotion",
            null,
            [
                Toggle(EditorToolbarMenuIds.Emotion, "editor-emotion-trigger", "Emotion — applies mood-based color styling and presentation hints. Used on segments, blocks, or inline text", "<span class=\"cdot\" style=\"background:#FFB840\"></span>" + ChevronDownIcon)
            ],
            [
                new("TPS Emotions", [
                    Wrap("emotion-warm", "<span class=\"cdot\" style=\"background:#FFB840\"></span> Warm <small>😊</small>", "Friendly, welcoming tone. Inline: [warm]text[/warm]", "editor-emotion-warm", "[warm]", "[/warm]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-concerned", "<span class=\"cdot\" style=\"background:#FF7A7A\"></span> Concerned <small>😟</small>", "Worried, empathetic. Inline: [concerned]text[/concerned]", "editor-emotion-concerned", "[concerned]", "[/concerned]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-focused", "<span class=\"cdot\" style=\"background:#4AE0A0\"></span> Focused <small>🎯</small>", "Concentrated, precise. Inline: [focused]text[/focused]", "editor-emotion-focused", "[focused]", "[/focused]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-motivational", "<span class=\"cdot\" style=\"background:#C88AFF\"></span> Motivational <small>💪</small>", "Inspiring, encouraging. Inline: [motivational]text[/motivational]", UiTestIds.Editor.EmotionMotivational, "[motivational]", "[/motivational]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-neutral", "<span class=\"cdot\" style=\"background:#8ECFFF\"></span> Neutral <small>😐</small>", "Default balanced tone. Inline: [neutral]text[/neutral]", "editor-emotion-neutral", "[neutral]", "[/neutral]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-urgent", "<span class=\"cdot\" style=\"background:#FF6060\"></span> Urgent <small>🚨</small>", "Critical, immediate attention. Inline: [urgent]text[/urgent]", "editor-emotion-urgent", "[urgent]", "[/urgent]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-happy", "<span class=\"cdot\" style=\"background:#FFE87A\"></span> Happy <small>😄</small>", "Joyful, positive. Inline: [happy]text[/happy]", "editor-emotion-happy", "[happy]", "[/happy]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-excited", "<span class=\"cdot\" style=\"background:#FF8AC8\"></span> Excited <small>🚀</small>", "Enthusiastic, energetic. Inline: [excited]text[/excited]", "editor-emotion-excited", "[excited]", "[/excited]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-sad", "<span class=\"cdot\" style=\"background:#A0A8FF\"></span> Sad <small>😢</small>", "Melancholy, somber. Inline: [sad]text[/sad]", "editor-emotion-sad", "[sad]", "[/sad]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-calm", "<span class=\"cdot\" style=\"background:#5EECC2\"></span> Calm <small>😌</small>", "Peaceful, relaxed. Inline: [calm]text[/calm]", "editor-emotion-calm", "[calm]", "[/calm]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-energetic", "<span class=\"cdot\" style=\"background:#FFA050\"></span> Energetic <small>⚡</small>", "High energy, dynamic. Inline: [energetic]text[/energetic]", "editor-emotion-energetic", "[energetic]", "[/energetic]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("emotion-professional", "<span class=\"cdot\" style=\"background:#80B8FF\"></span> Professional <small>💼</small>", "Business-like, formal. Inline: [professional]text[/professional]", "editor-emotion-professional", "[professional]", "[/professional]", cssClass: "tb-emo-item tb-tip")
                ]),
                new("Delivery Modes", [
                    Wrap("delivery-aside", "<span style=\"color:#B8C0C8;font-weight:700;width:44px;text-align:right\">ASIDE</span> Aside <small>[aside]</small>", "Mark the phrase as an aside. [aside]text[/aside]", "editor-delivery-aside", "[aside]", "[/aside]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("delivery-rhetorical", "<span style=\"color:#FFE87A;font-weight:700;width:44px;text-align:right\">WHY?</span> Rhetorical <small>[rhetorical]</small>", "Use rhetorical delivery without changing emotion. [rhetorical]text[/rhetorical]", "editor-delivery-rhetorical", "[rhetorical]", "[/rhetorical]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("delivery-sarcasm", "<span style=\"color:#F19A9A;font-weight:700;width:44px;text-align:right\">SARC</span> Sarcasm <small>[sarcasm]</small>", "Apply a sarcastic delivery cue. [sarcasm]text[/sarcasm]", "editor-delivery-sarcasm", "[sarcasm]", "[/sarcasm]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("delivery-building", "<span style=\"color:#D2B8E3;font-weight:700;width:44px;text-align:right\">BUILD</span> Building <small>[building]</small>", "Gradually build intensity through the phrase. [building]text[/building]", "editor-delivery-building", "[building]", "[/building]", cssClass: "tb-emo-item tb-tip")
                ], HasSeparatorBefore: true)
            ]),
        new(
            "pause",
            "Pause",
            EditorToolbarMenuIds.Pause,
            "editor-menu-pause",
            null,
            [
                Insert("pause-short", "/", "Short pause — inserts / marker (300ms silence)", "editor-pause-short", "/"),
                Insert("pause-medium", "//", "Medium pause — inserts // marker (600ms silence)", "editor-pause-medium", "//"),
                Toggle(EditorToolbarMenuIds.Pause, "editor-pause-trigger", "More pause options", PauseClockIcon + ChevronDownIcon)
            ],
            [
                new("Breath And Pauses", [
                    Insert("pause-breath", $"{PauseFloatIcon} Breath <small>[breath]</small>", "Breath mark between phrases. [breath]", "editor-pause-breath", "[breath]", cssClass: "tb-emo-item tb-tip"),
                    Insert("pause-short-menu", "<span style=\"color:var(--gold-text);font-weight:700;width:20px;text-align:center\">/</span> Short pause <small>300ms</small>", "Short 300ms pause between clauses. Inserted as / in text", "editor-pause-short-menu", "/", cssClass: "tb-emo-item tb-tip"),
                    Insert("pause-medium-menu", "<span style=\"color:var(--gold-text);font-weight:700;width:20px;text-align:center\">//</span> Medium pause <small>600ms</small>", "Medium 600ms pause between sentences. Inserted as // in text", "editor-pause-medium-menu", "//", cssClass: "tb-emo-item tb-tip"),
                    Insert("pause-1s", $"{PauseClockIcon} 1 second <small>[pause:1s]</small>", "1 second timed pause. [pause:1s]", "editor-pause-one-second", "[pause:1s]", cssClass: "tb-emo-item tb-tip"),
                    Insert("pause-2s", $"{PauseClockIcon} 2 seconds <small>[pause:2s]</small>", "2 second timed pause. [pause:2s]", "editor-pause-two-seconds", "[pause:2s]", cssClass: "tb-emo-item tb-tip"),
                    Insert("pause-3s", $"{PauseClockIcon} 3 seconds <small>[pause:3s]</small>", "3 second timed pause. [pause:3s]", "editor-pause-three-seconds", "[pause:3s]", cssClass: "tb-emo-item tb-tip"),
                    Insert("pause-custom", $"{PauseClockIcon} Custom (ms) <small>[pause:Nms]</small>", "Custom timed pause in milliseconds. [pause:500ms]", "editor-pause-custom", "[pause:500ms]", cssClass: "tb-emo-item tb-tip")
                ])
            ]),
        new(
            "speed",
            "Speed",
            EditorToolbarMenuIds.Speed,
            "editor-menu-speed",
            "min-width:240px",
            [
                Wrap("speed-xslow", "×.6", "Extra slow — base WPM × 0.6. Use for critical warnings, very careful delivery. [xslow]text[/xslow]", "editor-toolbar-speed-xslow", "[xslow]", "[/xslow]", style: "color:#FF8A8A", cssClass: "tb-btn tb-speed tb-tip"),
                Wrap("speed-slow", "×.8", "Slow — base WPM × 0.8. Use for important points, emphasis. [slow]text[/slow]", "editor-toolbar-speed-slow", "[slow]", "[/slow]", style: "color:#FFB86A", cssClass: "tb-btn tb-speed tb-tip"),
                Wrap("speed-normal", "×1", "Normal speed — resets to base WPM × 1.0. [normal]text[/normal]", "editor-speed-normal", "[normal]", "[/normal]", cssClass: "tb-btn tb-speed tb-tip"),
                Wrap("speed-fast", "×1.25", "Fast — base WPM × 1.25. Use for quick mentions, asides. [fast]text[/fast]", "editor-toolbar-speed-fast", "[fast]", "[/fast]", style: "color:#8ECFFF", cssClass: "tb-btn tb-speed tb-tip"),
                Wrap("speed-xfast", "×1.5", "Extra fast — base WPM × 1.5. Use for rapid transitions, low-importance text. [xfast]text[/xfast]", "editor-toolbar-speed-xfast", "[xfast]", "[/xfast]", style: "color:#80B8FF", cssClass: "tb-btn tb-speed tb-tip"),
                Toggle(EditorToolbarMenuIds.Speed, "editor-speed-trigger", "Custom WPM and more speed options", LightningIcon + ChevronDownIcon)
            ],
            [
                new("Speed Presets", [
                    Wrap("speed-xslow-menu", "<span style=\"color:#FF8A8A;font-family:var(--font-mono);font-size:11px;font-weight:700;width:36px;text-align:right\">×0.6</span> Extra Slow <small>[xslow]</small>", "Very careful delivery. Critical warnings. base × 0.6 = 84 WPM at base 140", "editor-speed-xslow-menu", "[xslow]", "[/xslow]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("speed-slow-menu", "<span style=\"color:#FFB86A;font-family:var(--font-mono);font-size:11px;font-weight:700;width:36px;text-align:right\">×0.8</span> Slow <small>[slow]</small>", "Important points, emphasis. base × 0.8 = 112 WPM at base 140", "editor-speed-slow-menu", "[slow]", "[/slow]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("speed-normal-menu", "<span style=\"color:var(--text-2);font-family:var(--font-mono);font-size:11px;font-weight:700;width:36px;text-align:right\">×1.0</span> Normal <small>[normal]</small>", "Reset to base speed. base × 1.0 = 140 WPM at base 140", "editor-speed-normal-menu", "[normal]", "[/normal]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("speed-fast-menu", "<span style=\"color:#8ECFFF;font-family:var(--font-mono);font-size:11px;font-weight:700;width:36px;text-align:right\">×1.25</span> Fast <small>[fast]</small>", "Quick mentions, asides. base × 1.25 = 175 WPM at base 140", "editor-speed-fast-menu", "[fast]", "[/fast]", cssClass: "tb-emo-item tb-tip"),
                    Wrap("speed-xfast-menu", "<span style=\"color:#80B8FF;font-family:var(--font-mono);font-size:11px;font-weight:700;width:36px;text-align:right\">×1.5</span> Extra Fast <small>[xfast]</small>", "Rapid transitions, low-importance text. base × 1.5 = 210 WPM at base 140", "editor-speed-xfast-menu", "[xfast]", "[/xfast]", cssClass: "tb-emo-item tb-tip")
                ]),
                new("Custom Speed", [
                    Wrap("speed-custom-wpm", $"{LightningIcon} Custom WPM <small>[NWPM]</small>", "Set an absolute WPM value for a text span. [180WPM]text[/180WPM]", "editor-speed-custom-wpm", "[180WPM]", "[/180WPM]", cssClass: "tb-emo-item tb-tip")
                ], HasSeparatorBefore: true)
            ]),
        new(
            "insert",
            "Insert",
            EditorToolbarMenuIds.Insert,
            "editor-menu-insert",
            "min-width:260px",
            [
                Insert("insert-edit-point", EditPointIconGold, "Edit Point — marks a natural place to stop or resume an editing session. [edit_point] or [edit_point:high]", "editor-insert-edit-point", "[edit_point]"),
                Wrap("insert-phonetic", PhoneticIcon, "Pronunciation guide — add IPA or simple pronunciation. [phonetic:IPA]word[/phonetic]", "editor-insert-phonetic", "[phonetic:IPA]", "[/phonetic]", placeholder: "word"),
                Insert("insert-segment", "##", "Segment — major script section with speaker, WPM, emotion, and optional timing. ## [Name|Speaker:Host|WPM|emotion|timing]", UiTestIds.Editor.InsertSegment, "## [Segment Name|Speaker:Host|140WPM|neutral|0:00-0:30]\n", cssClass: "tb-btn tb-tip tb-speed"),
                Insert("insert-block", "###", "Block — topic group within a segment. ### [Name|Speaker:Host|WPM|emotion]", UiTestIds.Editor.InsertBlock, "### [Block Name|Speaker:Host|140WPM|focused]\n", cssClass: "tb-btn tb-tip tb-speed"),
                Toggle(EditorToolbarMenuIds.Insert, "editor-insert-trigger", "More insert options", ChevronDownIcon)
            ],
            [
                new("Structure", [
                    Insert("insert-segment-menu", "<span style=\"color:var(--gold-text);font-weight:700;width:20px;text-align:center\">##</span> Segment <small>Speaker aware</small>", "Segment — order-independent TPS 1.1.0 header. ## [Name|Speaker:Host|140WPM|emotion|0:00-0:30]", UiTestIds.Editor.InsertSegmentMenu, "## [Segment Name|Speaker:Host|140WPM|neutral|0:00-0:30]\n", cssClass: "tb-emo-item tb-tip"),
                    Insert("insert-block-menu", "<span style=\"color:var(--gold-text);font-weight:700;width:20px;text-align:center\">###</span> Block <small>Speaker aware</small>", "Block — order-independent TPS 1.1.0 header. ### [Name|Speaker:Host|140WPM|emotion]", UiTestIds.Editor.InsertBlockMenu, "### [Block Name|Speaker:Host|140WPM|focused]\n", cssClass: "tb-emo-item tb-tip")
                ]),
                new("Edit Points", [
                    Insert("insert-edit-point-high", $"{EditPointIconRed} High priority <small>[edit_point:high]</small>", "High priority edit point — critical, must review. [edit_point:high]", "editor-insert-edit-point-high", "[edit_point:high]", cssClass: "tb-emo-item tb-tip"),
                    Insert("insert-edit-point-medium", $"{EditPointIconOrange} Medium priority <small>[edit_point:medium]</small>", "Medium priority edit point — important but not critical. [edit_point:medium]", "editor-insert-edit-point-medium", "[edit_point:medium]", cssClass: "tb-emo-item tb-tip"),
                    Insert("insert-edit-point-standard", $"{EditPointIconGold} Standard <small>[edit_point]</small>", "Standard edit point — marks a natural break. [edit_point]", "editor-insert-edit-point-standard", "[edit_point]", cssClass: "tb-emo-item tb-tip")
                ], HasSeparatorBefore: true),
                new("Pronunciation", [
                    Wrap("insert-phonetic-menu", $"{PhoneticIcon} Phonetic (IPA) <small>[phonetic:...]</small>", "IPA phonetic notation for precise pronunciation. [phonetic:IPA]word[/phonetic]", "editor-insert-phonetic-menu", "[phonetic:IPA]", "[/phonetic]", placeholder: "word", cssClass: "tb-emo-item tb-tip"),
                    Wrap("insert-pronunciation", "<span style=\"color:#D88AFF;font-weight:600;width:14px;text-align:center;flex-shrink:0;font-size:13px\">Aa</span> Pronunciation <small>[pronunciation:...]</small>", "Simple pronunciation guide for easy reading. [pronunciation:guide]word[/pronunciation]", "editor-insert-pronunciation", "[pronunciation:guide]", "[/pronunciation]", placeholder: "word", cssClass: "tb-emo-item tb-tip"),
                    Wrap("insert-stress-guide", "<span style=\"color:#FFD060;font-weight:700;width:14px;text-align:center;flex-shrink:0;font-size:13px\">S</span> Stress guide <small>[stress:...]</small>", "Stress cue with an explicit guide. [stress:rising]word[/stress]", "editor-insert-stress-guide", "[stress:rising]", "[/stress]", placeholder: "word", cssClass: "tb-emo-item tb-tip")
                ], HasSeparatorBefore: true)
            ]),
        new(
            "ai",
            "AI",
            null,
            null,
            null,
            [
                Ai(
                    "ai-simplify",
                    $"{SparkIcon} AI",
                    "AI Assistant — rewrite, expand, simplify, or auto-format your script with AI",
                    UiTestIds.Editor.Ai,
                    EditorAiAssistAction.Simplify,
                    "tb-btn tb-ai tb-tip")
            ],
            [])
    ];

    public static IReadOnlyList<IReadOnlyList<EditorToolbarActionDescriptor>> FloatingActionGroups { get; } =
    [
        [
            FloatingWrap("float-bold", "<b>B</b>", "Bold", "editor-float-bold", "**", "**"),
            FloatingWrap("float-italic", "<i>I</i>", "Italic", "editor-float-italic", "*", "*"),
            FloatingWrap("float-emphasis", "Em", "Emphasis [emphasis]", "editor-float-emphasis", "[emphasis]", "[/emphasis]"),
            FloatingWrap("float-highlight", HighlightIcon, "Highlight [highlight]", "editor-float-highlight", "[highlight]", "[/highlight]")
        ],
        [
            FloatingWrap("float-voice", "<span class=\"cdot\" style=\"background:#FFD060\"></span>", "Loud [loud]", "editor-float-color", "[loud]", "[/loud]"),
            FloatingToggle(EditorToolbarMenuIds.FloatingEmotion, "<span class=\"cdot\" style=\"background:#FFB840\"></span>" + ChevronDownIcon, "Emotion", UiTestIds.Editor.FloatingEmotion)
        ],
        [
            FloatingWrap("float-slow", ".8×", "Slow [slow]", "editor-floating-slow", "[slow]", "[/slow]"),
            FloatingWrap("float-fast", "1.25×", "Fast [fast]", "editor-float-fast", "[fast]", "[/fast]"),
            FloatingInsert("float-pause", PauseFloatIcon, "Breath [breath]", "editor-float-pause", "[breath]")
        ],
        [
            FloatingAi(
                "float-ai",
                $"{SparkIcon}AI",
                "AI — rewrite, expand, simplify",
                UiTestIds.Editor.FloatingAi,
                EditorAiAssistAction.Simplify)
        ]
    ];

    public static IReadOnlyList<EditorToolbarActionDescriptor> FloatingEmotionActions { get; } =
    [
        FloatingWrap("float-emotion-warm", "<span class=\"cdot\" style=\"background:#FFB840\"></span> Warm <small>😊</small>", "Friendly, welcoming tone. Inline: [warm]text[/warm]", "editor-float-emotion-warm", "[warm]", "[/warm]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-concerned", "<span class=\"cdot\" style=\"background:#FF7A7A\"></span> Concerned <small>😟</small>", "Worried, empathetic. Inline: [concerned]text[/concerned]", "editor-float-emotion-concerned", "[concerned]", "[/concerned]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-focused", "<span class=\"cdot\" style=\"background:#4AE0A0\"></span> Focused <small>🎯</small>", "Concentrated, precise. Inline: [focused]text[/focused]", "editor-float-emotion-focused", "[focused]", "[/focused]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-motivational", "<span class=\"cdot\" style=\"background:#C88AFF\"></span> Motivational <small>💪</small>", "Inspiring, encouraging. Inline: [motivational]text[/motivational]", UiTestIds.Editor.FloatingEmotionMotivational, "[motivational]", "[/motivational]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-neutral", "<span class=\"cdot\" style=\"background:#8ECFFF\"></span> Neutral <small>😐</small>", "Default balanced tone. Inline: [neutral]text[/neutral]", "editor-float-emotion-neutral", "[neutral]", "[/neutral]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-urgent", "<span class=\"cdot\" style=\"background:#FF6060\"></span> Urgent <small>🚨</small>", "Critical, immediate attention. Inline: [urgent]text[/urgent]", "editor-float-emotion-urgent", "[urgent]", "[/urgent]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-happy", "<span class=\"cdot\" style=\"background:#FFE87A\"></span> Happy <small>😄</small>", "Joyful, positive. Inline: [happy]text[/happy]", "editor-float-emotion-happy", "[happy]", "[/happy]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-excited", "<span class=\"cdot\" style=\"background:#FF8AC8\"></span> Excited <small>🚀</small>", "Enthusiastic, energetic. Inline: [excited]text[/excited]", "editor-float-emotion-excited", "[excited]", "[/excited]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-sad", "<span class=\"cdot\" style=\"background:#A0A8FF\"></span> Sad <small>😢</small>", "Melancholy, somber. Inline: [sad]text[/sad]", "editor-float-emotion-sad", "[sad]", "[/sad]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-calm", "<span class=\"cdot\" style=\"background:#5EECC2\"></span> Calm <small>😌</small>", "Peaceful, relaxed. Inline: [calm]text[/calm]", "editor-float-emotion-calm", "[calm]", "[/calm]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-energetic", "<span class=\"cdot\" style=\"background:#FFA050\"></span> Energetic <small>⚡</small>", "High energy, dynamic. Inline: [energetic]text[/energetic]", "editor-float-emotion-energetic", "[energetic]", "[/energetic]", cssClass: "efb-menu-item"),
        FloatingWrap("float-emotion-professional", "<span class=\"cdot\" style=\"background:#80B8FF\"></span> Professional <small>💼</small>", "Business-like, formal. Inline: [professional]text[/professional]", UiTestIds.Editor.FloatingEmotionProfessional, "[professional]", "[/professional]", cssClass: "efb-menu-item")
    ];

    private static EditorToolbarActionDescriptor Ai(
        string key,
        string contentHtml,
        string tooltip,
        string testId,
        EditorAiAssistAction aiAction,
        string cssClass) =>
        new(key, EditorToolbarActionType.Ai, cssClass, contentHtml, tooltip, testId, AiAction: aiAction);

    private static EditorToolbarActionDescriptor ClearColor(string key, string tooltip, string testId) =>
        new(
            key,
            EditorToolbarActionType.Command,
            "tb-cswatch tb-tip",
            string.Empty,
            tooltip,
            testId,
            "background:transparent;border:1px dashed var(--gold-20)",
            Command: new EditorCommandRequest(EditorCommandKind.ClearColor, string.Empty));

    private static EditorToolbarActionDescriptor FloatingAi(
        string key,
        string contentHtml,
        string tooltip,
        string testId,
        EditorAiAssistAction aiAction) =>
        new(
            key,
            EditorToolbarActionType.Ai,
            "efb-btn efb-ai",
            contentHtml,
            tooltip,
            testId,
            AiAction: aiAction,
            PreventMouseDown: true);

    private static EditorToolbarActionDescriptor FloatingInsert(string key, string contentHtml, string tooltip, string testId, string token) =>
        new(
            key,
            EditorToolbarActionType.Command,
            "efb-btn",
            contentHtml,
            tooltip,
            testId,
            Command: new EditorCommandRequest(EditorCommandKind.Insert, token),
            PreventMouseDown: true);

    private static EditorToolbarActionDescriptor FloatingToggle(string menuId, string contentHtml, string tooltip, string testId) =>
        new(
            menuId,
            EditorToolbarActionType.ToggleMenu,
            "efb-btn",
            contentHtml,
            tooltip,
            testId,
            MenuId: menuId,
            PreventMouseDown: true);

    private static EditorToolbarActionDescriptor FloatingWrap(string key, string contentHtml, string tooltip, string testId, string openingToken, string closingToken) =>
        FloatingWrap(key, contentHtml, tooltip, testId, openingToken, closingToken, "efb-btn");

    private static EditorToolbarActionDescriptor FloatingWrap(
        string key,
        string contentHtml,
        string tooltip,
        string testId,
        string openingToken,
        string closingToken,
        string cssClass) =>
        new(
            key,
            EditorToolbarActionType.Command,
            cssClass,
            contentHtml,
            tooltip,
            testId,
            Command: new EditorCommandRequest(EditorCommandKind.Wrap, openingToken, closingToken, "text"),
            PreventMouseDown: true);

    private static EditorToolbarActionDescriptor History(string key, string contentHtml, string tooltip, string testId, EditorHistoryCommand command) =>
        new(key, EditorToolbarActionType.History, "tb-btn tb-tip", contentHtml, tooltip, testId, HistoryCommand: command);

    private static EditorToolbarActionDescriptor Insert(
        string key,
        string contentHtml,
        string tooltip,
        string testId,
        string token,
        string cssClass = "tb-btn tb-tip") =>
        new(
            key,
            EditorToolbarActionType.Command,
            cssClass,
            contentHtml,
            tooltip,
            testId,
            Command: new EditorCommandRequest(EditorCommandKind.Insert, token));

    private static EditorToolbarActionDescriptor Toggle(string menuId, string testId, string tooltip, string contentHtml) =>
        new(menuId, EditorToolbarActionType.ToggleMenu, "tb-btn tb-has-dropdown tb-tip", contentHtml, tooltip, testId, MenuId: menuId);

    private static EditorToolbarActionDescriptor Wrap(
        string key,
        string contentHtml,
        string tooltip,
        string testId,
        string openingToken,
        string closingToken,
        string placeholder = "text",
        string cssClass = "tb-btn tb-tip",
        string? style = null) =>
        new(
            key,
            EditorToolbarActionType.Command,
            cssClass,
            contentHtml,
            tooltip,
            testId,
            style,
            Command: new EditorCommandRequest(EditorCommandKind.Wrap, openingToken, closingToken, placeholder));
}
