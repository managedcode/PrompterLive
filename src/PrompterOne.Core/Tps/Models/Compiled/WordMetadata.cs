namespace PrompterOne.Core.Models.CompiledScript;

public class WordMetadata
{
    public bool IsEmphasis { get; set; }
    public int EmphasisLevel { get; set; }
    public string? EmphasisStyle { get; set; }
    public bool IsPause { get; set; }
    public int? PauseDuration { get; set; }
    public string? Color { get; set; }
    public bool IsHighlight { get; set; }
    public bool IsBreath { get; set; }
    public bool IsEditPoint { get; set; }
    public string? EditPointPriority { get; set; }
    public string? EmotionHint { get; set; }
    public string? InlineEmotionHint { get; set; }
    public string? VolumeLevel { get; set; }
    public string? DeliveryMode { get; set; }
    public string? ArticulationStyle { get; set; }
    public int? EnergyLevel { get; set; }
    public int? MelodyLevel { get; set; }
    public string? PhoneticGuide { get; set; }
    public string? PronunciationGuide { get; set; }
    public string? StressText { get; set; }
    public string? StressGuide { get; set; }
    public int? SpeedOverride { get; set; }
    public float? SpeedMultiplier { get; set; }
    public string? Speaker { get; set; }
    public string? HeadCue { get; set; }
}
