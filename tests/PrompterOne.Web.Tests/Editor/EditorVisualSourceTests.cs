using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorVisualSourceTests : BunitContext
{
    private readonly AppHarness _harness;

    public EditorVisualSourceTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
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

    [Test]
    public void EditorPage_BodyEditsPreserveMetadataInPersistedDocument()
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

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.DoesNotContain(EditorVisualTestSource.AuthorFieldPrefix, visibleSource, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.FrontMatterFence, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.AuthorPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.HighlightedLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.CallToActionHeading, persistedText, StringComparison.Ordinal);
        }, TimeSpan.FromMilliseconds(EditorVisualTestSource.AutosaveAssertionTimeout));
    }

    [Test]
    public async Task EditorPage_StyledEditorViewUsesMonacoBackedSourceSurface()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        cut.WaitForAssertion(() =>
        {
            var mainPanel = cut.FindByTestId(UiTestIds.Editor.MainPanel);
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);

            Assert.Equal(UiTestIds.Editor.StyledAuthoringMode, mainPanel.GetAttribute("data-authoring-mode"));
            Assert.Contains("active", cut.FindByTestId(UiTestIds.Editor.WorkspaceEditorTab).GetAttribute("class"), StringComparison.Ordinal);
            Assert.DoesNotContain(UiTestIds.Editor.LegacyRenderedView, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("editor-rendered-card", cut.Markup, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.RenderedOpeningProbe, source.GetAttribute("value"), StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.RawSegmentPrefix, source.GetAttribute("value"), StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.RawTagOpen, source.GetAttribute("value"), StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.RawTagClose, source.GetAttribute("value"), StringComparison.Ordinal);
        });

        var nextSource = (cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty)
            .Replace(EditorVisualTestSource.RenderedOpeningProbe, EditorVisualTestSource.RenderedOpeningRewrite, StringComparison.Ordinal);
        await cut.FindByTestId(UiTestIds.Editor.SourceInput).InputAsync(nextSource);
        await cut.FindByTestId(UiTestIds.Editor.SourceTab).ClickAsync();

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.Contains(EditorVisualTestSource.RenderedOpeningRewrite, visibleSource, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.OpeningBlockHeading, visibleSource, StringComparison.Ordinal);
            Assert.Contains(AppTestData.Editor.BodyHeading, visibleSource, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.FrontMatterFence, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorVisualTestSource.RenderedOpeningRewrite, persistedText, StringComparison.Ordinal);
        }, TimeSpan.FromMilliseconds(EditorVisualTestSource.AutosaveAssertionTimeout));
    }

    [Test]
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
        public const string OpeningBlockHeading = "### [Opening Block|140WPM]";
        public const string RawSegmentPrefix = "##";
        public const string RawTagClose = "]";
        public const string RawTagOpen = "[";
        public const string RenderedOpeningProbe = "Good morning everyone";
        public const string RenderedOpeningRewrite = "Good morning everyone, welcome from Editor mode.";
        public const int IntroSegmentIndex = 0;
        public const int OpeningBlockIndex = 0;
        public const int AutosaveAssertionTimeout = 5_000;
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
