using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Settings.Components;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsAiSection : ComponentBase
{
    private const string ActiveCssClass = "active";
    private const string ApiKeyFieldId = "api-key";
    private const string ApiVersionFieldId = "api-version";
    private const string AzureOpenAiCardId = "ai-azure-openai";
    private const string ClientTypeFieldId = "client-type";
    private const string ClaudeApiCardId = "ai-claude-api";
    private const string ContextSizeFieldId = "context-size";
    private const string DeploymentFieldId = "deployment";
    private const string DisconnectedStatusClass = "set-dest-idle";
    private const string EndpointFieldId = "endpoint";
    private const string GpuLayersFieldId = "gpu-layers";
    private const string LlamaSharpCardId = "ai-llamasharp";
    private const string LocalStatusClass = "set-dest-local";
    private const string ModelFieldId = "model";
    private const string ModelPathFieldId = "model-path";
    private const string OllamaCardId = "ai-ollama";
    private const string OpenAiCardId = "ai-openai";
    private const string SubtitleSeparator = " · ";
    private const string UrlFieldId = "url";

    private static readonly IReadOnlyList<SettingsSelectOption> ClientTypeOptions =
    [
        new(AiProviderClientTypes.ChatCompletions, "Chat Completion"),
        new(AiProviderClientTypes.Responses, "Responses"),
        new(AiProviderClientTypes.Assistants, "Assistants"),
    ];

    private readonly Dictionary<string, string> _messages = new(StringComparer.Ordinal);
    private AiProviderSettings _settings = AiProviderSettings.CreateDefault();

    [Inject] private AiProviderSettingsStore SettingsStore { get; set; } = null!;

    [Parameter] public string DisplayStyle { get; set; } = string.Empty;
    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;
    [Parameter] public EventCallback<string> ToggleCard { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _settings = await SettingsStore.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private static string BuildCardCssClass(bool isConfigured) => isConfigured ? ActiveCssClass : string.Empty;
    private static string BuildStatusClass(bool isConfigured) => isConfigured ? LocalStatusClass : DisconnectedStatusClass;
    private string BuildStatusLabel(bool isConfigured) => isConfigured ? Text(UiTextKey.CommonSavedLocally) : Text(UiTextKey.CommonNotConfigured);

    private string BuildClaudeSubtitle(AnthropicAiProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiClaudeTitle), settings.Model)
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiClaudeTitle), Text(UiTextKey.CommonApiKey), Text(UiTextKey.CommonModel));

    private string BuildOpenAiSubtitle(OpenAiProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiOpenAiTitle), BuildClientTypeLabel(settings.ClientType), settings.Model)
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiOpenAiTitle), Text(UiTextKey.CommonClientType), Text(UiTextKey.CommonApiKey), Text(UiTextKey.CommonModel));

    private string BuildAzureOpenAiSubtitle(AzureOpenAiProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiAzureOpenAiTitle), BuildClientTypeLabel(settings.ClientType), BuildAuthorityLabel(settings.Endpoint), settings.Deployment)
            : JoinSubtitleParts(
                Text(UiTextKey.SettingsAiAzureOpenAiTitle),
                Text(UiTextKey.CommonClientType),
                Text(UiTextKey.CommonEndpoint),
                Text(UiTextKey.CommonDeployment),
                Text(UiTextKey.CommonApiVersion),
                Text(UiTextKey.CommonApiKey));

    private string BuildOllamaSubtitle(OllamaAiProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiSelfHosted), BuildAuthorityLabel(settings.Endpoint), settings.Model)
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiOllamaTitle), Text(UiTextKey.CommonEndpoint), Text(UiTextKey.CommonModel));

    private string BuildLlamaSharpSubtitle(LlamaSharpProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiLlamaSharpTitle), BuildModelPathLabel(settings.ModelPath), BuildContextLabel(settings.ContextSize))
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiLlamaSharpTitle), Text(UiTextKey.CommonModelPath), Text(UiTextKey.CommonContextSize), Text(UiTextKey.CommonGpuLayers));

    private string GetMessage(string providerId) => _messages.GetValueOrDefault(providerId) ?? string.Empty;

    private static string BuildAuthorityLabel(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) &&
            !string.IsNullOrWhiteSpace(endpointUri.Authority))
        {
            return endpointUri.Authority;
        }

        return endpoint;
    }

    private static string BuildModelPathLabel(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            return string.Empty;
        }

        return Path.GetFileName(modelPath.Trim());
    }

    private string BuildContextLabel(int contextSize) => string.Concat(Text(UiTextKey.CommonContextSize), " ", contextSize);

    private static string JoinSubtitleParts(params string[] parts) =>
        string.Join(SubtitleSeparator, parts.Where(static part => !string.IsNullOrWhiteSpace(part)).Select(static part => part.Trim()));

    private static string BuildClientTypeLabel(string clientType) =>
        ClientTypeOptions.FirstOrDefault(option => string.Equals(option.Value, clientType, StringComparison.Ordinal))?.Label
        ?? ClientTypeOptions[0].Label;

    private Task OnOpenAiClientTypeChanged(ChangeEventArgs args)
    {
        _settings.OpenAi.ClientType = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnAzureOpenAiClientTypeChanged(ChangeEventArgs args)
    {
        _settings.AzureOpenAi.ClientType = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task SaveProviderAsync(string providerId)
    {
        await SettingsStore.SaveAsync(_settings);
        _messages[providerId] = Text(UiTextKey.SettingsAiSavedLocallyDetail);
    }

    private async Task ClearProviderAsync(string providerId, Action reset)
    {
        reset();
        _messages[providerId] = Text(UiTextKey.SettingsAiProviderCleared);
        await SettingsStore.SaveAsync(_settings);
    }
}
