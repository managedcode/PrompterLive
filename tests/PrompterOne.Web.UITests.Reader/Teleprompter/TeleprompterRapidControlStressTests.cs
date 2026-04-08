using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterRapidControlStressTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const string RapidControlsScenario = "teleprompter-rapid-controls";
    private const string RapidControlsStep = "01-rapid-navigation";

    [Test]
    public Task Teleprompter_RapidNavigationClicks_DoNotTriggerFatalDiagnosticsOrUnhandledErrors() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(RapidControlsScenario);
            var errors = BrowserErrorCollector.Attach(page);

            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterLeadership);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await RapidControlDriver.BurstClickAsync(page, UiTestIds.Teleprompter.NextBlock, BrowserTestConstants.RapidInput.TeleprompterBlockBurstCount);
            await RapidControlDriver.BurstClickAsync(page, UiTestIds.Teleprompter.PreviousBlock, BrowserTestConstants.RapidInput.TeleprompterBlockBurstCount);
            await RapidControlDriver.BurstClickAsync(page, UiTestIds.Teleprompter.NextWord, BrowserTestConstants.RapidInput.TeleprompterWordBurstCount);
            await RapidControlDriver.BurstClickAsync(page, UiTestIds.Teleprompter.PreviousWord, BrowserTestConstants.RapidInput.TeleprompterWordBurstCount);
            await page.WaitForTimeoutAsync(BrowserTestConstants.RapidInput.PostBurstSettleDelayMs);

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.NextBlock)).ToBeVisibleAsync();
            await Assert.That(await page.GetByTestId(UiTestIds.Diagnostics.Fatal).CountAsync()).IsEqualTo(0);
            await errors.AssertNoCriticalUiErrorsAsync();

            await UiScenarioArtifacts.CapturePageAsync(page, RapidControlsScenario, RapidControlsStep);
        });
}
