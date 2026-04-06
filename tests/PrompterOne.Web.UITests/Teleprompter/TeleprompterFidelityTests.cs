using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed class TeleprompterFidelityTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string ContinuousEmphasisCssClass = "rd-g-emphasis";
    private const int ImmediateAlignmentFollowUpDelayMilliseconds = 180;
    private const double LegacyMaximumReaderWidthPixels = 1240d;
    private const string MaximumReaderWidthLabel = "100%";
    private const string MaximumReaderWidthValue = "100";
    private const int ParagraphMotionSettleDelayMilliseconds = 450;
    private const double ParagraphMotionTolerancePixels = 4d;
    private const int SecurityIncidentResponseCardIndex = 2;
    private const string StandaloneCommaWord = ",";
    private const string TrailingCommaWord = "exposed,";

    [Fact]
    public async Task TeleprompterLeadership_RepositionsReadingLineWhenFocalPointChanges()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterLeadership);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var focalGuide = page.GetByTestId(UiTestIds.Teleprompter.FocalGuide);
            var firstWord = page.GetByTestId(UiTestIds.Teleprompter.CardWord(0, 0, 0));

            await Expect(firstWord).ToBeVisibleAsync();
            await AssertGuideAlignmentAsync(page, focalGuide, firstWord);

            await page.GetByTestId(UiTestIds.Teleprompter.FocalSlider).EvaluateAsync(
                $$"""
                element => {
                    element.value = '{{BrowserTestConstants.Teleprompter.AdjustedFocalPointPercent}}';
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                }
                """);

            await Expect(focalGuide).ToHaveAttributeAsync("style", BrowserTestConstants.Teleprompter.AdjustedFocalGuideStyle);
            await AssertGuideAlignmentAsync(page, focalGuide, firstWord);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task TeleprompterScreen_UsesSingleFullBleedBackgroundCameraLayer()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.Locator(BrowserTestConstants.Elements.CameraOverlaySelector)).ToHaveCountAsync(0);
            await EnsureCameraLayerIsActiveAsync(page);

            var isFullBleed = await page.EvaluateAsync<bool>(
                $$"""
                () => {
                    const camera = document.querySelector('[data-testid="teleprompter-camera-layer-primary"]');
                    const shell = document.querySelector('{{BrowserTestConstants.Elements.TeleprompterShellSelector}}');
                    if (!(camera instanceof HTMLElement) || !(shell instanceof HTMLElement)) {
                        return false;
                    }

                    const cameraRect = camera.getBoundingClientRect();
                    const shellRect = shell.getBoundingClientRect();
                    return cameraRect.width >= shellRect.width * 0.95 && cameraRect.height >= shellRect.height * 0.95;
                }
                """);

            Assert.True(isFullBleed);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task TeleprompterDemo_KeepsParagraphStableWhenFirstWordsAdvance()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);

            var nextWord = page.GetByTestId(UiTestIds.Teleprompter.NextWord);

            await AssertParagraphMotionStableAfterWordAdvanceAsync(page, nextWord);
            await AssertParagraphMotionStableAfterWordAdvanceAsync(page, nextWord);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task TeleprompterDemo_KeepsParagraphStableWhenFontSizeChanges()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);
            await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
            await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);

            await AssertParagraphMotionStableAfterFontSizeChangeAsync(
                page,
                page.GetByTestId(UiTestIds.Teleprompter.FontUp));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task TeleprompterDemo_ActivatesNextWordDirectlyOnFocalGuideWithoutVisibleSettling()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);

            var focalGuide = page.GetByTestId(UiTestIds.Teleprompter.FocalGuide);
            await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();

            var activeWord = page.GetByTestId(UiTestIds.Teleprompter.CardText(0)).Locator(".rd-now");
            await Expect(activeWord).ToBeVisibleAsync();

            var immediateDelta = await MeasureVerticalCenterDeltaAsync(focalGuide, activeWord);
            await page.WaitForTimeoutAsync(ImmediateAlignmentFollowUpDelayMilliseconds);
            var settledDelta = await MeasureVerticalCenterDeltaAsync(focalGuide, activeWord);

            Assert.InRange(
                Math.Abs(immediateDelta),
                0d,
                BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
            Assert.InRange(
                Math.Abs(settledDelta),
                0d,
                BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
            Assert.InRange(
                Math.Abs(immediateDelta - settledDelta),
                0d,
                ParagraphMotionTolerancePixels);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task TeleprompterSecurityIncident_UsesMaximumWidthAndContinuousEmphasisWithoutStandaloneCommaWords()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentViewportWidth,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterSecurityIncident);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.WidthSlider)).ToHaveValueAsync(MaximumReaderWidthValue);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.WidthValue}")).ToHaveTextAsync(MaximumReaderWidthLabel);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardGroup(SecurityIncidentResponseCardIndex, 0)))
                .ToHaveClassAsync(new Regex($@"\b{ContinuousEmphasisCssClass}\b"));

            var clusterWrapWidth = await page.Locator($"#{UiDomIds.Teleprompter.ClusterWrap}")
                .EvaluateAsync<double>("element => element.getBoundingClientRect().width");
            var responseWords = await page.GetByTestId(UiTestIds.Teleprompter.CardText(SecurityIncidentResponseCardIndex))
                .Locator(".rd-w")
                .AllTextContentsAsync();

            Assert.True(clusterWrapWidth > LegacyMaximumReaderWidthPixels);
            Assert.Contains(TrailingCommaWord, responseWords);
            Assert.DoesNotContain(StandaloneCommaWord, responseWords);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task EnsureCameraLayerIsActiveAsync(Microsoft.Playwright.IPage page)
    {
        var cameraLayer = page.GetByTestId(UiTestIds.Teleprompter.CameraBackground);
        await TeleprompterCameraDriver.EnsureEnabledAsync(page);
        await Expect(cameraLayer).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
    }

    private static async Task AssertGuideAlignmentAsync(
        Microsoft.Playwright.IPage page,
        Microsoft.Playwright.ILocator focalGuide,
        Microsoft.Playwright.ILocator word)
    {
        var attemptCount = BrowserTestConstants.Teleprompter.AlignmentTimeoutMs /
            BrowserTestConstants.Teleprompter.AlignmentPollDelayMs;
        var lastDelta = double.MaxValue;

        for (var attempt = 0; attempt < attemptCount; attempt++)
        {
            lastDelta = await MeasureVerticalCenterDeltaAsync(focalGuide, word);
            if (Math.Abs(lastDelta) <= BrowserTestConstants.Teleprompter.AlignmentTolerancePx)
            {
                return;
            }

            await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.AlignmentPollDelayMs);
        }

        Assert.InRange(
            Math.Abs(lastDelta),
            0,
            BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
    }

    private static async Task<double> MeasureVerticalCenterDeltaAsync(
        Microsoft.Playwright.ILocator focalGuide,
        Microsoft.Playwright.ILocator word)
    {
        var focalGuideBox = await focalGuide.BoundingBoxAsync();
        var wordBox = await word.BoundingBoxAsync();

        Assert.NotNull(focalGuideBox);
        Assert.NotNull(wordBox);

        return (focalGuideBox.Y + (focalGuideBox.Height / 2d)) -
            (wordBox.Y + (wordBox.Height / 2d));
    }

    private static async Task AssertParagraphMotionStableAfterWordAdvanceAsync(
        Microsoft.Playwright.IPage page,
        Microsoft.Playwright.ILocator nextWord)
    {
        await nextWord.ClickAsync();

        var immediate = await CaptureParagraphMotionSampleAsync(page);
        await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);
        var settled = await CaptureParagraphMotionSampleAsync(page);

        Assert.False(string.IsNullOrWhiteSpace(immediate.ActiveText));
        Assert.Equal(immediate.ActiveText, settled.ActiveText);
        Assert.InRange(Math.Abs(immediate.TextTop - settled.TextTop), 0d, ParagraphMotionTolerancePixels);
        Assert.InRange(
            Math.Abs(immediate.ActiveCenterDelta - settled.ActiveCenterDelta),
            0d,
            ParagraphMotionTolerancePixels);
    }

    private static async Task AssertParagraphMotionStableAfterFontSizeChangeAsync(
        Microsoft.Playwright.IPage page,
        Microsoft.Playwright.ILocator fontSizeButton)
    {
        await fontSizeButton.ClickAsync();

        var immediate = await CaptureParagraphMotionSampleAsync(page);
        await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);
        var settled = await CaptureParagraphMotionSampleAsync(page);

        Assert.False(string.IsNullOrWhiteSpace(immediate.ActiveText));
        Assert.Equal(immediate.ActiveText, settled.ActiveText);
        Assert.InRange(Math.Abs(immediate.TextTop - settled.TextTop), 0d, ParagraphMotionTolerancePixels);
        Assert.InRange(
            Math.Abs(immediate.ActiveCenterDelta - settled.ActiveCenterDelta),
            0d,
            ParagraphMotionTolerancePixels);
    }

    private static Task<ReaderParagraphMotionSample> CaptureParagraphMotionSampleAsync(Microsoft.Playwright.IPage page) =>
        page.GetByTestId(UiTestIds.Teleprompter.Page).EvaluateAsync<ReaderParagraphMotionSample>(
            $$"""
            element => {
                const text = element.querySelector('#{{UiDomIds.Teleprompter.CardText(0)}}');
                const focalGuide = element.querySelector('#{{UiDomIds.Teleprompter.FocalGuide}}');
                const activeWord = element.querySelector('.rd-card-active .rd-w.rd-now');

                if (!(text instanceof HTMLElement) || !(focalGuide instanceof HTMLElement) || !(activeWord instanceof HTMLElement)) {
                    return null;
                }

                const textRect = text.getBoundingClientRect();
                const focalGuideRect = focalGuide.getBoundingClientRect();
                const activeWordRect = activeWord.getBoundingClientRect();
                return {
                    activeText: activeWord.textContent?.trim() ?? '',
                    textTop: textRect.top,
                    activeCenterDelta:
                        (focalGuideRect.top + focalGuideRect.height / 2) -
                        (activeWordRect.top + activeWordRect.height / 2)
                };
            }
            """);

    private sealed class ReaderParagraphMotionSample
    {
        public string ActiveText { get; set; } = string.Empty;
        public double TextTop { get; set; }
        public double ActiveCenterDelta { get; set; }
    }
}
