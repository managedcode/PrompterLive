namespace PrompterLive.Core.Models.Tps;

public class TpsPhrase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsEmphasis { get; set; }
    public bool IsSlow { get; set; }
    public int? PauseDuration { get; set; }
    public int? CustomWpm { get; set; }
    public List<TpsWord> Words { get; set; } = new();
}
