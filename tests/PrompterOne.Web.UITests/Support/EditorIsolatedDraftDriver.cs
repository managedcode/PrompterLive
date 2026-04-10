using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class EditorIsolatedDraftDriver
{
    internal static async Task OpenBlankDraftAsync(IPage page)
    {
        await EditorRouteDriver.OpenReadyAsync(page, BrowserTestConstants.Routes.Editor, "editor-open-blank-draft");
        var sourceInput = EditorMonacoDriver.SourceInput(page);
        var currentText = await sourceInput.InputValueAsync();

        if (string.IsNullOrEmpty(currentText))
        {
            return;
        }

        await EditorMonacoDriver.SetTextAsync(page, string.Empty);
        await Expect(sourceInput).ToHaveValueAsync(string.Empty, new()
        {
            Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs
        });
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
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(visibleText, new()
            {
                Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs
            });
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

    internal static async Task WaitForImportedDraftAsync(
        IPage page,
        string expectedTitle,
        params string[] expectedSourceFragments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedTitle);
        ArgumentNullException.ThrowIfNull(expectedSourceFragments);

        await EditorMonacoDriver.WaitUntilReadyAsync(page);
        await page.WaitForFunctionAsync(
            """
            args => {
                const titleElement = document.querySelector(`[data-test="${args.headerTitleTestId}"]`);
                const sourceElement = document.querySelector(`[data-test="${args.sourceInputTestId}"]`);
                const titleText = titleElement?.textContent?.trim() ?? "";
                const titleAttribute = titleElement?.getAttribute("title") ?? "";
                const sourceValue = sourceElement?.value ?? "";
                return titleText === args.expectedTitle
                    && titleAttribute === args.expectedTitle
                    && args.expectedSourceFragments.every(fragment => sourceValue.includes(fragment));
            }
            """,
            new
            {
                headerTitleTestId = UiTestIds.Header.Title,
                sourceInputTestId = UiTestIds.Editor.SourceInput,
                expectedTitle,
                expectedSourceFragments
            },
            new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });
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

    internal static Task WaitForAssignedScriptRouteAsync(IPage page) =>
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
