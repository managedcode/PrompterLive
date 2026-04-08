using Microsoft.Playwright;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Web.UITests;

internal static class GoLiveTestSeedHelper
{
    internal static Task SeedBrowserLocalRecordingPreferencesAsync(IPage page) =>
        GoLiveFlowTests.SeedRecordingPreferencesAsync(
            page,
            SettingsPagePreferences.Default with
            {
                HasSeenOnboarding = true,
                RecordingFolder = RecordingPreferenceCatalog.LocationLabels.BrowserLocalStore
            });
}
