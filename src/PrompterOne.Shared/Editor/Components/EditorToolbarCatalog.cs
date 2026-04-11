using Microsoft.Extensions.Localization;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Components.Editor;

public enum EditorToolbarActionType
{
    Command,
    History,
    ToggleMenu
}

public sealed record EditorToolbarActionDescriptor(
    string Key,
    EditorToolbarActionType ActionType,
    string CssClass,
    EditorActionContentDescriptor Content,
    string Tooltip,
    string? TestId = null,
    string? MenuId = null,
    EditorCommandRequest? Command = null,
    EditorHistoryCommand? HistoryCommand = null,
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
    string? DropdownPanelCssClass,
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
    public const string FloatingVoice = "floating-voice";
    public const string FloatingEmotion = "floating-emotion";
    public const string FloatingPause = "floating-pause";
    public const string FloatingSpeed = "floating-speed";
    public const string FloatingInsert = "floating-insert";
    public const string Pause = "pause";
    public const string Speed = "speed";
    public const string Insert = "insert";
}

public static class EditorToolbarCatalog
{
    public static IReadOnlyList<EditorToolbarSectionDescriptor> Sections { get; } =
    [
        new(
            "history",
            "History",
            null,
            null,
            null,
            [
                History("undo", EditorActionContents.Icon(EditorActionIconKind.Undo), "Undo last action (Ctrl+Z)", "editor-undo", EditorHistoryCommand.Undo),
                History("redo", EditorActionContents.Icon(EditorActionIconKind.Redo), "Redo last undone action (Ctrl+Y)", "editor-redo", EditorHistoryCommand.Redo)
            ],
            []),
        new(
            "format",
            "Format",
            EditorToolbarMenuIds.Format,
            "editor-menu-format",
            null,
            [
                Wrap("bold", EditorActionContents.Glyph("B", bold: true), "Bold — strong emphasis level 2. Markdown: **text**", "editor-bold", "**", "**"),
                Wrap("italic", EditorActionContents.Glyph("I", italic: true), "Italic — emphasis level 1. Markdown: *text*", "editor-italic", "*", "*"),
                Wrap("emphasis", EditorActionContents.Glyph("Em", EditorActionContentTone.Emphasis, bold: true), "Emphasis — standard emphasis level 1. TPS: [emphasis]text[/emphasis]", "editor-emphasis", "[emphasis]", "[/emphasis]"),
                Toggle(EditorToolbarMenuIds.Format, "editor-format-trigger", "More formatting options", EditorActionContents.Icon(EditorActionIconKind.ChevronDown))
            ],
            [
                new("Formatting", [
                    Wrap("format-bold", EditorActionContents.Label("B", "Bold", "**text**", mono: true), "Bold — strong emphasis level 2. Markdown: **text**", "editor-format-bold", "**", "**", cssClass: "tb-emo-item"),
                    Wrap("format-italic", EditorActionContents.Label("I", "Italic", "*text*", mono: true), "Italic — emphasis level 1. Markdown: *text*", "editor-format-italic", "*", "*", cssClass: "tb-emo-item"),
                    Wrap("format-emphasis", EditorActionContents.Label("Em", "Emphasis", "[emphasis]", EditorActionContentTone.Emphasis), "Emphasis — standard emphasis level 1. TPS: [emphasis]text[/emphasis]", "editor-format-emphasis", "[emphasis]", "[/emphasis]", cssClass: "tb-emo-item"),
                    Wrap("format-highlight", EditorActionContents.Label("H", "Highlight", "[highlight]", EditorActionContentTone.Highlight), "Highlight [highlight]", "editor-format-highlight", "[highlight]", "[/highlight]", cssClass: "tb-emo-item"),
                    Wrap("format-stress", EditorActionContents.Label("S", "Stress", "[stress]", EditorActionContentTone.Stress), "Stress wrap — mark a word or phrase as stressed. [stress]text[/stress]", "editor-format-stress", "[stress]", "[/stress]", cssClass: "tb-emo-item")
                ])
            ]),
        new(
            "voice",
            "Voice",
            EditorToolbarMenuIds.Color,
            "editor-menu-color",
            null,
            [
                Toggle(
                    EditorToolbarMenuIds.Color,
                    "editor-color-trigger",
                    "Voice cues — volume, articulation, energy, melody, and stress tags for the current selection",
                    EditorActionContents.Trigger(EditorActionIconKind.SemanticDotVoice, EditorActionIconTone.Voice),
                    "tb-btn tb-has-dropdown tb-tip tb-btn--voice")
            ],
            [
                ..EditorTpsMenuCatalog.BuildToolbarVoiceGroups()
            ]),
        new(
            "emotion",
            "Emotion",
            EditorToolbarMenuIds.Emotion,
            "editor-menu-emotion",
            null,
            [
                Toggle(
                    EditorToolbarMenuIds.Emotion,
                    "editor-emotion-trigger",
                    "Emotion — applies mood-based color styling and presentation hints. Used on segments, blocks, or inline text",
                    EditorActionContents.Trigger(EditorActionIconKind.SemanticDotEmotion, EditorActionIconTone.Emotion),
                    "tb-btn tb-has-dropdown tb-tip tb-btn--emotion")
            ],
            [
                ..EditorTpsMenuCatalog.BuildToolbarEmotionGroups()
            ]),
        new(
            "pause",
            "Pause",
            EditorToolbarMenuIds.Pause,
            "editor-menu-pause",
            null,
            [
                Insert("pause-short", EditorActionContents.Glyph("/", EditorActionContentTone.Structure, bold: true), "Short pause — inserts / marker (300ms silence)", "editor-pause-short", "/"),
                Insert("pause-medium", EditorActionContents.Glyph("//", EditorActionContentTone.Structure, bold: true), "Medium pause — inserts // marker (600ms silence)", "editor-pause-medium", "//"),
                Toggle(EditorToolbarMenuIds.Pause, "editor-pause-trigger", "More pause options", EditorActionContents.Trigger(EditorActionIconKind.PauseClock), "tb-btn tb-has-dropdown tb-tip tb-btn--pause")
            ],
            [
                ..EditorTpsMenuCatalog.BuildToolbarPauseGroups()
            ]),
        new(
            "speed",
            "Speed",
            EditorToolbarMenuIds.Speed,
            "editor-menu-speed",
            "tb-dropdown--speed",
            [
                Wrap("speed-xslow", EditorActionContents.Glyph("×.6", EditorActionContentTone.SpeedXslow, bold: true, mono: true), "Extra slow — base WPM × 0.6. Use for critical warnings, very careful delivery. [xslow]text[/xslow]", "editor-toolbar-speed-xslow", "[xslow]", "[/xslow]", cssClass: "tb-btn tb-speed tb-tip tb-speed--xslow"),
                Wrap("speed-slow", EditorActionContents.Glyph("×.8", EditorActionContentTone.SpeedSlow, bold: true, mono: true), "Slow — base WPM × 0.8. Use for important points, emphasis. [slow]text[/slow]", "editor-toolbar-speed-slow", "[slow]", "[/slow]", cssClass: "tb-btn tb-speed tb-tip tb-speed--slow"),
                Wrap("speed-normal", EditorActionContents.Glyph("×1", EditorActionContentTone.SpeedNormal, bold: true, mono: true), "Normal speed — resets to base WPM × 1.0. [normal]text[/normal]", "editor-speed-normal", "[normal]", "[/normal]", cssClass: "tb-btn tb-speed tb-tip tb-speed--normal"),
                Wrap("speed-fast", EditorActionContents.Glyph("×1.25", EditorActionContentTone.SpeedFast, bold: true, mono: true), "Fast — base WPM × 1.25. Use for quick mentions, asides. [fast]text[/fast]", "editor-toolbar-speed-fast", "[fast]", "[/fast]", cssClass: "tb-btn tb-speed tb-tip tb-speed--fast"),
                Wrap("speed-xfast", EditorActionContents.Glyph("×1.5", EditorActionContentTone.SpeedXfast, bold: true, mono: true), "Extra fast — base WPM × 1.5. Use for rapid transitions, low-importance text. [xfast]text[/xfast]", "editor-toolbar-speed-xfast", "[xfast]", "[/xfast]", cssClass: "tb-btn tb-speed tb-tip tb-speed--xfast"),
                Toggle(EditorToolbarMenuIds.Speed, "editor-speed-trigger", "Custom WPM and more speed options", EditorActionContents.Trigger(EditorActionIconKind.Lightning), "tb-btn tb-has-dropdown tb-tip tb-btn--speed")
            ],
            [
                ..EditorTpsMenuCatalog.BuildToolbarSpeedGroups()
            ]),
        new(
            "insert",
            "Insert",
            EditorToolbarMenuIds.Insert,
            "editor-menu-insert",
            "tb-dropdown--insert",
            [
                Insert("insert-edit-point", EditorActionContents.Icon(EditorActionIconKind.EditPoint), "Edit Point — marks a natural place to stop or resume an editing session. [edit_point] or [edit_point:high]", "editor-insert-edit-point", "[edit_point]", cssClass: "tb-btn tb-tip tb-btn--insert"),
                Wrap("insert-phonetic", EditorActionContents.Icon(EditorActionIconKind.Phonetic, EditorActionIconTone.Purple), "Pronunciation guide — add IPA or simple pronunciation. [phonetic:IPA]word[/phonetic]", "editor-insert-phonetic", "[phonetic:IPA]", "[/phonetic]", placeholder: "word"),
                Insert("insert-segment", EditorActionContents.Glyph("##", EditorActionContentTone.Structure, bold: true, mono: true), "Segment — major script section with speaker, WPM, emotion, and optional timing. ## [Name|Speaker:Host|WPM|emotion|timing]", UiTestIds.Editor.InsertSegment, "## [Segment Name|Speaker:Host|140WPM|neutral|0:00-0:30]\n", cssClass: "tb-btn tb-tip tb-speed"),
                Insert("insert-block", EditorActionContents.Glyph("###", EditorActionContentTone.Structure, bold: true, mono: true), "Block — topic group within a segment. ### [Name|Speaker:Host|WPM|emotion]", UiTestIds.Editor.InsertBlock, "### [Block Name|Speaker:Host|140WPM|focused]\n", cssClass: "tb-btn tb-tip tb-speed"),
                Toggle(EditorToolbarMenuIds.Insert, "editor-insert-trigger", "More insert options", EditorActionContents.Icon(EditorActionIconKind.ChevronDown), "tb-btn tb-has-dropdown tb-tip tb-btn--insert")
            ],
            [
                ..EditorTpsMenuCatalog.BuildToolbarInsertGroups()
            ])
    ];

