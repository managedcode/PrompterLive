using Bunit;
using PrompterLive.Core.Services.Samples;
using PrompterLive.Shared.Components.Library;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Services.Library;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class LibraryFolderInteractionTests : BunitContext
{
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

        Assert.Equal(SampleLibraryFolderCatalog.PresentationsFolderId, createdFolder.ParentId);
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
            SampleLibraryFolderCatalog.PresentationsFolderId);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.FindByTestId(UiTestIds.Library.CardMenu(SampleScriptCatalog.DemoSampleId)).Click();
        cut.FindByTestId(UiTestIds.Library.Move(SampleScriptCatalog.DemoSampleId, roadshowsFolder.Id)).Click();

        cut.WaitForAssertion(() =>
        {
            var document = _harness.Repository.GetAsync(SampleScriptCatalog.DemoSampleId).GetAwaiter().GetResult();
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
    public async Task LibraryPage_RestoresPersistedFolderSelectionAfterReload()
    {
        await _harness.FolderRepository.InitializeAsync(SampleLibraryFolderCatalog.CreateSeedFolders());
        await _harness.Repository.InitializeAsync(SampleScriptCatalog.CreateSeedDocuments());
        var roadshowsFolder = await _harness.FolderRepository.CreateAsync(
            AppTestData.Folders.Roadshows,
            SampleLibraryFolderCatalog.PresentationsFolderId);
        await _harness.Repository.MoveToFolderAsync(SampleScriptCatalog.DemoSampleId, roadshowsFolder.Id);
        _harness.JsRuntime.SavedValues["prompterlive.library"] = new LibraryViewState(
            SelectedFolderId: roadshowsFolder.Id,
            SortMode: LibrarySortMode.Date,
            ExpandedFolderIds: [SampleLibraryFolderCatalog.PresentationsFolderId, roadshowsFolder.Id]);

        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Product Launch", cut.Markup);
            Assert.DoesNotContain("Security Incident", cut.Markup);
            Assert.Contains(UiTestIds.Library.Folder(roadshowsFolder.Id), cut.Markup, StringComparison.Ordinal);
            Assert.Contains("active", cut.FindByTestId(UiTestIds.Library.SortDate).ClassName);
        });
    }
}
