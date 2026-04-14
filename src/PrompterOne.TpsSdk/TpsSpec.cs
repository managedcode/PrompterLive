namespace ManagedCode.Tps;

public static partial class TpsSpec
{
    public const int DefaultBaseWpm = 140;
    public const int MaximumWpm = 220;
    public const int MinimumWpm = 80;
    public const int MediumPauseDurationMs = 600;
    public const int ShortPauseDurationMs = 300;
    public const string DefaultEmotion = "neutral";
    public const string DefaultImplicitSegmentName = "Content";
    public const string DefaultProfile = "Actor";
    public const string ArchetypePrefix = "Archetype:";
    public const string SpeakerPrefix = "Speaker:";
    public const string WpmSuffix = "WPM";
    public const int EnergyLevelMin = 1;
    public const int EnergyLevelMax = 10;
    public const int MelodyLevelMin = 1;
    public const int MelodyLevelMax = 10;
    public const int ArchetypeRhythmMinimumWords = 12;

    public static class EmotionNames
    {
        public const string Neutral = DefaultEmotion;
        public const string Warm = "warm";
        public const string Professional = "professional";
        public const string Focused = "focused";
        public const string Concerned = "concerned";
        public const string Urgent = "urgent";
        public const string Motivational = "motivational";
        public const string Excited = "excited";
        public const string Happy = "happy";
        public const string Sad = "sad";
        public const string Calm = "calm";
        public const string Energetic = "energetic";
    }

    public static class EditPointPriorityNames
    {
        public const string High = "high";
        public const string Medium = "medium";
        public const string Low = "low";
    }

    public static class HeadCueCodes
    {
        public const string H0 = "H0";
        public const string H1 = "H1";
        public const string H4 = "H4";
        public const string H5 = "H5";
        public const string H6 = "H6";
        public const string H7 = "H7";
        public const string H8 = "H8";
        public const string H9 = "H9";
    }

    public static class PaletteHex
    {
        public const string AccentBlue = "#2563EB";
        public const string TextSlate900 = "#0F172A";
        public const string BackgroundBlue400 = "#60A5FA";
        public const string AccentOrange600 = "#EA580C";
        public const string TextStone900 = "#1C1917";
        public const string BackgroundOrange300 = "#FDBA74";
        public const string AccentBlue700 = "#1D4ED8";
        public const string BackgroundBlue300 = "#93C5FD";
        public const string AccentGreen700 = "#15803D";
        public const string TextGreen950 = "#052E16";
        public const string BackgroundGreen300 = "#86EFAC";
        public const string AccentRed700 = "#B91C1C";
        public const string TextRose950 = "#1F1111";
        public const string BackgroundRed300 = "#FCA5A5";
        public const string AccentRed600 = "#DC2626";
        public const string TextWhiteRose = "#FFF7F7";
        public const string AccentViolet600 = "#7C3AED";
        public const string TextWhite = "#FFFFFF";
        public const string BackgroundViolet300 = "#C4B5FD";
        public const string AccentPink600 = "#DB2777";
        public const string TextWhitePink = "#FFF7FB";
        public const string BackgroundPink300 = "#F9A8D4";
        public const string AccentAmber600 = "#D97706";
        public const string BackgroundAmber300 = "#FCD34D";
        public const string AccentIndigo600 = "#4F46E5";
        public const string TextIndigo50 = "#EEF2FF";
        public const string BackgroundIndigo300 = "#A5B4FC";
        public const string AccentTeal700 = "#0F766E";
        public const string TextTeal50 = "#F0FDFA";
        public const string BackgroundTeal200 = "#99F6E4";
        public const string AccentOrange700 = "#C2410C";
        public const string TextOrange50 = "#FFF7ED";
    }

    public static class FrontMatterKeys
    {
        public const string Author = "author";
        public const string BaseWpm = "base_wpm";
        public const string Created = "created";
        public const string Duration = "duration";
        public const string Profile = "profile";
        public const string SpeedOffsetsFast = "speed_offsets.fast";
        public const string SpeedOffsetsSlow = "speed_offsets.slow";
        public const string SpeedOffsetsXfast = "speed_offsets.xfast";
        public const string SpeedOffsetsXslow = "speed_offsets.xslow";
        public const string Title = "title";
        public const string Version = "version";
    }

