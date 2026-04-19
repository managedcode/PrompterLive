using Bunit;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LibraryBootstrapStateTests : Bunit.BunitContext
{
    [Test]
    public void LibraryPage_RendersRuntimeStartupSeeds_WhenHarnessSkipsTestFixtures()
    {
        TestHarnessFactory.Create(this, seedLibraryData: false);

        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Library.Page));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Library.CreateScript));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Library.FolderCreateTile));
            Assert.Contains(AppTestData.Scripts.TpsCueMatrixTitle, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(UiTestIds.Library.FolderChips, cut.Markup, StringComparison.Ordinal);
        });
    }
}
