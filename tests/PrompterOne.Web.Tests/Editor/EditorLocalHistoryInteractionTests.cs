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

[NotInParallel]
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

        cut.FindByTestId(UiTestIds.Editor.ToolsTab).Click();

        var updatedSource = string.Concat(
            cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"),
            Environment.NewLine,
            EditorLocalHistoryInteractionTestSource.LocalOnlyLine);

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(updatedSource);

        cut.WaitForAssertion(() =>
        {
            var persistedDocument = _harness.Repository
                .GetAsync(_harness.Session.State.ScriptId)
                .GetAwaiter()
                .GetResult();

            Assert.Equal(updatedSource, cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value"));
            Assert.Empty(LoadLocalHistoryEntries());
            Assert.DoesNotContain(
                EditorLocalHistoryInteractionTestSource.LocalOnlyLine,
                persistedDocument?.Text ?? string.Empty,
                StringComparison.Ordinal);
            Assert.Contains(UiTestIds.Editor.LocalHistoryEmpty, cut.Markup, StringComparison.Ordinal);
        }, EditorLocalHistoryInteractionTestSource.LocalHistoryAssertionTimeout);
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

        cut.FindByTestId(UiTestIds.Editor.ToolsTab).Click();

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
        cut.WaitForAssertion(() =>
        {
            Assert.Single(LoadLocalHistoryEntries());
            Assert.Contains(
                EditorLocalHistoryInteractionTestSource.FirstRevisionLine,
                _harness.Session.State.Text,
                StringComparison.Ordinal);
        }, EditorLocalHistoryInteractionTestSource.LocalHistoryAssertionTimeout);

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(secondRevisionSource);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, LoadLocalHistoryEntries().Count);
            Assert.Contains(UiTestIds.Editor.LocalHistoryPanel, cut.Markup, StringComparison.Ordinal);
            Assert.Contains(UiTestIds.Editor.LocalHistoryDiff(1), cut.Markup, StringComparison.Ordinal);
            Assert.Contains(UiTestIds.Editor.LocalHistoryRestore(1), cut.Markup, StringComparison.Ordinal);
            Assert.Equal(
                "+0 / -1",
                cut.FindByTestId(UiTestIds.Editor.LocalHistoryDiffSummary(1)).TextContent.Trim());
            Assert.Contains(
                EditorLocalHistoryInteractionTestSource.SecondRevisionLine,
                cut.FindByTestId(UiTestIds.Editor.LocalHistoryDiff(1)).TextContent,
                StringComparison.Ordinal);
            Assert.Contains(
                EditorLocalHistoryInteractionTestSource.SecondRevisionLine,
                _harness.Session.State.Text,
                StringComparison.Ordinal);
        }, EditorLocalHistoryInteractionTestSource.LocalHistoryAssertionTimeout);

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
        }, EditorLocalHistoryInteractionTestSource.LocalHistoryAssertionTimeout);
    }

    private IReadOnlyList<EditorLocalRevisionRecord> LoadLocalHistoryEntries() =>
        Services.GetRequiredService<EditorLocalRevisionStore>()
            .LoadAsync(_harness.Session.State.ScriptId)
            .GetAwaiter()
            .GetResult();

    private static class EditorLocalHistoryInteractionTestSource
    {
        public static readonly TimeSpan LocalHistoryAssertionTimeout = TimeSpan.FromSeconds(10);
        public const string FirstRevisionLine = "The first browser-local revision should survive.";
        public const string LocalOnlyLine = "This line must remain local when autosave is off.";
        public const string SecondRevisionLine = "The second browser-local revision should be restorable.";
    }
}