    public static IReadOnlyList<IReadOnlyList<EditorToolbarActionDescriptor>> FloatingActionGroups =>
        EditorFloatingToolbarCatalog.ActionGroups;

    public static IReadOnlyList<EditorFloatingMenuDescriptor> FloatingMenus =>
        EditorFloatingToolbarCatalog.Menus;

    public static IReadOnlyList<EditorToolbarSectionDescriptor> BuildSections(IStringLocalizer<SharedResource> localizer) =>
        Sections.Select(section => new EditorToolbarSectionDescriptor(
                section.Key,
                LocalizeSectionLabel(localizer, section.Key),
                section.MenuId,
                section.DropdownTestId,
                section.DropdownPanelCssClass,
                section.MainActions.Select(action => LocalizeAction(localizer, action)).ToArray(),
                section.DropdownGroups.Select(group => new EditorToolbarDropdownGroupDescriptor(
                    LocalizeGroupLabel(localizer, group.Label),
                    group.Actions.Select(action => LocalizeAction(localizer, action)).ToArray(),
                    group.HasSeparatorBefore)).ToArray()))
            .ToArray();

    private static EditorToolbarActionDescriptor History(string key, EditorActionContentDescriptor content, string tooltip, string testId, EditorHistoryCommand command) =>
        new(key, EditorToolbarActionType.History, "tb-btn tb-tip", content, tooltip, testId, HistoryCommand: command, PreventMouseDown: true);

