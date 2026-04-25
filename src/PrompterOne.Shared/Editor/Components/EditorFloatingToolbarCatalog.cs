using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Components.Editor;

public sealed record EditorFloatingMenuDescriptor(
    string MenuId,
    string TriggerTestId,
    string PanelTestId,
    string Label,
    IReadOnlyList<EditorToolbarDropdownGroupDescriptor> DropdownGroups,
    string? PanelCssClass = null);

internal static class EditorFloatingToolbarCatalog
{
    public static IReadOnlyList<IReadOnlyList<EditorToolbarActionDescriptor>> ActionGroups { get; } =
    [
        [
            EditorFloatingToolbarActionFactory.Wrap("float-bold", EditorActionContents.Glyph("B", bold: true), "Bold — strong emphasis level 2. Markdown: **text**", "editor-float-bold", "**", "**"),
            EditorFloatingToolbarActionFactory.Wrap("float-italic", EditorActionContents.Glyph("I", italic: true), "Italic — emphasis level 1. Markdown: *text*", "editor-float-italic", "*", "*"),
            EditorFloatingToolbarActionFactory.Wrap("float-emphasis", EditorActionContents.Glyph("Em", EditorActionContentTone.Emphasis, bold: true), "Emphasis [emphasis]", UiTestIds.Editor.FloatEmphasis, "[emphasis]", "[/emphasis]"),
            EditorFloatingToolbarActionFactory.Wrap("float-highlight", EditorActionContents.Icon(EditorActionIconKind.Highlight, EditorActionIconTone.Gold), "Highlight [highlight]", "editor-float-highlight", "[highlight]", "[/highlight]"),
            EditorFloatingToolbarActionFactory.Wrap("float-stress", EditorActionContents.Glyph("S", EditorActionContentTone.Stress, bold: true), "Stress [stress]", UiTestIds.Editor.FloatStress, "[stress]", "[/stress]")
        ],
        [
            EditorFloatingToolbarActionFactory.Wrap("float-voice-loud", EditorActionContents.Icon(EditorActionIconKind.SpeakerHigh), "Loud [loud]", UiTestIds.Editor.FloatingVoiceLoud, "[loud]", "[/loud]", cssClass: "efb-btn efb-btn--voice-action"),
            EditorFloatingToolbarActionFactory.Wrap("float-slow", EditorActionContents.Glyph(".8×", EditorActionContentTone.SpeedSlow, bold: true, mono: true), "Slow [slow]", UiTestIds.Editor.FloatingSlow, "[slow]", "[/slow]"),
            EditorFloatingToolbarActionFactory.Wrap("float-fast", EditorActionContents.Glyph("1.25×", EditorActionContentTone.SpeedFast, bold: true, mono: true), "Fast [fast]", "editor-float-fast", "[fast]", "[/fast]"),
            EditorFloatingToolbarActionFactory.Toggle(EditorToolbarMenuIds.FloatingVoice, EditorActionContents.Trigger(EditorActionIconKind.SemanticDotVoice, EditorActionIconTone.Voice), "Voice cues", UiTestIds.Editor.FloatingVoice, "efb-btn efb-btn--voice-menu"),
            EditorFloatingToolbarActionFactory.Toggle(EditorToolbarMenuIds.FloatingSpeed, EditorActionContents.Trigger(EditorActionIconKind.Lightning), "Speed cues", UiTestIds.Editor.FloatingSpeedTrigger, "efb-btn efb-btn--speed"),
            EditorFloatingToolbarActionFactory.Toggle(EditorToolbarMenuIds.FloatingEmotion, EditorActionContents.Trigger(EditorActionIconKind.SemanticDotEmotion, EditorActionIconTone.Emotion), "Emotion and delivery", UiTestIds.Editor.FloatingEmotion, "efb-btn efb-btn--emotion-menu")
        ],
        [
            EditorFloatingToolbarActionFactory.Insert("float-pause", EditorActionContents.Icon(EditorActionIconKind.PauseBars), "Breath [breath]", UiTestIds.Editor.FloatingPause, "[breath]", cssClass: "efb-btn efb-btn--pause"),
            EditorFloatingToolbarActionFactory.Toggle(EditorToolbarMenuIds.FloatingPause, EditorActionContents.Trigger(EditorActionIconKind.PauseClock), "Pause cues", UiTestIds.Editor.FloatingPauseTrigger, "efb-btn efb-btn--pause")
        ],
        [
            EditorFloatingToolbarActionFactory.Toggle(EditorToolbarMenuIds.FloatingInsert, EditorActionContents.Trigger(EditorActionIconKind.EditPoint), "Insert TPS helpers", UiTestIds.Editor.FloatingInsert, "efb-btn efb-btn--insert")
        ]
    ];

