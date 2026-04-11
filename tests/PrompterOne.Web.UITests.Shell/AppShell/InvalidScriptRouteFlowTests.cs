using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class InvalidScriptRouteFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    public static IEnumerable<(string ValidRoute, string MissingRoute, string PageTestId)> MissingPlaybackRoutes =>
    [
        (BrowserTestConstants.Routes.LearnDemo, BrowserTestConstants.Routes.LearnMissing, UiTestIds.Learn.Page),
        (BrowserTestConstants.Routes.TeleprompterDemo, BrowserTestConstants.Routes.TeleprompterMissing, UiTestIds.Teleprompter.Page)
    ];

    [Test]
    public Task EditorRoute_MissingScriptId_ResetsToUntitledBlankDraft() =>
        RunPageAsync(async page =>
        {
            await EditorRouteDriver.OpenReadyAsync(page, BrowserTestConstants.Routes.EditorDemo, "editor-missing-valid-editor");
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);

            await EditorRouteDriver.OpenReadyAsync(page, BrowserTestConstants.Routes.EditorMissing, "editor-missing-invalid-editor");
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.UntitledTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(string.Empty);
        });

    [Test]
    [MethodDataSource(nameof(MissingPlaybackRoutes))]
    public Task PlaybackRoute_MissingScriptId_DoesNotReusePreviousEditorSession(
        string validRoute,
        string missingRoute,
        string pageTestId) =>
        RunPageAsync(async page =>
        {
            await OpenPlaybackRouteAsync(page, validRoute, pageTestId, "missing-playback-valid");
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);

            await OpenPlaybackRouteAsync(page, missingRoute, pageTestId, "missing-playback-invalid");
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.UntitledTitle);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.Back),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, AppRoutes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(string.Empty);
            await Assert.That(new Uri(page.Url).AbsolutePath).IsEqualTo(AppRoutes.Editor);
            await Assert.That(string.IsNullOrWhiteSpace(new Uri(page.Url).Query)).IsTrue();
        });

    private static Task OpenPlaybackRouteAsync(IPage page, string route, string pageTestId, string scenarioName) =>
        pageTestId switch
        {
            UiTestIds.Learn.Page => PlaybackRouteDriver.OpenLearnAsync(page, route, $"{scenarioName}-{pageTestId}"),
            UiTestIds.Teleprompter.Page => PlaybackRouteDriver.OpenTeleprompterAsync(
                page,
                route,
                $"{scenarioName}-{pageTestId}",
                requireContent: !string.Equals(route, BrowserTestConstants.Routes.TeleprompterMissing, StringComparison.Ordinal)),
            _ => throw new ArgumentOutOfRangeException(nameof(pageTestId), pageTestId, "Unsupported playback page.")
        };
}
