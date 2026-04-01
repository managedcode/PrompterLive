using AngleSharp.Dom;
using Bunit;
using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services.Library;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class LibraryFolderInteractionTests : BunitContext
{
    private const string OpenClassName = "open";
    private readonly AppHarness _harness;

    public LibraryFolderInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void LibraryPage_ClickingSurface_DismissesOpenCardMenu()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup));

        cut.FindByTestId(UiTestIds.Library.CardMenu(AppTestData.Scripts.DemoId)).Click();

        cut.WaitForAssertion(() =>
            Assert.True(GetCardMenuWrap(cut, AppTestData.Scripts.DemoId).ClassList.Contains(OpenClassName)));

        cut.FindByTestId(UiTestIds.Library.Page).Click();

        cut.WaitForAssertion(() =>
            Assert.False(GetCardMenuWrap(cut, AppTestData.Scripts.DemoId).ClassList.Contains(OpenClassName)));
    }

    [Fact]
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
            Assert.Contains("active", tedTalksFolder.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain(UiTestIds.Library.FolderChips, cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
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

    [Fact]
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
            ExpandedFolderIds: [AppTestData.Folders.PresentationsId, roadshowsFolder.Id]);

        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Product Launch", cut.Markup);
            Assert.DoesNotContain("Security Incident", cut.Markup);
            Assert.Contains(UiTestIds.Library.Folder(roadshowsFolder.Id), cut.Markup, StringComparison.Ordinal);
            Assert.Contains("active", cut.FindByTestId(UiTestIds.Library.SortDate).ClassName);
        });
    }

    private static IElement GetCardMenuWrap(IRenderedComponent<LibraryPage> cut, string scriptId) =>
        cut.FindByTestId(UiTestIds.Library.CardMenu(scriptId)).ParentElement
        ?? throw new InvalidOperationException("Library card menu wrapper was not rendered.");
}
