using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class InvalidScriptRouteFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    public static TheoryData<string, string, string> MissingPlaybackRoutes =>
        new()
        {
            { BrowserTestConstants.Routes.LearnDemo, BrowserTestConstants.Routes.LearnMissing, UiTestIds.Learn.Page },
            { BrowserTestConstants.Routes.TeleprompterDemo, BrowserTestConstants.Routes.TeleprompterMissing, UiTestIds.Teleprompter.Page }
        };

    [Fact]
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

    [Theory]
    [MemberData(nameof(MissingPlaybackRoutes))]
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
            Assert.Equal(AppRoutes.Editor, new Uri(page.Url).AbsolutePath);
            Assert.True(string.IsNullOrWhiteSpace(new Uri(page.Url).Query));
        });
}
