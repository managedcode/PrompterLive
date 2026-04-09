using System.Text.Json;
using Microsoft.Playwright;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Web.UITests;

internal static class AiProviderTestSeeder
{
    internal static Task SeedConfiguredOpenAiAsync(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var settings = new AiProviderSettings
        {
            OpenAi = new OpenAiProviderSettings
            {
                ApiKey = BrowserTestConstants.Editor.AiProviderConfiguredApiKey,
                Model = BrowserTestConstants.Editor.AiProviderConfiguredModel
            }
        }.Normalize();

        var json = JsonSerializer.Serialize(settings);
        return page.EvaluateAsync(
            BrowserTestConstants.Localization.SetLocalStorageScript,
            new object[]
            {
                string.Concat(BrowserStorageKeys.SettingsPrefix, AiProviderSettings.StorageKey),
                json
            });
    }

    internal static Task SeedUnconfiguredAsync(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var json = JsonSerializer.Serialize(AiProviderSettings.CreateDefault().Normalize());
        return page.EvaluateAsync(
            BrowserTestConstants.Localization.SetLocalStorageScript,
            new object[]
            {
                string.Concat(BrowserStorageKeys.SettingsPrefix, AiProviderSettings.StorageKey),
                json
            });
    }
}
