using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class DiagnosticsUiTests(StandaloneAppFixture fixture)
{
    private const string ForcedFailureDetail = "Forced diagnostics failure from browser test.";

    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task LibraryScreen_ShowsDiagnosticsBannerWhenFolderCreateFails()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/library");
            await Expect(page.GetByTestId("library-page")).ToBeVisibleAsync();

            await page.EvaluateAsync(
                $$"""
                detail => {
                    const originalSetItem = Storage.prototype.setItem;
                    Storage.prototype.setItem = function (key, value) {
                        if (key === "prompterlive.folders.v1") {
                            throw new Error(detail);
                        }

                        return originalSetItem.call(this, key, value);
                    };
                }
                """,
                ForcedFailureDetail);

            await page.GetByTestId("library-folder-create-start").ClickAsync();
            await Expect(page.GetByTestId("library-new-folder-overlay")).ToBeVisibleAsync();
            await page.GetByTestId("library-new-folder-name").FillAsync("Diagnostics Failure Folder");
            await page.GetByTestId("library-new-folder-submit").ClickAsync();

            await Expect(page.GetByTestId("diagnostics-banner")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("diagnostics-banner")).ToContainTextAsync("Unable to create this folder.");
            await Expect(page.GetByTestId("diagnostics-banner")).ToContainTextAsync(ForcedFailureDetail);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
