namespace PrompterOne.Shared.Contracts;

public static class UiDataAttributes
{
    public static class Editor
    {
        public const string IconShape = "data-editor-icon-shape";
        public const string IconShapeDot = "dot";
        public const string IconShapeGlyph = "glyph";
    }

    public static class Live
    {
        public const string Level = "data-live-level";
        public const string State = "data-live-state";
    }

    public static class Teleprompter
    {
        public const string CardState = "data-reader-card-state";
        public const string DurationMilliseconds = "data-ms";
        public const string EffectiveWordsPerMinute = "data-effective-wpm";
        public const string PauseMilliseconds = "data-pause-ms";
        public const string Pronunciation = "data-pronunciation";
        public const string TotalMilliseconds = "data-total-ms";
        public const string TotalSeconds = "data-total-seconds";
        public const string WordState = "data-reader-word-state";
        public const string ActiveState = "active";
        public const string NextState = "next";
        public const string PreviousState = "previous";
        public const string ReadState = "read";
    }
}
