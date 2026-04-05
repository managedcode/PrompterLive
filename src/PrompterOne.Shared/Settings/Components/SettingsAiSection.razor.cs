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
    private const string AnthropicLabel = "Anthropic";
    private const string ClaudeApiCardId = "ai-claude-api";
    private const string DisconnectedStatusClass = "set-dest-idle";
    private const string LocalhostAuthority = "localhost:11434";
    private const string LocalStatusClass = "set-dest-local";
    private const string OpenAiLabel = "OpenAI";
    private const string OllamaCardId = "ai-ollama";
    private const string OpenAiCardId = "ai-openai";
    private const string SubtitleSeparator = " · ";

    private static readonly IReadOnlyList<SettingsSelectOption> ClaudeModelOptions =
    [
        new("claude-sonnet-4-6", "claude-sonnet-4-6"),
        new("claude-opus-4-6", "claude-opus-4-6"),
        new("claude-haiku-4-5", "claude-haiku-4-5"),
    ];

    private static readonly IReadOnlyList<SettingsSelectOption> OpenAiModelOptions =
    [
        new("gpt-4o", "gpt-4o"),
        new("gpt-4o-mini", "gpt-4o-mini"),
        new("o1", "o1"),
        new("o3-mini", "o3-mini"),
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

    private static string BuildCardCssClass(AnthropicAiProviderSettings settings) =>
        settings.IsConfigured() ? ActiveCssClass : string.Empty;

    private static string BuildCardCssClass(OpenAiProviderSettings settings) =>
        settings.IsConfigured() ? ActiveCssClass : string.Empty;

    private static string BuildCardCssClass(OllamaAiProviderSettings settings) =>
        settings.IsConfigured() ? ActiveCssClass : string.Empty;

    private static string BuildStatusClass(AnthropicAiProviderSettings settings) =>
        settings.IsConfigured() ? LocalStatusClass : DisconnectedStatusClass;

    private static string BuildStatusClass(OpenAiProviderSettings settings) =>
        settings.IsConfigured() ? LocalStatusClass : DisconnectedStatusClass;

    private static string BuildStatusClass(OllamaAiProviderSettings settings) =>
        settings.IsConfigured() ? LocalStatusClass : DisconnectedStatusClass;

    private string BuildStatusLabel(AnthropicAiProviderSettings settings) =>
        settings.IsConfigured() ? Text(UiTextKey.CommonSavedLocally) : Text(UiTextKey.CommonNotConfigured);

    private string BuildStatusLabel(OpenAiProviderSettings settings) =>
        settings.IsConfigured() ? Text(UiTextKey.CommonSavedLocally) : Text(UiTextKey.CommonNotConfigured);

    private string BuildStatusLabel(OllamaAiProviderSettings settings) =>
        settings.IsConfigured() ? Text(UiTextKey.CommonSavedLocally) : Text(UiTextKey.CommonNotConfigured);

    private static string BuildClaudeSubtitle(AnthropicAiProviderSettings settings) =>
        BuildCatalogSubtitle(AnthropicLabel, settings.Model, ClaudeModelOptions);

    private static string BuildOpenAiSubtitle(OpenAiProviderSettings settings) =>
        BuildCatalogSubtitle(OpenAiLabel, settings.Model, OpenAiModelOptions);

    private string BuildOllamaSubtitle(OllamaAiProviderSettings settings)
    {
        var endpointLabel = BuildOllamaEndpointLabel(settings.Endpoint);
        var modelLabel = string.IsNullOrWhiteSpace(settings.Model)
            ? Text(UiTextKey.CommonSavedLocally).ToLowerInvariant()
            : settings.Model.Trim();

        return string.Join(
            SubtitleSeparator,
            new[]
            {
                Text(UiTextKey.SettingsAiSelfHosted),
                endpointLabel,
                modelLabel
            });
    }

    private string GetMessage(string providerId) =>
        _messages.GetValueOrDefault(providerId) ?? string.Empty;

    private static string BuildCatalogSubtitle(
        string providerLabel,
        string configuredModel,
        IReadOnlyList<SettingsSelectOption> options)
    {
        var modelLabel = string.IsNullOrWhiteSpace(configuredModel)
            ? string.Join(SubtitleSeparator, options.Select(static option => option.Label))
            : configuredModel.Trim();
        return string.Concat(providerLabel, SubtitleSeparator, modelLabel);
    }

    private static string BuildOllamaEndpointLabel(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) &&
            !string.IsNullOrWhiteSpace(endpointUri.Authority))
        {
            return endpointUri.Authority;
        }

        return LocalhostAuthority;
    }

    private Task OnClaudeModelChanged(ChangeEventArgs args)
    {
        _settings.ClaudeApi.Model = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnOpenAiModelChanged(ChangeEventArgs args)
    {
        _settings.OpenAi.Model = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task SaveClaudeAsync()
    {
        await SettingsStore.SaveAsync(_settings);
        _messages[SettingsAiProviderIds.ClaudeApi] = Text(UiTextKey.SettingsAiSavedLocallyDetail);
    }

    private async Task SaveOpenAiAsync()
    {
        await SettingsStore.SaveAsync(_settings);
        _messages[SettingsAiProviderIds.OpenAi] = Text(UiTextKey.SettingsAiSavedLocallyDetail);
    }

    private async Task SaveOllamaAsync()
    {
        await SettingsStore.SaveAsync(_settings);
        _messages[SettingsAiProviderIds.Ollama] = Text(UiTextKey.SettingsAiSavedLocallyDetail);
    }

    private async Task ClearClaudeAsync()
    {
        _settings.ClaudeApi = new AnthropicAiProviderSettings();
        _messages[SettingsAiProviderIds.ClaudeApi] = Text(UiTextKey.SettingsAiProviderCleared);
        await SettingsStore.SaveAsync(_settings);
    }

    private async Task ClearOpenAiAsync()
    {
        _settings.OpenAi = new OpenAiProviderSettings();
        _messages[SettingsAiProviderIds.OpenAi] = Text(UiTextKey.SettingsAiProviderCleared);
        await SettingsStore.SaveAsync(_settings);
    }

    private async Task ClearOllamaAsync()
    {
        _settings.Ollama = new OllamaAiProviderSettings();
        _messages[SettingsAiProviderIds.Ollama] = Text(UiTextKey.SettingsAiProviderCleared);
        await SettingsStore.SaveAsync(_settings);
    }
}
