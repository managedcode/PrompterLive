using Bunit;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LibraryFolderOverlayTests : BunitContext
{
    public LibraryFolderOverlayTests()
    {
        TestHarnessFactory.Create(this);
    }

    [Test]
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
