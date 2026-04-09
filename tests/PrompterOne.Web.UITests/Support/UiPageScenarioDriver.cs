using Microsoft.Playwright;

namespace PrompterOne.Web.UITests;

internal static class UiPageScenarioDriver
{
    private const int DefaultAttemptCount = 2;

    internal static Task RunWithIsolatedPageRetryAsync(
        StandaloneAppFixture fixture,
        Func<IPage, Task> scenario,
        string failureLabel,
        int attemptCount = DefaultAttemptCount) =>
        RunWithIsolatedPageRetryAsync<object?>(
            fixture,
            async page =>
            {
                await scenario(page);
                return null;
            },
            failureLabel,
            attemptCount);

    internal static async Task<T> RunWithIsolatedPageRetryAsync<T>(
        StandaloneAppFixture fixture,
        Func<IPage, Task<T>> scenario,
        string failureLabel,
        int attemptCount = DefaultAttemptCount)
    {
        Exception? lastFailure = null;
        string? lastBrowserDiagnostics = null;

        for (var attempt = 1; attempt <= attemptCount; attempt++)
        {
            var page = await fixture.NewPageAsync(additionalContext: true);
            var browserErrors = BrowserErrorCollector.Attach(page);

            try
            {
                return await scenario(page);
            }
            catch (Exception exception) when (attempt < attemptCount && IsClosedTargetException(exception))
            {
                lastFailure = exception;
                lastBrowserDiagnostics = browserErrors.Describe();
            }
            catch (Exception exception)
            {
                throw BuildFailure(failureLabel, exception, browserErrors.Describe(), attempt, attemptCount);
            }
            finally
            {
                await SafeCloseAsync(page);
            }
        }

        throw BuildFailure(
            failureLabel,
            lastFailure ?? new InvalidOperationException("The page scenario did not complete."),
            lastBrowserDiagnostics ?? "No browser diagnostics were captured.",
            attemptCount,
            attemptCount);
    }

    private static InvalidOperationException BuildFailure(
        string failureLabel,
        Exception exception,
        string browserDiagnostics,
        int attempt,
        int attemptCount) =>
        new(
            $"{failureLabel} Attempt {attempt} of {attemptCount}.{Environment.NewLine}" +
            $"Captured browser errors:{Environment.NewLine}{browserDiagnostics}{Environment.NewLine}{exception}",
            exception);

    private static bool IsClosedTargetException(Exception exception) =>
        exception is PlaywrightException playwrightException
        && (
            playwrightException.Message.Contains("Target page, context or browser has been closed", StringComparison.Ordinal)
            || playwrightException.Message.Contains("Process exited", StringComparison.Ordinal));

    private static async Task SafeCloseAsync(IPage page)
    {
        try
        {
            await page.Context.CloseAsync();
        }
        catch
        {
        }
    }
}
