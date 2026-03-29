using Bunit;
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

        cut.Find("[data-testid='library-folder-create-start']").Click();
        var nameInput = cut.Find("[data-testid='library-new-folder-name']");

        Assert.Equal(string.Empty, nameInput.GetAttribute("value"));

        nameInput.Input("Roadshows");

        cut.WaitForAssertion(() =>
        {
            var updatedInput = cut.Find("[data-testid='library-new-folder-name']");
            Assert.Equal("Roadshows", updatedInput.GetAttribute("value"));
        });
    }
}
