using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LearnPrepNotesFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    [Test]
    public Task LearnPage_PrepNotes_AddPersistedSectionNoteWithoutChangingEditorSource() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.Learn.PrepNoteScenarioName);

            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesPanel))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesToggle))
                .ToHaveAttributeAsync("aria-expanded", bool.TrueString.ToLowerInvariant());

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.NotesToggle));
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesToggle))
                .ToHaveAttributeAsync("aria-expanded", bool.FalseString.ToLowerInvariant());
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesTextarea)).ToBeHiddenAsync();

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.NotesToggle));
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesToggle))
                .ToHaveAttributeAsync("aria-expanded", bool.TrueString.ToLowerInvariant());
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesTextarea)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Learn.NotesTextarea).FillAsync(BrowserTestConstants.Learn.PrepNoteText);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.NotesSave));

            await Expect(page.GetByTestId(UiTestIds.Learn.NotesList))
                .ToContainTextAsync(BrowserTestConstants.Learn.PrepNoteText);

            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesList))
                .ToContainTextAsync(BrowserTestConstants.Learn.PrepNoteText);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.Learn.PrepNoteScenarioName,
                BrowserTestConstants.Learn.PrepNoteStep);

            await EditorRouteDriver.OpenReadyAsync(
                page,
                BrowserTestConstants.Routes.EditorDemo,
                BrowserTestConstants.Learn.PrepNoteScenarioName);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight))
                .Not.ToContainTextAsync(BrowserTestConstants.Learn.PrepNoteText);
        });
}
