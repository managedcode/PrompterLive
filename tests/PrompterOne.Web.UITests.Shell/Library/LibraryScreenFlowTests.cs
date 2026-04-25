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
            await UiInteractionDriver.HoverAndContinueAsync(page, demoCard);
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const element = document.querySelector(`[data-test="${args.testId}"]`);
                    return (getComputedStyle(element).boxShadow ?? "") !== args.noneValue;
                }
                """,
                new
                {
                    noneValue = BrowserTestConstants.Library.HoverBoxShadowNone,
                    testId = BrowserTestConstants.Elements.DemoCard
                });
            var hoverBoxShadow = await demoCard.EvaluateAsync<string>("element => getComputedStyle(element).boxShadow");
            await Assert.That(hoverBoxShadow).IsNotEqualTo(BrowserTestConstants.Library.HoverBoxShadowNone);
            await UiInteractionDriver.FillAndContinueAsync(
                page,
                UiTestIds.Header.LibrarySearch,
                BrowserTestConstants.Library.SearchQuery);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.QuantumCard)).ToContainTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await Expect(demoCard).ToBeHiddenAsync();
            await UiInteractionDriver.FillAndContinueAsync(
                page,
                UiTestIds.Header.LibrarySearch,
                string.Empty);
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

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.OpenSettings),
                noWaitAfter: true);
            await ShellRouteDriver.WaitForSettingsReadyAsync(page);

            await ShellRouteDriver.OpenLibraryAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.LibraryNewScript),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await ShellRouteDriver.OpenLibraryAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CreateScript),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
        });

    [Test]
    public Task LibraryScreen_NewScriptActionsOpenEmptyEditorDraft() =>
        RunPageAsync(async page =>
        {
            await ShellRouteDriver.OpenLibraryAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.LibraryNewScript),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(string.Empty);

            await ShellRouteDriver.OpenLibraryAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CreateScript),
                noWaitAfter: true);
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

            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)),
                demoDropdown,
                noWaitAfter: true);
            await Expect(demoDropdown).ToBeVisibleAsync();
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.SortLabel));
            await Expect(demoDropdown).ToBeHiddenAsync();

            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)),
                demoDropdown,
                noWaitAfter: true);
            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.LeadershipId)),
                leadershipDropdown,
                noWaitAfter: true);
            await Expect(leadershipDropdown).ToBeVisibleAsync();
            await Expect(demoDropdown).ToBeHiddenAsync();

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderAll));
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
    public Task LibraryScreen_ToneMetadataToggle_HidesToneAsTopLevelCardIndicator() =>
        RunPageAsync(async page =>
        {
            await ShellRouteDriver.OpenLibraryAsync(page);

            var toneToggle = page.GetByTestId(UiTestIds.Library.ToneMetadataToggle);
            var demoTone = page.GetByTestId(UiTestIds.Library.CardTone(BrowserTestConstants.Scripts.DemoId));
            await Expect(toneToggle).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(demoTone).ToHaveTextAsync(BrowserTestConstants.Library.DemoToneLabel);
            await Expect(page.GetByTestId(UiTestIds.Library.CardDuration(BrowserTestConstants.Scripts.DemoId))).ToBeVisibleAsync();

            await UiInteractionDriver.ClickAndContinueAsync(toneToggle, noWaitAfter: true);

            await Expect(toneToggle).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.InactiveValue);
            await Expect(demoTone).ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Library.CardDuration(BrowserTestConstants.Scripts.DemoId))).ToBeVisibleAsync();

            await BrowserRouteDriver.ReloadPageAsync(
                page,
                BrowserTestConstants.Routes.Library,
                UiTestIds.Library.Page,
                "library-tone-toggle-reload");
            await Expect(page.GetByTestId(UiTestIds.Library.ToneMetadataToggle)).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.InactiveValue);
            await Expect(page.GetByTestId(UiTestIds.Library.CardTone(BrowserTestConstants.Scripts.DemoId))).ToHaveCountAsync(0);
        });

    [Test]
    public Task LibraryScreen_ProjectSortPersistsAcrossReloads() =>
        RunPageAsync(async page =>
        {
            await ShellRouteDriver.OpenLibraryAsync(page);

            var firstCardTitle = page
                .Locator($"[data-test='{UiTestIds.Library.CardsGrid}'] article.dcard:not(.dcard-create) .dcard-title")
                .First;

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.SortProject));

            await Expect(page.GetByTestId(UiTestIds.Library.SortProject)).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(firstCardTitle).ToHaveTextAsync(BrowserTestConstants.Scripts.HugeDraftTitle);

            await BrowserRouteDriver.ReloadPageAsync(
                page,
                BrowserTestConstants.Routes.Library,
                UiTestIds.Library.Page,
                "library-project-sort-reload");
            await Expect(page.GetByTestId(UiTestIds.Library.SortProject)).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(firstCardTitle).ToHaveTextAsync(BrowserTestConstants.Scripts.HugeDraftTitle);
            await Expect(page.GetByTestId(UiTestIds.Library.SortDuration)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.SortWpm)).ToBeVisibleAsync();
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
            var newFolderOverlay = page.GetByTestId(UiTestIds.Library.NewFolderOverlay);
            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Library.FolderCreateTile),
                newFolderOverlay,
                noWaitAfter: true);
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderCard)).ToBeVisibleAsync();
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.NewFolderCancel),
                noWaitAfter: true);
            await Expect(newFolderOverlay).ToBeHiddenAsync();

            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Library.FolderCreateStart),
                newFolderOverlay,
                noWaitAfter: true);
            await page.GetByTestId(UiTestIds.Library.NewFolderName).FillAsync(BrowserTestConstants.Folders.RoadshowsName);
            await page.GetByTestId(UiTestIds.Library.NewFolderParent).SelectOptionAsync(new[] { BrowserTestConstants.Folders.PresentationsId });
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.NewFolderSubmit),
                noWaitAfter: true);
            await Expect(newFolderOverlay).ToBeHiddenAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Library.FolderAll));
            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)),
                page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.DemoId)),
                noWaitAfter: true);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.Move(BrowserTestConstants.Scripts.DemoId, BrowserTestConstants.Folders.RoadshowsId)),
                noWaitAfter: true);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder),
                noWaitAfter: true);

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

            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                demoCardMenu,
                page.GetByTestId(UiTestIds.Library.CardMenuDropdown(BrowserTestConstants.Scripts.DemoId)),
                noWaitAfter: true);
        });
}
