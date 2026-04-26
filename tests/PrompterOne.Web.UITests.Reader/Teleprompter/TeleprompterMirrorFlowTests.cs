using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterMirrorFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private readonly record struct ReaderContentFitProbe(
        double StageBottom,
        double StageLeft,
        double StageRight,
        double StageTop,
        double TextBottom,
        double TextLeft,
        double TextRight,
        double TextTop);

    private readonly record struct LeftRailLayoutProbe(
        double AlignmentLeft,
        double AlignmentRight,
        double ClusterLeft,
        double MirrorBottom,
        double MirrorLeft,
        double MirrorRight);

    [Test]
    public Task TeleprompterScreen_SyncsMirrorAndOrientationTransformsAcrossTextAndCameraBackground() =>
        RunPageAsync(async page =>
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var backButton = page.GetByTestId(UiTestIds.Teleprompter.Back);
            var mirrorHorizontal = page.GetByTestId(UiTestIds.Teleprompter.MirrorHorizontalToggle);
            var mirrorVertical = page.GetByTestId(UiTestIds.Teleprompter.MirrorVerticalToggle);
            var orientationToggle = page.GetByTestId(UiTestIds.Teleprompter.OrientationToggle);
            var clusterWrap = page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap);
            var cameraBackground = page.GetByTestId(UiTestIds.Teleprompter.CameraBackground);

            await Expect(backButton).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.MirrorControls)).ToBeVisibleAsync();
            await Expect(mirrorHorizontal).ToBeVisibleAsync();
            await Expect(mirrorVertical).ToBeVisibleAsync();
            await Expect(orientationToggle).ToBeVisibleAsync();
            await TeleprompterCameraDriver.EnsureEnabledAsync(page);

            var backButtonColor = await GetComputedStyleValueAsync(backButton, BrowserTestConstants.TeleprompterFlow.ColorProperty);
            var mirrorButtonColor = await GetComputedStyleValueAsync(mirrorHorizontal, BrowserTestConstants.TeleprompterFlow.ColorProperty);
            var backButtonBackground = await GetComputedStyleValueAsync(backButton, BrowserTestConstants.TeleprompterFlow.BackgroundColorProperty);

            await Assert.That(backButtonColor).IsEqualTo(mirrorButtonColor);
            await Assert.That(backButtonBackground).IsNotEqualTo(BrowserTestConstants.TeleprompterFlow.TransparentBackgroundColor);

            await mirrorHorizontal.ClickAsync();
            await Expect(mirrorHorizontal).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.Teleprompter.ActiveStateValue);
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.MirrorHorizontalTransform), RegexOptions.Compiled));
            await Expect(cameraBackground).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.MirrorHorizontalTransform), RegexOptions.Compiled));

            await mirrorVertical.ClickAsync();
            await Expect(mirrorVertical).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.Teleprompter.ActiveStateValue);
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.MirrorVerticalTransform), RegexOptions.Compiled));
            await Expect(cameraBackground).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.MirrorVerticalTransform), RegexOptions.Compiled));

            await orientationToggle.ClickAsync();
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderOrientationAttribute,
                BrowserTestConstants.TeleprompterFlow.OrientationPortraitValue);
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.OrientationPortraitTransform), RegexOptions.Compiled));
            await Expect(cameraBackground).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.OrientationPortraitTransform), RegexOptions.Compiled));

            await orientationToggle.ClickAsync();
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderOrientationAttribute,
                BrowserTestConstants.TeleprompterFlow.OrientationInvertedValue);
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.OrientationInvertedTransform), RegexOptions.Compiled));
            await Expect(cameraBackground).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.OrientationInvertedTransform), RegexOptions.Compiled));

            await orientationToggle.ClickAsync();
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderOrientationAttribute,
                BrowserTestConstants.TeleprompterFlow.OrientationPortraitCounterClockwiseValue);
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.OrientationPortraitCounterClockwiseTransform), RegexOptions.Compiled));
            await Expect(cameraBackground).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.OrientationPortraitCounterClockwiseTransform), RegexOptions.Compiled));

            var fit = await MeasureReaderContentFitAsync(page);
            await Assert.That(fit.TextLeft >= fit.StageLeft - BrowserTestConstants.TeleprompterFlow.MaxReaderContentOverflowPx)
                .IsTrue()
                .Because($"Expected rotated reader text left edge {fit.TextLeft:0.##} to stay inside stage left edge {fit.StageLeft:0.##}.");
            await Assert.That(fit.TextRight <= fit.StageRight + BrowserTestConstants.TeleprompterFlow.MaxReaderContentOverflowPx)
                .IsTrue()
                .Because($"Expected rotated reader text right edge {fit.TextRight:0.##} to stay inside stage right edge {fit.StageRight:0.##}.");
            await Assert.That(fit.TextTop >= fit.StageTop - BrowserTestConstants.TeleprompterFlow.MaxReaderContentOverflowPx)
                .IsTrue()
                .Because($"Expected rotated reader text top edge {fit.TextTop:0.##} to stay inside stage top edge {fit.StageTop:0.##}.");
            await Assert.That(fit.TextBottom <= fit.StageBottom + BrowserTestConstants.TeleprompterFlow.MaxReaderContentOverflowPx)
                .IsTrue()
                .Because($"Expected rotated reader text bottom edge {fit.TextBottom:0.##} to stay inside stage bottom edge {fit.StageBottom:0.##}.");

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.MirrorScenarioName,
                BrowserTestConstants.TeleprompterFlow.MirrorStep);
        });

    [Test]
    public Task TeleprompterScreen_KeepsAlignmentControlsDockedOnLeftRail() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TeleprompterFlow.LeftRailScenarioName);

            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.MirrorControls)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.AlignmentControls)).ToBeVisibleAsync();

            var layout = await page.EvaluateAsync<LeftRailLayoutProbe>(
                """
                (args) => {
                    const byTestId = testId => document.querySelector(`[data-test="${testId}"]`);
                    const alignment = byTestId(args.alignmentControls);
                    const cluster = byTestId(args.clusterWrap);
                    const mirror = byTestId(args.mirrorControls);
                    const alignmentRect = alignment?.getBoundingClientRect();
                    const clusterRect = cluster?.getBoundingClientRect();
                    const mirrorRect = mirror?.getBoundingClientRect();

                    return {
                        alignmentLeft: alignmentRect?.left ?? 0,
                        alignmentRight: alignmentRect?.right ?? 0,
                        clusterLeft: clusterRect?.left ?? 0,
                        mirrorBottom: mirrorRect?.bottom ?? 0,
                        mirrorLeft: mirrorRect?.left ?? 0,
                        mirrorRight: mirrorRect?.right ?? 0
                    };
                }
                """,
                new
                {
                    alignmentControls = UiTestIds.Teleprompter.AlignmentControls,
                    clusterWrap = UiTestIds.Teleprompter.ClusterWrap,
                    mirrorControls = UiTestIds.Teleprompter.MirrorControls
                });

            await Assert.That(Math.Abs(layout.AlignmentLeft - layout.MirrorLeft))
                .IsBetween(0d, BrowserTestConstants.TeleprompterFlow.MaximumLeftRailGroupLeftDeltaPx);
            await Assert.That(layout.ClusterLeft - layout.AlignmentRight)
                .IsBetween(BrowserTestConstants.TeleprompterFlow.MinimumLeftRailStageGapPx, double.MaxValue);
            await Assert.That(layout.AlignmentRight)
                .IsBetween(0d, layout.MirrorRight);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.LeftRailScenarioName,
                BrowserTestConstants.TeleprompterFlow.LeftRailStep);
        });

    private static Task<string> GetComputedStyleValueAsync(ILocator locator, string propertyName) =>
        locator.EvaluateAsync<string>(
            "(element, propertyName) => getComputedStyle(element)[propertyName]",
            propertyName);

    private static Task<ReaderContentFitProbe> MeasureReaderContentFitAsync(IPage page) =>
        page.EvaluateAsync<ReaderContentFitProbe>(
            """
            args => {
                const byTestId = testId => document.querySelector(`[data-test="${testId}"]`);
                const stage = byTestId(args.stage);
                const text = byTestId(args.text);
                const stageRect = stage?.getBoundingClientRect();
                const textRect = text?.getBoundingClientRect();

                return {
                    stageBottom: stageRect?.bottom ?? 0,
                    stageLeft: stageRect?.left ?? 0,
                    stageRight: stageRect?.right ?? 0,
                    stageTop: stageRect?.top ?? 0,
                    textBottom: textRect?.bottom ?? 0,
                    textLeft: textRect?.left ?? 0,
                    textRight: textRect?.right ?? 0,
                    textTop: textRect?.top ?? 0
                };
            }
            """,
            new
            {
                stage = UiTestIds.Teleprompter.Stage,
                text = UiTestIds.Teleprompter.CardText(0)
            });
}
