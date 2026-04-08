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
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorMissing);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
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
            await page.GotoAsync(validRoute);
            await Expect(page.GetByTestId(pageTestId)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);

            await page.GotoAsync(missingRoute);
            await Expect(page.GetByTestId(pageTestId)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.UntitledTitle);

            await page.GetByTestId(UiTestIds.Header.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Editor));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(string.Empty);
            await Assert.That(new Uri(page.Url).AbsolutePath).IsEqualTo(AppRoutes.Editor);
            await Assert.That(string.IsNullOrWhiteSpace(new Uri(page.Url).Query)).IsTrue();
        });
}
