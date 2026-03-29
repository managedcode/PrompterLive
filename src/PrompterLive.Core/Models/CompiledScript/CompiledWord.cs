namespace PrompterLive.Core.Models.CompiledScript;

public class CompiledWord
{
    public string CleanText { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
    public int ORPPosition { get; set; }
    public TimeSpan DisplayDuration { get; set; }
    public WordMetadata Metadata { get; set; } = new();
}
