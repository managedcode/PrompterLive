using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Localization;

public sealed class AppCulturePreferenceService(
    IJSRuntime jsRuntime,
    IUserSettingsStore settingsStore,
    ILogger<AppCulturePreferenceService>? logger = null)
{
    private const string ApplyDocumentLanguageFailureMessage = "Failed to apply the browser document language.";
    private const string InitializeFailureMessage = "Failed to initialize the browser culture preference.";
    private const string LoadBrowserLanguagesFailureMessage = "Failed to read browser languages.";

    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly ILogger<AppCulturePreferenceService> _logger = logger ?? NullLogger<AppCulturePreferenceService>.Instance;
    private readonly IUserSettingsStore _settingsStore = settingsStore;

    private bool _initialized;

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
            var storedPreferenceCulture = NormalizeStoredCulture(preferences.LanguageCulture);
            var legacyCulture = await LoadLegacyCultureAsync(cancellationToken);
            var browserCultures = await LoadBrowserCulturesAsync(cancellationToken);

            var preferredCulture = !string.IsNullOrWhiteSpace(storedPreferenceCulture)
                ? AppCultureCatalog.ResolveSupportedCulture(storedPreferenceCulture)
                : !string.IsNullOrWhiteSpace(legacyCulture)
                    ? AppCultureCatalog.ResolveSupportedCulture(legacyCulture)
                    : AppCultureCatalog.ResolvePreferredCulture(browserCultures);

            ApplyCulture(preferredCulture);
            await ApplyDocumentLanguageAsync(preferredCulture, cancellationToken);
            await MigrateLegacyCultureAsync(preferences, storedPreferenceCulture, legacyCulture, cancellationToken);
        }
        catch (Exception exception)
        {
            _initialized = false;
            _logger.LogError(exception, InitializeFailureMessage);
            throw;
        }
    }

    private async Task ApplyDocumentLanguageAsync(string cultureName, CancellationToken cancellationToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                BrowserCultureInteropMethodNames.SetDocumentLanguage,
                cancellationToken,
                cultureName);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ApplyDocumentLanguageFailureMessage);
            throw;
        }
    }

    private static void ApplyCulture(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private async Task<IReadOnlyList<string>> LoadBrowserCulturesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string[]?>(
                    BrowserCultureInteropMethodNames.GetBrowserLanguages,
                    cancellationToken)
                ?? [];
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, LoadBrowserLanguagesFailureMessage);
            throw;
        }
    }

    private async Task<string?> LoadLegacyCultureAsync(CancellationToken cancellationToken)
    {
        var storedCulture = await _jsRuntime.InvokeAsync<string?>(
            BrowserStorageMethodNames.LoadStorageValue,
            cancellationToken,
            BrowserStorageKeys.CultureSetting);

        return NormalizeStoredCulture(storedCulture);
    }

    private async Task MigrateLegacyCultureAsync(
        SettingsPagePreferences preferences,
        string? storedPreferenceCulture,
        string? legacyCulture,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(legacyCulture))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(storedPreferenceCulture))
        {
            await RemoveLegacyCultureAsync(cancellationToken);
            return;
        }

        var migratedCulture = AppCultureCatalog.ResolveSupportedCulture(legacyCulture);
        var updatedPreferences = preferences with
        {
            LanguageCulture = migratedCulture
        };

        await _settingsStore.SaveAsync(SettingsPagePreferences.StorageKey, updatedPreferences, cancellationToken);
        await RemoveLegacyCultureAsync(cancellationToken);
    }

    private Task RemoveLegacyCultureAsync(CancellationToken cancellationToken)
    {
        return _jsRuntime.InvokeVoidAsync(
            BrowserStorageMethodNames.RemoveStorageValue,
            cancellationToken,
            BrowserStorageKeys.CultureSetting).AsTask();
    }

    private static string? NormalizeStoredCulture(string? storedCulture)
    {
        if (string.IsNullOrWhiteSpace(storedCulture))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string>(storedCulture) ?? storedCulture;
        }
        catch (JsonException)
        {
            return storedCulture;
        }
    }
}
