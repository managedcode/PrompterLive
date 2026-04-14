namespace PrompterOne.Shared.Services;

public static class RuntimeTelemetrySuppressionPolicy
{
    private static readonly string[] BrowserTestForcedFailureFragments =
    [
        "Forced diagnostics failure from browser test.",
        "Forced LiveKit connect failure from browser test."
    ];

    public static bool ShouldSuppressOutboundSentry(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return ContainsBrowserTestForcedFailure(exception.Message);
    }

    public static bool ShouldSuppressOutboundSentry(SentryEvent sentryEvent)
    {
        ArgumentNullException.ThrowIfNull(sentryEvent);

        return (sentryEvent.Exception is not null && ShouldSuppressOutboundSentry(sentryEvent.Exception)) ||
            ContainsBrowserTestForcedFailure(sentryEvent.Message?.Formatted) ||
            ContainsBrowserTestForcedFailure(sentryEvent.Message?.Message);
    }

    private static bool ContainsBrowserTestForcedFailure(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        BrowserTestForcedFailureFragments.Any(fragment =>
            value.Contains(fragment, StringComparison.Ordinal));
}
