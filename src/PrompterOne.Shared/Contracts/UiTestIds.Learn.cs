namespace PrompterOne.Shared.Contracts;

public static partial class UiTestIds
{
    public static class Learn
    {
        public const string Crosshair = "learn-crosshair";
        public const string ContextLeft = "learn-context-left";
        public const string ContextRight = "learn-context-right";
        public const string Display = "learn-display";
        public const string ActivePhraseHighlight = "learn-active-phrase-highlight";
        public const string FocusRow = "learn-focus-row";
        public const string LoopToggle = "learn-loop-toggle";
        public const string NextPhrase = "learn-next-phrase";
        public const string NotesEmpty = "learn-notes-empty";
        public const string NotesList = "learn-notes-list";
        public const string NotesPanel = "learn-notes-panel";
        public const string NotesSave = "learn-notes-save";
        public const string NotesSection = "learn-notes-section";
        public const string NotesTextarea = "learn-notes-textarea";
        public const string OrpLine = "learn-orp-line";
        public const string Page = "learn-page";
        public const string PauseIcon = "learn-pause-icon";
        public const string PlayToggle = "learn-play-toggle";
        public const string PlayIcon = "learn-play-icon";
        public const string ProgressFill = "learn-progress-fill";
        public const string ProgressLabel = "learn-progress-label";
        public const string RestartPhrase = "learn-restart-phrase";
        public const string SpeedValue = "learn-speed-value";
        public const string SpeedDown = "learn-speed-down";
        public const string SpeedUp = "learn-speed-up";
        public const string StepBackward = "learn-step-backward";
        public const string StepBackwardLarge = "learn-step-backward-large";
        public const string StepForward = "learn-step-forward";
        public const string StepForwardLarge = "learn-step-forward-large";
        public const string Word = "learn-word";
        public const string WordLeading = "learn-word-leading";
        public const string WordOrp = "learn-word-orp";
        public const string WordShell = "learn-word-shell";
        public const string WordTrailing = "learn-word-trailing";

        public static string NoteItem(string id) => $"learn-note-{id}";
    }
}
