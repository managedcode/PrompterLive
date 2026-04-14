using System.Text.Json.Serialization;

namespace ManagedCode.Tps.Models;

public sealed record WordMetadata
{
    [JsonPropertyName("isEmphasis")]
    public bool IsEmphasis { get; init; }

    [JsonPropertyName("emphasisLevel")]
    public int EmphasisLevel { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("emphasisStyle")]
    public string? EmphasisStyle { get; init; }

    [JsonPropertyName("isPause")]
    public bool IsPause { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("pauseDurationMs")]
    public int? PauseDurationMs { get; init; }

    [JsonPropertyName("isHighlight")]
    public bool IsHighlight { get; init; }

    [JsonPropertyName("isBreath")]
    public bool IsBreath { get; init; }

    [JsonPropertyName("isEditPoint")]
    public bool IsEditPoint { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("editPointPriority")]
    public string? EditPointPriority { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("emotionHint")]
    public string? EmotionHint { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("inlineEmotionHint")]
    public string? InlineEmotionHint { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("volumeLevel")]
    public string? VolumeLevel { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("deliveryMode")]
    public string? DeliveryMode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("articulationStyle")]
    public string? ArticulationStyle { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("energyLevel")]
    public int? EnergyLevel { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("melodyLevel")]
    public int? MelodyLevel { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("phoneticGuide")]
    public string? PhoneticGuide { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("pronunciationGuide")]
    public string? PronunciationGuide { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("stressText")]
    public string? StressText { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("stressGuide")]
    public string? StressGuide { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("speedOverride")]
    public int? SpeedOverride { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("speedMultiplier")]
    public double? SpeedMultiplier { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("speaker")]
    public string? Speaker { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("headCue")]
    public string? HeadCue { get; init; }
}

public sealed class CompiledWord
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("index")]
    public required int Index { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonInclude]
    [JsonPropertyName("cleanText")]
    public string CleanText { get; internal set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName("characterCount")]
    public int CharacterCount { get; internal set; }

    [JsonInclude]
    [JsonPropertyName("orpPosition")]
    public int OrpPosition { get; internal set; }

    [JsonPropertyName("displayDurationMs")]
    public required int DisplayDurationMs { get; init; }

    [JsonPropertyName("startMs")]
    public required int StartMs { get; init; }

    [JsonPropertyName("endMs")]
    public required int EndMs { get; init; }

    [JsonPropertyName("metadata")]
    public required WordMetadata Metadata { get; init; }

    [JsonPropertyName("segmentId")]
    public required string SegmentId { get; init; }

    [JsonPropertyName("blockId")]
    public required string BlockId { get; init; }

    [JsonInclude]
    [JsonPropertyName("phraseId")]
    public string PhraseId { get; internal set; } = string.Empty;
}

public sealed class CompiledPhrase
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("startWordIndex")]
    public required int StartWordIndex { get; init; }

    [JsonPropertyName("endWordIndex")]
    public required int EndWordIndex { get; init; }

    [JsonPropertyName("startMs")]
    public required int StartMs { get; init; }

    [JsonPropertyName("endMs")]
    public required int EndMs { get; init; }

    [JsonPropertyName("words")]
    public required IReadOnlyList<CompiledWord> Words { get; init; }
}
