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
            await ShellRouteDriver.OpenLibraryAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.AllScriptsName);
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, GoLiveIndicatorStates.Idle);
            await Expect(page.GetByTestId(UiTestIds.Header.GoLiveDot)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.GoLiveLabel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.GoLiveStatus)).ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Library.FolderChips)).ToHaveCountAsync(0);

            var presentationsFolder = page.GetByTestId(BrowserTestConstants.Elements.PresentationsFolder);
            await UiInteractionDriver.ClickAndContinueAsync(presentationsFolder);
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.PresentationsName);
            UiScenarioArtifacts.ResetScenario(StartupScenarioName);
            await UiScenarioArtifacts.CapturePageAsync(page, StartupScenarioName, StartupScenarioStep);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderAll));

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
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.SortDate));
            await Expect(page.GetByTestId(UiTestIds.Library.SortDate)).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            var tedTalksFolder = page.GetByTestId(BrowserTestConstants.Elements.TedTalksFolder);
            await UiInteractionDriver.ClickAndContinueAsync(tedTalksFolder);
            await Expect(tedTalksFolder).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.TedTalksName);

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
            var leadershipMenuDropdown = page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.LeadershipId));
            var leadershipMenuTrigger = page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.LeadershipId));
            var leadershipDuplicateAction = leadershipMenuDropdown.GetByTestId(
                UiTestIds.Library.CardDuplicate(BrowserTestConstants.Scripts.LeadershipId));
            await Expect(leadershipMenuDropdown).ToBeHiddenAsync();
            await UiInteractionDriver.ClickAndWaitForVisibleAsync(leadershipMenuTrigger, leadershipMenuDropdown);
            await UiInteractionDriver.ClickAndContinueAsync(leadershipDuplicateAction);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.OpenSettings));
            await ShellRouteDriver.WaitForSettingsReadyAsync(page);

            await ShellRouteDriver.OpenLibraryAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Header.LibraryNewScript));
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await ShellRouteDriver.OpenLibraryAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.CreateScript));
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
        });

    [Test]
    public Task LibraryScreen_NewScriptActionsOpenEmptyEditorDraft() =>
        RunPageAsync(async page =>
        {
            await ShellRouteDriver.OpenLibraryAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Header.LibraryNewScript));
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(string.Empty);

            await ShellRouteDriver.OpenLibraryAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.CreateScript));
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(string.Empty);
        });

    [Test]
    public Task LibraryScreen_OpenScriptImportsLocalFileAndNavigatesIntoEditor() =>
        RunPageAsync(async page =>
        {
            var importPath = await CreateImportedScriptAsync();

            try
            {
                await ShellRouteDriver.OpenLibraryAsync(page);

                await page.GetByTestId(UiTestIds.Header.LibraryOpenScriptInput)
                    .SetInputFilesAsync(importPath);

                await EditorIsolatedDraftDriver.WaitForImportedDraftAsync(page, ImportedTitle, ImportedBodyOnly);
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
            await ShellRouteDriver.OpenLibraryAsync(page);

            var demoDropdown = page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.DemoId));
            var leadershipDropdown = page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.LeadershipId));

            await Expect(demoDropdown).ToBeHiddenAsync();
            await Expect(leadershipDropdown).ToBeHiddenAsync();

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)));
            await Expect(demoDropdown).ToBeVisibleAsync();
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.SortLabel));
            await Expect(demoDropdown).ToBeHiddenAsync();

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)));
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.LeadershipId)));
            await Expect(leadershipDropdown).ToBeVisibleAsync();
            await Expect(demoDropdown).ToBeHiddenAsync();

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(BrowserTestConstants.Elements.QuantumCard));
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
            await ShellRouteDriver.OpenLibraryAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Library.FolderChips)).ToHaveCountAsync(0);
            var tedTalksFolder = page.GetByTestId(BrowserTestConstants.Elements.TedTalksFolder);
            var presentationsFolder = page.GetByTestId(BrowserTestConstants.Elements.PresentationsFolder);
            await Expect(tedTalksFolder).ToBeVisibleAsync();
            await Expect(presentationsFolder).ToBeVisibleAsync();

            await UiInteractionDriver.ClickAndContinueAsync(tedTalksFolder);

            await Expect(tedTalksFolder).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.TedTalksName);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderAll));

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
            await ShellRouteDriver.OpenLibraryAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderAll));

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
            await ShellRouteDriver.OpenLibraryAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderCreateTile));
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderCard)).ToBeVisibleAsync();
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.NewFolderCancel));
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync();

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderCreateStart));
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.NewFolderName).FillAsync(BrowserTestConstants.Folders.RoadshowsName);
            await page.GetByTestId(UiTestIds.Library.NewFolderParent).SelectOptionAsync(new[] { BrowserTestConstants.Folders.PresentationsId });
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.NewFolderSubmit));
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderAll));
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)));
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.Move(BrowserTestConstants.Scripts.DemoId, BrowserTestConstants.Folders.RoadshowsId)));
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder));

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.SecurityIncidentCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await BrowserRouteDriver.ReloadPageAsync(
                page,
                BrowserTestConstants.Routes.Library,
                UiTestIds.Library.Page,
                "library-create-folder-reload");
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
            await ShellRouteDriver.OpenLibraryAsync(page);

            var demoCardMenu = page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId));
            var demoCardMenuOpacity = await demoCardMenu.EvaluateAsync<double>(
                """
                element => Number.parseFloat(getComputedStyle(element).opacity)
                """);

            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToBeHiddenAsync();
            await Assert.That(demoCardMenuOpacity >= BrowserTestConstants.LibraryFlow.MinimumTouchMenuOpacity).IsTrue();

            await UiInteractionDriver.ClickAndContinueAsync(demoCardMenu);
            await Expect(page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.DemoId)))
                .ToBeVisibleAsync();
        });
}
