using System.Text;
using ManagedCode.Tps.Models;

namespace ManagedCode.Tps.Internal;

internal sealed record InheritedFormattingState(int TargetWpm, string Emotion, string? Speaker, IReadOnlyDictionary<string, int> SpeedOffsets, string? Archetype = null);

internal sealed record ContentCompilationResult(List<WordSeed> Words, List<PhraseSeed> Phrases);

internal sealed class WordSeed(string kind, string cleanText, int characterCount, int orpPosition, int displayDurationMs, WordMetadata metadata)
{
    public string Kind { get; } = kind;

    public string CleanText { get; set; } = cleanText;

    public int CharacterCount { get; set; } = characterCount;

    public int OrpPosition { get; set; } = orpPosition;

    public int DisplayDurationMs { get; } = displayDurationMs;

    public WordMetadata Metadata { get; } = metadata;
}

internal sealed record PhraseSeed(List<WordSeed> Words, string Text);

internal sealed record InlineScope(
    string Name,
    int EmphasisLevel = 0,
    string? EmphasisStyle = null,
    bool Highlight = false,
    string? InlineEmotion = null,
    string? VolumeLevel = null,
    string? DeliveryMode = null,
    string? ArticulationStyle = null,
    int? EnergyLevel = null,
    int? MelodyLevel = null,
    string? PhoneticGuide = null,
    string? PronunciationGuide = null,
    string? StressGuide = null,
    bool StressWrap = false,
    int? AbsoluteSpeed = null,
    double? RelativeSpeedMultiplier = null,
    bool ResetSpeed = false);

internal sealed record ActiveInlineState(
    string Emotion,
    string? InlineEmotion,
    string? Speaker,
    int EmphasisLevel,
    string? EmphasisStyle,
    bool Highlight,
    string? VolumeLevel,
    string? DeliveryMode,
    string? ArticulationStyle,
    int? EnergyLevel,
    int? MelodyLevel,
    string? PhoneticGuide,
    string? PronunciationGuide,
    string? StressGuide,
    bool StressWrap,
    bool HasAbsoluteSpeed,
    int AbsoluteSpeed,
    bool HasRelativeSpeed,
    double RelativeSpeedMultiplier);

internal sealed record TagToken(string Raw, string Name, string? Argument, bool IsClosing);

internal sealed class TokenAccumulator
{
    private readonly StringBuilder _stressText = new();

    public int EmphasisLevel { get; private set; }

    public string? EmphasisStyle { get; private set; }

    public bool IsHighlight { get; private set; }

    public string EmotionHint { get; private set; } = string.Empty;

    public string? InlineEmotionHint { get; private set; }

    public string? VolumeLevel { get; private set; }

    public string? DeliveryMode { get; private set; }

    public string? ArticulationStyle { get; private set; }

    public int? EnergyLevel { get; private set; }

    public int? MelodyLevel { get; private set; }

    public string? PhoneticGuide { get; private set; }

    public string? PronunciationGuide { get; private set; }

    public string? StressGuide { get; private set; }

    public bool HasAbsoluteSpeed { get; private set; }

    public int AbsoluteSpeed { get; private set; }

    public bool HasRelativeSpeed { get; private set; }

    public double RelativeSpeedMultiplier { get; private set; } = 1d;

    public string? Speaker { get; private set; }

    public void Apply(ActiveInlineState state, char character)
    {
        if (state.EmphasisLevel > EmphasisLevel ||
            state.EmphasisLevel == EmphasisLevel && !string.IsNullOrWhiteSpace(state.EmphasisStyle))
        {
            EmphasisStyle = state.EmphasisStyle ?? EmphasisStyle;
        }

        EmphasisLevel = Math.Max(EmphasisLevel, state.EmphasisLevel);
        IsHighlight |= state.Highlight;
        EmotionHint = state.Emotion;
        InlineEmotionHint = state.InlineEmotion ?? InlineEmotionHint;
        VolumeLevel = state.VolumeLevel ?? VolumeLevel;
        DeliveryMode = state.DeliveryMode ?? DeliveryMode;
        ArticulationStyle = state.ArticulationStyle ?? ArticulationStyle;
        if (state.EnergyLevel is int stateEnergy)
        {
            EnergyLevel = stateEnergy;
        }

        if (state.MelodyLevel is int stateMelody)
        {
            MelodyLevel = stateMelody;
        }

        PhoneticGuide = state.PhoneticGuide ?? PhoneticGuide;
        PronunciationGuide = state.PronunciationGuide ?? PronunciationGuide;
        StressGuide = state.StressGuide ?? StressGuide;
        Speaker = state.Speaker;

        if (state.StressWrap)
        {
            _stressText.Append(character);
        }

        if (!char.IsWhiteSpace(character) && !TpsTextRules.IsStandalonePunctuationToken(character.ToString()))
        {
            HasAbsoluteSpeed = state.HasAbsoluteSpeed;
            AbsoluteSpeed = state.AbsoluteSpeed;
            HasRelativeSpeed = state.HasRelativeSpeed;
            RelativeSpeedMultiplier = state.RelativeSpeedMultiplier;
        }
    }

    public WordMetadata BuildWordMetadata(int inheritedWpm)
    {
        var headCue = TpsSpec.EmotionHeadCues.TryGetValue(EmotionHint, out var cue)
            ? cue
            : TpsSpec.EmotionHeadCues[TpsSpec.DefaultEmotion];
        var metadata = new WordMetadata
        {
            IsEmphasis = EmphasisLevel > 0,
            EmphasisLevel = EmphasisLevel,
            EmphasisStyle = EmphasisStyle,
            IsHighlight = IsHighlight,
            EmotionHint = EmotionHint,
            InlineEmotionHint = InlineEmotionHint,
            VolumeLevel = VolumeLevel,
            DeliveryMode = DeliveryMode,
            ArticulationStyle = ArticulationStyle,
            EnergyLevel = EnergyLevel,
            MelodyLevel = MelodyLevel,
            PhoneticGuide = PhoneticGuide,
            PronunciationGuide = PronunciationGuide,
            StressText = _stressText.Length > 0 ? _stressText.ToString() : null,
            StressGuide = StressGuide,
            Speaker = Speaker,
            HeadCue = headCue
        };

        if (HasAbsoluteSpeed)
        {
            var effectiveWpm = HasRelativeSpeed
                ? Math.Max(1, (int)Math.Round(AbsoluteSpeed * RelativeSpeedMultiplier, MidpointRounding.AwayFromZero))
                : AbsoluteSpeed;
            if (effectiveWpm != inheritedWpm)
            {
                metadata = metadata with { SpeedOverride = effectiveWpm };
            }
        }
        else if (HasRelativeSpeed && Math.Abs(RelativeSpeedMultiplier - 1d) > TpsSpec.NumericComparisons.SpeedMultiplierTolerance)
        {
            metadata = metadata with { SpeedMultiplier = RelativeSpeedMultiplier };
        }

        return metadata;
    }
}
