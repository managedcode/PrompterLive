using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LibraryScreenFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const string ImportedBodyOnly =
        """
        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Services talk over real boundaries.
        """;

    private const string ImportedCreatedDate = "2026-03-25";
    private const string ImportedFileName = "system-design-and-software-architecture.tps.md";
    private const string ImportedProfile = "Actor";
    private const string ImportedTitle = "System Design and Software Architecture for Vibe Coders";
    private const string ImportedFrontMatterDocument =
        """
        ---
        title: "System Design and Software Architecture for Vibe Coders"
        profile: Actor
        duration: "145:00"
        base_wpm: 140
        author: "Konstantin Semenenko"
        created: "2026-03-25"
        version: "1.0"
        ---

        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Services talk over real boundaries.
        """;

    private const string StartupScenarioName = "library-startup";
    private const string StartupScenarioStep = "loaded";

    [Test]
    public Task LibraryScreen_NavigatesIntoEditorAndSettings() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.AllScriptsName);
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, GoLiveIndicatorStates.Idle);
            await Expect(page.GetByTestId(UiTestIds.Header.GoLiveDot)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.GoLiveLabel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.GoLiveStatus)).ToHaveCountAsync(0);
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
            await Expect(demoCard).ToContainTextAsync(BrowserTestConstants.Library.ModeLabel);
            await demoCard.HoverAsync();
            var hoverBoxShadow = await demoCard.EvaluateAsync<string>("element => getComputedStyle(element).boxShadow");
            await Assert.That(hoverBoxShadow).IsNotEqualTo(BrowserTestConstants.Library.HoverBoxShadowNone);
            await page.GetByTestId(UiTestIds.Header.LibrarySearch).FillAsync(BrowserTestConstants.Library.SearchQuery);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.QuantumCard)).ToContainTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await Expect(demoCard).ToBeHiddenAsync();
            await page.GetByTestId(UiTestIds.Header.LibrarySearch).FillAsync(string.Empty);
            await page.GetByTestId(UiTestIds.Library.SortDate).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.SortDate)).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            var tedTalksFolder = page.GetByTestId(BrowserTestConstants.Elements.TedTalksFolder);
            await tedTalksFolder.ClickAsync();
            await Expect(tedTalksFolder).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
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

    [Test]
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

    [Test]
    public Task LibraryScreen_OpenScriptImportsLocalFileAndNavigatesIntoEditor() =>
        RunPageAsync(async page =>
        {
            var importPath = await CreateImportedScriptAsync();

            try
            {
                await page.GotoAsync(BrowserTestConstants.Routes.Library);
                await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

                await page.GetByTestId(UiTestIds.Header.LibraryOpenScriptInput)
                    .SetInputFilesAsync(importPath);

                await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
                await Expect(page.GetByTestId(UiTestIds.Header.Title)).ToHaveTextAsync(ImportedTitle);
                await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(ImportedBodyOnly);
                await Expect(page.GetByTestId(UiTestIds.Editor.Profile)).ToHaveValueAsync(ImportedProfile);
                await Expect(page.GetByTestId(UiTestIds.Editor.Created)).ToHaveValueAsync(ImportedCreatedDate);

                await Assert.That(new Uri(page.Url).AbsolutePath).IsEqualTo(AppRoutes.Editor);
                await Assert.That(new Uri(page.Url).Query).Contains($"{AppRoutes.ScriptIdQueryKey}=");
            }
            finally
            {
                File.Delete(importPath);
            }
        });

    [Test]
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
            await Assert.That(new Uri(page.Url).AbsolutePath).IsEqualTo(BrowserTestConstants.Routes.Library);
        });

    private static async Task<string> CreateImportedScriptAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{ImportedFileName}");
        await File.WriteAllTextAsync(path, ImportedFrontMatterDocument);
        return path;
    }

    [Test]
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

            await Expect(tedTalksFolder).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.TedTalksName);

            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Library.FolderAll)).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
        });

    [Test]
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

            await Assert.That(allScriptsOccurrences).IsEqualTo(1);
        });

    [Test]
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

    [Test]
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
            await Assert.That(demoCardMenuOpacity >= BrowserTestConstants.LibraryFlow.MinimumTouchMenuOpacity).IsTrue();

            await demoCardMenu.ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.DemoId)))
                .ToBeVisibleAsync();
        });
}
