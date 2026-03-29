namespace PrompterLive.Core.Models.Tps;

public class TpsWord
{
    public string Text { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsEmphasis { get; set; }
    public bool IsSlow { get; set; }
}
