using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Learn.Services;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LearnKeyboardShortcutTests : BunitContext
{
    private const string PressedAttributeName = "aria-pressed";
    private const string PrepNoteText = "Pause before the KPI sentence.";
    private const string TrueValue = "true";

    [Test]
    public void LearnPage_EscapeShortcut_NavigatesBackToEditor()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.LearnQuantum);

        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.Learn.Page);
            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.Escape });

            Assert.EndsWith(AppTestData.Routes.EditorQuantum, Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
        });
    }

    [Test]
    public void LearnPage_LShortcut_TogglesLoopPlayback()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.LearnQuantum);

        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.Learn.Page);
            var loopToggle = cut.FindByTestId(UiTestIds.Learn.LoopToggle);

            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.LLower });

            Assert.Equal(TrueValue, loopToggle.GetAttribute(PressedAttributeName));
        });
    }

    [Test]
    public void LearnPage_SpaceShortcut_TogglesPlayback()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.LearnQuantum);

        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.Learn.Page);
            var playToggle = cut.FindByTestId(UiTestIds.Learn.PlayToggle);

            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.Space });

            Assert.Equal(TrueValue, playToggle.GetAttribute(PressedAttributeName));
        });
    }

    [Test]
    public void LearnPage_PrepNotes_AddsSectionNoteWithoutChangingScriptSource()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.LearnQuantum);

        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.NotesPanel));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.NotesTextarea));
        });

        var sourceBeforeNote = Services.GetRequiredService<IScriptSessionService>().State.Text;
        cut.FindByTestId(UiTestIds.Learn.NotesTextarea).Input(PrepNoteText);
        cut.FindByTestId(UiTestIds.Learn.NotesSave).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(PrepNoteText, cut.FindByTestId(UiTestIds.Learn.NotesList).TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain(PrepNoteText, Services.GetRequiredService<IScriptSessionService>().State.Text, StringComparison.Ordinal);
            Assert.Equal(sourceBeforeNote, Services.GetRequiredService<IScriptSessionService>().State.Text);

            var savedNotes = harness.JsRuntime.GetSavedValue<LearnPrepNotesSnapshot>(BrowserAppSettingsKeys.LearnPrepNotes);
            Assert.Contains(savedNotes.Notes, note =>
                string.Equals(note.Text, PrepNoteText, StringComparison.Ordinal) &&
                string.Equals(note.ScriptKey, AppTestData.Scripts.QuantumId, StringComparison.Ordinal));
        });
    }
}
