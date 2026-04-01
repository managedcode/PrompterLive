using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Services;

public sealed class BrowserThemeService(
    IJSRuntime jsRuntime,
    IUserSettingsStore settingsStore,
    ILogger<BrowserThemeService>? logger = null)
{
    private const string ApplyThemeFailureMessage = "Failed to apply browser theme settings.";
    private const string InitializeThemeFailureMessage = "Failed to initialize browser theme settings.";

    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly ILogger<BrowserThemeService> _logger = logger ?? NullLogger<BrowserThemeService>.Instance;
    private readonly IUserSettingsStore _settingsStore = settingsStore;

    private bool _initialized;

    public async Task ApplyAsync(SettingsPagePreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        try
        {
            await _jsRuntime.InvokeVoidAsync(
                BrowserThemeInteropMethodNames.ApplySettingsTheme,
                cancellationToken,
                preferences.ColorScheme,
                preferences.AccentColor,
                preferences.UiDensity);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ApplyThemeFailureMessage);
            throw;
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            var preferences = await _settingsStore.LoadAsync<SettingsPagePreferences>(
                SettingsPagePreferences.StorageKey,
                cancellationToken)
                ?? SettingsPagePreferences.Default;

            await ApplyAsync(preferences, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, InitializeThemeFailureMessage);
            throw;
        }
    }
}
