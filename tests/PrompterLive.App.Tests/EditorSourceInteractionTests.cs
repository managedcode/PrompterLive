using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class EditorSourceInteractionTests : BunitContext
{
    private readonly AppHarness _harness;

    public EditorSourceInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void EditorPage_UsesVisibleBodyTextareaAndRebuildsStructureWhenSourceChanges()
    {
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.Find("[data-testid='editor-source-input']");
            Assert.Contains("## [Intro|140WPM|warm]", source.GetAttribute("value"));
        });

        var updatedSource =
            """
            ## [Fresh Opening|160WPM|focused]
            ### [Renamed Block|160WPM]
            Fresh copy for the editor runtime.
            """;

        cut.Find("[data-testid='editor-source-input']").Input(updatedSource);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.Find("[data-testid='editor-source-input']").GetAttribute("value"));
            Assert.Contains("title: \"Product Launch\"", _harness.Session.State.Text, StringComparison.Ordinal);
            Assert.Contains("author: \"Jane Doe\"", _harness.Session.State.Text, StringComparison.Ordinal);
            Assert.Contains(updatedSource, _harness.Session.State.Text, StringComparison.Ordinal);
            Assert.Contains("Fresh Opening", cut.Markup);
            Assert.Contains("Renamed Block", cut.Markup);
            Assert.Contains("1 Segments", cut.Markup);
        });
    }

    [Fact]
    public void EditorPage_MetadataChangesRewritePersistedFrontMatterWithoutLeakingIntoVisibleBody()
    {
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.Find("[data-testid='editor-profile']").Change("RSVP");
        cut.Find("[data-testid='editor-base-wpm']").Change("210");
        cut.Find("[data-testid='editor-author']").Change("Test Speaker");
        cut.Find("[data-testid='editor-created']").Change("2026-03-26");
        cut.Find("[data-testid='editor-version']").Change("2.0");

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.Find("[data-testid='editor-source-input']").GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.DoesNotContain("profile:", visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain("author:", visibleSource, StringComparison.Ordinal);
            Assert.Contains("profile: \"RSVP\"", persistedText, StringComparison.Ordinal);
            Assert.Contains("base_wpm: 210", persistedText, StringComparison.Ordinal);
            Assert.Contains("author: \"Test Speaker\"", persistedText, StringComparison.Ordinal);
            Assert.Contains("created: \"2026-03-26\"", persistedText, StringComparison.Ordinal);
            Assert.Contains("version: \"2.0\"", persistedText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void EditorPage_HistoryButtonsReplaySourceChanges()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/editor?id=rsvp-tech-demo");
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.Find("[data-testid='editor-source-input']");
            Assert.Contains("## [Intro|140WPM|warm]", source.GetAttribute("value"));
        });

        var sourceEditor = cut.Find("[data-testid='editor-source-input']");
        var initialSource = sourceEditor.GetAttribute("value")!;
        var updatedSource = $"{initialSource}\n[edit_point]";

        sourceEditor.Input(updatedSource);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.Find("[data-testid='editor-source-input']").GetAttribute("value"));
        });

        cut.Find("[data-testid='editor-undo']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(initialSource, cut.Find("[data-testid='editor-source-input']").GetAttribute("value"));
        });

        cut.Find("[data-testid='editor-redo']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.Find("[data-testid='editor-source-input']").GetAttribute("value"));
        });
    }
}
