using Bunit;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class LibraryFolderOverlayTests : BunitContext
{
    public LibraryFolderOverlayTests()
    {
        TestHarnessFactory.Create(this);
    }

    [Fact]
    public void LibraryPage_FolderOverlay_StartsWithEmptyDraft_AndKeepsTypedValue()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.FindByTestId(UiTestIds.Library.FolderCreateStart).Click();
        var nameInput = cut.FindByTestId(UiTestIds.Library.NewFolderName);

        Assert.Equal(string.Empty, nameInput.GetAttribute("value"));

        nameInput.Input(AppTestData.Folders.Roadshows);

        cut.WaitForAssertion(() =>
        {
            var updatedInput = cut.FindByTestId(UiTestIds.Library.NewFolderName);
            Assert.Equal(AppTestData.Folders.Roadshows, updatedInput.GetAttribute("value"));
        });
    }
}
