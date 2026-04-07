using System.Text.Json;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class EditorMonacoDriver
{
    internal sealed record DroppedFileDescriptor(string FileName, string Text);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static ILocator SourceInput(IPage page) =>
        page.GetByTestId(UiTestIds.Editor.SourceInput);

    internal static ILocator SourceStage(IPage page) =>
        page.GetByTestId(UiTestIds.Editor.SourceStage);

    internal static async Task WaitUntilReadyAsync(IPage page)
    {
        var browserErrors = BrowserErrorCollector.Attach(page);

        await Expect(SourceStage(page)).ToBeVisibleAsync(new()
        {
            Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
        });

        try
        {
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const host = document.querySelector(`[data-testid="${args.testId}"]`);
                    return host?.getAttribute(args.readyAttributeName) === args.readyValue;
                }
                """,
                new
                {
                    readyAttributeName = EditorMonacoRuntimeContract.EditorReadyAttributeName,
                    readyValue = "true",
                    testId = UiTestIds.Editor.SourceStage
                },
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Monaco editor did not become ready within {BrowserTestConstants.Timing.DefaultVisibleTimeoutMs}ms.{Environment.NewLine}" +
                $"Captured browser diagnostics:{Environment.NewLine}{browserErrors.Describe()}{Environment.NewLine}{exception}");
        }
    }

    internal static async Task ClickAsync(IPage page, Position? position = null)
    {
        if (position is null)
        {
            await SourceStage(page).ClickAsync();
            return;
        }

        await SourceStage(page).ClickAsync(new() { Position = position });
    }

    internal static async Task ClickUncoveredStageAreaAsync(IPage page)
    {
        var stageBounds = await SourceStage(page).BoundingBoxAsync();
        await Assert.That(stageBounds).IsNotNull();

        var clickX = Math.Max(0, stageBounds!.Width - BrowserTestConstants.Editor.MenuDismissRightInsetPx);
        var clickY = Math.Max(0, stageBounds.Height * BrowserTestConstants.Editor.MenuDismissClickVerticalFactor);

        await ClickAsync(page, new Position
        {
            X = (float)clickX,
            Y = (float)clickY
        });
    }

    internal static async Task ClearAndTypeAsync(IPage page, string text)
    {
        await ClickAsync(page);
        await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.SelectAll);
        await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Backspace);
        await page.Keyboard.TypeAsync(text, new() { Delay = 0 });
    }

    internal static async Task FocusAsync(IPage page)
    {
        _ = await InvokeHarnessAsync<EditorMonacoState>(page, "focus");
    }

    internal static async Task<EditorMonacoState> GetStateAsync(IPage page)
    {
        var state = await InvokeHarnessAsync<EditorMonacoState?>(page, "getState");
        await Assert.That(state).IsNotNull();
        return state!;
    }

    internal static async Task<EditorMonacoCompletionList> GetCompletionsAsync(IPage page, int lineNumber, int column)
    {
        var completions = await InvokeHarnessAsync<EditorMonacoCompletionList?>(page, "getCompletions", new
        {
            lineNumber,
            column
        });

        await Assert.That(completions).IsNotNull();
        return completions!;
    }

    internal static async Task<EditorMonacoHoverResult?> GetHoverAsync(IPage page, int lineNumber, int column) =>
        await InvokeHarnessAsync<EditorMonacoHoverResult?>(page, "getHover", new
        {
            lineNumber,
            column
        });

    internal static async Task<EditorMonacoTokenizedLine> TokenizeLineAsync(IPage page, int lineNumber)
    {
        var tokenizedLine = await InvokeHarnessAsync<EditorMonacoTokenizedLine?>(page, "tokenizeLine", new
        {
            lineNumber
        });

        await Assert.That(tokenizedLine).IsNotNull();
        return tokenizedLine!;
    }

    internal static async Task SetCaretAtEndAsync(IPage page)
    {
        var state = await GetStateAsync(page);
        await SetSelectionAsync(page, state.Text.Length, state.Text.Length);
    }

    internal static async Task SetCaretAtTextStartAsync(IPage page, string targetText)
    {
        var state = await GetStateAsync(page);
        var targetStart = FindTextStart(state.Text, targetText);
        await SetSelectionAsync(page, targetStart, targetStart);
    }

    internal static async Task SetCaretAtTextEndAsync(IPage page, string targetText)
    {
        var state = await GetStateAsync(page);
        var targetStart = FindTextStart(state.Text, targetText);
        var caret = targetStart + targetText.Length;
        await SetSelectionAsync(page, caret, caret);
    }

    internal static async Task SetForwardSelectionFromTextStartAsync(IPage page, string targetText, int characterCount)
    {
        var state = await GetStateAsync(page);
        var targetStart = FindTextStart(state.Text, targetText);
        var targetEnd = Math.Min(state.Text.Length, targetStart + characterCount);
        await SetSelectionAsync(page, targetStart, targetEnd);
    }

    internal static async Task SetBackwardSelectionFromTextEndAsync(IPage page, string targetText, int characterCount)
    {
        var state = await GetStateAsync(page);
        var targetStart = FindTextStart(state.Text, targetText);
        var targetEnd = targetStart + targetText.Length;
        var selectionStart = Math.Max(0, targetEnd - characterCount);
        await SetSelectionAsync(page, targetEnd, selectionStart);
    }

    internal static async Task SetSelectionAsync(IPage page, int start, int end, bool revealSelection = true)
    {
        _ = await InvokeHarnessAsync<EditorMonacoSelection>(page, "setSelection", new
        {
            start,
            end,
            revealSelection
        });

        await page.WaitForFunctionAsync(
            """
            (args) => {
                const harness = window[args.harnessGlobalName];
                const state = harness?.getState(args.testId);
                return state?.selection?.start === args.start && state?.selection?.end === args.end;
            }
            """,
            new
            {
                end,
                harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                start,
                testId = UiTestIds.Editor.SourceStage
            },
            new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
    }

    internal static async Task SetSelectionByTextAsync(IPage page, string targetText)
    {
        var state = await GetStateAsync(page);
        var start = FindTextStart(state.Text, targetText);
        await SetSelectionAsync(page, start, start + targetText.Length);
    }

    internal static async Task WaitForSelectionScrollAsync(IPage page, int minimumScrollTop, int timeoutMs)
    {
        await page.WaitForFunctionAsync(
            """
            (args) => {
                const harness = window[args.harnessGlobalName];
                const state = harness?.getState(args.testId);
                const scrollTop = state?.scrollTop;
                return typeof scrollTop === "number" && scrollTop >= args.minimumScrollTop;
            }
            """,
            new
            {
                harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                minimumScrollTop,
                testId = UiTestIds.Editor.SourceStage
            },
            new() { Timeout = timeoutMs });
    }

    internal static async Task SetTextAsync(IPage page, string text)
    {
        _ = await InvokeHarnessAsync<EditorMonacoState>(page, "setText", new { text });
        await Expect(SourceInput(page)).ToHaveValueAsync(text, new()
        {
            Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
        });

        await page.WaitForFunctionAsync(
            """
            (args) => {
                const overlay = document.querySelector(`[data-testid="${args.overlayTestId}"]`);
                return Number.parseInt(overlay?.dataset?.renderedLength ?? '-1', 10) === args.expectedLength;
            }
            """,
            new
            {
                expectedLength = text.Length,
                overlayTestId = UiTestIds.Editor.SourceHighlight
            },
            new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
    }

    internal static Task DropFilesAsync(IPage page, params DroppedFileDescriptor[] files) =>
        page.EvaluateAsync(
            """
            async (args) => {
                const target = document.querySelector(`[data-testid="${args.testId}"]`);
                if (!target) {
                    throw new Error("Unable to resolve the editor drop target.");
                }

                const dataTransfer = new DataTransfer();
                for (const file of args.files ?? []) {
                    dataTransfer.items.add(
                        new File(
                            [file.text ?? ""],
                            file.fileName ?? "dropped-script.txt",
                            { type: "text/plain" }));
                }

                target.dispatchEvent(new DragEvent("dragover", {
                    bubbles: true,
                    cancelable: true,
                    dataTransfer
                }));
                target.dispatchEvent(new DragEvent("drop", {
                    bubbles: true,
                    cancelable: true,
                    dataTransfer
                }));

                await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
            }
            """,
            new
            {
                files = files.Select(file => new
                {
                    fileName = file.FileName,
                    text = file.Text
                }).ToArray(),
                testId = UiTestIds.Editor.SourceStage
            });

    internal static async Task ReplaceTextAsync(IPage page, Func<string, string> update)
    {
        var currentState = await GetStateAsync(page);
        await SetTextAsync(page, update(currentState.Text));
    }

    private static Task<T?> InvokeHarnessAsync<T>(IPage page, string methodName, object? payload = null) =>
        InvokeHarnessCoreAsync<T>(page, methodName, payload);

    private static async Task<T?> InvokeHarnessCoreAsync<T>(IPage page, string methodName, object? payload)
    {
        var json = await page.EvaluateAsync<string>(
            """
            async (args) => {
                const harness = window[args.harnessGlobalName];
                if (!harness || typeof harness[args.methodName] !== 'function') {
                    throw new Error(`Editor Monaco harness method "${args.methodName}" is unavailable.`);
                }

                const result = await harness[args.methodName](args.testId, ...(args.arguments ?? []));
                return JSON.stringify(result ?? null);
            }
            """,
            new
            {
                arguments = BuildArguments(payload),
                harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                methodName,
                testId = UiTestIds.Editor.SourceStage
            });

        return string.IsNullOrWhiteSpace(json) || string.Equals(json, "null", StringComparison.Ordinal)
            ? default
            : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static object?[] BuildArguments(object? payload) =>
        payload is null
            ? []
            : payload.GetType()
                .GetProperties()
                .OrderBy(property => property.MetadataToken)
                .Select(property => property.GetValue(payload))
                .ToArray();

    private static int FindTextStart(string sourceText, string targetText)
    {
        var targetStart = sourceText.IndexOf(targetText, StringComparison.Ordinal);
        if (targetStart < 0)
        {
            throw new InvalidOperationException($"Unable to locate \"{targetText}\" in the Monaco editor text.");
        }

        return targetStart;
    }

}
