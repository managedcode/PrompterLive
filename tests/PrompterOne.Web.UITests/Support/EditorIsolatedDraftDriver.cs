using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class EditorIsolatedDraftDriver
{
    internal static async Task OpenBlankDraftAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.Editor);
        await Expect(page.GetByTestId(UiTestIds.Editor.Page))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await EditorMonacoDriver.WaitUntilReadyAsync(page);
    }

    internal static async Task CreateDraftAsync(
        IPage page,
        string visibleText,
        string? title = null,
        bool waitForPersistedRoute = true)
    {
        await OpenBlankDraftAsync(page);
        await EditorMonacoDriver.SetTextAsync(page, visibleText);
        if (waitForPersistedRoute)
        {
            await WaitForAssignedScriptRouteAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(visibleText);
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            await SetTitleAsync(page, title);
        }
    }

    internal static Task CreateSeededDraftAsync(
        IPage page,
        string seedScriptId,
        bool setSeedTitle = true,
        bool waitForPersistedRoute = true)
    {
        var visibleText = BrowserTestLibrarySeedData.GetSeededScriptVisibleText(seedScriptId);
        var title = setSeedTitle
            ? BrowserTestLibrarySeedData.GetSeededScriptTitle(seedScriptId)
            : null;
        return CreateDraftAsync(page, visibleText, title, waitForPersistedRoute);
    }

    private static async Task SetTitleAsync(IPage page, string title)
    {
        var titleInput = page.GetByTestId(UiTestIds.Editor.Title);
        var currentTitle = await titleInput.InputValueAsync();
        if (string.Equals(currentTitle, title, StringComparison.Ordinal))
        {
            return;
        }

        await titleInput.FillAsync(title);
        await page.GetByTestId(UiTestIds.Editor.Author).ClickAsync();
        await Expect(titleInput).ToHaveValueAsync(title);
        await Expect(page.GetByTestId(UiTestIds.Header.Title)).ToHaveTextAsync(title);
    }

    private static Task WaitForAssignedScriptRouteAsync(IPage page) =>
        page.WaitForFunctionAsync(
            """
            (args) => {
                const location = window.location;
                if (!location || location.pathname !== args.route) {
                    return false;
                }

                const scriptId = new URLSearchParams(location.search).get(args.scriptIdKey);
                return typeof scriptId === "string" && scriptId.trim().length > 0;
            }
            """,
            new
            {
                route = AppRoutes.Editor,
                scriptIdKey = AppRoutes.ScriptIdQueryKey
            },
            new() { Timeout = BrowserTestConstants.Timing.DefaultNavigationTimeoutMs });
}
