using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorLocalHistoryFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    [Test]
    public Task EditorScreen_LocalHistoryPersistsAcrossReload_AndRestoresOlderRevision() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.LocalHistoryScenario);

            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(
                page,
                BrowserTestConstants.Scripts.DemoId,
                setSeedTitle: false);
            await page.GetByTestId(UiTestIds.Editor.ToolsTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.ToolsPanel)).ToBeVisibleAsync();

            var sourceInput = EditorMonacoDriver.SourceInput(page);
            var initialText = await sourceInput.InputValueAsync();
            var firstRevisionText = string.Concat(initialText, "\n", BrowserTestConstants.Editor.LocalHistoryFirstLine);
            var secondRevisionText = string.Concat(firstRevisionText, "\n", BrowserTestConstants.Editor.LocalHistorySecondLine);

            await EditorMonacoDriver.SetTextAsync(page, firstRevisionText);
            await Expect(page.GetByTestId(UiTestIds.Editor.LocalHistoryItem(BrowserTestConstants.Editor.LocalHistoryPreviousRevisionIndex)))
                .ToBeVisibleAsync();

            await EditorMonacoDriver.SetTextAsync(page, secondRevisionText);
            await Expect(page.GetByTestId(UiTestIds.Editor.LocalHistoryItem(BrowserTestConstants.Editor.LocalHistoryOriginalRevisionIndex)))
                .ToBeVisibleAsync();
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.LocalHistoryScenario,
                BrowserTestConstants.EditorFlow.LocalHistorySavedStep);

            await page.ReloadAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await page.GetByTestId(UiTestIds.Editor.ToolsTab).ClickAsync();
            await Expect(sourceInput).ToHaveValueAsync(secondRevisionText);

            await page.GetByTestId(UiTestIds.Editor.LocalHistoryRestore(BrowserTestConstants.Editor.LocalHistoryPreviousRevisionIndex))
                .ClickAsync();
            await Expect(sourceInput).ToHaveValueAsync(firstRevisionText);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.LocalHistoryScenario,
                BrowserTestConstants.EditorFlow.LocalHistoryRestoredStep);

            await page.ReloadAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await page.GetByTestId(UiTestIds.Editor.ToolsTab).ClickAsync();
            await Expect(sourceInput).ToHaveValueAsync(firstRevisionText);
        });

    [Test]
    public Task EditorScreen_AutosaveToggleControlsWhetherEditsSurviveReload() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.LocalHistoryAutosaveScenario);

            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(
                page,
                BrowserTestConstants.Scripts.DemoId,
                setSeedTitle: false);
            var draftUrl = page.Url;
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            var originalText = await sourceInput.InputValueAsync();

            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await page.GetByTestId(UiTestIds.Settings.NavFiles).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.FilesPanel)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.FileAutoSave).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.FileAutoSave))
                .ToHaveAttributeAsync(BrowserTestConstants.State.EnabledAttribute, BrowserTestConstants.State.DisabledValue);

            await page.GotoAsync(draftUrl);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await page.GetByTestId(UiTestIds.Editor.ToolsTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.ToolsPanel)).ToBeVisibleAsync();

            var unsavedText = string.Concat(originalText, "\n", BrowserTestConstants.Editor.LocalHistoryUnsavedLine);

            await EditorMonacoDriver.SetTextAsync(page, unsavedText);
            await page.WaitForTimeoutAsync(BrowserTestConstants.Editor.LocalHistoryAutosaveObservationDelayMs);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.LocalHistoryAutosaveScenario,
                BrowserTestConstants.EditorFlow.LocalHistoryAutosaveDisabledStep);

            await page.ReloadAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(sourceInput).ToHaveValueAsync(originalText);

            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await page.GetByTestId(UiTestIds.Settings.NavFiles).ClickAsync();
            await page.GetByTestId(UiTestIds.Settings.FileAutoSave).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.FileAutoSave))
                .ToHaveAttributeAsync(BrowserTestConstants.State.EnabledAttribute, BrowserTestConstants.State.EnabledValue);

            await page.GotoAsync(draftUrl);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await page.GetByTestId(UiTestIds.Editor.ToolsTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.ToolsPanel)).ToBeVisibleAsync();

            var resavedText = string.Concat(
                await sourceInput.InputValueAsync(),
                "\n",
                BrowserTestConstants.Editor.LocalHistoryResavedLine);

            await EditorMonacoDriver.SetTextAsync(page, resavedText);
            await Expect(page.GetByTestId(UiTestIds.Editor.LocalHistoryItem(BrowserTestConstants.Editor.LocalHistoryPreviousRevisionIndex)))
                .ToBeVisibleAsync();
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.LocalHistoryAutosaveScenario,
                BrowserTestConstants.EditorFlow.LocalHistoryAutosaveEnabledStep);

            await page.ReloadAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(sourceInput).ToHaveValueAsync(resavedText);
        });
}
