using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class EditorStructureAuthoringTests : BunitContext
{
    private readonly AppHarness _harness;

    public EditorStructureAuthoringTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void EditorPage_ChangingSourceHeadersRefreshesStructureTreeWithoutLegacyInspector()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/editor?id=quantum-computing");
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Quantum Computing", cut.Markup);
            Assert.Contains("Introduction", cut.Markup);
            Assert.DoesNotContain("ACTIVE SEGMENT", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("ACTIVE BLOCK", cut.Markup, StringComparison.Ordinal);
        });

        var source = cut.Find("[data-testid='editor-source-input']");
        var updatedSource = (source.GetAttribute("value") ?? string.Empty)
            .Replace("## [Introduction|280WPM|neutral|0:00-1:10]", "## [Launch Angle|305WPM|focused|1:00-2:00]", StringComparison.Ordinal)
            .Replace("### [Overview Block|280WPM|neutral]", "### [Signal Block|305WPM|professional]", StringComparison.Ordinal);

        source.Input(updatedSource);

        cut.WaitForAssertion(() =>
        {
            var currentSource = cut.Find("[data-testid='editor-source-input']").GetAttribute("value");
            Assert.Contains("## [Launch Angle|305WPM|focused|1:00-2:00]", currentSource);
            Assert.Contains("### [Signal Block|305WPM|professional]", currentSource);
            Assert.Contains("data-nav=\"seg-0\"", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Launch Angle", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Signal Block", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("ACTIVE SEGMENT", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void EditorPage_ChangingSpeedOffsetsRewritesFrontMatter()
    {
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Product Launch", cut.Markup);
            Assert.Equal("-40", cut.Find("[data-testid='editor-speed-xslow']").GetAttribute("value"));
        });

        cut.Find("[data-testid='editor-speed-xslow']").Change("-45");
        cut.Find("[data-testid='editor-speed-slow']").Change("-15");
        cut.Find("[data-testid='editor-speed-fast']").Change("30");
        cut.Find("[data-testid='editor-speed-xfast']").Change("55");

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.Find("[data-testid='editor-source-input']").GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.DoesNotContain("xslow_offset:", visibleSource, StringComparison.Ordinal);
            Assert.Contains("xslow_offset: -45", persistedText, StringComparison.Ordinal);
            Assert.Contains("slow_offset: -15", persistedText, StringComparison.Ordinal);
            Assert.Contains("fast_offset: 30", persistedText, StringComparison.Ordinal);
            Assert.Contains("xfast_offset: 55", persistedText, StringComparison.Ordinal);
        });
    }
}
