namespace PrompterOne.Shared.Settings.Models;

public sealed class AiProviderSettings
{
    public const string StorageKey = "prompterone.ai-providers";

    public AnthropicAiProviderSettings ClaudeApi { get; set; } = new();

    public OllamaAiProviderSettings Ollama { get; set; } = new();

    public OpenAiProviderSettings OpenAi { get; set; } = new();

    public static AiProviderSettings CreateDefault() => new();

    public AiProviderSettings Normalize()
    {
        ClaudeApi ??= new AnthropicAiProviderSettings();
        OpenAi ??= new OpenAiProviderSettings();
        Ollama ??= new OllamaAiProviderSettings();
        return this;
    }
}

public sealed class AnthropicAiProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = "claude-sonnet-4-6";
}

public sealed class OpenAiProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4o";
}

public sealed class OllamaAiProviderSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = string.Empty;
}
