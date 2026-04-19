using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LibrarySearchInteractionTests : BunitContext
{
    private const string MatchingFileNameQuery = "starter-tps-cue-matrix";
    private const string MatchingBodyQuery = "xslow";
    private const string NonMatchingQuery = "zzz-nothing-matches-this";

    [Test]
    public void LibraryPage_SearchMatchesStoredDocumentName()
    {
        _ = TestHarnessFactory.Create(this);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.TpsCueMatrixTitle, cut.Markup, StringComparison.Ordinal));

        Services.GetRequiredService<AppShellService>().UpdateLibrarySearch(MatchingFileNameQuery);

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.TpsCueMatrixTitle, cut.Markup, StringComparison.Ordinal));

        Services.GetRequiredService<AppShellService>().UpdateLibrarySearch(NonMatchingQuery);

        cut.WaitForAssertion(() => Assert.DoesNotContain(AppTestData.Scripts.TpsCueMatrixTitle, cut.Markup, StringComparison.Ordinal));
    }

    [Test]
    public void LibraryPage_SearchMatchesStoredScriptBodyText()
    {
        _ = TestHarnessFactory.Create(this);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.TpsCueMatrixTitle, cut.Markup, StringComparison.Ordinal));

        Services.GetRequiredService<AppShellService>().UpdateLibrarySearch(MatchingBodyQuery);

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.TpsCueMatrixTitle, cut.Markup, StringComparison.Ordinal));

        Services.GetRequiredService<AppShellService>().UpdateLibrarySearch(NonMatchingQuery);

        cut.WaitForAssertion(() => Assert.DoesNotContain(AppTestData.Scripts.TpsCueMatrixTitle, cut.Markup, StringComparison.Ordinal));
    }
}
