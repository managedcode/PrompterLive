using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Localization;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class AppCulturePreferenceServiceTests : BunitContext
{
    private readonly TestJsRuntime _jsRuntime;
    private readonly AppCulturePreferenceService _service;
    private readonly IUserSettingsStore _settingsStore;

    public AppCulturePreferenceServiceTests()
    {
        TestHarnessFactory.Create(this, seedLibraryData: false);
        _jsRuntime = Services.GetRequiredService<TestJsRuntime>();
        _service = Services.GetRequiredService<AppCulturePreferenceService>();
        _settingsStore = Services.GetRequiredService<IUserSettingsStore>();
    }

    [Fact]
    public async Task InitializeAsync_UsesSavedPreferenceBeforeBrowserCulture()
    {
        using var _ = new CultureResetScope();
        _jsRuntime.SetBrowserLanguages("de-DE", "en-US");
        await _settingsStore.SaveAsync(
            SettingsPagePreferences.StorageKey,
            SettingsPagePreferences.Default with { LanguageCulture = AppCultureCatalog.FrenchCultureName });

        await _service.InitializeAsync();

        Assert.Equal(AppCultureCatalog.FrenchCultureName, CultureInfo.CurrentCulture.Name);
        Assert.Equal(AppCultureCatalog.FrenchCultureName, CultureInfo.CurrentUICulture.Name);
        Assert.Equal(AppCultureCatalog.FrenchCultureName, _jsRuntime.DocumentLanguage);
    }

    [Fact]
    public async Task InitializeAsync_UsesSupportedBrowserCulture_WhenUserPreferenceIsMissing()
    {
        using var _ = new CultureResetScope();
        _jsRuntime.SetBrowserLanguages("de-DE", "en-US");

        await _service.InitializeAsync();

        Assert.Equal(AppCultureCatalog.GermanCultureName, CultureInfo.CurrentUICulture.Name);
        Assert.Equal(AppCultureCatalog.GermanCultureName, _jsRuntime.DocumentLanguage);
    }

    [Fact]
    public async Task InitializeAsync_MigratesLegacyCultureSetting_WhenTypedPreferenceIsMissing()
    {
        using var _ = new CultureResetScope();
        _jsRuntime.SavedValues[BrowserStorageKeys.CultureSetting] = AppCultureCatalog.GermanCultureName;
        _jsRuntime.SetBrowserLanguages("en-US");

        await _service.InitializeAsync();

        var savedPreferences = await _settingsStore.LoadAsync<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);

        Assert.NotNull(savedPreferences);
        Assert.Equal(AppCultureCatalog.GermanCultureName, savedPreferences!.LanguageCulture);
        Assert.Equal(AppCultureCatalog.GermanCultureName, CultureInfo.CurrentUICulture.Name);
        Assert.Equal(AppCultureCatalog.GermanCultureName, _jsRuntime.DocumentLanguage);
        Assert.DoesNotContain(BrowserStorageKeys.CultureSetting, _jsRuntime.SavedValues.Keys);
        Assert.DoesNotContain(BrowserStorageKeys.CultureSetting, _jsRuntime.SavedJsonValues.Keys);
    }

    private sealed class CultureResetScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;
        private readonly CultureInfo? _originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        private readonly CultureInfo? _originalDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = _originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _originalDefaultUiCulture;
        }
    }
}
