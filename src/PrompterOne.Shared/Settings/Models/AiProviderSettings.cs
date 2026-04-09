namespace PrompterOne.Shared.Settings.Models;

public sealed class AiProviderSettings
{
    public const string StorageKey = "prompterone.ai-providers";

    public AnthropicAiProviderSettings ClaudeApi { get; set; } = new();

    public AzureOpenAiProviderSettings AzureOpenAi { get; set; } = new();

    public LlamaSharpProviderSettings LlamaSharp { get; set; } = new();

    public OllamaAiProviderSettings Ollama { get; set; } = new();

    public OpenAiProviderSettings OpenAi { get; set; } = new();

    public static AiProviderSettings CreateDefault() => new();

    public AiProviderSettings Normalize()
    {
        ClaudeApi = (ClaudeApi ?? new AnthropicAiProviderSettings()).Normalize();
        AzureOpenAi = (AzureOpenAi ?? new AzureOpenAiProviderSettings()).Normalize();
        LlamaSharp = (LlamaSharp ?? new LlamaSharpProviderSettings()).Normalize();
        OpenAi = (OpenAi ?? new OpenAiProviderSettings()).Normalize();
        Ollama = (Ollama ?? new OllamaAiProviderSettings()).Normalize();
        return this;
    }

    public bool HasConfiguredProvider() =>
        ClaudeApi.IsConfigured() ||
        AzureOpenAi.IsConfigured() ||
        LlamaSharp.IsConfigured() ||
        OpenAi.IsConfigured() ||
        Ollama.IsConfigured();
}

public static class AiProviderClientTypes
{
    public const string Assistants = "assistants";
    public const string ChatCompletions = "chat-completions";
    public const string Responses = "responses";

    public static string Normalize(string? value) =>
        value switch
        {
            Assistants => Assistants,
            Responses => Responses,
            _ => ChatCompletions
        };
}

public sealed class AnthropicAiProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = "claude-sonnet-4-6";

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(Model);

    public AnthropicAiProviderSettings Normalize() => this;
}

public sealed class OpenAiProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string ClientType { get; set; } = AiProviderClientTypes.ChatCompletions;

    public string Model { get; set; } = "gpt-4o";

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(Model);

    public OpenAiProviderSettings Normalize()
    {
        ClientType = AiProviderClientTypes.Normalize(ClientType);
        return this;
    }
}

public sealed class AzureOpenAiProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = string.Empty;

    public string ClientType { get; set; } = AiProviderClientTypes.ChatCompletions;

    public string Deployment { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ApiVersion) &&
        !string.IsNullOrWhiteSpace(Deployment) &&
        !string.IsNullOrWhiteSpace(Endpoint);

    public AzureOpenAiProviderSettings Normalize()
    {
        ClientType = AiProviderClientTypes.Normalize(ClientType);
        return this;
    }
}

public sealed class OllamaAiProviderSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(Model);

    public OllamaAiProviderSettings Normalize() => this;
}

public sealed class LlamaSharpProviderSettings
{
    private const int DefaultContextSize = 4096;

    public int ContextSize { get; set; } = DefaultContextSize;

    public int GpuLayers { get; set; }

    public string ModelPath { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ModelPath);

    public LlamaSharpProviderSettings Normalize()
    {
        ContextSize = ContextSize > 0 ? ContextSize : DefaultContextSize;
        GpuLayers = Math.Max(0, GpuLayers);
        return this;
    }
}
