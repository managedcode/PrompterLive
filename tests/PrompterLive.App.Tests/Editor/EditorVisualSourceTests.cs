using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterLive.Shared.Contracts;
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
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var highlightedText = cut.FindByTestId(UiTestIds.Editor.SourceHighlight).TextContent;

            Assert.DoesNotContain(EditorVisualTestSource.FrontMatterFence, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorVisualTestSource.TitleFieldPrefix, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorVisualTestSource.AuthorFieldPrefix, visibleSource, StringComparison.Ordinal);
            Assert.StartsWith(AppTestData.Editor.BodyHeading, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorVisualTestSource.TitleFieldPrefix, highlightedText, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorVisualTestSource.AuthorFieldPrefix, highlightedText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task EditorPage_BodyEditsPreserveMetadataInPersistedDocument()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.Author).Change(AppTestData.Editor.TestSpeaker);
        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(EditorVisualTestSource.RewrittenBody);

        await Task.Delay(EditorVisualTestSource.PostAutosaveObservationDelay);

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.DoesNotContain(EditorVisualTestSource.AuthorFieldPrefix, visibleSource, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.FrontMatterFence, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.AuthorPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.HighlightedLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.CallToActionHeading, persistedText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void EditorPage_DoesNotRenderInventedAiPanelSurface()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(EditorVisualTestSource.LegacyAiPanelClass, cut.Markup, StringComparison.Ordinal);
        });
    }

    private static class EditorVisualTestSource
    {
        public const string AuthorFieldPrefix = "author:";
        public const string TitleFieldPrefix = "title:";
        public const string FrontMatterFence = "---";
        public const string LegacyAiPanelClass = "ed-ai-panel";
        public const string AuthorPersistenceLine = "author: \"Test Speaker\"";
        public const string HighlightedLine = "[highlight]Stay with us[/highlight]";
        public const string CallToActionHeading = "## [Call to Action|150WPM|motivational]";
        public const int PostAutosaveObservationDelay = 1_700;
        public const string RewrittenBody =
            """
            ## [Intro|140WPM|warm]
            ### [Opening Block|140WPM]
            Good morning everyone, / and [emphasis]welcome[/emphasis] to the new platform. //

            ## [Call to Action|150WPM|motivational]
            ### [Closing Block|150WPM]
            [highlight]Stay with us[/highlight] through the final reveal.
            """;
    }
}
