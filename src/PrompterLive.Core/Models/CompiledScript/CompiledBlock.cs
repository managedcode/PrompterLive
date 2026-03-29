namespace PrompterLive.Core.Models.CompiledScript;

public class CompiledBlock
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TargetWPM { get; set; }
    public string? Emotion { get; set; }
    public List<CompiledPhrase> Phrases { get; set; } = new();
    public List<CompiledWord> Words { get; set; } = new();
}
