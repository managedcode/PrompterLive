using System.Text.Json;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class EditorIsolatedDraftDriver
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        if (waitForPersistedRoute)
        {
            var draftId = $"test-draft-{Guid.NewGuid():N}";
            var draftTitle = string.IsNullOrWhiteSpace(title) ? BrowserTestConstants.Scripts.UntitledTitle : title.Trim();
            await SeedDraftDocumentAsync(page, draftId, draftTitle, visibleText);
            await EditorRouteDriver.OpenReadyAsync(page, AppRoutes.EditorWithId(draftId), $"editor-isolated-draft-{draftId}");
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(visibleText, new()
            {
                Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs
            });
            return;
        }

        await OpenBlankDraftAsync(page);
        await EditorMonacoDriver.SetTextAsync(page, visibleText);

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
        await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Editor.Author));
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

    private static Task SeedDraftDocumentAsync(IPage page, string draftId, string title, string visibleText)
    {
        var document = new BrowserStoredScriptDocumentDto
        {
            Id = draftId,
            Title = title,
            Text = visibleText,
            DocumentName = $"{draftId}.tps",
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var revision = new EditorLocalRevisionRecord(
            document.UpdatedAt.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture),
            title,
            document.DocumentName,
            visibleText,
            document.UpdatedAt);

        return page.EvaluateAsync(
            """
            (args) => {
                const documents = JSON.parse(window.localStorage.getItem(args.documentLibraryKey) || "[]");
                const document = JSON.parse(args.documentJson);
                const nextDocuments = documents.filter(candidate => candidate?.id !== document.id && candidate?.Id !== document.Id);
                nextDocuments.push(document);
                window.localStorage.setItem(args.documentLibraryKey, JSON.stringify(nextDocuments));
                window.localStorage.setItem(args.documentSeedVersionKey, args.materializationVersion);
                window.localStorage.setItem(args.historyKey, args.historyJson);
            }
            """,
            new
            {
                documentJson = JsonSerializer.Serialize(document, JsonOptions),
                documentLibraryKey = BrowserStorageKeys.DocumentLibrary,
                documentSeedVersionKey = BrowserStorageKeys.DocumentSeedVersion,
                historyJson = JsonSerializer.Serialize(new[] { revision }, JsonOptions),
                historyKey = string.Concat(
                    BrowserStorageKeys.SettingsPrefix,
                    BrowserStorageKeys.EditorLocalHistoryKeyPrefix,
                    draftId),
                materializationVersion = BrowserStorageKeys.LibraryMaterializationVersion
            });
    }
}
