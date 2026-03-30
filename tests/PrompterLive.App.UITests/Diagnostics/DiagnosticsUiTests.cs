using Microsoft.Playwright;
using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

public sealed class DiagnosticsUiTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
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

    [Fact]
    public async Task AppShell_ShowsStyledBootstrapErrorOverlay()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await page.EvaluateAsync(
                BrowserTestConstants.Diagnostics.ShowBootstrapErrorScript,
                BrowserTestConstants.Diagnostics.BootstrapDetail);

            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Bootstrap)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Bootstrap))
                .ToContainTextAsync(BrowserTestConstants.Diagnostics.BootstrapDetail);
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.BootstrapReload)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Diagnostics.BootstrapDismiss).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Bootstrap)).ToBeHiddenAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task AppShell_LogsBootstrapErrorsToBrowserConsole()
    {
        var page = await _fixture.NewPageAsync();
        var consoleMessageSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        void HandleConsole(object? _, IConsoleMessage message)
        {
            if (!string.Equals(message.Type, BrowserTestConstants.Diagnostics.ConsoleErrorType, StringComparison.Ordinal))
            {
                return;
            }

            if (!message.Text.Contains(BrowserTestConstants.Diagnostics.BootstrapDetail, StringComparison.Ordinal))
            {
                return;
            }

            consoleMessageSource.TrySetResult(message.Text);
        }

        page.Console += HandleConsole;

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await page.EvaluateAsync(
                BrowserTestConstants.Diagnostics.ShowBootstrapErrorScript,
                BrowserTestConstants.Diagnostics.BootstrapDetail);

            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Bootstrap)).ToBeVisibleAsync();

            var consoleMessage = await consoleMessageSource.Task.WaitAsync(
                TimeSpan.FromMilliseconds(BrowserTestConstants.Timing.DefaultVisibleTimeoutMs));

            Assert.Contains(BrowserTestConstants.Diagnostics.ShellConsoleErrorPrefix, consoleMessage, StringComparison.Ordinal);
            Assert.Contains(BrowserTestConstants.Diagnostics.BootstrapManualSource, consoleMessage, StringComparison.Ordinal);
            Assert.Contains(BrowserTestConstants.Diagnostics.BootstrapDetail, consoleMessage, StringComparison.Ordinal);
        }
        finally
        {
            page.Console -= HandleConsole;
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task AppShell_ShowsConnectivityOverlayForOfflineAndOnlineStates()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            await page.EvaluateAsync(BrowserTestConstants.Diagnostics.ShowConnectivityOfflineScript);
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Connectivity)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Connectivity))
                .ToContainTextAsync(BrowserTestConstants.Diagnostics.ConnectivityOfflineTitle);

            await page.EvaluateAsync(BrowserTestConstants.Diagnostics.ShowConnectivityOnlineScript);
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Connectivity))
                .ToContainTextAsync(BrowserTestConstants.Diagnostics.ConnectivityOnlineTitle);

            await page.GetByTestId(UiTestIds.Diagnostics.ConnectivityDismiss).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Connectivity)).ToBeHiddenAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
