using System.Globalization;
using Microsoft.Extensions.Configuration;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;

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

    [Test]
    public void AppSettingsFactory_MapsAzureOpenAiEndpointAndModelsIntoProviderSettings()
    {
        const string DefaultEndpointName = "eastus-primary";
        const string AzureDeploymentId = "gpt-4o";
        const int AzureContextWindowTokens = 128000;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiProvider:Type"] = "AzureOpenAI",
                ["AiProvider:DefaultEndpoint"] = DefaultEndpointName,
                ["AiProvider:Endpoints:eastus-primary:Endpoint"] = AzureEndpoint,
                ["AiProvider:Endpoints:eastus-primary:ApiKey"] = AzureApiKey,
                ["AiProvider:Models:chat.gpt-4o:DeploymentId"] = AzureDeploymentId,
                ["AiProvider:Models:chat.gpt-4o:Type"] = "Chat",
                ["AiProvider:Models:chat.gpt-4o:Connection"] = DefaultEndpointName,
                ["AiProvider:Models:chat.gpt-4o:ContextWindowTokens"] = AzureContextWindowTokens.ToString(CultureInfo.InvariantCulture)
            })
            .Build();

        var settings = AiProviderAppSettingsFactory.Create(configuration);

        Assert.NotNull(settings);
        Assert.True(settings.AzureOpenAi.IsConfigured());
        Assert.Equal(AzureApiKey, settings.AzureOpenAi.ApiKey);
        Assert.Equal(AzureEndpoint, settings.AzureOpenAi.Endpoint);
        Assert.Equal(AzureDeploymentId, settings.AzureOpenAi.Deployment);
        Assert.Equal(AzureDeploymentId, settings.AzureOpenAi.Models.Single().Name);
        Assert.Equal(AzureContextWindowTokens, settings.AzureOpenAi.Models.Single().ContextSize);
        Assert.Equal(AiProviderModelTypes.Text, settings.AzureOpenAi.Models.Single().Type);
    }
}
