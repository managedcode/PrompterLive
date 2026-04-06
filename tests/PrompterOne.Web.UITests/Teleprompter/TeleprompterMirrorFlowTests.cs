using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterMirrorFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture){
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
            await Expect(mirrorHorizontal).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.MirrorHorizontalTransform), RegexOptions.Compiled));

            await mirrorVertical.ClickAsync();
            await Expect(mirrorVertical).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                new Regex(Regex.Escape(BrowserTestConstants.TeleprompterFlow.MirrorVerticalTransform), RegexOptions.Compiled));

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.MirrorScenarioName,
                BrowserTestConstants.TeleprompterFlow.MirrorStep);
        });

    private static Task<string> GetComputedStyleValueAsync(ILocator locator, string propertyName) =>
        locator.EvaluateAsync<string>(
            "(element, propertyName) => getComputedStyle(element)[propertyName]",
            propertyName);
}