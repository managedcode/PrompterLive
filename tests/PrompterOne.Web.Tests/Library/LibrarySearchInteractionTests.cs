using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LibrarySearchInteractionTests : BunitContext
{
    private const string BodySearchQuery = "intuition";
    private const string FileNameSearchQuery = "starter-quantum-computing.tps";

    [Test]
    public void LibraryPage_SearchMatchesStoredDocumentName()
    {
        _ = TestHarnessFactory.Create(this);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.QuantumTitle, cut.Markup, StringComparison.Ordinal));

        Services.GetRequiredService<AppShellService>().UpdateLibrarySearch(FileNameSearchQuery);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(AppTestData.Scripts.QuantumTitle, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(AppTestData.Scripts.DemoTitle, cut.Markup, StringComparison.Ordinal);
        });
    }

    [Test]
    public void LibraryPage_SearchMatchesStoredScriptBodyText()
    {
        _ = TestHarnessFactory.Create(this);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.QuantumTitle, cut.Markup, StringComparison.Ordinal));

        Services.GetRequiredService<AppShellService>().UpdateLibrarySearch(BodySearchQuery);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(AppTestData.Scripts.QuantumTitle, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(AppTestData.Scripts.DemoTitle, cut.Markup, StringComparison.Ordinal);
        });
    }
}
