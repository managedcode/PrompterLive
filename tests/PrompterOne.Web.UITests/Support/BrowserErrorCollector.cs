using Microsoft.Playwright;

namespace PrompterOne.Web.UITests;

internal sealed class BrowserErrorCollector
{
    private readonly object _gate = new();
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
        await Assert.That(Snapshot(_pageErrors)).DoesNotContain(IsCriticalUiError);
        await Assert.That(Snapshot(_consoleErrors)).DoesNotContain(IsCriticalUiError);
    }

    public string Describe() =>
        string.Join(
            Environment.NewLine,
            Snapshot(_consoleErrors)
                .Where(IsActionableConsoleDiagnostic)
                .Concat(Snapshot(_pageErrors).Select(message => $"pageerror: {message}"))
                .DefaultIfEmpty("No captured critical browser console or page errors."));

    private void OnConsoleMessage(object? sender, IConsoleMessage message)
    {
        lock (_gate)
        {
            _consoleErrors.Add($"{message.Type}: {message.Text}");
        }
    }

    private void OnPageError(object? sender, string message)
    {
        lock (_gate)
        {
            _pageErrors.Add(message);
        }
    }

    private string[] Snapshot(List<string> messages)
    {
        lock (_gate)
        {
            return [.. messages];
        }
    }

    private static bool IsActionableConsoleDiagnostic(string message) =>
        message.StartsWith("error:", StringComparison.OrdinalIgnoreCase) ||
        IsCriticalUiError(message);

    private static bool IsCriticalUiError(string message) =>
        message.Contains(BrowserTestConstants.RapidInput.UnhandledUiExceptionFragment, StringComparison.Ordinal) ||
        message.Contains(BrowserTestConstants.RapidInput.ObjectDisposedExceptionFragment, StringComparison.Ordinal) ||
        message.Contains(BrowserTestConstants.RapidInput.DisposedCancellationTokenFragment, StringComparison.Ordinal);
}
