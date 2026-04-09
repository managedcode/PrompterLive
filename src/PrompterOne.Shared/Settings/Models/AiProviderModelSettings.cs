namespace PrompterOne.Shared.Settings.Models;

public static class AiProviderModelTypes
{
    public const string AudioToText = "audio-to-text";
    public const string Embeddings = "embeddings";
    public const string Text = "text";
    public const string TextToAudio = "text-to-audio";

    public static string Normalize(string? value) =>
        value switch
        {
            AudioToText => AudioToText,
            Embeddings => Embeddings,
            TextToAudio => TextToAudio,
            _ => Text
        };
}

public sealed class AiProviderModelSettings
{
    public const int DefaultContextSize = 8192;

    public int ContextSize { get; set; } = DefaultContextSize;

    public string ModelPath { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = AiProviderModelTypes.Text;

    public bool HasAnyValue() =>
        !string.IsNullOrWhiteSpace(Name) ||
        !string.IsNullOrWhiteSpace(ModelPath);

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Name);

    public bool IsConfiguredWithLocalPath() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(ModelPath);

    public AiProviderModelSettings Normalize()
    {
        Name = Name.Trim();
        ModelPath = ModelPath.Trim();
        Type = AiProviderModelTypes.Normalize(Type);
        ContextSize = ContextSize > 0 ? ContextSize : DefaultContextSize;

        if (string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(ModelPath))
        {
            Name = Path.GetFileNameWithoutExtension(ModelPath);
        }

        return this;
    }

    public static AiProviderModelSettings Create(
        string name,
        string type,
        int contextSize,
        string modelPath = "") =>
        new AiProviderModelSettings
        {
            ContextSize = contextSize,
            ModelPath = modelPath,
            Name = name,
            Type = type
        }.Normalize();
}

public static class AiProviderModelCatalogDefaults
{
    public const int AnthropicContextSize = 200000;
    public const int CloudAudioContextSize = 32000;
    public const int CloudEmbeddingsContextSize = 8192;
    public const int CloudTextContextSize = 128000;
    public const string AnthropicPrimaryModel = "claude-sonnet-4-6";
    public const string AzureOpenAiPrimaryDeployment = "gpt-4o-mini";
    public const string LlamaSharpPrimaryModel = "llama-3.2-3b-instruct";
    public const string OllamaPrimaryModel = "llama3.2:3b";
    public const string OpenAiPrimaryModel = "gpt-4o-mini";

    public static List<AiProviderModelSettings> CreateAnthropic() =>
    [
        AiProviderModelSettings.Create(AnthropicPrimaryModel, AiProviderModelTypes.Text, AnthropicContextSize)
    ];

    public static List<AiProviderModelSettings> CreateAzureOpenAi() =>
    [
        AiProviderModelSettings.Create(AzureOpenAiPrimaryDeployment, AiProviderModelTypes.Text, CloudTextContextSize),
        AiProviderModelSettings.Create("text-embedding-3-small", AiProviderModelTypes.Embeddings, CloudEmbeddingsContextSize),
        AiProviderModelSettings.Create("gpt-4o-mini-transcribe", AiProviderModelTypes.AudioToText, CloudAudioContextSize),
        AiProviderModelSettings.Create("gpt-4o-mini-tts", AiProviderModelTypes.TextToAudio, CloudAudioContextSize)
    ];

    public static List<AiProviderModelSettings> CreateEmptyLocal() =>
    [
        AiProviderModelSettings.Create(string.Empty, AiProviderModelTypes.Text, AiProviderModelSettings.DefaultContextSize)
    ];

    public static List<AiProviderModelSettings> CreateEmptyRemote() =>
    [
        AiProviderModelSettings.Create(string.Empty, AiProviderModelTypes.Text, CloudTextContextSize)
    ];

    public static List<AiProviderModelSettings> CreateLlamaSharp() =>
    [
        AiProviderModelSettings.Create(LlamaSharpPrimaryModel, AiProviderModelTypes.Text, AiProviderModelSettings.DefaultContextSize),
        AiProviderModelSettings.Create("bge-small-en-v1.5", AiProviderModelTypes.Embeddings, AiProviderModelSettings.DefaultContextSize)
    ];

    public static List<AiProviderModelSettings> CreateOllama() =>
    [
        AiProviderModelSettings.Create(OllamaPrimaryModel, AiProviderModelTypes.Text, AiProviderModelSettings.DefaultContextSize),
        AiProviderModelSettings.Create("nomic-embed-text", AiProviderModelTypes.Embeddings, AiProviderModelSettings.DefaultContextSize)
    ];

    public static List<AiProviderModelSettings> CreateOpenAi() =>
    [
        AiProviderModelSettings.Create(OpenAiPrimaryModel, AiProviderModelTypes.Text, CloudTextContextSize),
        AiProviderModelSettings.Create("text-embedding-3-small", AiProviderModelTypes.Embeddings, CloudEmbeddingsContextSize),
        AiProviderModelSettings.Create("gpt-4o-mini-transcribe", AiProviderModelTypes.AudioToText, CloudAudioContextSize),
        AiProviderModelSettings.Create("gpt-4o-mini-tts", AiProviderModelTypes.TextToAudio, CloudAudioContextSize)
    ];
}