    private static EditorToolbarActionDescriptor Insert(
        string key,
        EditorActionContentDescriptor content,
        string tooltip,
        string testId,
        string token,
        string cssClass = "tb-btn tb-tip") =>
        new(
            key,
            EditorToolbarActionType.Command,
            cssClass,
            content,
            tooltip,
            testId,
            Command: new EditorCommandRequest(EditorCommandKind.Insert, token),
            PreventMouseDown: true);

    private static EditorToolbarActionDescriptor Toggle(
        string menuId,
        string testId,
        string tooltip,
        EditorActionContentDescriptor content,
        string cssClass = "tb-btn tb-has-dropdown tb-tip") =>
        new(menuId, EditorToolbarActionType.ToggleMenu, cssClass, content, tooltip, testId, MenuId: menuId, PreventMouseDown: true);

    private static EditorToolbarActionDescriptor Wrap(
        string key,
        EditorActionContentDescriptor content,
        string tooltip,
        string testId,
        string openingToken,
        string closingToken,
        string placeholder = "text",
        string cssClass = "tb-btn tb-tip") =>
        new(
            key,
            EditorToolbarActionType.Command,
            cssClass,
            content,
            tooltip,
            testId,
            Command: new EditorCommandRequest(EditorCommandKind.Wrap, openingToken, closingToken, placeholder),
            PreventMouseDown: true);

