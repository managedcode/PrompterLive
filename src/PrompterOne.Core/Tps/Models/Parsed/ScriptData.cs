namespace PrompterOne.Core.Models.Documents;

public record ScriptData
{
    public string? ScriptId { get; init; }

    public string? Title { get; init; }

    public string? Content { get; init; }

    public bool IsPreview { get; init; }

    public int TargetWpm { get; init; } = 450;

    public ScriptSegment[]? Segments { get; init; }
}

public record ScriptSegment
{
    public required string Name { get; init; }

    public string Emotion { get; init; } = "neutral";
    public string? Speaker { get; init; }
    public string? Archetype { get; init; }
    public string? Timing { get; init; }

    public string? BackgroundColor { get; init; }

    public string? TextColor { get; init; }

    public string? AccentColor { get; init; }

    public int? WpmOverride { get; init; }

    public int? WpmMax { get; init; }

    public string? StartTime { get; init; }

    public string? EndTime { get; init; }

    public int StartIndex { get; init; }

    public int EndIndex { get; init; }

    public required string Content { get; init; }

    public ScriptBlock[]? Blocks { get; init; }
}

public record ScriptBlock
{
    public required string Name { get; init; }

    public string? Emotion { get; init; }
    public string? Speaker { get; init; }
    public string? Archetype { get; init; }

    public int? WpmOverride { get; init; }

    public int StartIndex { get; init; }

    public int EndIndex { get; init; }

    public required string Content { get; init; }

    public ScriptPhrase[]? Phrases { get; init; }
}

public record ScriptPhrase
{
    public required string Text { get; init; }

    public int StartIndex { get; init; }

    public int EndIndex { get; init; }

    public int? PauseDuration { get; init; }

    public ScriptWord[]? Words { get; init; }
}

public record ScriptWord
{
    public required string Text { get; init; }

    public int OrpIndex { get; init; }

    public int? WpmOverride { get; init; }
    public float? SpeedMultiplier { get; init; }

    public int EmphasisLevel { get; init; }

    public string? EmphasisStyle { get; init; }

    public string? Color { get; init; }
    public bool IsHighlight { get; init; }
    public bool IsBreath { get; init; }
    public string? Emotion { get; init; }
    public string? VolumeLevel { get; init; }
    public string? DeliveryMode { get; init; }
    public string? ArticulationStyle { get; init; }
    public int? EnergyLevel { get; init; }
    public int? MelodyLevel { get; init; }

    public int? PauseAfter { get; init; }

    public string? Pronunciation { get; init; }
    public string? StressText { get; init; }
    public string? StressGuide { get; init; }
    public string? Speaker { get; init; }

    public bool IsEditPoint { get; init; }

    public string? EditPointPriority { get; init; }
}