    public static class LegacyKeys
    {
        public const string DisplayDuration = "display_duration";
        public const string FastOffset = "fast_offset";
        public const string PresetsFast = "presets.fast";
        public const string PresetsSlow = "presets.slow";
        public const string PresetsXfast = "presets.xfast";
        public const string PresetsXslow = "presets.xslow";
        public const string SlowOffset = "slow_offset";
        public const string XfastOffset = "xfast_offset";
        public const string XslowOffset = "xslow_offset";
    }

    public static class Tags
    {
        public const string Aside = "aside";
        public const string Breath = "breath";
        public const string Building = "building";
        public const string EditPoint = "edit_point";
        public const string Emphasis = "emphasis";
        public const string Energy = "energy";
        public const string Fast = "fast";
        public const string Highlight = "highlight";
        public const string Legato = "legato";
        public const string Loud = "loud";
        public const string Melody = "melody";
        public const string Normal = "normal";
        public const string Pause = "pause";
        public const string Phonetic = "phonetic";
        public const string Pronunciation = "pronunciation";
        public const string Rhetorical = "rhetorical";
        public const string Sarcasm = "sarcasm";
        public const string Slow = "slow";
        public const string Soft = "soft";
        public const string Staccato = "staccato";
        public const string Stress = "stress";
        public const string Whisper = "whisper";
        public const string Xfast = "xfast";
        public const string Xslow = "xslow";
    }

    public static class WordKinds
    {
        public const string Word = "word";
        public const string Pause = "pause";
        public const string Breath = "breath";
        public const string EditPoint = "edit-point";
    }

    public static class Markers
    {
        public const string ClosingTagPrefix = "/";
        public const char ShortPauseMarker = '/';
        public const char TagArgumentSeparator = ':';
        public const string MarkdownItalicScope = "__markdown-italic__";
        public const string MarkdownStrongScope = "__markdown-strong__";
    }

    public static class EmphasisStyles
    {
        public const string Emphasis = "emphasis";
        public const string MarkdownItalic = "markdown-italic";
        public const string MarkdownStrong = "markdown-strong";
    }

    public static class NumericComparisons
    {
        public const double SpeedMultiplierTolerance = 0.0001d;
    }

    public static class DiagnosticCodes
    {
        public const string ArchetypeArticulationMismatch = "archetype-articulation-mismatch";
        public const string ArchetypeEnergyMismatch = "archetype-energy-mismatch";
        public const string ArchetypeMelodyMismatch = "archetype-melody-mismatch";
        public const string ArchetypeRhythmEmphasisDensity = "archetype-rhythm-emphasis-density";
        public const string ArchetypeRhythmPauseDuration = "archetype-rhythm-pause-duration";
        public const string ArchetypeRhythmPauseFrequency = "archetype-rhythm-pause-frequency";
        public const string ArchetypeRhythmPhraseLength = "archetype-rhythm-phrase-length";
        public const string ArchetypeRhythmSpeedVariation = "archetype-rhythm-speed-variation";
        public const string ArchetypeSpeedMismatch = "archetype-speed-mismatch";
        public const string ArchetypeVolumeMismatch = "archetype-volume-mismatch";
        public const string InvalidEnergyLevel = "invalid-energy-level";
        public const string InvalidFrontMatter = "invalid-front-matter";
        public const string InvalidHeader = "invalid-header";
        public const string InvalidHeaderParameter = "invalid-header-parameter";
        public const string InvalidMelodyLevel = "invalid-melody-level";
        public const string InvalidPause = "invalid-pause";
        public const string InvalidTagArgument = "invalid-tag-argument";
        public const string InvalidWpm = "invalid-wpm";
        public const string MismatchedClosingTag = "mismatched-closing-tag";
        public const string UnclosedTag = "unclosed-tag";
        public const string UnknownArchetype = "unknown-archetype";
        public const string UnknownTag = "unknown-tag";
        public const string UnterminatedTag = "unterminated-tag";
    }
}
