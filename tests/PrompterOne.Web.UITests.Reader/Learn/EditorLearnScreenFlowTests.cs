using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorLearnScreenFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    [Test]
    public Task EditorAndLearnScreens_ExposeExpectedInteractiveControls() =>
        RunPageAsync(async page =>
        {
            await EditorRouteDriver.OpenReadyAsync(
                page,
                BrowserTestConstants.Routes.EditorDemo,
                "editor-learn-screen-flow-editor-demo");
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync(BrowserTestConstants.Editor.BodyHeading);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Opening Block");
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Purpose Block");
            await page.GetByTestId(UiTestIds.Editor.FormatTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuFormat)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuColor)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.Bold).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.AiSpotlight)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Header.AiSpotlight).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.AiSpotlight.Overlay)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.AiSpotlight.Close).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.BlockNavigation(2, 1)).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(2, 1))).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(2))).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Benefits Block");

            await Expect(page.GetByTestId(UiTestIds.Header.EditorLearn)).ToBeVisibleAsync();
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.EditorLearn),
                noWaitAfter: true);
            await PlaybackRouteDriver.WaitForLearnReadyAsync(page, BrowserTestConstants.Routes.LearnDemo);

            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Header.Center))
                .ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle, new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase)).Not.ToHaveTextAsync(string.Empty);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.SpeedUp));
            await Expect(page.GetByTestId(UiTestIds.Learn.SpeedValue)).ToHaveTextAsync(BrowserTestConstants.EditorFlow.LearnSpeedAfterIncrease);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.StepBackward));
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.StepForward));
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.PlayToggle));
            await Expect(page.GetByTestId(UiTestIds.Learn.PlayToggle)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase)).Not.ToHaveTextAsync(string.Empty);
        });
}
