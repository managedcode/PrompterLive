namespace PrompterLive.Core.Models.Tps;

public class TpsSegment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? TargetWPM { get; set; }
    public string? Emotion { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public string? AccentColor { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? LeadingContent { get; set; }
    public List<TpsBlock> Blocks { get; set; } = new();
}
