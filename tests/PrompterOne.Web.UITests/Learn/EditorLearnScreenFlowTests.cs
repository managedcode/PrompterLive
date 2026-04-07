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
            await AiProviderTestSeeder.SeedConfiguredOpenAiAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync(BrowserTestConstants.Editor.BodyHeading);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Opening Block");
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Purpose Block");
            await page.GetByTestId(UiTestIds.Editor.FormatTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuFormat)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuColor)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.Bold).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.Ai)).ToBeEnabledAsync();
            await page.GetByTestId(UiTestIds.Editor.Ai).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.BlockNavigation(2, 1)).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(2, 1))).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(2))).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Benefits Block");

            await Expect(page.GetByTestId(UiTestIds.Header.EditorLearn)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Header.EditorLearn).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnDemo));
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GotoAsync(BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Header.Center))
                .ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle, new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase)).Not.ToHaveTextAsync(string.Empty);
            await page.GetByTestId(UiTestIds.Learn.SpeedUp).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Learn.SpeedValue)).ToHaveTextAsync(BrowserTestConstants.EditorFlow.LearnSpeedAfterIncrease);
            await page.GetByTestId(UiTestIds.Learn.StepBackward).ClickAsync();
            await page.GetByTestId(UiTestIds.Learn.StepForward).ClickAsync();
            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Learn.PlayToggle)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase)).Not.ToHaveTextAsync(string.Empty);
        });
}
