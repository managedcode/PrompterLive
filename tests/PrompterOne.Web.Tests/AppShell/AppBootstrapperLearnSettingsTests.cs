using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class AppBootstrapperLearnSettingsTests : BunitContext
{
    private const string LegacyLearnSettingsJson =
        """
        {
          "WordsPerMinute": 300,
          "ContextWords": 2,
          "IgnoreScriptSpeeds": false,
          "AutoPlay": false,
          "LoopPlayback": false,
          "ShowPhrasePreview": true
        }
        """;

    [Test]
    public async Task AppBootstrapper_NormalizesLegacyDefaultLearnSpeed_FromBrowserStorage()
    {
        var harness = TestHarnessFactory.Create(this);
        var bootstrapper = Services.GetRequiredService<AppBootstrapper>();

        harness.JsRuntime.SavedJsonValues[BuildSettingsStorageKey(BrowserAppSettingsKeys.LearnSettings)] =
            LegacyLearnSettingsJson;

        await bootstrapper.EnsureReadyAsync();

        Assert.Equal(
            LearnSettingsDefaults.WordsPerMinute,
            Services.GetRequiredService<IScriptSessionService>().State.LearnSettings.WordsPerMinute);

        var savedSettings = harness.JsRuntime.GetSavedValue<LearnSettings>(BrowserAppSettingsKeys.LearnSettings);
        Assert.Equal(LearnSettingsDefaults.WordsPerMinute, savedSettings.WordsPerMinute);
        Assert.False(savedSettings.HasCustomizedWordsPerMinute);
    }

    [Test]
    public async Task AppBootstrapper_PreservesCustomizedLegacyLearnSpeed_FromBrowserStorage()
    {
        var harness = TestHarnessFactory.Create(this);
        var bootstrapper = Services.GetRequiredService<AppBootstrapper>();
        var customizedSettings = new LearnSettings(
            HasCustomizedWordsPerMinute: true,
            WordsPerMinute: LearnSettingsDefaults.LegacyWordsPerMinute);

        harness.JsRuntime.SavedJsonValues[BuildSettingsStorageKey(BrowserAppSettingsKeys.LearnSettings)] =
            JsonSerializer.Serialize(customizedSettings);

        await bootstrapper.EnsureReadyAsync();

        var restoredSettings = Services.GetRequiredService<IScriptSessionService>().State.LearnSettings;
        Assert.Equal(LearnSettingsDefaults.LegacyWordsPerMinute, restoredSettings.WordsPerMinute);
        Assert.True(restoredSettings.HasCustomizedWordsPerMinute);
    }

    private static string BuildSettingsStorageKey(string key) =>
        string.Concat(BrowserStorageKeys.SettingsPrefix, key);
}
