using Microsoft.AspNetCore.Components;
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
    private const string DisconnectedStatusLabel = "Not configured";
    private const string LocalOnlySavedMessage = "Saved locally in this browser. Runtime connection testing is not available yet.";
    private const string LocalhostAuthority = "localhost:11434";
    private const string LocalStatusClass = "set-dest-local";
    private const string LocalStatusLabel = "Saved locally";
    private const string OpenAiLabel = "OpenAI";
    private const string OllamaCardId = "ai-ollama";
    private const string OpenAiCardId = "ai-openai";
    private const string ProviderClearedMessage = "Provider draft cleared from this browser.";
    private const string SelfHostedLabel = "Self-hosted";
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
        IsConfigured(settings) ? ActiveCssClass : string.Empty;

    private static string BuildCardCssClass(OpenAiProviderSettings settings) =>
        IsConfigured(settings) ? ActiveCssClass : string.Empty;

    private static string BuildCardCssClass(OllamaAiProviderSettings settings) =>
        IsConfigured(settings) ? ActiveCssClass : string.Empty;

    private static string BuildStatusClass(AnthropicAiProviderSettings settings) =>
        IsConfigured(settings) ? LocalStatusClass : DisconnectedStatusClass;

    private static string BuildStatusClass(OpenAiProviderSettings settings) =>
        IsConfigured(settings) ? LocalStatusClass : DisconnectedStatusClass;

    private static string BuildStatusClass(OllamaAiProviderSettings settings) =>
        IsConfigured(settings) ? LocalStatusClass : DisconnectedStatusClass;

    private static string BuildStatusLabel(AnthropicAiProviderSettings settings) =>
        IsConfigured(settings) ? LocalStatusLabel : DisconnectedStatusLabel;

    private static string BuildStatusLabel(OpenAiProviderSettings settings) =>
        IsConfigured(settings) ? LocalStatusLabel : DisconnectedStatusLabel;

    private static string BuildStatusLabel(OllamaAiProviderSettings settings) =>
        IsConfigured(settings) ? LocalStatusLabel : DisconnectedStatusLabel;

    private static string BuildClaudeSubtitle(AnthropicAiProviderSettings settings) =>
        BuildCatalogSubtitle(AnthropicLabel, settings.Model, ClaudeModelOptions);

    private static string BuildOpenAiSubtitle(OpenAiProviderSettings settings) =>
        BuildCatalogSubtitle(OpenAiLabel, settings.Model, OpenAiModelOptions);

    private static string BuildOllamaSubtitle(OllamaAiProviderSettings settings)
    {
        var endpointLabel = BuildOllamaEndpointLabel(settings.Endpoint);
        var modelLabel = string.IsNullOrWhiteSpace(settings.Model)
            ? LocalStatusLabel.ToLowerInvariant()
            : settings.Model.Trim();

        return string.Join(
            SubtitleSeparator,
            new[]
            {
                SelfHostedLabel,
                endpointLabel,
                modelLabel
            });
    }

    private string GetMessage(string providerId) =>
        _messages.GetValueOrDefault(providerId) ?? string.Empty;

    private static bool IsConfigured(AnthropicAiProviderSettings settings) =>
        !string.IsNullOrWhiteSpace(settings.ApiKey) &&
        !string.IsNullOrWhiteSpace(settings.Model);

    private static bool IsConfigured(OpenAiProviderSettings settings) =>
        !string.IsNullOrWhiteSpace(settings.ApiKey) &&
        !string.IsNullOrWhiteSpace(settings.Model);

    private static bool IsConfigured(OllamaAiProviderSettings settings) =>
        !string.IsNullOrWhiteSpace(settings.Endpoint) &&
        !string.IsNullOrWhiteSpace(settings.Model);

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
        _messages[SettingsAiProviderIds.ClaudeApi] = LocalOnlySavedMessage;
    }

    private async Task SaveOpenAiAsync()
    {
        await SettingsStore.SaveAsync(_settings);
        _messages[SettingsAiProviderIds.OpenAi] = LocalOnlySavedMessage;
    }

    private async Task SaveOllamaAsync()
    {
        await SettingsStore.SaveAsync(_settings);
        _messages[SettingsAiProviderIds.Ollama] = LocalOnlySavedMessage;
    }

    private async Task ClearClaudeAsync()
    {
        _settings.ClaudeApi = new AnthropicAiProviderSettings();
        _messages[SettingsAiProviderIds.ClaudeApi] = ProviderClearedMessage;
        await SettingsStore.SaveAsync(_settings);
    }

    private async Task ClearOpenAiAsync()
    {
        _settings.OpenAi = new OpenAiProviderSettings();
        _messages[SettingsAiProviderIds.OpenAi] = ProviderClearedMessage;
        await SettingsStore.SaveAsync(_settings);
    }

    private async Task ClearOllamaAsync()
    {
        _settings.Ollama = new OllamaAiProviderSettings();
        _messages[SettingsAiProviderIds.Ollama] = ProviderClearedMessage;
        await SettingsStore.SaveAsync(_settings);
    }
}
