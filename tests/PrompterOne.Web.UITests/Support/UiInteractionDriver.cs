using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class UiInteractionDriver
{
    internal static async Task ClickAndContinueAsync(
        ILocator locator,
        bool noWaitAfter = false)
    {
        Exception? lastFailure = null;

        for (var attempt = 1; attempt <= BrowserTestConstants.Timing.InteractionRetryCount; attempt++)
        {
            try
            {
                await WaitUntilInteractableAsync(locator);
                if (noWaitAfter)
                {
                    await locator.EvaluateAsync("element => element.click()");
                    return;
                }

                await locator.ClickAsync(new()
                {
                    Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
                });
                return;
            }
            catch (TimeoutException exception) when (attempt < BrowserTestConstants.Timing.InteractionRetryCount)
            {
                lastFailure = exception;
            }
            catch (PlaywrightException exception) when (
                attempt < BrowserTestConstants.Timing.InteractionRetryCount &&
                IsRetryableInteractionFailure(exception))
            {
                lastFailure = exception;
            }

            await Task.Delay(BrowserTestConstants.Timing.InteractionRetryDelayMs);
        }

        throw lastFailure ?? new InvalidOperationException("The UI interaction driver exhausted its click retries without capturing a failure.");
    }

    internal static async Task ClickAndWaitForVisibleAsync(
        ILocator trigger,
        ILocator visibleLocator,
        bool noWaitAfter = false)
    {
        Exception? lastFailure = null;

        for (var attempt = 1; attempt <= BrowserTestConstants.Timing.InteractionRetryCount; attempt++)
        {
            try
            {
                if (await visibleLocator.IsVisibleAsync())
                {
                    return;
                }

                await WaitUntilInteractableAsync(trigger);

                if (noWaitAfter)
                {
                    await trigger.EvaluateAsync("element => element.click()");
                }
                else
                {
                    await trigger.ClickAsync(new()
                    {
                        Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
                    });
                }

                await visibleLocator.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs
                });
                return;
            }
            catch (TimeoutException exception) when (attempt < BrowserTestConstants.Timing.InteractionRetryCount)
            {
                lastFailure = exception;
            }
            catch (PlaywrightException exception) when (
                attempt < BrowserTestConstants.Timing.InteractionRetryCount &&
                IsRetryableInteractionFailure(exception))
            {
                lastFailure = exception;
            }

            await Task.Delay(BrowserTestConstants.Timing.InteractionRetryDelayMs);
        }

        throw lastFailure ?? new InvalidOperationException("The UI interaction driver exhausted its menu-open retries without capturing a failure.");
    }

    internal static async Task HoverAndContinueAsync(IPage page, ILocator locator)
    {
        Exception? lastFailure = null;

        for (var attempt = 1; attempt <= BrowserTestConstants.Timing.InteractionRetryCount; attempt++)
        {
            try
            {
                await WaitUntilInteractableAsync(locator);
                var bounds = await locator.BoundingBoxAsync() ?? throw new PlaywrightException("The target locator has no bounding box for hover.");

                await page.Mouse.MoveAsync(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                return;
            }
            catch (TimeoutException exception) when (attempt < BrowserTestConstants.Timing.InteractionRetryCount)
            {
                lastFailure = exception;
            }
            catch (PlaywrightException exception) when (
                attempt < BrowserTestConstants.Timing.InteractionRetryCount &&
                IsRetryableInteractionFailure(exception))
            {
                lastFailure = exception;
            }

            await Task.Delay(BrowserTestConstants.Timing.InteractionRetryDelayMs);
        }

        throw lastFailure ?? new InvalidOperationException("The UI interaction driver exhausted its hover retries without capturing a failure.");
    }

    internal static async Task FillAndContinueAsync(
        IPage page,
        string testId,
        string value,
        int? timeoutMs = null)
    {
        await WaitUntilEditableAsync(page, testId, timeoutMs ?? BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);
        await page.EvaluateAsync(
            """
            ({ testId, value }) => {
                const element = document.querySelector(`[data-test="${testId}"]`);
                if (!(element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement)) {
                    throw new Error(`Expected editable input for data-test '${testId}'.`);
                }

                element.focus();
                const setter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(element), 'value')?.set;
                if (setter) {
                    setter.call(element, value);
                } else {
                    element.value = value;
                }

                element.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: value }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
            }
            """,
            new
            {
                testId,
                value
            });
    }

    internal static async Task FocusAndContinueAsync(
        IPage page,
        string testId,
        int? timeoutMs = null)
    {
        await WaitUntilEditableAsync(page, testId, timeoutMs ?? BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);
        await page.EvaluateAsync(
            """
            testId => {
                const element = document.querySelector(`[data-test="${testId}"]`);
                if (!(element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement)) {
                    throw new Error(`Expected editable input for data-test '${testId}'.`);
                }

                element.focus();
            }
            """,
            testId);
    }

    internal static Task<ElementBounds> GetBoundingClientRectAsync(IPage page, string testId) =>
        page.EvaluateAsync<ElementBounds>(
            """
            testId => {
                const element = document.querySelector(`[data-test="${testId}"]`);
                if (!(element instanceof Element)) {
                    throw new Error(`Expected element for data-test '${testId}'.`);
                }

                const rect = element.getBoundingClientRect();
                return {
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                };
            }
            """,
            testId);

    private static async Task WaitUntilInteractableAsync(ILocator locator)
    {
        await locator.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
        });
        await Expect(locator).ToBeEnabledAsync(new()
        {
            Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
        });
        await locator.EvaluateAsync(
            """
            element => element.scrollIntoView({
                block: 'center',
                inline: 'center',
                behavior: 'auto'
            })
            """);
    }

    private static Task WaitUntilEditableAsync(IPage page, string testId, int timeoutMs) =>
        page.WaitForFunctionAsync(
            """
            testId => {
                const element = document.querySelector(`[data-test="${testId}"]`);
                if (!(element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement)) {
                    return false;
                }

                const style = window.getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                return !element.disabled
                    && !element.readOnly
                    && style.display !== 'none'
                    && style.visibility !== 'hidden'
                    && rect.width > 0
                    && rect.height > 0;
            }
            """,
            testId,
            new() { Timeout = timeoutMs });

    private static bool IsRetryableInteractionFailure(PlaywrightException exception) =>
        !exception.Message.Contains("Target page, context or browser has been closed", StringComparison.Ordinal)
        && !exception.Message.Contains("Process exited", StringComparison.Ordinal);

    internal readonly record struct ElementBounds(
        double X,
        double Y,
        double Width,
        double Height);
}
