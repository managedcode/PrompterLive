using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Components.Editor;

internal static partial class EditorTpsMenuCatalog
{
    private static IReadOnlyList<EditorTpsMenuGroupDefinition> CreateEmotionGroupDefinitions() =>
    [
        new("Delivery Modes", [
            Wrap("delivery-sarcasm", "float-delivery-sarcasm", "editor-delivery-sarcasm", UiTestIds.Editor.FloatingDeliverySarcasm, EditorActionContents.Label("SARC", "Sarcasm", "[sarcasm]", EditorActionContentTone.DeliverySarcasm), "Apply a sarcastic delivery cue. [sarcasm]text[/sarcasm]", "[sarcasm]", "[/sarcasm]"),
            Wrap("delivery-aside", "float-delivery-aside", "editor-delivery-aside", "editor-float-delivery-aside", EditorActionContents.Label("ASIDE", "Aside", "[aside]", EditorActionContentTone.DeliveryAside), "Mark the phrase as an aside. [aside]text[/aside]", "[aside]", "[/aside]"),
            Wrap("delivery-rhetorical", "float-delivery-rhetorical", "editor-delivery-rhetorical", "editor-float-delivery-rhetorical", EditorActionContents.Label("WHY?", "Rhetorical", "[rhetorical]", EditorActionContentTone.DeliveryRhetorical), "Use rhetorical delivery without changing emotion. [rhetorical]text[/rhetorical]", "[rhetorical]", "[/rhetorical]"),
            Wrap("delivery-building", "float-delivery-building", "editor-delivery-building", "editor-float-delivery-building", EditorActionContents.Label("BUILD", "Building", "[building]", EditorActionContentTone.DeliveryBuilding), "Gradually build intensity through the phrase. [building]text[/building]", "[building]", "[/building]")
        ]),
        new("TPS Emotions", [
            Wrap("emotion-neutral", "float-emotion-neutral", "editor-emotion-neutral", "editor-float-emotion-neutral", EditorActionContents.Emotion("Neutral", "😐", EditorActionContentTone.EmotionNeutral), "Default balanced tone. Inline: [neutral]text[/neutral]", "[neutral]", "[/neutral]"),
            Wrap("emotion-warm", "float-emotion-warm", "editor-emotion-warm", "editor-float-emotion-warm", EditorActionContents.Emotion("Warm", "😊", EditorActionContentTone.EmotionWarm), "Friendly, welcoming tone. Inline: [warm]text[/warm]", "[warm]", "[/warm]"),
            Wrap("emotion-professional", "float-emotion-professional", "editor-emotion-professional", UiTestIds.Editor.FloatingEmotionProfessional, EditorActionContents.Emotion("Professional", "💼", EditorActionContentTone.EmotionProfessional), "Business-like, formal. Inline: [professional]text[/professional]", "[professional]", "[/professional]"),
            Wrap("emotion-focused", "float-emotion-focused", "editor-emotion-focused", "editor-float-emotion-focused", EditorActionContents.Emotion("Focused", "🎯", EditorActionContentTone.EmotionFocused), "Concentrated, precise. Inline: [focused]text[/focused]", "[focused]", "[/focused]"),
            Wrap("emotion-concerned", "float-emotion-concerned", "editor-emotion-concerned", "editor-float-emotion-concerned", EditorActionContents.Emotion("Concerned", "😟", EditorActionContentTone.EmotionConcerned), "Worried, empathetic. Inline: [concerned]text[/concerned]", "[concerned]", "[/concerned]"),
            Wrap("emotion-urgent", "float-emotion-urgent", "editor-emotion-urgent", "editor-float-emotion-urgent", EditorActionContents.Emotion("Urgent", "🚨", EditorActionContentTone.EmotionUrgent), "Critical, immediate attention. Inline: [urgent]text[/urgent]", "[urgent]", "[/urgent]"),
            Wrap("emotion-motivational", "float-emotion-motivational", UiTestIds.Editor.EmotionMotivational, UiTestIds.Editor.FloatingEmotionMotivational, EditorActionContents.Emotion("Motivational", "💪", EditorActionContentTone.EmotionMotivational), "Inspiring, encouraging. Inline: [motivational]text[/motivational]", "[motivational]", "[/motivational]"),
            Wrap("emotion-excited", "float-emotion-excited", "editor-emotion-excited", "editor-float-emotion-excited", EditorActionContents.Emotion("Excited", "🚀", EditorActionContentTone.EmotionExcited), "Enthusiastic, energetic. Inline: [excited]text[/excited]", "[excited]", "[/excited]"),
            Wrap("emotion-happy", "float-emotion-happy", "editor-emotion-happy", "editor-float-emotion-happy", EditorActionContents.Emotion("Happy", "😄", EditorActionContentTone.EmotionHappy), "Joyful, positive. Inline: [happy]text[/happy]", "[happy]", "[/happy]"),
            Wrap("emotion-sad", "float-emotion-sad", "editor-emotion-sad", "editor-float-emotion-sad", EditorActionContents.Emotion("Sad", "😢", EditorActionContentTone.EmotionSad), "Melancholy, somber. Inline: [sad]text[/sad]", "[sad]", "[/sad]"),
            Wrap("emotion-calm", "float-emotion-calm", "editor-emotion-calm", "editor-float-emotion-calm", EditorActionContents.Emotion("Calm", "😌", EditorActionContentTone.EmotionCalm), "Peaceful, relaxed. Inline: [calm]text[/calm]", "[calm]", "[/calm]"),
            Wrap("emotion-energetic", "float-emotion-energetic", "editor-emotion-energetic", "editor-float-emotion-energetic", EditorActionContents.Emotion("Energetic", "⚡", EditorActionContentTone.EmotionEnergetic), "High energy, dynamic. Inline: [energetic]text[/energetic]", "[energetic]", "[/energetic]")
        ], HasSeparatorBefore: true)
    ];

