namespace PrompterOne.Web.Tests;

public sealed class AiSpotlightStylesheetContractTests
{
    private const string ExpectedBackdropOpacityMix = "color-mix(in srgb, var(--bg-deep) 8%, transparent)";
    private const string ExpectedNoBackdropBlur = "backdrop-filter: none;";
    private const string ExpectedDesktopTopOffset = "padding: min(22vh, 200px) 16px 16px;";
    private const string ExpectedMobileTopOffset = "padding-top: min(18vh, 132px);";
    private const string ForbiddenHeavyBackdropMix = "var(--bg-deep) 34%";
    private const string ForbiddenOpaqueBackdropMix = "var(--bg-deep) 72%";

    private static readonly string ComponentStylesheetPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/AppShell/Components/AiSpotlightOverlay.razor.css"));

    [Test]
    public void SpotlightBackdrop_UsesTranslucentTreatment()
    {
        var stylesheet = File.ReadAllText(ComponentStylesheetPath);

        Assert.Contains(ExpectedBackdropOpacityMix, stylesheet, StringComparison.Ordinal);
        Assert.Contains(ExpectedNoBackdropBlur, stylesheet, StringComparison.Ordinal);
        Assert.Contains(ExpectedDesktopTopOffset, stylesheet, StringComparison.Ordinal);
        Assert.Contains(ExpectedMobileTopOffset, stylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(ForbiddenHeavyBackdropMix, stylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(ForbiddenOpaqueBackdropMix, stylesheet, StringComparison.Ordinal);
    }
}
