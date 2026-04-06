using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class DiagnosticsUiTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task LibraryScreen_ShowsDiagnosticsBannerWhenFolderCreateFails()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.AddInitScriptAsync(
                $$"""
                (() => {
                    const storageKey = {{System.Text.Json.JsonSerializer.Serialize(BrowserTestConstants.Diagnostics.FolderStorageKey)}};
                    const detail = {{System.Text.Json.JsonSerializer.Serialize(BrowserTestConstants.Diagnostics.ForcedFailureDetail)}};
                    const toggleGlobal = {{System.Text.Json.JsonSerializer.Serialize(BrowserTestConstants.Diagnostics.FolderCreateFailureToggleGlobal)}};
                    const originalSetItem = Storage.prototype.setItem;

                    Storage.prototype.setItem = function (key, value) {
                        if (window[toggleGlobal] === true && key === storageKey) {
                            throw new Error(detail);
                        }

                        return originalSetItem.call(this, key, value);
                    };
                })();
                """);

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.EvaluateAsync(
                $$"""
                ({ toggleGlobal }) => {
                    window[toggleGlobal] = true;
                }
                """,
                new
                {
                    toggleGlobal = BrowserTestConstants.Diagnostics.FolderCreateFailureToggleGlobal
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

    [Test]
    public async Task AppShell_ShowsConnectivityOverlayForOfflineAndOnlineStates()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.Context.SetOfflineAsync(true);
            await page.EvaluateAsync(BrowserTestConstants.Diagnostics.DispatchOfflineEventScript);
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Connectivity))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Connectivity))
                .ToContainTextAsync(
                    BrowserTestConstants.Diagnostics.ConnectivityOfflineTitle,
                    new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.Context.SetOfflineAsync(false);
            await page.EvaluateAsync(BrowserTestConstants.Diagnostics.DispatchOnlineEventScript);
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Connectivity))
                .ToContainTextAsync(
                    BrowserTestConstants.Diagnostics.ConnectivityOnlineTitle,
                    new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Diagnostics.ConnectivityDismiss).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Connectivity))
                .ToBeHiddenAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}