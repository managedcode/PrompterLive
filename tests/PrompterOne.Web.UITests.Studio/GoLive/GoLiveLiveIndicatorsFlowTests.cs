using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class GoLiveLiveIndicatorsFlowTests(StandaloneAppFixture fixture)
{
    private const string LiveBadgeLabel = "On air";

    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task GoLivePage_IdleSession_DoesNotShowOnAirBadgeOrLivePreviewDot()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ScreenTitle)).ToHaveTextAsync(BrowserTestConstants.GoLive.ScreenTitle);
            await Expect(page.GetByTestId(UiTestIds.GoLive.ScreenTitle))
                .Not.ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);

            var activeSourceBadge = page.GetByTestId(UiTestIds.GoLive.SourceCameraBadge(BrowserTestConstants.GoLive.FirstSourceId));
            var previewLiveDot = page.GetByTestId(UiTestIds.GoLive.PreviewLiveDot);

            await Expect(activeSourceBadge)
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);
            await Expect(activeSourceBadge).Not.ToContainTextAsync(LiveBadgeLabel);

            await Expect(previewLiveDot)
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);
            var previewDotBackground = await previewLiveDot.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");
            await Assert.That(previewDotBackground).DoesNotContain(BrowserTestConstants.GoLive.IdleDotColorChannel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_RecordingSession_ShowsOnAirBadgeAndLivePreviewDot()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeActiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var activeSourceBadge = page.GetByTestId(UiTestIds.GoLive.SourceCameraBadge(BrowserTestConstants.GoLive.FirstSourceId));
            var previewLiveDot = page.GetByTestId(UiTestIds.GoLive.PreviewLiveDot);

            await Expect(activeSourceBadge)
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);
            await Expect(activeSourceBadge).ToContainTextAsync(LiveBadgeLabel);

            await Expect(previewLiveDot)
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
