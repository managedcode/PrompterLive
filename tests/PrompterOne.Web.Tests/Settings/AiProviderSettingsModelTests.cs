using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Web.Tests;

public sealed class AiProviderSettingsModelTests
{
    private const string AnthropicApiKey = "sk-ant-test";
    private const string AnthropicLegacyModel = "claude-opus-4-6";
    private const string AzureApiKey = "azure-key";
    private const string AzureApiVersion = "2025-04-01-preview";
    private const int AzureCustomContextSize = 65536;
    private const string AzureDeployment = "gpt-4.1-mini";
    private const string AzureEndpoint = "https://example.openai.azure.com";
    private const int LlamaContextSize = 16384;
    private const string LlamaLegacyModelPath = "/models/llama-3.2-8b-instruct.gguf";
    private const string OllamaEndpoint = "http://localhost:11434";
    private const string OllamaLegacyModel = "llama3.2:8b";
    private const string OpenAiApiKey = "sk-live-openai";
    private const string OpenAiLegacyModel = "gpt-4.1-mini";

    [Test]
    public void Normalize_MigratesLegacySingleModelFields_IntoProviderModelCatalogs()
    {
        var settings = new AiProviderSettings
        {
            ClaudeApi = new AnthropicAiProviderSettings
            {
                ApiKey = AnthropicApiKey,
                Model = AnthropicLegacyModel,
                Models = []
            },
            OpenAi = new OpenAiProviderSettings
            {
                ApiKey = OpenAiApiKey,
                Model = OpenAiLegacyModel,
                Models = []
            },
            AzureOpenAi = new AzureOpenAiProviderSettings
            {
                ApiKey = AzureApiKey,
                ApiVersion = AzureApiVersion,
                Deployment = AzureDeployment,
                Endpoint = AzureEndpoint,
                Models = []
            },
            Ollama = new OllamaAiProviderSettings
            {
                Endpoint = OllamaEndpoint,
                Model = OllamaLegacyModel,
                Models = []
            },
            LlamaSharp = new LlamaSharpProviderSettings
            {
                ContextSize = LlamaContextSize,
                ModelPath = LlamaLegacyModelPath,
                Models = []
            }
        }.Normalize();

        Assert.Contains(settings.ClaudeApi.Models, model => model.Name == AnthropicLegacyModel);
        Assert.Contains(settings.OpenAi.Models, model => model.Name == OpenAiLegacyModel);
        Assert.Contains(
            settings.AzureOpenAi.Models,
            model => model.Name == AzureDeployment && model.ContextSize == AiProviderModelCatalogDefaults.CloudTextContextSize);
        Assert.Contains(settings.Ollama.Models, model => model.Name == OllamaLegacyModel);
        Assert.Contains(
            settings.LlamaSharp.Models,
            model =>
                model.ModelPath == LlamaLegacyModelPath &&
                model.Name == "llama-3.2-8b-instruct" &&
                model.ContextSize == LlamaContextSize);
        Assert.Equal(LlamaLegacyModelPath, settings.LlamaSharp.ModelPath);
        Assert.Equal(LlamaContextSize, settings.LlamaSharp.ContextSize);
        Assert.True(settings.HasConfiguredProvider());
    }

    [Test]
    public void CreateDefault_Normalize_SeedsStarterModelsWithCapabilityMetadata()
    {
        var settings = AiProviderSettings.CreateDefault().Normalize();

        Assert.Contains(settings.ClaudeApi.Models, model => model.Type == AiProviderModelTypes.Text);
        Assert.Contains(settings.OpenAi.Models, model => model.Type == AiProviderModelTypes.Text);
        Assert.Contains(settings.OpenAi.Models, model => model.Type == AiProviderModelTypes.Embeddings);
        Assert.Contains(settings.OpenAi.Models, model => model.Type == AiProviderModelTypes.AudioToText);
        Assert.Contains(settings.OpenAi.Models, model => model.Type == AiProviderModelTypes.TextToAudio);
        Assert.Contains(settings.AzureOpenAi.Models, model => model.Name == AiProviderModelCatalogDefaults.AzureOpenAiPrimaryDeployment);
        Assert.Contains(settings.Ollama.Models, model => model.Name == AiProviderModelCatalogDefaults.OllamaPrimaryModel);
        Assert.Contains(settings.LlamaSharp.Models, model => model.Type == AiProviderModelTypes.Text);
        Assert.Contains(settings.LlamaSharp.Models, model => model.Type == AiProviderModelTypes.Embeddings);
        Assert.False(settings.Ollama.IsConfigured());
        Assert.False(settings.HasConfiguredProvider());
        Assert.All(
            settings.OpenAi.Models.Concat(settings.AzureOpenAi.Models).Concat(settings.Ollama.Models).Concat(settings.LlamaSharp.Models),
            model => Assert.True(model.ContextSize > 0));
    }

    [Test]
    public void Normalize_PreservesExplicitModelMetadata_WhenCatalogEntriesAlreadyExist()
    {
        const string CustomModelName = "custom-voice-model";
        const int CustomContextSize = AzureCustomContextSize;

        var settings = new AiProviderSettings
        {
            OpenAi = new OpenAiProviderSettings
            {
                ApiKey = OpenAiApiKey,
                Model = string.Empty,
                Models =
                [
                    AiProviderModelSettings.Create(CustomModelName, AiProviderModelTypes.TextToAudio, CustomContextSize)
                ]
            }
        }.Normalize();

        var model = Assert.Single(settings.OpenAi.Models);
        Assert.Equal(CustomModelName, model.Name);
        Assert.Equal(AiProviderModelTypes.TextToAudio, model.Type);
        Assert.Equal(CustomContextSize, model.ContextSize);
    }
}
