using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Localization;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Layout;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class MainLayoutOnboardingTests : BunitContext
{
    private const string UkrainianDismissLabel = "Не цікаво";
    private const string UkrainianWelcomeTitle = "Як працює PrompterOne";

    [Fact]
    public void MainLayout_FirstRun_RendersOnboardingOverlay()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            HasSeenOnboarding = false
        };
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Onboarding.Surface));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Onboarding.Title));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Onboarding.Next));
        });
    }

    [Fact]
    public void MainLayout_OnboardingStaysHidden_WhenPreferenceAlreadySaved()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            HasSeenOnboarding = true
        };

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
            Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Onboarding.Surface))));
    }

    [Fact]
    public async Task MainLayout_DismissingOnboarding_PersistsSeenFlag()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            HasSeenOnboarding = false
        };
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = RenderLayout();
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.Onboarding.Dismiss)));

        cut.FindByTestId(UiTestIds.Onboarding.Dismiss).Click();

        cut.WaitForAssertion(() =>
            Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Onboarding.Surface))));

        Assert.EndsWith(AppRoutes.Library, navigation.Uri, StringComparison.Ordinal);

        var savedPreferences = await Services
            .GetRequiredService<IUserSettingsStore>()
            .LoadAsync<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);

        Assert.NotNull(savedPreferences);
        Assert.True(savedPreferences!.HasSeenOnboarding);
    }

    [Fact]
    public async Task MainLayout_FinishingOnboarding_ReturnsToLibrary_AndPersistsSeenFlag()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            HasSeenOnboarding = false
        };
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = RenderLayout();
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.Onboarding.Next)));

        for (var stepIndex = 0; stepIndex < 5; stepIndex++)
        {
            cut.FindByTestId(UiTestIds.Onboarding.Next).Click();
        }

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Onboarding.Surface))));
        Assert.EndsWith(AppRoutes.Library, navigation.Uri, StringComparison.Ordinal);

        var savedPreferences = await Services
            .GetRequiredService<IUserSettingsStore>()
            .LoadAsync<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);

        Assert.NotNull(savedPreferences);
        Assert.True(savedPreferences!.HasSeenOnboarding);
    }

    [Fact]
    public void MainLayout_OnboardingCanBeReopened_FromShellButtonAfterCompletion()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            HasSeenOnboarding = true
        };
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = RenderLayout();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Onboarding.Surface))));

        cut.FindByTestId(UiTestIds.Header.Onboarding).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Onboarding.Surface));
            Assert.EndsWith(AppRoutes.Library, navigation.Uri, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void MainLayout_OnboardingRendersLocalizedCopy_WhenCurrentCultureIsUkrainian()
    {
        using var cultureScope = new CultureScope(AppCultureCatalog.UkrainianCultureName);
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            HasSeenOnboarding = false
        };
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(UkrainianWelcomeTitle, cut.FindByTestId(UiTestIds.Onboarding.Title).TextContent, StringComparison.Ordinal);
            Assert.Contains(UkrainianDismissLabel, cut.FindByTestId(UiTestIds.Onboarding.Dismiss).TextContent, StringComparison.Ordinal);
        });
    }

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));
}