    private static EditorToolbarActionDescriptor LocalizeAction(
        IStringLocalizer<SharedResource> localizer,
        EditorToolbarActionDescriptor action)
    {
        var tooltip = ResolveLocalizedTooltip(localizer, action);
        return string.Equals(tooltip, action.Tooltip, StringComparison.Ordinal)
            ? action
            : action with { Tooltip = tooltip };
    }

    private static string LocalizeGroupLabel(IStringLocalizer<SharedResource> localizer, string label) =>
        label switch
        {
            "Formatting" => Text(localizer, UiTextKey.EditorToolbarGroupFormatting),
            "Volume" => Text(localizer, UiTextKey.EditorToolbarGroupVolume),
            "Articulation" => Text(localizer, UiTextKey.EditorToolbarGroupArticulation),
            "Dynamics" => Text(localizer, UiTextKey.EditorToolbarGroupDynamics),
            "Stress" => Text(localizer, UiTextKey.EditorToolbarGroupStress),
            "Reset" => Text(localizer, UiTextKey.EditorToolbarGroupReset),
            "TPS Emotions" => Text(localizer, UiTextKey.EditorToolbarGroupTpsEmotions),
            "Delivery Modes" => Text(localizer, UiTextKey.EditorToolbarGroupDeliveryModes),
            "Breath And Pauses" => Text(localizer, UiTextKey.EditorToolbarGroupBreathAndPauses),
            "Speed Presets" => Text(localizer, UiTextKey.EditorToolbarGroupSpeedPresets),
            "Custom Speed" => Text(localizer, UiTextKey.EditorToolbarGroupCustomSpeed),
            "Structure" => Text(localizer, UiTextKey.EditorToolbarGroupStructure),
            "Edit Points" => Text(localizer, UiTextKey.EditorToolbarGroupEditPoints),
            "Pronunciation" => Text(localizer, UiTextKey.EditorToolbarGroupPronunciation),
            _ => label
        };

    private static string LocalizeSectionLabel(IStringLocalizer<SharedResource> localizer, string key) =>
        key switch
        {
            "history" => Text(localizer, UiTextKey.EditorToolbarSectionHistory),
            "format" => Text(localizer, UiTextKey.EditorToolbarSectionFormat),
            "voice" => Text(localizer, UiTextKey.EditorToolbarSectionVoice),
            "emotion" => Text(localizer, UiTextKey.EditorToolbarSectionEmotion),
            "pause" => Text(localizer, UiTextKey.EditorToolbarSectionPause),
            "speed" => Text(localizer, UiTextKey.EditorToolbarSectionSpeed),
            "insert" => Text(localizer, UiTextKey.EditorToolbarSectionInsert),
            _ => key
        };

    private static string ResolveLocalizedTooltip(
        IStringLocalizer<SharedResource> localizer,
        EditorToolbarActionDescriptor action)
    {
        return action.TestId switch
        {
            "editor-undo" => Text(localizer, UiTextKey.EditorToolbarTooltipUndo),
            "editor-redo" => Text(localizer, UiTextKey.EditorToolbarTooltipRedo),
            "editor-format-trigger" => Text(localizer, UiTextKey.EditorToolbarTooltipMoreFormatting),
            "editor-color-trigger" => Text(localizer, UiTextKey.EditorToolbarTooltipVoiceCues),
            UiTestIds.Editor.EmotionTrigger => Text(localizer, UiTextKey.EditorToolbarTooltipEmotionTrigger),
            UiTestIds.Editor.EmotionMotivational => Text(localizer, UiTextKey.EditorToolbarTooltipEmotionMotivational),
            UiTestIds.Editor.PauseTrigger => Text(localizer, UiTextKey.EditorToolbarTooltipPauseCues),
            UiTestIds.Editor.SpeedTrigger => Text(localizer, UiTextKey.EditorToolbarTooltipSpeedCues),
            UiTestIds.Editor.InsertTrigger => Text(localizer, UiTextKey.EditorToolbarTooltipMoreInsertOptions),
            "editor-reset-color" => Text(localizer, UiTextKey.EditorToolbarTooltipRemoveInlineTags),
            UiTestIds.Editor.InsertSegmentArchetypeMenu => Text(localizer, UiTextKey.EditorToolbarTooltipInsertSegmentArchetype),
            UiTestIds.Editor.InsertBlockArchetypeMenu => Text(localizer, UiTextKey.EditorToolbarTooltipInsertBlockArchetype),
            _ => action.Tooltip
        };
    }

    private static string Text(IStringLocalizer<SharedResource> localizer, UiTextKey key) =>
        localizer[key.ToString()];
}
