namespace PrompterLive.Core.Models.CompiledScript;

public class CompiledPhrase
{
    public string Id { get; set; } = string.Empty;
    public List<CompiledWord> Words { get; set; } = new();
}
