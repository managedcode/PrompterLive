using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Localization;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    private const string PersistPreferencesOperation = "Settings save preferences";
    private const string PersistPreferencesMessage = "Unable to save general settings.";
    private const string ActiveCssClass = "active";
    private const string OnCssClass = "on";
    private const string SetToggleCssClass = "set-toggle";

    private readonly HashSet<string> _openCards = new(StringComparer.Ordinal)
    {
        "cloud-onedrive",
        "files-scripts",
        "recording-general",
        "ai-claude-api",
        "appearance-theme",
        "about-onboarding",
        "about-app",
        "about-team"
    };

    private SettingsPagePreferences _pagePreferences = SettingsPagePreferences.Default;

    [Inject] private BrowserThemeService ThemeService { get; set; } = null!;

    private bool IsCardOpen(string cardId) => _openCards.Contains(cardId);

    private async Task LoadPreferencesAsync()
    {
        var storedPreferences = await SettingsStore.LoadAsync<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);
        _pagePreferences = storedPreferences ?? SettingsPagePreferences.Default;
        await ThemeService.ApplyAsync(_pagePreferences);
    }

    private async Task ReloadPreferencesAsync()
    {
        await LoadPreferencesAsync();
        await InvokeAsync(StateHasChanged);
    }

    private Task PersistPreferencesAsync() =>
        Diagnostics.RunAsync(
            PersistPreferencesOperation,
            PersistPreferencesMessage,
            () => SettingsStore.SaveAsync(SettingsPagePreferences.StorageKey, _pagePreferences));

    private async Task TogglePreferenceCardAsync(string cardId)
    {
        if (!_openCards.Add(cardId))
        {
            _openCards.Remove(cardId);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleAutoRecordWhenStreamingAsync()
    {
        _pagePreferences = _pagePreferences with { AutoRecordWhenStreaming = !_pagePreferences.AutoRecordWhenStreaming };
        await PersistPreferencesAsync();
    }

    private async Task ToggleSplitRecordingHourlyAsync()
    {
        _pagePreferences = _pagePreferences with { SplitRecordingHourly = !_pagePreferences.SplitRecordingHourly };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingNamingPatternAsync(string value)
    {
        _pagePreferences = _pagePreferences with { RecordingNamingPattern = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingContainerAsync(string value)
    {
        _pagePreferences = _pagePreferences with { RecordingContainer = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingVideoCodecAsync(string value)
    {
        _pagePreferences = _pagePreferences with { RecordingVideoCodec = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingVideoResolutionAsync(string value)
    {
        _pagePreferences = _pagePreferences with { RecordingVideoResolution = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingVideoFrameRateAsync(string value)
    {
        _pagePreferences = _pagePreferences with { RecordingVideoFrameRate = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingVideoBitrateAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var value))
        {
            return;
        }

        _pagePreferences = _pagePreferences with { RecordingVideoBitrateKbps = Math.Max(500, value) };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingAudioCodecAsync(string value)
    {
        _pagePreferences = _pagePreferences with { RecordingAudioCodec = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingAudioSampleRateAsync(string value)
    {
        _pagePreferences = _pagePreferences with { RecordingAudioSampleRate = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingAudioBitrateAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var value))
        {
            return;
        }

        _pagePreferences = _pagePreferences with { RecordingAudioBitrateKbps = Math.Max(96, value) };
        await PersistPreferencesAsync();
    }

    private async Task UpdateRecordingAudioChannelsAsync(string value)
    {
        _pagePreferences = _pagePreferences with { RecordingAudioChannels = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateColorSchemeAsync(string value)
    {
        _pagePreferences = _pagePreferences with { ColorScheme = value };
        await PersistPreferencesAsync();
        await ThemeService.ApplyAsync(_pagePreferences);
    }

    private async Task UpdateAccentColorAsync(string value)
    {
        _pagePreferences = _pagePreferences with { AccentColor = value };
        await PersistPreferencesAsync();
        await ThemeService.ApplyAsync(_pagePreferences);
    }

    private async Task UpdateTeleprompterFontAsync(string value)
    {
        _pagePreferences = _pagePreferences with { TeleprompterFont = value };
        await PersistPreferencesAsync();
    }

    private async Task UpdateTeleprompterFontSizeAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var value))
        {
            return;
        }

        _pagePreferences = _pagePreferences with { TeleprompterFontSize = Math.Clamp(value, 24, 96) };
        await PersistPreferencesAsync();
    }

    private async Task UpdateTeleprompterTextColorAsync(string value)
    {
        _pagePreferences = _pagePreferences with { TeleprompterTextColor = value };
        await PersistPreferencesAsync();
    }

    private async Task ToggleMirrorTeleprompterTextAsync()
    {
        _pagePreferences = _pagePreferences with { MirrorTeleprompterText = !_pagePreferences.MirrorTeleprompterText };
        await PersistPreferencesAsync();
    }

    private async Task ToggleShowWordHighlightAsync()
    {
        _pagePreferences = _pagePreferences with { ShowWordHighlight = !_pagePreferences.ShowWordHighlight };
        await PersistPreferencesAsync();
    }

    private async Task UpdateUiDensityAsync(string value)
    {
        _pagePreferences = _pagePreferences with { UiDensity = value };
        await PersistPreferencesAsync();
        await ThemeService.ApplyAsync(_pagePreferences);
    }

    private async Task ToggleReduceMotionAsync()
    {
        _pagePreferences = _pagePreferences with { ReduceMotion = !_pagePreferences.ReduceMotion };
        await PersistPreferencesAsync();
    }

    private async Task ToggleShowShortcutOverlayAsync()
    {
        _pagePreferences = _pagePreferences with { ShowShortcutOverlay = !_pagePreferences.ShowShortcutOverlay };
        await PersistPreferencesAsync();
    }

    private async Task UpdateLanguageCultureAsync(string value)
    {
        var cultureName = AppCultureCatalog.ResolveSupportedCulture(value);
        _pagePreferences = _pagePreferences with { LanguageCulture = cultureName };
        await PersistPreferencesAsync();
        Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
    }

    private Task HandleOpenOnboardingAsync()
    {
        Navigation.NavigateTo(AppRoutes.LibraryWithOnboarding());
        return Task.CompletedTask;
    }

    private static string BuildToggleCssClass(bool isOn) =>
        isOn ? $"{SetToggleCssClass} {OnCssClass}" : SetToggleCssClass;
}
