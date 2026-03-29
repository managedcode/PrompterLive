namespace PrompterLive.Core.Models.CompiledScript;

public class CompiledSegment
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? TargetWPM { get; set; }
    public string? Emotion { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public string? AccentColor { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<CompiledBlock> Blocks { get; set; } = new();
    public List<CompiledWord> Words { get; set; } = new();
}
