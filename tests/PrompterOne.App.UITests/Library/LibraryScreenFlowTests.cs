using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class LibraryScreenFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private const string StartupScenarioName = "library-startup";
    private const string StartupScenarioStep = "loaded";

    [Fact]
    public Task LibraryScreen_NavigatesIntoEditorAndSettings() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.AllScriptsName);
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveClassAsync(BrowserTestConstants.Regexes.GoLiveHeaderClass);
            await Expect(page.GetByTestId(UiTestIds.Library.FolderChips)).ToHaveCountAsync(0);

            var presentationsFolder = page.GetByTestId(BrowserTestConstants.Elements.PresentationsFolder);
            await presentationsFolder.ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.PresentationsName);
            UiScenarioArtifacts.ResetScenario(StartupScenarioName);
            await UiScenarioArtifacts.CapturePageAsync(page, StartupScenarioName, StartupScenarioStep);

            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();

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
    public Task LibraryScreen_KeepsOnlyOneCardMenuOpen_AndOutsideClickDismissesDropdown() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            var demoDropdown = page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.DemoId));
            var leadershipDropdown = page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.LeadershipId));

            await Expect(demoDropdown).ToBeHiddenAsync();
            await Expect(leadershipDropdown).ToBeHiddenAsync();

            await page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)).ClickAsync();
            await Expect(demoDropdown).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.SortLabel).ClickAsync();
            await Expect(demoDropdown).ToBeHiddenAsync();

            await page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)).ClickAsync();
            await page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.LeadershipId)).ClickAsync();
            await Expect(leadershipDropdown).ToBeVisibleAsync();
            await Expect(demoDropdown).ToBeHiddenAsync();

            await page.GetByTestId(BrowserTestConstants.Elements.QuantumCard).ClickAsync();
            await Expect(leadershipDropdown).ToBeHiddenAsync();
            Assert.Equal(BrowserTestConstants.Routes.Library, new Uri(page.Url).AbsolutePath);
        });

    [Fact]
    public Task LibraryScreen_SidebarFoldersFilterCards() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.FolderChips)).ToHaveCountAsync(0);
            var tedTalksFolder = page.GetByTestId(BrowserTestConstants.Elements.TedTalksFolder);
            var presentationsFolder = page.GetByTestId(BrowserTestConstants.Elements.PresentationsFolder);
            await Expect(tedTalksFolder).ToBeVisibleAsync();
            await Expect(presentationsFolder).ToBeVisibleAsync();

            await tedTalksFolder.ClickAsync();

            await Expect(tedTalksFolder).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.TedTalksName);

            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Library.FolderAll)).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
        });

    [Fact]
    public Task LibraryScreen_RootBreadcrumb_RendersSingleAllScriptsLabel() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.AllScriptsName);

            var headerCenterText = (await page.GetByTestId(UiTestIds.Header.Center).TextContentAsync()) ?? string.Empty;
            var allScriptsOccurrences = headerCenterText.Split(
                BrowserTestConstants.Folders.AllScriptsName,
                StringSplitOptions.None).Length - 1;

            Assert.Equal(1, allScriptsOccurrences);
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
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });
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

    [Fact]
    public Task LibraryScreen_TouchViewport_ShowsCardMenuWithoutHover_AndHidesBreadcrumb() =>
        RunPageAsync(async page =>
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.ResponsiveLayout.IphoneMediumWidth,
                BrowserTestConstants.ResponsiveLayout.IphoneMediumHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            var demoCardMenu = page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId));
            var demoCardMenuOpacity = await demoCardMenu.EvaluateAsync<double>(
                """
                element => Number.parseFloat(getComputedStyle(element).opacity)
                """);

            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToBeHiddenAsync();
            Assert.True(demoCardMenuOpacity >= BrowserTestConstants.LibraryFlow.MinimumTouchMenuOpacity);

            await demoCardMenu.ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.DemoId)))
                .ToBeVisibleAsync();
        });
}
