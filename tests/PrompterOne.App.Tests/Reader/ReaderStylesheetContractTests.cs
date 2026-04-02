using PrompterOne.Shared.Contracts;

namespace PrompterOne.App.Tests;

public sealed class ReaderStylesheetContractTests
{
    private const string LearnImport = "@import url(\"./modules/30-rsvp.css\");";
    private const string LearnPreloadMarkup = "<link rel=\"preload\" href=\"_content/PrompterOne.Shared/design/learn.css\" as=\"style\" />";
    private const string LearnStylesheetLinkMarkup = "<link rel=\"stylesheet\" href=\"_content/PrompterOne.Shared/design/learn.css\"";
    private const string ReaderShellImport = "@import url(\"./modules/reader/00-shell.css\");";
    private const string ReaderStatesImport = "@import url(\"./modules/reader/10-reading-states.css\");";
    private const string ReaderControlsImport = "@import url(\"./modules/reader/20-controls.css\");";
    private static readonly string LearnPagePath = ResolvePath("../../../../../src/PrompterOne.Shared/Learn/Pages/LearnPage.razor");
    private static readonly string LearnStylesheetPath = ResolvePath("../../../../../src/PrompterOne.Shared/wwwroot/design/learn.css");
    private static readonly string SharedStylesheetPath = ResolvePath("../../../../../src/PrompterOne.Shared/wwwroot/design/styles.css");
    private static readonly string TeleprompterPagePath = ResolvePath("../../../../../src/PrompterOne.Shared/Teleprompter/Pages/TeleprompterPage.razor");
    private static readonly string TeleprompterStylesheetPath = ResolvePath("../../../../../src/PrompterOne.Shared/wwwroot/design/teleprompter.css");
    private static readonly string HostIndexPath = ResolvePath("../../../../../src/PrompterOne.App/wwwroot/index.html");

    [Fact]
    public void LearnAndTeleprompterFeatureStyles_AreNotBundledIntoSharedManifest()
    {
        var sharedStylesheet = File.ReadAllText(SharedStylesheetPath);

        Assert.DoesNotContain(LearnImport, sharedStylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(ReaderShellImport, sharedStylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(ReaderStatesImport, sharedStylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(ReaderControlsImport, sharedStylesheet, StringComparison.Ordinal);
    }

    [Fact]
    public void LearnStylesheet_ContainsOnlyLearnFeatureImports()
    {
        var learnStylesheet = File.ReadAllText(LearnStylesheetPath);

        Assert.Contains(LearnImport, learnStylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(ReaderShellImport, learnStylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(ReaderStatesImport, learnStylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(ReaderControlsImport, learnStylesheet, StringComparison.Ordinal);
    }

    [Fact]
    public void TeleprompterStylesheet_ContainsOnlyTeleprompterFeatureImports()
    {
        var teleprompterStylesheet = File.ReadAllText(TeleprompterStylesheetPath);

        Assert.DoesNotContain(LearnImport, teleprompterStylesheet, StringComparison.Ordinal);
        Assert.Contains(ReaderShellImport, teleprompterStylesheet, StringComparison.Ordinal);
        Assert.Contains(ReaderStatesImport, teleprompterStylesheet, StringComparison.Ordinal);
        Assert.Contains(ReaderControlsImport, teleprompterStylesheet, StringComparison.Ordinal);
    }

    [Fact]
    public void LearnPage_UsesRouteOwnedStylesheet_AndTeleprompterUsesHostLoadedStylesheet()
    {
        var learnPage = File.ReadAllText(LearnPagePath);
        var teleprompterPage = File.ReadAllText(TeleprompterPagePath);
        var hostIndex = File.ReadAllText(HostIndexPath);

        Assert.Contains("<HeadContent>", learnPage, StringComparison.Ordinal);
        Assert.Contains("DesignStylesheetPaths.Learn", learnPage, StringComparison.Ordinal);

        Assert.DoesNotContain("DesignStylesheetPaths.Teleprompter", teleprompterPage, StringComparison.Ordinal);
        Assert.DoesNotContain("<HeadContent>", teleprompterPage, StringComparison.Ordinal);

        Assert.Contains("_content/PrompterOne.Shared/design/styles.css", hostIndex, StringComparison.Ordinal);
        Assert.Contains(LearnPreloadMarkup, hostIndex, StringComparison.Ordinal);
        Assert.DoesNotContain(LearnStylesheetLinkMarkup, hostIndex, StringComparison.Ordinal);
        Assert.Contains(DesignStylesheetPaths.Teleprompter, hostIndex, StringComparison.Ordinal);
    }

    private static string ResolvePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
}
