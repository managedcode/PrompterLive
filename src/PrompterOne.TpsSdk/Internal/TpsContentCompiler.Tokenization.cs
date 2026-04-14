using System.Text;
using ManagedCode.Tps.Models;

namespace ManagedCode.Tps.Internal;

internal sealed partial class TpsContentCompiler
{
    private static void AppendLiteral(string literal, List<InlineScope> scopes, InheritedFormattingState inherited, StringBuilder builder, ref TokenAccumulator? token)
    {
        foreach (var character in literal)
        {
            AppendCharacter(character, scopes, inherited, builder, ref token);
        }
    }

    private static void AppendCharacter(char character, List<InlineScope> scopes, InheritedFormattingState inherited, StringBuilder builder, ref TokenAccumulator? token)
    {
        token ??= new TokenAccumulator();
        token.Apply(ResolveActiveState(scopes, inherited), character);
        builder.Append(character);
    }

    private static void FinalizeToken(
        List<WordSeed> words,
        List<PhraseSeed> phrases,
        List<WordSeed> currentPhrase,
        StringBuilder builder,
        ref TokenAccumulator? token,
        InheritedFormattingState inherited)
    {
        if (builder.Length == 0 || token is null)
        {
            builder.Clear();
            token = null;
            return;
        }

        var text = TpsEscaping.Restore(builder.ToString()).Trim();
        builder.Clear();
        if (TpsTextRules.IsStandalonePunctuationToken(text))
        {
            if (AttachStandalonePunctuation(words, currentPhrase, text) && TpsSupport.IsSentenceEndingPunctuation(text))
            {
                FlushPhrase(phrases, currentPhrase);
            }

            token = null;
            return;
        }

        var metadata = token.BuildWordMetadata(inherited.TargetWpm);
        var effectiveWpm = TpsSupport.ResolveEffectiveWpm(inherited.TargetWpm, metadata.SpeedOverride, metadata.SpeedMultiplier);
        var word = new WordSeed(
            TpsSpec.WordKinds.Word,
            text,
            text.Length,
            TpsSupport.CalculateOrpIndex(text),
            TpsSupport.CalculateWordDurationMs(text, effectiveWpm),
            metadata);

        words.Add(word);
        currentPhrase.Add(word);
        if (TpsSupport.IsSentenceEndingPunctuation(text))
        {
            FlushPhrase(phrases, currentPhrase);
        }

        token = null;
    }

    private static bool AttachStandalonePunctuation(List<WordSeed> words, List<WordSeed> currentPhrase, string punctuation)
    {
        var target = currentPhrase.LastOrDefault(IsSpokenWord) ?? words.LastOrDefault(IsSpokenWord);
        if (target is null)
        {
            return false;
        }

        target.CleanText = string.Concat(target.CleanText, TpsTextRules.BuildStandalonePunctuationSuffix(punctuation));
        target.CharacterCount = target.CleanText.Length;
        target.OrpPosition = TpsSupport.CalculateOrpIndex(target.CleanText);
        return true;
    }

    private static void FlushPhrase(List<PhraseSeed> phrases, List<WordSeed> currentPhrase)
    {
        if (currentPhrase.Count == 0)
        {
            return;
        }

        phrases.Add(new PhraseSeed([.. currentPhrase], string.Join(' ', currentPhrase.Where(IsSpokenWord).Select(word => word.CleanText))));
        currentPhrase.Clear();
    }

    private static WordSeed CreateControlWord(string kind, InheritedFormattingState inherited, int? pauseDuration = null, string? editPointPriority = null)
    {
        var headCue = TpsSpec.EmotionHeadCues.TryGetValue(inherited.Emotion, out var cue)
            ? cue
            : TpsSpec.EmotionHeadCues[TpsSpec.DefaultEmotion];
        return new WordSeed(
            kind,
            string.Empty,
            0,
            0,
            pauseDuration ?? 0,
            new WordMetadata
            {
                IsPause = kind == TpsSpec.WordKinds.Pause,
                PauseDurationMs = pauseDuration,
                IsBreath = kind == TpsSpec.WordKinds.Breath,
                IsEditPoint = kind == TpsSpec.WordKinds.EditPoint,
                EditPointPriority = editPointPriority,
                EmotionHint = inherited.Emotion,
                Speaker = inherited.Speaker,
                HeadCue = headCue
            });
    }

