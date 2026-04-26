using Bunit;
using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services.Library;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LibraryFolderInteractionTests : BunitContext
{
    private const string ActiveStateValue = "active";
    private const string ClosedExpandedStateValue = "closed";
    private const string InactiveStateValue = "inactive";
    private const string OpenExpandedStateValue = "open";
    private readonly AppHarness _harness;

    public LibraryFolderInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public async Task LibraryPage_CreatesFolderInsideSelectedParent()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.FindByTestId(UiTestIds.Library.FolderCreateTile).Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains(UiTestIds.Library.NewFolderOverlay, cut.Markup, StringComparison.Ordinal);
            Assert.Contains(UiTestIds.Library.NewFolderCard, cut.Markup, StringComparison.Ordinal);
        });
        cut.FindByTestId(UiTestIds.Library.NewFolderName).Input(AppTestData.Folders.Roadshows);
        cut.FindByTestId(UiTestIds.Library.NewFolderParent).Change(AppTestData.Folders.PresentationsId);
        cut.FindByTestId(UiTestIds.Library.NewFolderSubmit).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Roadshows", cut.Markup);
            Assert.Contains(UiTestIds.Library.Folder("roadshows"), cut.Markup, StringComparison.Ordinal);
        });

        var createdFolder = (await _harness.FolderRepository.ListAsync())
            .Single(folder => folder.Name == AppTestData.Folders.Roadshows);

        Assert.Equal(AppTestData.Folders.PresentationsId, createdFolder.ParentId);
    }

    [Test]
    public async Task LibraryPage_OrganizationTerminologySwitch_PreservesFolderRepositoryBehavior()
    {
        const string showName = "Roadshow Season";

        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Folders", cut.FindByTestId(UiTestIds.Library.SectionFoldersTitle).TextContent.Trim());
            Assert.Contains("Product Launch", cut.Markup);
        });

        cut.FindByTestId(UiTestIds.Library.OrganizationModeOption("shows")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Shows", cut.FindByTestId(UiTestIds.Library.SectionFoldersTitle).TextContent.Trim());
            Assert.Contains("New show", cut.FindByTestId(UiTestIds.Library.FolderCreateTile).TextContent);
        });

        cut.FindByTestId(UiTestIds.Library.FolderCreateStart).Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Create Show", cut.FindByTestId(UiTestIds.Library.NewFolderTitle).TextContent.Trim());
            Assert.Equal("Morning show", cut.FindByTestId(UiTestIds.Library.NewFolderName).GetAttribute("placeholder"));
        });
        cut.FindByTestId(UiTestIds.Library.NewFolderName).Input(showName);
        cut.FindByTestId(UiTestIds.Library.NewFolderSubmit).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(showName, cut.Markup);
            Assert.Equal("Shows", cut.FindByTestId(UiTestIds.Library.SectionFoldersTitle).TextContent.Trim());
        });

        var createdFolder = (await _harness.FolderRepository.ListAsync())
            .Single(folder => folder.Name == showName);
        Assert.Null(createdFolder.ParentId);
    }

    [Test]
    public void LibraryPage_ToneMetadataToggle_HidesAndRestoresToneBadge()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.ToneMetadataToggle).GetAttribute("data-active"));
            Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup);
            Assert.Contains(UiTestIds.Library.CardTone(AppTestData.Scripts.DemoId), cut.Markup, StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.Library.ToneMetadataToggle).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(InactiveStateValue, cut.FindByTestId(UiTestIds.Library.ToneMetadataToggle).GetAttribute("data-active"));
            Assert.DoesNotContain(UiTestIds.Library.CardTone(AppTestData.Scripts.DemoId), cut.Markup, StringComparison.Ordinal);
            Assert.Contains(UiTestIds.Library.CardDuration(AppTestData.Scripts.DemoId), cut.Markup, StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.Library.ToneMetadataToggle).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.ToneMetadataToggle).GetAttribute("data-active"));
            Assert.Contains(UiTestIds.Library.CardTone(AppTestData.Scripts.DemoId), cut.Markup, StringComparison.Ordinal);
        });
    }

    [Test]
    public void LibraryPage_SidebarToggleClosesAndReopensSidebar()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(OpenExpandedStateValue, cut.FindByTestId(UiTestIds.Library.Page).GetAttribute("data-sidebar-state"));
            Assert.Equal("true", cut.FindByTestId(UiTestIds.Library.SidebarToggle).GetAttribute("aria-expanded"));
            Assert.Equal("false", cut.FindByTestId(UiTestIds.Library.Sidebar).GetAttribute("aria-hidden"));
            Assert.Contains(UiTestIds.Library.SidebarClose, cut.Markup, StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.Library.SidebarClose).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ClosedExpandedStateValue, cut.FindByTestId(UiTestIds.Library.Page).GetAttribute("data-sidebar-state"));
            Assert.Equal("false", cut.FindByTestId(UiTestIds.Library.SidebarToggle).GetAttribute("aria-expanded"));
            Assert.Equal("true", cut.FindByTestId(UiTestIds.Library.Sidebar).GetAttribute("aria-hidden"));
            Assert.DoesNotContain(UiTestIds.Library.SidebarScrim, cut.Markup, StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.Library.SidebarToggle).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(OpenExpandedStateValue, cut.FindByTestId(UiTestIds.Library.Page).GetAttribute("data-sidebar-state"));
            Assert.Equal("true", cut.FindByTestId(UiTestIds.Library.SidebarToggle).GetAttribute("aria-expanded"));
            Assert.Equal("false", cut.FindByTestId(UiTestIds.Library.Sidebar).GetAttribute("aria-hidden"));
            Assert.Contains(UiTestIds.Library.SidebarScrim, cut.Markup, StringComparison.Ordinal);
        });
    }

    [Test]
    public void LibraryPage_CardPlaybackActionsUseClearModeLabels()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Practice", cut.FindByTestId(UiTestIds.Library.CardLearn(AppTestData.Scripts.DemoId)).TextContent.Trim());
            Assert.DoesNotContain(UiTestIds.Library.CardPrep(AppTestData.Scripts.DemoId), cut.Markup, StringComparison.Ordinal);
            Assert.Equal("Teleprompter", cut.FindByTestId(UiTestIds.Library.CardRead(AppTestData.Scripts.DemoId)).TextContent.Trim());
        });
    }

    [Test]
    public void LibraryPage_ProjectSortOrdersCardsByFolderName_AndPersistsSelection()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup));

        cut.FindByTestId(UiTestIds.Library.SortProject).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.SortProject).GetAttribute("data-active"));
            Assert.Equal(AppTestData.Scripts.LearnWpmBoundaryTitle, ResolveFirstCardTitle(cut));
        });

        cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.SortProject).GetAttribute("data-active"));
            Assert.Equal(AppTestData.Scripts.LearnWpmBoundaryTitle, ResolveFirstCardTitle(cut));
        });
    }

    [Test]
    public void LibraryPage_FavoriteButtonPinsScriptAndFavoritesFilterPersists()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(InactiveStateValue, cut.FindByTestId(UiTestIds.Library.CardFavorite(AppTestData.Scripts.DemoId)).GetAttribute("data-active"));
            Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup);
        });

        cut.FindByTestId(UiTestIds.Library.CardFavorite(AppTestData.Scripts.DemoId)).Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.CardFavorite(AppTestData.Scripts.DemoId)).GetAttribute("data-active")));

        cut.FindByTestId(UiTestIds.Library.FolderFavorites).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.FolderFavorites).GetAttribute("data-active"));
            Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup);
            Assert.DoesNotContain(AppTestData.Scripts.SecurityIncidentTitle, cut.Markup);
        });

        cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.FolderFavorites).GetAttribute("data-active"));
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.CardFavorite(AppTestData.Scripts.DemoId)).GetAttribute("data-active"));
            Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup);
            Assert.DoesNotContain(AppTestData.Scripts.SecurityIncidentTitle, cut.Markup);
        });
    }

    [Test]
    public async Task LibraryPage_CancelsFolderOverlay_WithoutCreatingFolder()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.FindByTestId(UiTestIds.Library.FolderCreateStart).Click();
        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Library.NewFolderOverlay, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Library.NewFolderCancel).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(UiTestIds.Library.NewFolderOverlay, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(UiTestIds.Library.NewFolderCard, cut.Markup, StringComparison.Ordinal);
        });

        var folders = await _harness.FolderRepository.ListAsync();
        Assert.DoesNotContain(folders, folder => folder.Name == AppTestData.Folders.Roadshows);
    }

    [Test]
    public async Task LibraryPage_MovesScriptIntoFolder_AndUpdatesVisibleCards()
    {
        var roadshowsFolder = await _harness.FolderRepository.CreateAsync(
            AppTestData.Folders.Roadshows,
            AppTestData.Folders.PresentationsId);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.FindByTestId(UiTestIds.Library.CardMenu(AppTestData.Scripts.DemoId)).Click();
        cut.FindByTestId(UiTestIds.Library.Move(AppTestData.Scripts.DemoId, roadshowsFolder.Id)).Click();

        cut.WaitForAssertion(() =>
        {
            var document = _harness.Repository.GetAsync(AppTestData.Scripts.DemoId).GetAwaiter().GetResult();
            Assert.Equal(roadshowsFolder.Id, document?.FolderId);
        });

        cut.FindByTestId(UiTestIds.Library.Folder(roadshowsFolder.Id)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Product Launch", cut.Markup);
            Assert.DoesNotContain("Security Incident", cut.Markup);
            Assert.Contains(UiTestIds.Library.Folder(roadshowsFolder.Id), cut.Markup, StringComparison.Ordinal);
        });
    }

    [Test]
    public void LibraryPage_ClickingSurface_DismissesOpenCardMenu()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup));

        cut.FindByTestId(UiTestIds.Library.CardMenu(AppTestData.Scripts.DemoId)).Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                OpenExpandedStateValue,
                cut.FindByTestId(UiTestIds.Library.CardMenuWrap(AppTestData.Scripts.DemoId))
                    .GetAttribute("data-expanded")));

        cut.FindByTestId(UiTestIds.Library.Page).Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                ClosedExpandedStateValue,
                cut.FindByTestId(UiTestIds.Library.CardMenuWrap(AppTestData.Scripts.DemoId))
                    .GetAttribute("data-expanded")));
    }

    [Test]
    public void LibraryPage_SelectsSidebarFolder_AndFiltersCards()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup));

        var tedTalksFolder = cut.FindByTestId(UiTestIds.Library.Folder(AppTestData.Folders.TedTalksId));
        tedTalksFolder.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(AppTestData.Scripts.TedLeadershipTitle, cut.Markup);
            Assert.DoesNotContain(AppTestData.Scripts.DemoTitle, cut.Markup);
            Assert.Equal(ActiveStateValue, tedTalksFolder.GetAttribute("data-active"));
            Assert.DoesNotContain(UiTestIds.Library.FolderChips, cut.Markup, StringComparison.Ordinal);
        });
    }

    [Test]
    public async Task LibraryPage_SelectingNestedParentFolder_TogglesItsChildrenInSidebar()
    {
        const string nestedFolderName = "Launch Decks";

        await _harness.FolderRepository.InitializeAsync(AppTestLibrarySeedData.CreateFolders());
        await _harness.Repository.InitializeAsync(AppTestLibrarySeedData.CreateDocuments());
        var nestedFolder = await _harness.FolderRepository.CreateAsync(
            nestedFolderName,
            AppTestData.Folders.ProductId);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(UiTestIds.Library.Folder(AppTestData.Folders.ProductId), cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(UiTestIds.Library.Folder(nestedFolder.Id), cut.Markup, StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.Library.Folder(AppTestData.Folders.ProductId)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(UiTestIds.Library.Folder(nestedFolder.Id), cut.Markup, StringComparison.Ordinal);
            Assert.Contains(nestedFolderName, cut.Markup, StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.Library.Folder(AppTestData.Folders.ProductId)).Click();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain(UiTestIds.Library.Folder(nestedFolder.Id), cut.Markup, StringComparison.Ordinal));
    }

    [Test]
    public async Task LibraryPage_RestoresPersistedFolderSelectionAfterReload()
    {
        await _harness.FolderRepository.InitializeAsync(AppTestLibrarySeedData.CreateFolders());
        await _harness.Repository.InitializeAsync(AppTestLibrarySeedData.CreateDocuments());
        var roadshowsFolder = await _harness.FolderRepository.CreateAsync(
            AppTestData.Folders.Roadshows,
            AppTestData.Folders.PresentationsId);
        await _harness.Repository.MoveToFolderAsync(AppTestData.Scripts.DemoId, roadshowsFolder.Id);
        _harness.JsRuntime.SavedValues["prompterone.library"] = new LibraryViewState(
            SelectedFolderId: roadshowsFolder.Id,
            SortMode: LibrarySortMode.Date,
            OrganizationMode: LibraryOrganizationMode.Folders,
            ShowToneMetadata: true,
            ExpandedFolderIds: [AppTestData.Folders.PresentationsId, roadshowsFolder.Id]);

        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Product Launch", cut.Markup);
            Assert.DoesNotContain("Security Incident", cut.Markup);
            Assert.Contains(UiTestIds.Library.Folder(roadshowsFolder.Id), cut.Markup, StringComparison.Ordinal);
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Library.SortDate).GetAttribute("data-active"));
        });
    }

    private static string ResolveFirstCardTitle(IRenderedComponent<LibraryPage> cut) =>
        cut.Find($"[data-test='{UiTestIds.Library.CardsGrid}'] article.dcard:not(.dcard-create) .dcard-title")
            .TextContent
            .Trim();
}
