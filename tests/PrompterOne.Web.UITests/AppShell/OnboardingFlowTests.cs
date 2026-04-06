using System.Text.Json;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed class OnboardingFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public Task FirstRunOnboarding_WalksAcrossCoreProductRoutes_AndStaysDismissedAfterCompletion() =>
        RunPageAsync(async page =>
        {
            await SeedPendingOnboardingAsync(page);

            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.OnboardingEnglishWelcomeTitle);
            await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.AppShellFlow.OnboardingScenario, BrowserTestConstants.AppShellFlow.OnboardingLibraryStep);

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.EditorQuantum));
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.OnboardingEditorTitle);
            await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.AppShellFlow.OnboardingScenario, BrowserTestConstants.AppShellFlow.OnboardingEditorStep);

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnQuantum));
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.OnboardingLearnTitle);
            await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.AppShellFlow.OnboardingScenario, BrowserTestConstants.AppShellFlow.OnboardingLearnStep);

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.TeleprompterQuantum));
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.OnboardingTeleprompterTitle);
            await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.AppShellFlow.OnboardingScenario, BrowserTestConstants.AppShellFlow.OnboardingTeleprompterStep);

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.GoLiveQuantum));
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.OnboardingGoLiveTitle);
            await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.AppShellFlow.OnboardingScenario, BrowserTestConstants.AppShellFlow.OnboardingGoLiveStep);

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Library));
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeHiddenAsync();

            await page.ReloadAsync();
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeHiddenAsync();
        });

    [Fact]
    public Task FirstRunOnboarding_DismissReturnsToLibrary_AndStaysHiddenAfterReload() =>
        RunPageAsync(async page =>
        {
            await SeedPendingOnboardingAsync(page);

            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Onboarding.Dismiss).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Library));
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeHiddenAsync();

            await page.ReloadAsync();
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeHiddenAsync();
        });

    [Fact]
    public Task OnboardingReopen_FromSettings_ReturnsToLibraryWithOverlay() =>
        RunPageAsync(async page =>
        {
            await SeedCompletedOnboardingAsync(page);

            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await page.GetByTestId(UiTestIds.Settings.NavAbout).ClickAsync();

            await page.GetByTestId(UiTestIds.Settings.AboutOnboardingRestart).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LibraryWithOnboarding));

            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.OnboardingEnglishWelcomeTitle);

            await page.GetByTestId(UiTestIds.Onboarding.Dismiss).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Library));
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeHiddenAsync();
        });

    [Fact]
    public Task FirstRunOnboarding_UsesUkrainianBrowserCulture_ForLocalizedCopy() =>
        RunPageAsync(async page =>
        {
            await SeedPendingOnboardingAsync(page);
            await page.AddInitScriptAsync(BrowserTestConstants.Localization.BuildNavigatorLanguagesInitScript("uk-UA", "en-US"));

            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            await Expect(page.GetByTestId(UiTestIds.Onboarding.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.OnboardingUkrainianWelcomeTitle);
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Dismiss))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.OnboardingUkrainianDismiss);
        });

    private static Task SeedPendingOnboardingAsync(Microsoft.Playwright.IPage page)
    {
        var pendingPreferences = JsonSerializer.Serialize(
            SettingsPagePreferences.Default with
            {
                HasSeenOnboarding = false
            },
            JsonOptions);

        return page.EvaluateAsync(
            BrowserTestConstants.Localization.SetLocalStorageScript,
            new object[]
            {
                BrowserStorageKeys.SettingsPrefix + SettingsPagePreferences.StorageKey,
                pendingPreferences
            });
    }

    private static Task SeedCompletedOnboardingAsync(Microsoft.Playwright.IPage page)
    {
        var completedPreferences = JsonSerializer.Serialize(
            SettingsPagePreferences.Default with
            {
                HasSeenOnboarding = true
            },
            JsonOptions);

        return page.EvaluateAsync(
            BrowserTestConstants.Localization.SetLocalStorageScript,
            new object[]
            {
                BrowserStorageKeys.SettingsPrefix + SettingsPagePreferences.StorageKey,
                completedPreferences
            });
    }
}
