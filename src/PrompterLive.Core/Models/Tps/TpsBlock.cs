namespace PrompterLive.Core.Models.Tps;

public class TpsBlock
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? TargetWPM { get; set; }
    public string? Emotion { get; set; }
    public List<TpsPhrase> Phrases { get; set; } = new();
}
