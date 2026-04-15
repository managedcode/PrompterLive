using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.Tps;
using SdkModels = ManagedCode.Tps.Models;

namespace PrompterOne.Core.Services;

internal static class TpsSdkMapper
{
    public static TpsDocument ToLocalDocument(SdkModels.TpsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new TpsDocument
        {
            Metadata = new Dictionary<string, string>(document.Metadata, StringComparer.OrdinalIgnoreCase),
            Segments = document.Segments.Select(ToLocalSegment).ToList()
        };
    }

    public static CompiledScript ToLocalCompiledScript(SdkModels.CompiledScript script)
    {
        ArgumentNullException.ThrowIfNull(script);

        return new CompiledScript
        {
            Metadata = new Dictionary<string, string>(script.Metadata, StringComparer.OrdinalIgnoreCase),
            Segments = script.Segments.Select(ToLocalCompiledSegment).ToList()
        };
    }

    private static TpsSegment ToLocalSegment(SdkModels.TpsSegment segment)
    {
        return new TpsSegment
        {
            Id = segment.Id,
            Name = segment.Name,
            Content = segment.Content,
            TargetWPM = segment.TargetWpm,
            Emotion = segment.Emotion,
            Speaker = segment.Speaker,
            Archetype = segment.Archetype,
            Timing = segment.Timing,
            BackgroundColor = segment.BackgroundColor,
            TextColor = segment.TextColor,
            AccentColor = segment.AccentColor,
            LeadingContent = segment.LeadingContent,
            Blocks = segment.Blocks.Select(ToLocalBlock).ToList()
        };
    }

    private static TpsBlock ToLocalBlock(SdkModels.TpsBlock block)
    {
        return new TpsBlock
        {
            Id = block.Id,
            Name = block.Name,
            Content = block.Content,
            TargetWPM = block.TargetWpm,
            Emotion = block.Emotion,
            Speaker = block.Speaker,
            Archetype = block.Archetype
        };
    }

    private static CompiledSegment ToLocalCompiledSegment(SdkModels.CompiledSegment segment)
    {
        return new CompiledSegment
        {
            Id = segment.Id,
            Name = segment.Name,
            TargetWPM = segment.TargetWpm,
            Emotion = segment.Emotion,
            Speaker = segment.Speaker,
            Archetype = segment.Archetype,
            Timing = segment.Timing,
            BackgroundColor = segment.BackgroundColor,
            TextColor = segment.TextColor,
            AccentColor = segment.AccentColor,
            Blocks = segment.Blocks.Select(ToLocalCompiledBlock).ToList(),
            Words = segment.Words.Select(ToLocalCompiledWord).ToList()
        };
    }

    private static CompiledBlock ToLocalCompiledBlock(SdkModels.CompiledBlock block)
    {
        return new CompiledBlock
        {
            Id = block.Id,
            Name = block.Name,
            TargetWPM = block.TargetWpm,
            Emotion = block.Emotion,
            Speaker = block.Speaker,
            Archetype = block.Archetype,
            Phrases = block.Phrases.Select(ToLocalCompiledPhrase).ToList(),
            Words = block.Words.Select(ToLocalCompiledWord).ToList()
        };
    }

    private static CompiledPhrase ToLocalCompiledPhrase(SdkModels.CompiledPhrase phrase)
    {
        return new CompiledPhrase
        {
            Id = phrase.Id,
            Words = phrase.Words.Select(ToLocalCompiledWord).ToList()
        };
    }

    private static CompiledWord ToLocalCompiledWord(SdkModels.CompiledWord word)
    {
        return new CompiledWord
        {
            CleanText = word.CleanText,
            CharacterCount = word.CharacterCount,
            ORPPosition = word.OrpPosition,
            DisplayDuration = TimeSpan.FromMilliseconds(word.DisplayDurationMs),
            Metadata = ToLocalWordMetadata(word.Metadata)
        };
    }

    private static WordMetadata ToLocalWordMetadata(SdkModels.WordMetadata metadata)
    {
        return new WordMetadata
        {
            IsEmphasis = metadata.IsEmphasis,
            EmphasisLevel = metadata.EmphasisLevel,
            EmphasisStyle = metadata.EmphasisStyle,
            IsPause = metadata.IsPause,
            PauseDuration = metadata.PauseDurationMs,
            IsHighlight = metadata.IsHighlight,
            IsBreath = metadata.IsBreath,
            IsEditPoint = metadata.IsEditPoint,
            EditPointPriority = metadata.EditPointPriority,
            EmotionHint = metadata.EmotionHint,
            InlineEmotionHint = metadata.InlineEmotionHint,
            VolumeLevel = metadata.VolumeLevel,
            DeliveryMode = metadata.DeliveryMode,
            ArticulationStyle = metadata.ArticulationStyle,
            EnergyLevel = metadata.EnergyLevel,
            MelodyLevel = metadata.MelodyLevel,
            PhoneticGuide = metadata.PhoneticGuide,
            PronunciationGuide = metadata.PronunciationGuide,
            StressText = metadata.StressText,
            StressGuide = metadata.StressGuide,
            SpeedOverride = metadata.SpeedOverride,
            SpeedMultiplier = metadata.SpeedMultiplier is null ? null : (float)metadata.SpeedMultiplier.Value,
            Speaker = metadata.Speaker,
            HeadCue = metadata.HeadCue
        };
    }
}