    private static IReadOnlyList<EditorTpsMenuGroupDefinition> CreateVoiceGroupDefinitions() =>
    [
        new("Volume", [
            Wrap("voice-loud", "float-voice-loud-menu", UiTestIds.Editor.ColorLoud, "editor-float-voice-loud-menu", EditorActionContents.Label("LOUD", "Loud", "[loud]", EditorActionContentTone.Loud), "Raise vocal force without changing emotion. [loud]text[/loud]", "[loud]", "[/loud]"),
            Wrap("voice-soft", "float-voice-soft-menu", UiTestIds.Editor.ColorSoft, "editor-float-voice-soft-menu", EditorActionContents.Label("SOFT", "Soft", "[soft]", EditorActionContentTone.Soft), "Soften delivery and keep the phrase gentle. [soft]text[/soft]", "[soft]", "[/soft]"),
            Wrap("voice-whisper", "float-voice-whisper-menu", UiTestIds.Editor.ColorWhisper, UiTestIds.Editor.FloatingVoiceWhisper, EditorActionContents.Label("LOW", "Whisper", "[whisper]", EditorActionContentTone.Whisper), "Very quiet, intimate delivery. [whisper]text[/whisper]", "[whisper]", "[/whisper]")
        ]),
        new("Articulation", [
            Wrap("voice-legato", "float-voice-legato-menu", UiTestIds.Editor.ColorLegato, UiTestIds.Editor.FloatingVoiceLegato, EditorActionContents.Label("FLOW", "Legato", "[legato]", EditorActionContentTone.Legato), "Smooth connected phrasing. [legato]text[/legato]", "[legato]", "[/legato]"),
            Wrap("voice-staccato", "float-voice-staccato-menu", UiTestIds.Editor.ColorStaccato, UiTestIds.Editor.FloatingVoiceStaccato, EditorActionContents.Label("CUT", "Staccato", "[staccato]", EditorActionContentTone.Staccato), "Crisp separated phrasing. [staccato]text[/staccato]", "[staccato]", "[/staccato]")
        ], HasSeparatorBefore: true),
        new("Dynamics", [
            Wrap("voice-energy", "float-voice-energy-menu", UiTestIds.Editor.ColorEnergy, UiTestIds.Editor.FloatingVoiceEnergy, EditorActionContents.Label("PUSH", "Energy", "[energy:8]", EditorActionContentTone.Energy), "Explicit delivery energy from 1 to 10. [energy:8]text[/energy]", "[energy:8]", "[/energy]"),
            Wrap("voice-melody", "float-voice-melody-menu", UiTestIds.Editor.ColorMelody, UiTestIds.Editor.FloatingVoiceMelody, EditorActionContents.Label("ARC", "Melody", "[melody:4]", EditorActionContentTone.Melody), "Explicit melody and pitch variation from 1 to 10. [melody:4]text[/melody]", "[melody:4]", "[/melody]")
        ], HasSeparatorBefore: true),
        new("Stress", [
            Wrap("voice-stress", "float-voice-stress-menu", UiTestIds.Editor.ColorStress, "editor-float-voice-stress-menu", EditorActionContents.Label("MARK", "Stress", "[stress]", EditorActionContentTone.Stress), "Stress a word or phrase inline. [stress]text[/stress]", "[stress]", "[/stress]"),
            Wrap("voice-stress-guide", "float-voice-stress-guide-menu", UiTestIds.Editor.ColorGuide, "editor-float-voice-stress-guide-menu", EditorActionContents.Label("GUIDE", "Stress guide", "[stress:...]", EditorActionContentTone.Stress), "Add a spoken stress cue. [stress:rising]text[/stress]", "[stress:rising]", "[/stress]")
        ], HasSeparatorBefore: true),
        new("Reset", [
            Clear("voice-clear", "float-voice-clear", UiTestIds.Editor.ColorClear, "editor-float-voice-clear", "Remove supported inline TPS wrappers from the selected text")
        ], HasSeparatorBefore: true)
    ];
}
