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
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.AddInitScriptAsync(BrowserTestConstants.Localization.BuildNavigatorLanguagesInitScript("de-DE", "en-US"));
            await page.GotoAsync(BrowserTestConstants.Routes.Library);

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
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await page.GetByTestId(UiTestIds.Settings.NavLanguage).ClickAsync();
            await page.GetByTestId(UiTestIds.Settings.LanguageSelect).ClickAsync();
            await page.GetByTestId(UiTestIds.Settings.SelectOption(UiTestIds.Settings.LanguageSelect, BrowserTestConstants.Localization.FrenchCultureName)).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await Expect(page.GetByTestId(UiTestIds.Settings.LanguageSelect))
                .ToContainTextAsync(BrowserTestConstants.Localization.FrenchLanguageLabel);

            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            await Expect(page.GetByTestId(UiTestIds.Library.SortLabel))
                .ToHaveTextAsync(BrowserTestConstants.Localization.FrenchSortByLabel);
            await Expect(page.GetByTestId(UiTestIds.Library.SectionFoldersTitle))
                .ToHaveTextAsync(BrowserTestConstants.Localization.FrenchFoldersLabel);

            await page.GetByTestId(UiTestIds.Library.FolderCreateStart).ClickAsync();
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
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.AddInitScriptAsync(BrowserTestConstants.Localization.BuildNavigatorLanguagesInitScript("ru-RU", "uk-UA"));
            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            await Expect(page.GetByTestId(UiTestIds.Library.SortLabel))
                .ToHaveTextAsync(BrowserTestConstants.Localization.EnglishSortByLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
