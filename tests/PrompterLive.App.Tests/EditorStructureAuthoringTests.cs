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
    public void EditorPage_ChangingActiveStructureRewritesTpsHeaders()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/editor?id=quantum-computing");
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Quantum Computing", cut.Markup);
            Assert.Equal("Introduction", cut.Find("[data-testid='editor-active-segment-name']").GetAttribute("value"));
        });

        cut.Find("[data-testid='editor-active-segment-name']").Change("Introduction");
        cut.Find("[data-testid='editor-active-segment-wpm']").Change("280");
        cut.Find("[data-testid='editor-active-segment-emotion']").Change("Neutral");
        cut.Find("[data-testid='editor-active-segment-timing']").Change("0:00-0:00");
        cut.Find("[data-testid='editor-active-block-name']").Change("Overview Block");
        cut.Find("[data-testid='editor-active-block-wpm']").Change("280");
        cut.Find("[data-testid='editor-active-block-emotion']").Change("Neutral");

        cut.WaitForAssertion(() =>
        {
            var source = cut.Find("[data-testid='editor-source-input']").GetAttribute("value");
            Assert.Contains("## [Introduction|280WPM|neutral|0:00-0:00]", source);
            Assert.Contains("### [Overview Block|280WPM|neutral]", source);
            Assert.Contains("Overview Block", cut.Markup);
            Assert.Contains("280WPM", cut.Markup);
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
