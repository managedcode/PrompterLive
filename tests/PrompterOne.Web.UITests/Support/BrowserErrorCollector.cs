using Microsoft.Playwright;

namespace PrompterOne.Web.UITests;

internal sealed class BrowserErrorCollector
{
    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _pageErrors = [];

    private BrowserErrorCollector()
    {
    }

    public static BrowserErrorCollector Attach(IPage page)
    {
        var collector = new BrowserErrorCollector();
        page.Console += collector.OnConsoleMessage;
        page.PageError += collector.OnPageError;
        return collector;
    }

    public async Task AssertNoCriticalUiErrorsAsync()
    {
        await Assert.That(_pageErrors).DoesNotContain(IsCriticalUiError);
        await Assert.That(_consoleErrors).DoesNotContain(IsCriticalUiError);
    }

    public string Describe() =>
        string.Join(
            Environment.NewLine,
            _consoleErrors
                .Concat(_pageErrors)
                .DefaultIfEmpty("No captured browser console or page errors."));

    private void OnConsoleMessage(object? sender, IConsoleMessage message)
    {
        if (!string.Equals(message.Type, BrowserTestConstants.RapidInput.ConsoleErrorType, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(message.Type, BrowserTestConstants.RapidInput.ConsoleWarningType, StringComparison.OrdinalIgnoreCase) &&
            !message.Text.Contains(BrowserTestConstants.RapidInput.CriticalConsolePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _consoleErrors.Add($"{message.Type}: {message.Text}");
    }

    private void OnPageError(object? sender, string message) =>
        _pageErrors.Add(message);

    private static bool IsCriticalUiError(string message) =>
        message.Contains(BrowserTestConstants.RapidInput.UnhandledUiExceptionFragment, StringComparison.Ordinal) ||
        message.Contains(BrowserTestConstants.RapidInput.ObjectDisposedExceptionFragment, StringComparison.Ordinal) ||
        message.Contains(BrowserTestConstants.RapidInput.DisposedCancellationTokenFragment, StringComparison.Ordinal);
}
