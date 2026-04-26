using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class PrepModePresentationFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const string Scenario = "practice-mode-presentation";
    private const string PracticeLaunchStep = "library-practice-launch";

    [Test]
    public Task LibraryPracticeAction_IsSingleRehearsalEntry_AndPrepRouteRedirects() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(Scenario);

            await page.SetViewportSizeAsync(
                BrowserTestConstants.ResponsiveLayout.IpadProHeight,
                BrowserTestConstants.ResponsiveLayout.IpadProWidth);

            await ShellRouteDriver.OpenLibraryAsync(page);

            await Expect(page.GetByTestId(UiTestIds.Library.CardLearn(BrowserTestConstants.Scripts.QuantumId)))
                .ToHaveTextAsync(BrowserTestConstants.Library.PracticeActionLabel);
            await Expect(page.GetByTestId(UiTestIds.Library.CardPrep(BrowserTestConstants.Scripts.QuantumId)))
                .ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Library.CardRead(BrowserTestConstants.Scripts.QuantumId)))
                .ToHaveTextAsync(BrowserTestConstants.Library.TeleprompterActionLabel);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CardLearn(BrowserTestConstants.Scripts.QuantumId)),
                noWaitAfter: true);
            await PlaybackRouteDriver.WaitForLearnReadyAsync(page, BrowserTestConstants.Routes.LearnQuantum);
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(page, Scenario, PracticeLaunchStep);

            await page.GotoAsync(BrowserTestConstants.Routes.PrepQuantum);
            await PlaybackRouteDriver.WaitForLearnReadyAsync(page, BrowserTestConstants.Routes.LearnQuantum);
        });
}
