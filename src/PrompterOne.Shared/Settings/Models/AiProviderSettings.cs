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

    public string Model { get; set; } = AiProviderModelCatalogDefaults.AnthropicPrimaryModel;

    public List<AiProviderModelSettings> Models { get; set; } = AiProviderModelCatalogDefaults.CreateAnthropic();

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        Models.Any(static model => model.IsConfigured());

    public AnthropicAiProviderSettings Normalize()
    {
        ApiKey = ApiKey.Trim();
        BaseUrl = BaseUrl.Trim();
        Models = AiProviderSettingsModelCatalog.Normalize(
            Models,
            AiProviderModelCatalogDefaults.CreateAnthropic(),
            AiProviderModelSettings.Create(Model, AiProviderModelTypes.Text, AiProviderModelCatalogDefaults.AnthropicContextSize));
        Model = AiProviderSettingsModelCatalog.GetPrimaryModelName(Models, AiProviderModelCatalogDefaults.AnthropicPrimaryModel);
        return this;
    }
}

public sealed class OpenAiProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string ClientType { get; set; } = AiProviderClientTypes.ChatCompletions;

    public string Model { get; set; } = AiProviderModelCatalogDefaults.OpenAiPrimaryModel;

    public List<AiProviderModelSettings> Models { get; set; } = AiProviderModelCatalogDefaults.CreateOpenAi();

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        Models.Any(static model => model.IsConfigured());

    public OpenAiProviderSettings Normalize()
    {
        ApiKey = ApiKey.Trim();
        BaseUrl = BaseUrl.Trim();
        ClientType = AiProviderClientTypes.Normalize(ClientType);
        Models = AiProviderSettingsModelCatalog.Normalize(
            Models,
            AiProviderModelCatalogDefaults.CreateOpenAi(),
            AiProviderModelSettings.Create(Model, AiProviderModelTypes.Text, AiProviderModelCatalogDefaults.CloudTextContextSize));
        Model = AiProviderSettingsModelCatalog.GetPrimaryModelName(Models, AiProviderModelCatalogDefaults.OpenAiPrimaryModel);
        return this;
    }
}

public sealed class AzureOpenAiProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = string.Empty;

    public string ClientType { get; set; } = AiProviderClientTypes.ChatCompletions;

    public string Deployment { get; set; } = AiProviderModelCatalogDefaults.AzureOpenAiPrimaryDeployment;

    public string Endpoint { get; set; } = string.Empty;

    public List<AiProviderModelSettings> Models { get; set; } = AiProviderModelCatalogDefaults.CreateAzureOpenAi();

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ApiVersion) &&
        !string.IsNullOrWhiteSpace(Endpoint) &&
        Models.Any(static model => model.IsConfigured());

    public AzureOpenAiProviderSettings Normalize()
    {
        ApiKey = ApiKey.Trim();
        ApiVersion = ApiVersion.Trim();
        ClientType = AiProviderClientTypes.Normalize(ClientType);
        Deployment = Deployment.Trim();
        Endpoint = Endpoint.Trim();
        Models = AiProviderSettingsModelCatalog.Normalize(
            Models,
            AiProviderModelCatalogDefaults.CreateAzureOpenAi(),
            AiProviderModelSettings.Create(Deployment, AiProviderModelTypes.Text, AiProviderModelCatalogDefaults.CloudTextContextSize));
        Deployment = AiProviderSettingsModelCatalog.GetPrimaryModelName(Models, AiProviderModelCatalogDefaults.AzureOpenAiPrimaryDeployment);
        return this;
    }
}

public sealed class OllamaAiProviderSettings
{
    public string Endpoint { get; set; } = string.Empty;

    public string Model { get; set; } = AiProviderModelCatalogDefaults.OllamaPrimaryModel;

    public List<AiProviderModelSettings> Models { get; set; } = AiProviderModelCatalogDefaults.CreateOllama();

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        Models.Any(static model => model.IsConfigured());

    public OllamaAiProviderSettings Normalize()
    {
        Endpoint = Endpoint.Trim();
        Models = AiProviderSettingsModelCatalog.Normalize(
            Models,
            AiProviderModelCatalogDefaults.CreateOllama(),
            AiProviderModelSettings.Create(Model, AiProviderModelTypes.Text, AiProviderModelSettings.DefaultContextSize));
        Model = AiProviderSettingsModelCatalog.GetPrimaryModelName(Models, AiProviderModelCatalogDefaults.OllamaPrimaryModel);
        return this;
    }
}

public sealed class LlamaSharpProviderSettings
{
    public int ContextSize { get; set; } = AiProviderModelSettings.DefaultContextSize;

    public int GpuLayers { get; set; }

    public string ModelPath { get; set; } = string.Empty;

    public List<AiProviderModelSettings> Models { get; set; } = AiProviderModelCatalogDefaults.CreateLlamaSharp();

    public bool IsConfigured() =>
        Models.Any(static model => model.IsConfiguredWithLocalPath());

    public LlamaSharpProviderSettings Normalize()
    {
        GpuLayers = Math.Max(0, GpuLayers);
        Models = AiProviderSettingsModelCatalog.Normalize(
            Models,
            AiProviderModelCatalogDefaults.CreateLlamaSharp(),
            AiProviderModelSettings.Create(
                string.Empty,
                AiProviderModelTypes.Text,
                ContextSize,
                ModelPath));
        ContextSize = AiProviderSettingsModelCatalog.GetPrimaryContextSize(Models, AiProviderModelSettings.DefaultContextSize);
        ModelPath = AiProviderSettingsModelCatalog.GetPrimaryModelPath(Models);
        return this;
    }
}

internal static class AiProviderSettingsModelCatalog
{
    public static List<AiProviderModelSettings> Normalize(
        List<AiProviderModelSettings>? models,
        List<AiProviderModelSettings> defaultModels,
        params AiProviderModelSettings[] legacyModels)
    {
        var normalizedModels = (models ?? [])
            .Select(static model => (model ?? new AiProviderModelSettings()).Normalize())
            .Where(static model => model.HasAnyValue())
            .ToList();

        foreach (var legacyModel in legacyModels)
        {
            var normalizedLegacyModel = (legacyModel ?? new AiProviderModelSettings()).Normalize();
            if (!normalizedLegacyModel.HasAnyValue() || ContainsModel(normalizedModels, normalizedLegacyModel))
            {
                continue;
            }

            normalizedModels.Add(normalizedLegacyModel);
        }

        return normalizedModels.Count > 0
            ? normalizedModels
            : defaultModels.Select(static model => model.Normalize()).ToList();
    }

    public static int GetPrimaryContextSize(IReadOnlyList<AiProviderModelSettings> models, int fallback) =>
        models.FirstOrDefault(static model => model.IsConfiguredWithLocalPath())?.ContextSize
        ?? models.FirstOrDefault(static model => model.IsConfigured())?.ContextSize
        ?? fallback;

    public static string GetPrimaryModelName(IReadOnlyList<AiProviderModelSettings> models, string fallback) =>
        models.FirstOrDefault(static model => model.IsConfigured())?.Name
        ?? fallback;

    public static string GetPrimaryModelPath(IReadOnlyList<AiProviderModelSettings> models) =>
        models.FirstOrDefault(static model => model.IsConfiguredWithLocalPath())?.ModelPath
        ?? string.Empty;

    private static bool ContainsModel(IReadOnlyCollection<AiProviderModelSettings> models, AiProviderModelSettings candidate) =>
        models.Any(
            model =>
                string.Equals(model.Name, candidate.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(model.ModelPath, candidate.ModelPath, StringComparison.OrdinalIgnoreCase));
}
