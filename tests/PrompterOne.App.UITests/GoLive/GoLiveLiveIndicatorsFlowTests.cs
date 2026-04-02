using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class GoLiveLiveIndicatorsFlowTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string LiveBadgeLabel = "On air";
    private const string LiveCardCssClass = "gl-cam-onair";
    private const string LiveDotCssClass = "gl-air-dot-live";

    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
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
            var activeSourceCard = page.GetByTestId(UiTestIds.GoLive.SourceCamera(BrowserTestConstants.GoLive.FirstSourceId));
            var previewLiveDot = page.GetByTestId(UiTestIds.GoLive.PreviewLiveDot);

            await Expect(activeSourceBadge)
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);
            await Expect(activeSourceBadge).Not.ToContainTextAsync(LiveBadgeLabel);
            Assert.DoesNotContain(
                LiveCardCssClass,
                await activeSourceCard.GetAttributeAsync(BrowserTestConstants.Html.ClassAttribute) ?? string.Empty,
                StringComparison.Ordinal);

            await Expect(previewLiveDot)
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);
            Assert.DoesNotContain(
                LiveDotCssClass,
                await previewLiveDot.GetAttributeAsync(BrowserTestConstants.Html.ClassAttribute) ?? string.Empty,
                StringComparison.Ordinal);
            var previewDotBackground = await previewLiveDot.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");
            Assert.DoesNotContain(BrowserTestConstants.GoLive.IdleDotColorChannel, previewDotBackground, StringComparison.Ordinal);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
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
            var activeSourceCard = page.GetByTestId(UiTestIds.GoLive.SourceCamera(BrowserTestConstants.GoLive.FirstSourceId));
            var previewLiveDot = page.GetByTestId(UiTestIds.GoLive.PreviewLiveDot);

            await Expect(activeSourceBadge)
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);
            await Expect(activeSourceBadge).ToContainTextAsync(LiveBadgeLabel);
            Assert.Contains(
                LiveCardCssClass,
                await activeSourceCard.GetAttributeAsync(BrowserTestConstants.Html.ClassAttribute) ?? string.Empty,
                StringComparison.Ordinal);

            await Expect(previewLiveDot)
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);
            Assert.Contains(
                LiveDotCssClass,
                await previewLiveDot.GetAttributeAsync(BrowserTestConstants.Html.ClassAttribute) ?? string.Empty,
                StringComparison.Ordinal);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
