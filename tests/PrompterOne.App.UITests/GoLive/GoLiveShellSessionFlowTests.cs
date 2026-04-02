using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class GoLiveShellSessionFlowTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task GoLivePage_StartStream_LeavesPersistentWidgetAndReturnsToActiveSession()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await page.EvaluateAsync(BrowserTestConstants.GoLive.EnableObsStudioScript);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.StartStream).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ActiveSourceLabel)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);

            await page.GetByTestId(UiTestIds.GoLive.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Library));
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LiveWidget)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);

            await CaptureScreenshotAsync(page, BrowserTestConstants.GoLive.WidgetReturnScreenshotPath);
            await page.GetByTestId(UiTestIds.Header.LiveWidget).ClickAsync();

            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.GoLiveDemo));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ActiveSourceLabel)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_StartRecording_MarksHeaderIndicatorAsRecordingOutsideStudioRoute()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await page.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeActiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.Header.GoLive)).ToHaveCountAsync(0);
            await page.GetByTestId(UiTestIds.GoLive.OpenSettings).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Settings));
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_RecordingState_PropagatesAcrossSharedTabsAndReturnsToIdleAfterStop()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.GoLive.CrossTabIndicatorScenario);

        var pages = await _fixture.NewSharedPagesAsync(BrowserTestConstants.GoLive.SharedContextPageCount);
        var primaryPage = pages[0];
        var secondaryPage = pages[1];

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(primaryPage);

            await secondaryPage.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(secondaryPage.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
            await Expect(secondaryPage.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);

            await primaryPage.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(primaryPage.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await primaryPage.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();
            await primaryPage.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeActiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(secondaryPage.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);

            await UiScenarioArtifacts.CapturePageAsync(
                secondaryPage,
                BrowserTestConstants.GoLive.CrossTabIndicatorScenario,
                BrowserTestConstants.GoLive.CrossTabIndicatorActiveStep);

            await primaryPage.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();
            await primaryPage.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeInactiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(secondaryPage.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);

            await UiScenarioArtifacts.CapturePageAsync(
                secondaryPage,
                BrowserTestConstants.GoLive.CrossTabIndicatorScenario,
                BrowserTestConstants.GoLive.CrossTabIndicatorIdleStep);
        }
        finally
        {
            await primaryPage.Context.CloseAsync();
        }
    }

    private static async Task CaptureScreenshotAsync(Microsoft.Playwright.IPage page, string relativePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await page.ScreenshotAsync(new() { Path = fullPath, FullPage = true });
    }
}
