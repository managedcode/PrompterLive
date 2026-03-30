using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PrompterLive.Shared.Contracts;
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
    public async Task EditorPage_UsesVisibleBodyTextareaAndRebuildsStructureWhenSourceChanges()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        var updatedSource =
            """
            ## [Fresh Opening|160WPM|focused]
            ### [Renamed Block|160WPM]
            Fresh copy for the editor runtime.
            """;

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(updatedSource);

        await Task.Delay(EditorSourceInteractionTestSource.PostDraftAnalysisObservationDelay);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.Contains("Fresh Opening", cut.Markup);
            Assert.Contains("Renamed Block", cut.Markup);
            Assert.Contains(EditorSourceInteractionTestSource.SingleSegmentLabel, cut.Markup);
        });

        await Task.Delay(EditorSourceInteractionTestSource.PostAutosaveObservationDelay);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(EditorSourceInteractionTestSource.TitlePersistenceLine, _harness.Session.State.Text, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.AuthorPersistenceLine, _harness.Session.State.Text, StringComparison.Ordinal);
            Assert.Contains(updatedSource, _harness.Session.State.Text, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void EditorPage_MetadataChangesRewritePersistedFrontMatterWithoutLeakingIntoVisibleBody()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.Profile).Change(EditorSourceInteractionTestSource.ProfileRsvp);
        cut.FindByTestId(UiTestIds.Editor.BaseWpm).Change(EditorSourceInteractionTestSource.BaseWpm210);
        cut.FindByTestId(UiTestIds.Editor.Author).Change(AppTestData.Editor.TestSpeaker);
        cut.FindByTestId(UiTestIds.Editor.Created).Change(AppTestData.Editor.CreatedDate);
        cut.FindByTestId(UiTestIds.Editor.Version).Change(AppTestData.Editor.Version);

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.DoesNotContain(EditorSourceInteractionTestSource.ProfileField, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorSourceInteractionTestSource.AuthorField, visibleSource, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.ProfilePersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.BaseWpmPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.TestSpeakerPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.CreatedPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.VersionPersistenceLine, persistedText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task EditorPage_HistoryButtonsReplaySourceChanges()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        var sourceEditor = cut.FindByTestId(UiTestIds.Editor.SourceInput);
        var initialSource = sourceEditor.GetAttribute("value")!;
        var updatedSource = string.Concat(initialSource, Environment.NewLine, EditorSourceInteractionTestSource.EditPointToken);

        sourceEditor.Input(updatedSource);

        await Task.Delay(EditorSourceInteractionTestSource.PostDraftAnalysisObservationDelay);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.Undo).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(initialSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.Redo).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        });
    }

    [Fact]
    public async Task EditorPage_TypingStaysLocalFirstUntilAutosaveDebounce()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        var sourceEditor = cut.FindByTestId(UiTestIds.Editor.SourceInput);
        var initialSource = sourceEditor.GetAttribute("value")!;
        var initialSessionWordCount = _harness.Session.State.WordCount;
        var updatedSource = string.Concat(
            initialSource,
            Environment.NewLine,
            EditorSourceInteractionTestSource.LocalFirstTypingLine);

        sourceEditor.Input(updatedSource);

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(
                EditorSourceInteractionTestSource.LocalFirstTypingLine,
                _harness.Session.State.Text,
                StringComparison.Ordinal);
            Assert.Equal(initialSessionWordCount, _harness.Session.State.WordCount);
        });

        await Task.Delay(EditorSourceInteractionTestSource.PostDraftAnalysisObservationDelay);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.Contains(EditorSourceInteractionTestSource.LocalFirstTypingLine, cut.Markup, StringComparison.Ordinal);
            Assert.Equal(initialSessionWordCount, _harness.Session.State.WordCount);
        });

        await Task.Delay(EditorSourceInteractionTestSource.PostAutosaveObservationDelay);

        cut.WaitForAssertion(() =>
        {
            Assert.True(_harness.Session.State.WordCount > initialSessionWordCount);
        });
    }

    [Fact]
    public async Task EditorPage_KeyboardUndoAndRedoReplaySourceChanges()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        var sourceEditor = cut.FindByTestId(UiTestIds.Editor.SourceInput);
        var initialSource = sourceEditor.GetAttribute("value")!;
        var updatedSource = string.Concat(initialSource, Environment.NewLine, EditorSourceInteractionTestSource.EditPointToken);

        sourceEditor.Input(updatedSource);

        await Task.Delay(EditorSourceInteractionTestSource.PostDraftAnalysisObservationDelay);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        });

        sourceEditor.TriggerEvent("onkeydown", new KeyboardEventArgs
        {
            CtrlKey = true,
            Key = EditorSourceInteractionTestSource.UndoKey
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(initialSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        });

        sourceEditor.TriggerEvent("onkeydown", new KeyboardEventArgs
        {
            CtrlKey = true,
            Key = EditorSourceInteractionTestSource.RedoKey
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        });
    }

    [Fact]
    public void EditorPage_ColorMenuIncludesClearAction()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.ColorTrigger).Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.ColorClear)));
    }

    private static class EditorSourceInteractionTestSource
    {
        public const string AuthorField = "author:";
        public const string AuthorPersistenceLine = "author: \"Jane Doe\"";
        public const string BaseWpm210 = "210";
        public const string BaseWpmPersistenceLine = "base_wpm: 210";
        public const string CreatedPersistenceLine = "created: \"2026-03-26\"";
        public const string EditPointToken = "[edit_point]";
        public const string ProfileField = "profile:";
        public const string ProfilePersistenceLine = "profile: \"RSVP\"";
        public const string ProfileRsvp = "RSVP";
        public const string RedoKey = "y";
        public const int PostDraftAnalysisObservationDelay = 1_200;
        public const int PostAutosaveObservationDelay = 1_700;
        public const string SingleSegmentLabel = "1 Segments";
        public const string TestSpeakerPersistenceLine = "author: \"Test Speaker\"";
        public const string TitlePersistenceLine = "title: \"Product Launch\"";
        public const string UndoKey = "z";
        public const string VersionPersistenceLine = "version: \"2.0\"";
        public const string LocalFirstTypingLine = "steady local typing proof";
    }
}
