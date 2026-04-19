using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsAiSection : ComponentBase
{
    private const string ActiveCssClass = "active";
    private const string ApiKeyFieldId = "api-key";
    private const string AzureOpenAiCardId = "ai-azure-openai";
    private const string ClaudeApiCardId = "ai-claude-api";
    private const string DisconnectedStatusClass = "set-dest-idle";
    private const string EndpointFieldId = "endpoint";
    private const string LlamaSharpCardId = "ai-llamasharp";
    private const string LocalStatusClass = "set-dest-local";
    private const string OllamaCardId = "ai-ollama";
    private const string OpenAiCardId = "ai-openai";
    private const string SubtitleSeparator = " · ";
    private const string UrlFieldId = "url";

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
    private string BuildStatusClass(string providerId) =>
        _settings.IsActiveProvider(providerId) ? LocalStatusClass : DisconnectedStatusClass;

    private string BuildStatusLabel(string providerId)
    {
        if (_settings.IsActiveProvider(providerId))
        {
            return Text(UiTextKey.SettingsAiActiveProvider);
        }

        return _settings.IsProviderConfigured(providerId)
            ? Text(UiTextKey.CommonSavedLocally)
            : Text(UiTextKey.CommonNotConfigured);
    }

    private string BuildClaudeSubtitle(AnthropicAiProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiClaudeTitle), BuildModelCatalogLabel(settings.Models))
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiClaudeTitle), Text(UiTextKey.CommonApiKey), Text(UiTextKey.CommonModels));

    private string BuildOpenAiSubtitle(OpenAiProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiOpenAiTitle), BuildModelCatalogLabel(settings.Models))
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiOpenAiTitle), Text(UiTextKey.CommonApiKey), Text(UiTextKey.CommonModels));

    private string BuildAzureOpenAiSubtitle(AzureOpenAiProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiAzureOpenAiTitle), BuildAuthorityLabel(settings.Endpoint), BuildModelCatalogLabel(settings.Models))
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiAzureOpenAiTitle), Text(UiTextKey.CommonEndpoint), Text(UiTextKey.CommonDeployment));

    private string BuildOllamaSubtitle(OllamaAiProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiSelfHosted), BuildAuthorityLabel(settings.Endpoint), BuildModelCatalogLabel(settings.Models))
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiOllamaTitle), Text(UiTextKey.CommonEndpoint), Text(UiTextKey.CommonModels));

    private string BuildLlamaSharpSubtitle(LlamaSharpProviderSettings settings) =>
        settings.IsConfigured()
            ? JoinSubtitleParts(Text(UiTextKey.SettingsAiLlamaSharpTitle), BuildLocalModelCatalogLabel(settings.Models))
            : JoinSubtitleParts(Text(UiTextKey.SettingsAiLlamaSharpTitle), Text(UiTextKey.CommonModelPath));

    private static string BuildAuthorityLabel(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) &&
            !string.IsNullOrWhiteSpace(endpointUri.Authority))
        {
            return endpointUri.Authority;
        }

        return endpoint;
    }

    private static string BuildLocalModelCatalogLabel(IEnumerable<AiProviderModelSettings> models)
    {
        var localModels = models.Where(static model => model.IsConfiguredWithLocalPath()).ToList();
        if (localModels.Count == 0)
        {
            return string.Empty;
        }

        var firstLabel = Path.GetFileName(localModels[0].ModelPath);
        return localModels.Count == 1 ? firstLabel : string.Concat(firstLabel, " +", localModels.Count - 1);
    }

    private static string BuildModelCatalogLabel(IEnumerable<AiProviderModelSettings> models)
    {
        var namedModels = models.Where(static model => model.IsConfigured()).ToList();
        if (namedModels.Count == 0)
        {
            return string.Empty;
        }

        return namedModels.Count == 1 ? namedModels[0].Name : string.Concat(namedModels[0].Name, " +", namedModels.Count - 1);
    }

    private string GetMessage(string providerId) => _messages.GetValueOrDefault(providerId) ?? string.Empty;

    private Task HandleModelCatalogChangedAsync() => InvokeAsync(StateHasChanged);

    private static string JoinSubtitleParts(params string[] parts) =>
        string.Join(SubtitleSeparator, parts.Where(static part => !string.IsNullOrWhiteSpace(part)).Select(static part => part.Trim()));

    private async Task SaveProviderAsync(string providerId)
    {
        _settings.ActiveProviderId = providerId;
        await SettingsStore.SaveAsync(_settings);
        _messages[providerId] = Text(UiTextKey.SettingsAiSavedLocallyDetail);
    }

    private async Task ClearProviderAsync(string providerId, Action reset)
    {
        reset();
        if (_settings.IsActiveProvider(providerId))
        {
            _settings.ActiveProviderId = string.Empty;
        }

        _messages[providerId] = Text(UiTextKey.SettingsAiProviderCleared);
        await SettingsStore.SaveAsync(_settings);
    }
}
