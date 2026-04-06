using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services.Editor;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorLocalHistoryInteractionTests : BunitContext
{
    private readonly AppHarness _harness;

    public EditorLocalHistoryInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public async Task EditorPage_AutosaveDisabled_KeepsTypingLocalOnlyAfterDebounce()
    {
        await Services.GetRequiredService<BrowserFileStorageStore>()
            .SaveSettingsAsync(BrowserFileStorageSettings.Default with { FileAutoSaveEnabled = false });

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        var updatedSource = string.Concat(
            cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"),
            Environment.NewLine,
            EditorLocalHistoryInteractionTestSource.LocalOnlyLine);

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(updatedSource);

        await Task.Delay(EditorLocalHistoryInteractionTestSource.PostAutosaveObservationDelayMs);

        cut.WaitForAssertion(() =>
        {
            var persistedDocument = _harness.Repository
                .GetAsync(_harness.Session.State.ScriptId)
                .GetAwaiter()
                .GetResult();

            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.DoesNotContain(
                EditorLocalHistoryInteractionTestSource.LocalOnlyLine,
                persistedDocument?.Text ?? string.Empty,
                StringComparison.Ordinal);
            Assert.Contains(UiTestIds.Editor.LocalHistoryEmpty, cut.Markup, StringComparison.Ordinal);
        });
    }

    [Test]
    public async Task EditorPage_LocalHistoryRestore_ReappliesOlderAutosavedRevision()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        var initialSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value")!;
        var firstRevisionSource = string.Concat(
            initialSource,
            Environment.NewLine,
            EditorLocalHistoryInteractionTestSource.FirstRevisionLine);
        var secondRevisionSource = string.Concat(
            firstRevisionSource,
            Environment.NewLine,
            EditorLocalHistoryInteractionTestSource.SecondRevisionLine);

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(firstRevisionSource);
        await Task.Delay(EditorLocalHistoryInteractionTestSource.PostAutosaveObservationDelayMs);
        cut.WaitForAssertion(() =>
            Assert.Single(LoadLocalHistoryEntries()));

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(secondRevisionSource);
        await Task.Delay(EditorLocalHistoryInteractionTestSource.PostAutosaveObservationDelayMs);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, LoadLocalHistoryEntries().Count);
            Assert.Contains(UiTestIds.Editor.LocalHistoryPanel, cut.Markup, StringComparison.Ordinal);
            Assert.Contains(UiTestIds.Editor.LocalHistoryRestore(1), cut.Markup, StringComparison.Ordinal);
            Assert.Contains(
                EditorLocalHistoryInteractionTestSource.SecondRevisionLine,
                _harness.Session.State.Text,
                StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.Editor.LocalHistoryRestore(1)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(firstRevisionSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.Contains(
                EditorLocalHistoryInteractionTestSource.FirstRevisionLine,
                _harness.Session.State.Text,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                EditorLocalHistoryInteractionTestSource.SecondRevisionLine,
                _harness.Session.State.Text,
                StringComparison.Ordinal);
        });
    }

    private IReadOnlyList<EditorLocalRevisionRecord> LoadLocalHistoryEntries() =>
        Services.GetRequiredService<EditorLocalRevisionStore>()
            .LoadAsync(_harness.Session.State.ScriptId)
            .GetAwaiter()
            .GetResult();

    private static class EditorLocalHistoryInteractionTestSource
    {
        public const string FirstRevisionLine = "The first browser-local revision should survive.";
        public const string LocalOnlyLine = "This line must remain local when autosave is off.";
        public const int PostAutosaveObservationDelayMs = 1700;
        public const string SecondRevisionLine = "The second browser-local revision should be restorable.";
    }
}
