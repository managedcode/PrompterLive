using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class PrepModePresentationFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const string Scenario = "prep-mode-presentation";
    private const string PrepLaunchStep = "library-prep-launch";

    [Test]
    public Task LibraryPrepAction_OpensAdditivePrepRoute_AndLearnRouteRemainsAvailable() =>
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
                .ToHaveTextAsync(BrowserTestConstants.Library.PrepActionLabel);
            await Expect(page.GetByTestId(UiTestIds.Library.CardRead(BrowserTestConstants.Scripts.QuantumId)))
                .ToHaveTextAsync(BrowserTestConstants.Library.TeleprompterActionLabel);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CardPrep(BrowserTestConstants.Scripts.QuantumId)),
                noWaitAfter: true);
            await PlaybackRouteDriver.WaitForLearnReadyAsync(page, BrowserTestConstants.Routes.PrepQuantum);
            await Expect(page.GetByTestId(UiTestIds.Learn.NotesPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(page, Scenario, PrepLaunchStep);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.Back),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await PlaybackRouteDriver.OpenLearnAsync(
                page,
                BrowserTestConstants.Routes.LearnQuantum,
                $"{Scenario}-{UiTestIds.Learn.Page}");
        });
}
