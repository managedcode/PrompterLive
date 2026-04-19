using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterFidelityTests(StandaloneAppFixture fixture)
{
    private const int ImmediateAlignmentFollowUpDelayMilliseconds = 180;
    private const double LegacyMaximumReaderWidthPixels = 1240d;
    private const string MaximumReaderWidthLabel = "100%";
    private const string MaximumReaderWidthValue = "100";
    private const int ParagraphMotionSettleDelayMilliseconds = 450;
    private const double ParagraphMotionTolerancePixels = 4d;
    private const int ReaderFontAdjustmentStep = 4;
    private const int SecurityIncidentResponseCardIndex = 2;
    private const string StandaloneCommaWord = ",";
    private const string TrailingCommaWord = "exposed,";

    [Test]
    public async Task TeleprompterLeadership_RepositionsReadingLineWhenFocalPointChanges()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterLeadership);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var focalGuide = page.GetByTestId(UiTestIds.Teleprompter.FocalGuide);
            var firstWordSelector = TeleprompterReaderAlignmentAssertions.BuildDataTestSelector(
                UiTestIds.Teleprompter.CardWord(0, 0, 0));
            var firstWord = page.Locator(firstWordSelector);

            await Expect(firstWord).ToBeVisibleAsync();
            await TeleprompterReaderAlignmentAssertions.AssertWordAlignedToGuideAsync(page, firstWordSelector);

            await page.GetByTestId(UiTestIds.Teleprompter.FocalSlider).EvaluateAsync(
                $$"""
                element => {
                    element.value = '{{BrowserTestConstants.Teleprompter.AdjustedFocalPointPercent}}';
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                    element.dispatchEvent(new Event('change', { bubbles: true }));
                }
                """);

            await Expect(focalGuide).ToHaveAttributeAsync("style", BrowserTestConstants.Teleprompter.AdjustedFocalGuideStyle);
            await TeleprompterReaderAlignmentAssertions.AssertWordAlignedToGuideAsync(page, firstWordSelector);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task TeleprompterScreen_UsesSingleFullBleedBackgroundCameraLayer()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveCountAsync(1);
            await EnsureCameraLayerIsActiveAsync(page);

            var isFullBleed = await page.EvaluateAsync<bool>(
                $$"""
                args => {
                    const camera = document.querySelector(`[data-test="${args.cameraTestId}"]`);
                    const shell = document.querySelector(`[data-test="${args.shellTestId}"]`);
                    if (!(camera instanceof HTMLElement) || !(shell instanceof HTMLElement)) {
                        return false;
                    }

                    const cameraRect = camera.getBoundingClientRect();
                    const shellRect = shell.getBoundingClientRect();
                    return cameraRect.width >= shellRect.width * 0.95 && cameraRect.height >= shellRect.height * 0.95;
                }
                """,
                new
                {
                    cameraTestId = UiTestIds.Teleprompter.CameraBackground,
                    shellTestId = UiTestIds.Teleprompter.Page
                });

            await Assert.That(isFullBleed).IsTrue();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task TeleprompterDemo_KeepsParagraphStableWhenFirstWordsAdvance()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
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

    [Test]
    public async Task TeleprompterDemo_KeepsParagraphStableWhenFontSizeChanges()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);
            await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
            await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);

            await AssertParagraphMotionStableAfterFontSizeChangeAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task TeleprompterDemo_ActivatesNextWordDirectlyOnFocalGuideWithoutVisibleSettling()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);

            var focalGuide = page.GetByTestId(UiTestIds.Teleprompter.FocalGuide);
            await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();

            var activeWord = page.Locator(BrowserTestConstants.Teleprompter.ActiveWordSelector);
            await Expect(activeWord).ToBeVisibleAsync();

            var immediateDelta = await MeasureVerticalCenterDeltaAsync(focalGuide, activeWord);
            await page.WaitForTimeoutAsync(ImmediateAlignmentFollowUpDelayMilliseconds);
            var settledDelta = await MeasureVerticalCenterDeltaAsync(focalGuide, activeWord);

            await Assert.That(Math.Abs(immediateDelta)).IsBetween(0d, BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
            await Assert.That(Math.Abs(settledDelta)).IsBetween(0d, BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
            await Assert.That(Math.Abs(immediateDelta - settledDelta)).IsBetween(0d, ParagraphMotionTolerancePixels);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task TeleprompterSecurityIncident_UsesMaximumWidthAndContinuousEmphasisWithoutStandaloneCommaWords()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentViewportWidth,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentViewportHeight);
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterSecurityIncident);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.WidthSlider)).ToHaveValueAsync(MaximumReaderWidthValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.WidthValue)).ToHaveTextAsync(MaximumReaderWidthLabel);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardGroup(SecurityIncidentResponseCardIndex, 0)))
                .ToHaveAttributeAsync(
                    "data-emphasis",
                    BrowserTestConstants.State.EnabledValue);

            var clusterWrapWidth = await page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap)
                .EvaluateAsync<double>("element => element.getBoundingClientRect().width");
            var responseWords = await page.Locator($"[data-test^='{UiTestIds.Teleprompter.CardWordPrefix(SecurityIncidentResponseCardIndex)}']")
                .AllTextContentsAsync();

            await Assert.That(clusterWrapWidth > LegacyMaximumReaderWidthPixels).IsTrue();
            await Assert.That(responseWords).Contains(TrailingCommaWord);
            await Assert.That(responseWords).DoesNotContain(StandaloneCommaWord);
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
        await Expect(cameraLayer).ToHaveAttributeAsync(
            BrowserTestConstants.State.ActiveAttribute,
            BrowserTestConstants.Teleprompter.ActiveStateValue);
    }

    private static async Task<double> MeasureVerticalCenterDeltaAsync(
        Microsoft.Playwright.ILocator focalGuide,
        Microsoft.Playwright.ILocator word)
    {
        var focalGuideBox = await focalGuide.BoundingBoxAsync();
        var wordBox = await word.BoundingBoxAsync();

        await Assert.That(focalGuideBox).IsNotNull();
        await Assert.That(wordBox).IsNotNull();

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

        await Assert.That(string.IsNullOrWhiteSpace(immediate.ActiveText)).IsFalse();
        await Assert.That(settled.ActiveText).IsEqualTo(immediate.ActiveText);
        await Assert.That(Math.Abs(immediate.TextTop - settled.TextTop)).IsBetween(0d, ParagraphMotionTolerancePixels);
        await Assert.That(Math.Abs(immediate.ActiveCenterDelta - settled.ActiveCenterDelta)).IsBetween(0d, ParagraphMotionTolerancePixels);
    }

    private static async Task AssertParagraphMotionStableAfterFontSizeChangeAsync(Microsoft.Playwright.IPage page)
    {
        var fontSlider = page.GetByTestId(UiTestIds.Teleprompter.FontSlider);
        var currentValue = await fontSlider.InputValueAsync();
        var nextValue = int.Parse(currentValue, CultureInfo.InvariantCulture) + ReaderFontAdjustmentStep;
        await fontSlider.EvaluateAsync(
            """
            (element, value) => {
                element.value = String(value);
                element.dispatchEvent(new Event('input', { bubbles: true }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
            }
            """,
            nextValue);

        // Font-size changes intentionally reflow the paragraph first and then
        // re-align the active word to the guide on the next render pass. That
        // makes the "right after input dispatch" sample inherently racy.
        // Validate the post-settle contract instead: once the realignment
        // lands, the paragraph must stop drifting and keep the active word
        // anchored to the guide.
        await page.WaitForTimeoutAsync(ParagraphMotionSettleDelayMilliseconds);
        var settled = await CaptureParagraphMotionSampleAsync(page);
        await page.WaitForTimeoutAsync(ImmediateAlignmentFollowUpDelayMilliseconds);
        var confirmed = await CaptureParagraphMotionSampleAsync(page);

        await Assert.That(string.IsNullOrWhiteSpace(settled.ActiveText)).IsFalse();
        await Assert.That(confirmed.ActiveText).IsEqualTo(settled.ActiveText);
        await Assert.That(Math.Abs(settled.TextTop - confirmed.TextTop)).IsBetween(0d, ParagraphMotionTolerancePixels);
        await Assert.That(Math.Abs(settled.ActiveCenterDelta)).IsBetween(0d, BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
        await Assert.That(Math.Abs(confirmed.ActiveCenterDelta)).IsBetween(0d, BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
        await Assert.That(Math.Abs(settled.ActiveCenterDelta - confirmed.ActiveCenterDelta)).IsBetween(0d, ParagraphMotionTolerancePixels);
    }

    private static Task<ReaderParagraphMotionSample> CaptureParagraphMotionSampleAsync(Microsoft.Playwright.IPage page) =>
        page.GetByTestId(UiTestIds.Teleprompter.Page).EvaluateAsync<ReaderParagraphMotionSample>(
            $$"""
            (element, args) => {
                const text = element.querySelector(`[data-test="${args.textTestId}"]`);
                const focalGuide = element.querySelector(`[data-test="${args.focalGuideTestId}"]`);
                const activeWord = element.querySelector(args.activeWordSelector);

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
            """,
            new
            {
                focalGuideTestId = UiTestIds.Teleprompter.FocalGuide,
                activeWordSelector = BrowserTestConstants.Teleprompter.ActiveWordSelector,
                textTestId = UiTestIds.Teleprompter.CardText(0)
            });

    private sealed class ReaderParagraphMotionSample
    {
        public string ActiveText { get; set; } = string.Empty;
        public double TextTop { get; set; }
        public double ActiveCenterDelta { get; set; }
    }
}
