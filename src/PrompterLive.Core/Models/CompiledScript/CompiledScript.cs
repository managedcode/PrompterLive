namespace PrompterLive.Core.Models.CompiledScript;

public class CompiledScript
{
    public List<CompiledSegment> Segments { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
