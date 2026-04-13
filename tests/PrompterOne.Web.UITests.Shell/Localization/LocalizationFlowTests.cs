using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LocalizationFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task LibraryScreen_UsesBrowserGermanCulture_ForLocalizedChrome()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.AddInitScriptAsync(BrowserTestConstants.Localization.BuildNavigatorLanguagesInitScript("de-DE", "en-US"));
            await ShellRouteDriver.OpenLibraryAsync(page);

            await Expect(page.GetByTestId(UiTestIds.Library.SortLabel))
                .ToHaveTextAsync(BrowserTestConstants.Localization.GermanSortByLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task SettingsLanguageSelection_PersistsFrenchCulture_AfterReload()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ShellRouteDriver.OpenSettingsAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Settings.NavLanguage));
            await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.LanguagePreferencesCard);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Settings.LanguageSelect));
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Settings.SelectOption(
                    UiTestIds.Settings.LanguageSelect,
                    BrowserTestConstants.Localization.FrenchCultureName)));
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await ShellRouteDriver.OpenSettingsAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Settings.NavLanguage));
            await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.LanguagePreferencesCard);

            await Expect(page.GetByTestId(UiTestIds.Settings.LanguageSelect))
                .ToContainTextAsync(BrowserTestConstants.Localization.FrenchLanguageLabel);

            await ShellRouteDriver.OpenLibraryAsync(page);

            await Expect(page.GetByTestId(UiTestIds.Library.SortLabel))
                .ToHaveTextAsync(BrowserTestConstants.Localization.FrenchSortByLabel);
            await Expect(page.GetByTestId(UiTestIds.Library.SectionFoldersTitle))
                .ToHaveTextAsync(BrowserTestConstants.Localization.FrenchFoldersLabel);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderCreateStart));
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderTitle))
                .ToHaveTextAsync(BrowserTestConstants.Localization.FrenchCreateFolderTitle);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LibraryScreen_FallsBackToEnglish_WhenBrowserCultureIsRussian()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.AddInitScriptAsync(BrowserTestConstants.Localization.BuildNavigatorLanguagesInitScript("ru-RU", "uk-UA"));
            await ShellRouteDriver.OpenLibraryAsync(page);

            await Expect(page.GetByTestId(UiTestIds.Library.SortLabel))
                .ToHaveTextAsync(BrowserTestConstants.Localization.EnglishSortByLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
