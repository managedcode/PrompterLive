using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

public sealed class LibraryScreenFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    [Fact]
    public Task LibraryScreen_NavigatesIntoEditorAndSettings() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            var demoCard = page.GetByTestId(BrowserTestConstants.Elements.DemoCard);
            await Expect(demoCard).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(demoCard.Locator(BrowserTestConstants.Selectors.CardCoverMeta))
                .ToContainTextAsync(BrowserTestConstants.Library.ModeLabel);
            await demoCard.HoverAsync();
            var hoverBoxShadow = await demoCard.EvaluateAsync<string>("element => getComputedStyle(element).boxShadow");
            Assert.NotEqual(BrowserTestConstants.Library.HoverBoxShadowNone, hoverBoxShadow);
            await page.GetByTestId(UiTestIds.Header.LibrarySearch).FillAsync(BrowserTestConstants.Library.SearchQuery);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.QuantumCard)).ToContainTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await Expect(demoCard).ToBeHiddenAsync();
            await page.GetByTestId(UiTestIds.Header.LibrarySearch).FillAsync(string.Empty);
            await page.GetByTestId(UiTestIds.Library.SortDate).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.SortDate)).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            var tedTalksFolder = page.GetByTestId(BrowserTestConstants.Elements.TedTalksFolder);
            await tedTalksFolder.ClickAsync();
            await Expect(tedTalksFolder).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.TedTalksName);

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
            await page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.LeadershipId)).ClickAsync();
            await page.GetByTestId(UiTestIds.Library.CardDuplicate(BrowserTestConstants.Scripts.LeadershipId)).ClickAsync();

            await page.GetByTestId(UiTestIds.Library.OpenSettings).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Settings));
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await page.GetByTestId(UiTestIds.Header.LibraryNewScript).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Editor));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await page.GetByTestId(UiTestIds.Library.CreateScript).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Editor));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
        });

    [Fact]
    public Task LibraryScreen_NewScriptActionsOpenEmptyEditorDraft() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Header.LibraryNewScript).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Editor));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(string.Empty);

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Library.CreateScript).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Editor));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(string.Empty);
        });

    [Fact]
    public Task LibraryScreen_FolderChipsFilterCards() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.FolderChips)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.TedTalksChip)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.PresentationsChip)).ToBeVisibleAsync();

            await page.GetByTestId(BrowserTestConstants.Elements.TedTalksChip).ClickAsync();

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.TedTalksChip)).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.TedTalksName);

            await page.GetByTestId(BrowserTestConstants.Elements.PresentationsChip).ClickAsync();

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.PresentationsChip)).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToBeHiddenAsync();
        });

    [Fact]
    public Task LibraryScreen_CreatesFolderAndMovesScript() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.FolderCreateTile).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderCard)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.NewFolderCancel).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync();

            await page.GetByTestId(UiTestIds.Library.FolderCreateStart).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.NewFolderName).FillAsync(BrowserTestConstants.Folders.RoadshowsName);
            await page.GetByTestId(UiTestIds.Library.NewFolderParent).SelectOptionAsync(new[] { BrowserTestConstants.Folders.PresentationsId });
            await page.GetByTestId(UiTestIds.Library.NewFolderSubmit).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();
            await page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)).ClickAsync();
            await page.GetByTestId(UiTestIds.Library.Move(BrowserTestConstants.Scripts.DemoId, BrowserTestConstants.Folders.RoadshowsId)).ClickAsync();
            await page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder).ClickAsync();

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.SecurityIncidentCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await page.ReloadAsync();

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.SecurityIncidentCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);
        });
}
