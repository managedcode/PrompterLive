using System.Text.Json;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class OnboardingFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    public Task FirstRunOnboarding_WalksAcrossCoreProductRoutes_AndStaysDismissedAfterCompletion() =>
        RunPageAsync(async page =>
        {
            await SeedPendingOnboardingAsync(page);

            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeVisibleAsync();
            await AssertOnboardingStepAsync(
                page,
                BrowserTestConstants.AppShellFlow.OnboardingEnglishWelcomeTitle,
                BrowserTestConstants.AppShellFlow.OnboardingLibraryStep);

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await AssertOnboardingStepAsync(
                page,
                BrowserTestConstants.AppShellFlow.OnboardingTpsTitle,
                BrowserTestConstants.AppShellFlow.OnboardingTpsStep,
                BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.EditorQuantum));

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await AssertOnboardingStepAsync(
                page,
                BrowserTestConstants.AppShellFlow.OnboardingEditorTitle,
                BrowserTestConstants.AppShellFlow.OnboardingEditorStep);

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await AssertOnboardingStepAsync(
                page,
                BrowserTestConstants.AppShellFlow.OnboardingLearnTitle,
                BrowserTestConstants.AppShellFlow.OnboardingLearnStep,
                BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnQuantum));

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await AssertOnboardingStepAsync(
                page,
                BrowserTestConstants.AppShellFlow.OnboardingTeleprompterTitle,
                BrowserTestConstants.AppShellFlow.OnboardingTeleprompterStep,
                BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.TeleprompterQuantum));

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await AssertOnboardingStepAsync(
                page,
                BrowserTestConstants.AppShellFlow.OnboardingGoLiveTitle,
                BrowserTestConstants.AppShellFlow.OnboardingGoLiveStep,
                BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.GoLiveQuantum));

            await page.GetByTestId(UiTestIds.Onboarding.Next).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Library));
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeHiddenAsync();

            await page.ReloadAsync();
            await Expect(page.GetByTestId(UiTestIds.Onboarding.Surface)).ToBeHiddenAsync();
        });

    [Test]
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

    [Test]
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

    [Test]
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

    private static async Task AssertOnboardingStepAsync(
        Microsoft.Playwright.IPage page,
        string expectedTitle,
        string artifactStep,
        string? expectedRoutePattern = null)
    {
        if (!string.IsNullOrWhiteSpace(expectedRoutePattern))
        {
            await page.WaitForURLAsync(expectedRoutePattern);
        }

        await Expect(page.GetByTestId(UiTestIds.Onboarding.Title))
            .ToHaveTextAsync(expectedTitle);
        await UiScenarioArtifacts.CapturePageAsync(
            page,
            BrowserTestConstants.AppShellFlow.OnboardingScenario,
            artifactStep);
    }
}