    public static IReadOnlyList<EditorFloatingMenuDescriptor> Menus { get; } =
    [
        new(
            EditorToolbarMenuIds.FloatingVoice,
            UiTestIds.Editor.FloatingVoice,
            UiTestIds.Editor.FloatingVoiceMenu,
            "Voice Cues",
            EditorTpsMenuCatalog.BuildFloatingVoiceGroups()),
        new(
            EditorToolbarMenuIds.FloatingSpeed,
            UiTestIds.Editor.FloatingSpeedTrigger,
            UiTestIds.Editor.FloatingSpeedMenu,
            "Speed",
            EditorTpsMenuCatalog.BuildFloatingSpeedGroups(),
            "efb-dropdown--wide"),
        new(
            EditorToolbarMenuIds.FloatingPause,
            UiTestIds.Editor.FloatingPauseTrigger,
            UiTestIds.Editor.FloatingPauseMenu,
            "Pause",
            EditorTpsMenuCatalog.BuildFloatingPauseGroups()),
        new(
            EditorToolbarMenuIds.FloatingEmotion,
            UiTestIds.Editor.FloatingEmotion,
            UiTestIds.Editor.FloatingEmotionMenu,
            "Emotion",
            EditorTpsMenuCatalog.BuildFloatingEmotionGroups()),
        new(
            EditorToolbarMenuIds.FloatingInsert,
            UiTestIds.Editor.FloatingInsert,
            UiTestIds.Editor.FloatingInsertMenu,
            "Insert",
            EditorTpsMenuCatalog.BuildFloatingInsertGroups(),
            "efb-dropdown--wide")
    ];

    public static EditorFloatingMenuDescriptor? FindMenu(string? menuId) =>
        Menus.FirstOrDefault(menu => string.Equals(menu.MenuId, menuId, StringComparison.Ordinal));

    public static IReadOnlyList<IReadOnlyList<EditorToolbarActionDescriptor>> BuildActionGroups(IStringLocalizer<SharedResource> localizer) =>
        ActionGroups
            .Select(group => group.Select(action => LocalizeAction(localizer, action)).ToArray() as IReadOnlyList<EditorToolbarActionDescriptor>)
            .ToArray();

    public static IReadOnlyList<EditorFloatingMenuDescriptor> BuildMenus(IStringLocalizer<SharedResource> localizer) =>
        Menus.Select(menu => new EditorFloatingMenuDescriptor(
                menu.MenuId,
                menu.TriggerTestId,
                menu.PanelTestId,
                LocalizeMenuLabel(localizer, menu.Label),
                menu.DropdownGroups.Select(group => new EditorToolbarDropdownGroupDescriptor(
                    LocalizeGroupLabel(localizer, group.Label),
                    group.Actions.Select(action => LocalizeAction(localizer, action)).ToArray(),
                    group.HasSeparatorBefore)).ToArray(),
                menu.PanelCssClass))
            .ToArray();

    private static EditorToolbarActionDescriptor LocalizeAction(
        IStringLocalizer<SharedResource> localizer,
        EditorToolbarActionDescriptor action)
    {
        var tooltip = action.TestId switch
        {
            UiTestIds.Editor.FloatingVoice => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingVoiceTrigger),
            UiTestIds.Editor.FloatingEmotion => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingEmotionTrigger),
            UiTestIds.Editor.FloatingEmotionMotivational => Text(localizer, UiTextKey.EditorToolbarTooltipEmotionMotivational),
            UiTestIds.Editor.FloatingVoiceWhisper => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingVoiceWhisper),
            UiTestIds.Editor.FloatingVoiceLegato => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingVoiceLegato),
            UiTestIds.Editor.FloatingVoiceEnergy => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingVoiceEnergy),
            UiTestIds.Editor.FloatingPauseTrigger => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingPauseTrigger),
            UiTestIds.Editor.FloatingPauseTimed => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingPauseTimed),
            UiTestIds.Editor.FloatingSpeedTrigger => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingSpeedTrigger),
            UiTestIds.Editor.FloatingSpeedCustomWpm => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingSpeedCustomWpm),
            UiTestIds.Editor.FloatingInsert => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingInsertTrigger),
            UiTestIds.Editor.FloatingInsertPronunciation => Text(localizer, UiTextKey.EditorToolbarTooltipFloatingInsertPronunciation),
            _ => action.Tooltip
        };

        return string.Equals(tooltip, action.Tooltip, StringComparison.Ordinal)
            ? action
            : action with { Tooltip = tooltip };
    }

    private static string LocalizeGroupLabel(IStringLocalizer<SharedResource> localizer, string label) =>
        label switch
        {
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
            "Edit Points" => Text(localizer, UiTextKey.EditorToolbarGroupEditPoints),
            "Pronunciation" => Text(localizer, UiTextKey.EditorToolbarGroupPronunciation),
            _ => label
        };

    private static string LocalizeMenuLabel(IStringLocalizer<SharedResource> localizer, string label) =>
        label switch
        {
            "Voice Cues" => Text(localizer, UiTextKey.EditorToolbarSectionVoice),
            "Emotion" => Text(localizer, UiTextKey.EditorToolbarSectionEmotion),
            "Pause" => Text(localizer, UiTextKey.EditorToolbarSectionPause),
            "Speed" => Text(localizer, UiTextKey.EditorToolbarSectionSpeed),
            "Insert" => Text(localizer, UiTextKey.EditorToolbarSectionInsert),
            _ => label
        };

    private static string Text(IStringLocalizer<SharedResource> localizer, UiTextKey key) =>
        localizer[key.ToString()];
}
