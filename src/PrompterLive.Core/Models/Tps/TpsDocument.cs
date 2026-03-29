namespace PrompterLive.Core.Models.Tps;

public class TpsDocument
{
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<TpsSegment> Segments { get; set; } = new();
}
