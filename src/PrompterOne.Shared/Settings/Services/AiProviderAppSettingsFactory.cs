using System.Globalization;
using Microsoft.Extensions.Configuration;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Settings.Services;

internal static class AiProviderAppSettingsFactory
{
    private const string AiProviderSectionName = "AiProvider";
    private const string AlternateAiProviderSectionName = "AI";
    private const string ApiKeyKey = "ApiKey";
    private const string ApiVersionKey = "ApiVersion";
    private const string AzureOpenAiSectionName = "AzureOpenAi";
    private const string AzureOpenAiTypeName = "AzureOpenAI";
    private const string AzureOpenAiTypeNameWithSpace = "Azure OpenAI";
    private const string ContextWindowTokensKey = "ContextWindowTokens";
    private const string DefaultAzureApiVersion = "2025-04-01-preview";
    private const string DefaultEndpointKey = "DefaultEndpoint";
    private const string DeploymentIdKey = "DeploymentId";
    private const string EndpointKey = "Endpoint";
    private const string EndpointsSectionName = "Endpoints";
    private const string ModelsSectionName = "Models";
    private const string NameKey = "Name";
    private const string TypeKey = "Type";

    public static AiProviderSettings? Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = ResolveAiProviderSection(configuration);
        if (section is null || !IsAzureOpenAiSection(section))
        {
            return null;
        }

        var azureOpenAi = CreateAzureOpenAiSettings(section);
        return azureOpenAi.IsConfigured()
            ? new AiProviderSettings { AzureOpenAi = azureOpenAi }.Normalize()
            : null;
    }

    private static AzureOpenAiProviderSettings CreateAzureOpenAiSettings(IConfigurationSection section)
    {
        var endpointSection = ResolveEndpointSection(section);
        var models = CreateModels(section.GetSection(ModelsSectionName));
        var primaryModel = models.FirstOrDefault(static model => model.Type == AiProviderModelTypes.Text && model.IsConfigured())
            ?? models.FirstOrDefault(static model => model.IsConfigured());
        var deployment = section[DeploymentIdKey] ?? primaryModel?.Name ?? AiProviderModelCatalogDefaults.AzureOpenAiPrimaryDeployment;

        return new AzureOpenAiProviderSettings
        {
            ApiKey = endpointSection?[ApiKeyKey] ?? section[ApiKeyKey] ?? string.Empty,
            ApiVersion = section[ApiVersionKey] ?? DefaultAzureApiVersion,
            ClientType = AiProviderClientTypes.ChatCompletions,
            Deployment = deployment,
            Endpoint = endpointSection?[EndpointKey] ?? section[EndpointKey] ?? string.Empty,
            Models = models.Count > 0 ? models : AiProviderModelCatalogDefaults.CreateAzureOpenAi()
        }.Normalize();
    }

    private static List<AiProviderModelSettings> CreateModels(IConfigurationSection modelsSection)
    {
        var models = new List<AiProviderModelSettings>();
        foreach (var modelSection in modelsSection.GetChildren())
        {
            var name = modelSection[DeploymentIdKey] ?? modelSection[NameKey] ?? modelSection.Key;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            models.Add(AiProviderModelSettings.Create(
                name,
                NormalizeModelType(modelSection[TypeKey]),
                ReadPositiveInt(modelSection[ContextWindowTokensKey]) ?? AiProviderModelCatalogDefaults.CloudTextContextSize));
        }

        return models;
    }

    private static IConfigurationSection? ResolveAiProviderSection(IConfiguration configuration)
    {
        var section = configuration.GetSection(AiProviderSectionName);
        if (section.Exists())
        {
            return section;
        }

        section = configuration.GetSection(AlternateAiProviderSectionName);
        if (section.Exists())
        {
            return section;
        }

        section = configuration.GetSection(AzureOpenAiSectionName);
        return section.Exists() ? section : null;
    }

    private static IConfigurationSection? ResolveEndpointSection(IConfigurationSection section)
    {
        var endpoints = section.GetSection(EndpointsSectionName);
        if (!endpoints.Exists())
        {
            return null;
        }

        var defaultEndpoint = section[DefaultEndpointKey];
        if (!string.IsNullOrWhiteSpace(defaultEndpoint))
        {
            var defaultSection = endpoints.GetSection(defaultEndpoint);
            if (defaultSection.Exists())
            {
                return defaultSection;
            }
        }

        return endpoints.GetChildren().FirstOrDefault();
    }

    private static bool IsAzureOpenAiSection(IConfigurationSection section)
    {
        var providerType = section[TypeKey];
        if (string.IsNullOrWhiteSpace(providerType))
        {
            return string.Equals(section.Key, AzureOpenAiSectionName, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(providerType, AzureOpenAiTypeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(providerType, AzureOpenAiTypeNameWithSpace, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeModelType(string? type) =>
        type?.Trim().ToLowerInvariant() switch
        {
            "audio-to-text" or "audio_to_text" or "transcribe" or "transcription" => AiProviderModelTypes.AudioToText,
            "embedding" or "embeddings" => AiProviderModelTypes.Embeddings,
            "text-to-audio" or "text_to_audio" or "tts" => AiProviderModelTypes.TextToAudio,
            _ => AiProviderModelTypes.Text
        };

    private static int? ReadPositiveInt(string? value) =>
        int.TryParse(value, CultureInfo.InvariantCulture, out var result) && result > 0
            ? result
            : null;
}
