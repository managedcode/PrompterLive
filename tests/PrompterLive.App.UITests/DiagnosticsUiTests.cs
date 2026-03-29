using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class DiagnosticsUiTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task LibraryScreen_ShowsDiagnosticsBannerWhenFolderCreateFails()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.EvaluateAsync(
                $$"""
                ({ detail, storageKey }) => {
                    const originalSetItem = Storage.prototype.setItem;
                    Storage.prototype.setItem = function (key, value) {
                        if (key === storageKey) {
                            throw new Error(detail);
                        }

                        return originalSetItem.call(this, key, value);
                    };
                }
                """,
                new
                {
                    detail = BrowserTestConstants.Diagnostics.ForcedFailureDetail,
                    storageKey = BrowserTestConstants.Diagnostics.FolderStorageKey
                });

            await page.GetByTestId(UiTestIds.Library.FolderCreateStart).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.NewFolderName).FillAsync("Diagnostics Failure Folder");
            await page.GetByTestId(UiTestIds.Library.NewFolderSubmit).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner))
                .ToContainTextAsync(BrowserTestConstants.Diagnostics.CreateFolderFailure);
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner))
                .ToContainTextAsync(BrowserTestConstants.Diagnostics.ForcedFailureDetail);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