    private static ActiveInlineState ResolveActiveState(List<InlineScope> scopes, InheritedFormattingState inherited)
    {
        var absoluteSpeed = inherited.TargetWpm;
        var hasAbsoluteSpeed = false;
        var hasRelativeSpeed = false;
        var relativeSpeedMultiplier = 1d;
        var emphasisLevel = 0;
        string? emphasisStyle = null;
        var highlight = false;
        var emotion = inherited.Emotion;
        string? inlineEmotion = null;
        string? volumeLevel = null;
        string? deliveryMode = null;
        string? articulationStyle = null;
        int? energyLevel = null;
        int? melodyLevel = null;
        string? phoneticGuide = null;
        string? pronunciationGuide = null;
        string? stressGuide = null;
        var stressWrap = false;

        foreach (var scope in scopes)
        {
            if (scope.AbsoluteSpeed is int scopedAbsoluteSpeed)
            {
                absoluteSpeed = scopedAbsoluteSpeed;
                hasAbsoluteSpeed = true;
                hasRelativeSpeed = false;
                relativeSpeedMultiplier = 1d;
            }

            if (scope.ResetSpeed)
            {
                hasRelativeSpeed = false;
                relativeSpeedMultiplier = 1d;
            }

            if (scope.RelativeSpeedMultiplier is double scopedRelativeSpeed)
            {
                hasRelativeSpeed = true;
                relativeSpeedMultiplier *= scopedRelativeSpeed;
            }

            if (scope.EmphasisLevel > emphasisLevel ||
                scope.EmphasisLevel == emphasisLevel && !string.IsNullOrWhiteSpace(scope.EmphasisStyle))
            {
                emphasisStyle = scope.EmphasisStyle ?? emphasisStyle;
            }

            emphasisLevel = Math.Max(emphasisLevel, scope.EmphasisLevel);
            highlight |= scope.Highlight;
            if (scope.InlineEmotion is not null)
            {
                emotion = scope.InlineEmotion;
                inlineEmotion = scope.InlineEmotion;
            }

            volumeLevel = scope.VolumeLevel ?? volumeLevel;
            deliveryMode = scope.DeliveryMode ?? deliveryMode;
            articulationStyle = scope.ArticulationStyle ?? articulationStyle;
            if (scope.EnergyLevel is int scopeEnergy)
            {
                energyLevel = scopeEnergy;
            }

            if (scope.MelodyLevel is int scopeMelody)
            {
                melodyLevel = scopeMelody;
            }

            phoneticGuide = scope.PhoneticGuide ?? phoneticGuide;
            pronunciationGuide = scope.PronunciationGuide ?? pronunciationGuide;
            stressGuide = scope.StressGuide ?? stressGuide;
            stressWrap |= scope.StressWrap;
        }

        return new ActiveInlineState(
            emotion,
            inlineEmotion,
            inherited.Speaker,
            emphasisLevel,
            emphasisStyle,
            highlight,
            volumeLevel,
            deliveryMode,
            articulationStyle,
            energyLevel,
            melodyLevel,
            phoneticGuide,
            pronunciationGuide,
            stressGuide,
            stressWrap,
            hasAbsoluteSpeed,
            absoluteSpeed,
            hasRelativeSpeed,
            relativeSpeedMultiplier);
    }

    private static bool IsSpokenWord(WordSeed word) =>
        string.Equals(word.Kind, TpsSpec.WordKinds.Word, StringComparison.Ordinal) && !string.IsNullOrEmpty(word.CleanText);
}
