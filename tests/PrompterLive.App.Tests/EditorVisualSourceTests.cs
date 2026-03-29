using Bunit;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class EditorVisualSourceTests : BunitContext
{
    private readonly AppHarness _harness;

    public EditorVisualSourceTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void EditorPage_HidesFrontMatterFromVisibleSourceSurface()
    {
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.Find("[data-testid='editor-source-input']").GetAttribute("value") ?? string.Empty;
            var highlightedText = cut.Find("[data-testid='editor-source-highlight']").TextContent;

            Assert.DoesNotContain("---", visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain("title:", visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain("author:", visibleSource, StringComparison.Ordinal);
            Assert.StartsWith("## [Intro|140WPM|warm]", visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain("title:", highlightedText, StringComparison.Ordinal);
            Assert.DoesNotContain("author:", highlightedText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void EditorPage_BodyEditsPreserveMetadataInPersistedDocument()
    {
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.Find("[data-testid='editor-author']").Change("Test Speaker");
        cut.Find("[data-testid='editor-source-input']").Input(
            """
            ## [Intro|140WPM|warm]
            ### [Opening Block|140WPM]
            Good morning everyone, / and [emphasis]welcome[/emphasis] to the new platform. //

            ## [Call to Action|150WPM|motivational]
            ### [Closing Block|150WPM]
            [highlight]Stay with us[/highlight] through the final reveal.
            """);

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.Find("[data-testid='editor-source-input']").GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.DoesNotContain("author:", visibleSource, StringComparison.Ordinal);
            Assert.Contains("---", persistedText, StringComparison.Ordinal);
            Assert.Contains("author: \"Test Speaker\"", persistedText, StringComparison.Ordinal);
            Assert.Contains("[highlight]Stay with us[/highlight]", persistedText, StringComparison.Ordinal);
            Assert.Contains("## [Call to Action|150WPM|motivational]", persistedText, StringComparison.Ordinal);
        });
    }
}
