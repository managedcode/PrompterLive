using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

[NotInParallel]
public sealed class EditorSourceInteractionTests : BunitContext
{
    private readonly AppHarness _harness;

    public EditorSourceInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public void EditorPage_UsesVisibleBodyTextareaAndRebuildsStructureWhenSourceChanges()
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

        cut.WaitForAssertion(() =>
        {
            var segmentsStatus = cut.FindByTestId(UiTestIds.Editor.StatusSegments).TextContent;

            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.Contains("Fresh Opening", cut.Markup);
            Assert.Contains("Renamed Block", cut.Markup);
            Assert.Contains(EditorSourceInteractionTestSource.SingleSegmentCount, segmentsStatus, StringComparison.Ordinal);
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.AutosaveAssertionTimeout));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(EditorSourceInteractionTestSource.TitlePersistenceLine, _harness.Session.State.Text, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.AuthorPersistenceLine, _harness.Session.State.Text, StringComparison.Ordinal);
            Assert.Contains(updatedSource, _harness.Session.State.Text, StringComparison.Ordinal);
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.AutosaveAssertionTimeout));
    }

    [Test]
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

    [Test]
    public void EditorPage_HistoryButtonsReplaySourceChanges()
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

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.AutosaveAssertionTimeout));

        cut.WaitForAssertion(() =>
        {
            Assert.Null(cut.FindByTestId(UiTestIds.Editor.Undo).GetAttribute("disabled"));
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.HistoryAssertionTimeout));

        cut.FindByTestId(UiTestIds.Editor.Undo).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(initialSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.Null(cut.FindByTestId(UiTestIds.Editor.Redo).GetAttribute("disabled"));
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.HistoryAssertionTimeout));

        cut.FindByTestId(UiTestIds.Editor.Redo).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.HistoryAssertionTimeout));
    }

    [Test]
    public void EditorPage_TypingStaysLocalFirstUntilAutosaveDebounce()
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

        Assert.DoesNotContain(
            EditorSourceInteractionTestSource.LocalFirstTypingLine,
            _harness.Session.State.Text,
            StringComparison.Ordinal);
        Assert.Equal(initialSessionWordCount, _harness.Session.State.WordCount);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.Contains(EditorSourceInteractionTestSource.LocalFirstTypingLine, cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.AutosaveAssertionTimeout));

        cut.WaitForState(
            () => _harness.Session.State.WordCount > initialSessionWordCount,
            TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.AutosaveAssertionTimeout));

        Assert.True(_harness.Session.State.WordCount > initialSessionWordCount);
    }

    [Test]
    public void EditorPage_PastedFrontMatterHydratesMetadataAndKeepsVisibleBodyClean()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(EditorSourceInteractionTestSource.ImportedFrontMatterDocument);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedBodyOnly,
                cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedProfile,
                cut.FindByTestId(UiTestIds.Editor.Profile).GetAttribute("value"));
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedBaseWpm,
                cut.FindByTestId(UiTestIds.Editor.BaseWpm).GetAttribute("value"));
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedDuration,
                cut.FindByTestId(UiTestIds.Editor.Duration).GetAttribute("value"));
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedAuthor,
                cut.FindByTestId(UiTestIds.Editor.Author).GetAttribute("value"));
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedCreatedDate,
                cut.FindByTestId(UiTestIds.Editor.Created).GetAttribute("value"));
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedVersion,
                cut.FindByTestId(UiTestIds.Editor.Version).GetAttribute("value"));
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedSlowOffset,
                cut.FindByTestId(UiTestIds.Editor.SpeedSlow).GetAttribute("value"));
            Assert.Equal(
                EditorSourceInteractionTestSource.ImportedFastOffset,
                cut.FindByTestId(UiTestIds.Editor.SpeedFast).GetAttribute("value"));
        });

        cut.WaitForAssertion(() =>
        {
            var persistedText = _harness.Session.State.Text;

            Assert.Contains(EditorSourceInteractionTestSource.ImportedTitlePersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.ImportedDurationPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.ImportedAuthorPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.ImportedCreatedPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.ImportedVersionPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.ImportedSlowOffsetPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.ImportedFastOffsetPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorSourceInteractionTestSource.ImportedBodyOnly, persistedText, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorSourceInteractionTestSource.FrontMatterDelimiterWithTitle, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"), StringComparison.Ordinal);
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.AutosaveAssertionTimeout));
    }

    [Test]
    public void EditorPage_KeyboardUndoAndRedoReplaySourceChanges()
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

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.AutosaveAssertionTimeout));

        var currentSourceEditor = cut.FindByTestId(UiTestIds.Editor.SourceInput);

        currentSourceEditor.TriggerEvent("onkeydown", new KeyboardEventArgs
        {
            CtrlKey = true,
            Key = EditorSourceInteractionTestSource.UndoKey
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(initialSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.HistoryAssertionTimeout));

        currentSourceEditor = cut.FindByTestId(UiTestIds.Editor.SourceInput);

        currentSourceEditor.TriggerEvent("onkeydown", new KeyboardEventArgs
        {
            CtrlKey = true,
            Key = EditorSourceInteractionTestSource.RedoKey
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
        }, TimeSpan.FromMilliseconds(EditorSourceInteractionTestSource.HistoryAssertionTimeout));
    }

    [Test]
    public void EditorPage_VoiceMenuIncludesInlineResetAction()
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
        public const int AutosaveAssertionTimeout = 5_000;
        public const int HistoryAssertionTimeout = 15_000;
        public const string AuthorField = "author:";
        public const string AuthorPersistenceLine = "author: \"Jane Doe\"";
        public const string BaseWpm210 = "210";
        public const string BaseWpmPersistenceLine = "base_wpm: 210";
        public const string CreatedPersistenceLine = "created: \"2026-03-26\"";
        public const string EditPointToken = "[edit_point]";
        public const string FrontMatterDelimiterWithTitle = "---\ntitle:";
        public const string ImportedAuthor = "Konstantin Semenenko";
        public const string ImportedAuthorPersistenceLine = "author: \"Konstantin Semenenko\"";
        public const string ImportedBaseWpm = "140";
        public const string ImportedBodyOnly =
            """
            ## [Architecture Intro|140WPM|focused]
            ### [Structure Block|140WPM]
            Keep the body in the visible editor only.
            """;
        public const string ImportedCreatedDate = "2026-03-25";
        public const string ImportedCreatedPersistenceLine = "created: \"2026-03-25\"";
        public const string ImportedDuration = "145:00";
        public const string ImportedDurationPersistenceLine = "duration: \"145:00\"";
        public const string ImportedFastOffset = "14";
        public const string ImportedFastOffsetPersistenceLine = "  fast: 14";
        public const string ImportedFrontMatterDocument =
            """
            ---
            title: "System Design and Software Architecture for Vibe Coders"
            profile: Actor
            duration: "145:00"
            base_wpm: 140
            speed_offsets:
              slow: -14
              fast: 14
            author: "Konstantin Semenenko"
            created: "2026-03-25"
            version: "1.0"
            ---

            ## [Architecture Intro|140WPM|focused]
            ### [Structure Block|140WPM]
            Keep the body in the visible editor only.
            """;
        public const string ImportedProfile = "Actor";
        public const string ImportedSlowOffset = "-14";
        public const string ImportedSlowOffsetPersistenceLine = "  slow: -14";
        public const string ImportedTitlePersistenceLine = "title: \"System Design and Software Architecture for Vibe Coders\"";
        public const string ImportedVersion = "1.0";
        public const string ImportedVersionPersistenceLine = "version: \"1.0\"";
        public const string ProfileField = "profile:";
        public const string ProfilePersistenceLine = "profile: \"RSVP\"";
        public const string ProfileRsvp = "RSVP";
        public const string RedoKey = "y";
        public const string SingleSegmentCount = "1";
        public const string TestSpeakerPersistenceLine = "author: \"Test Speaker\"";
        public const string TitlePersistenceLine = "title: \"Product Launch\"";
        public const string UndoKey = "z";
        public const string VersionPersistenceLine = "version: \"2.0\"";
        public const string LocalFirstTypingLine = "steady local typing proof";
    }
}
