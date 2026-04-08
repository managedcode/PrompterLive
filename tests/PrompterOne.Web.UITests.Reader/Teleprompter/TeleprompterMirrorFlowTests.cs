using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterMirrorFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private readonly record struct LeftRailLayoutProbe(
        double AlignmentLeft,
        double AlignmentRight,
        double ClusterLeft,
        double MirrorBottom,
        double MirrorLeft,
        double MirrorRight);

    [Test]
    public Task TeleprompterScreen_ExposesVisibleBackButtonAndMirrorControls() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var backButton = page.GetByTestId(UiTestIds.Teleprompter.Back);
            var mirrorHorizontal = page.GetByTestId(UiTestIds.Teleprompter.MirrorHorizontalToggle);
            var mirrorVertical = page.GetByTestId(UiTestIds.Teleprompter.MirrorVerticalToggle);
            var clusterWrap = page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap);

            await Expect(backButton).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.MirrorControls)).ToBeVisibleAsync();
            await Expect(mirrorHorizontal).ToBeVisibleAsync();
            await Expect(mirrorVertical).ToBeVisibleAsync();

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

            await mirrorVertical.ClickAsync();
            await Expect(mirrorVertical).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.Teleprompter.ActiveStateValue);
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.MirrorVerticalTransform), RegexOptions.Compiled));

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

            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
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
}
