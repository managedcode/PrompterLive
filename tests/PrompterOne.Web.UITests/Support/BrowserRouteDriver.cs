using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class BrowserRouteDriver
{
    private const int RouteBootstrapAttemptCount = 2;
    private const string RouteFailurePrefix = "route-open";
    private const string RouteReloadFailurePrefix = "route-reload";
    private const WaitUntilState RouteNavigationReadyState = WaitUntilState.Commit;

    internal static async Task OpenPageAsync(
        IPage page,
        string route,
        string pageTestId,
        string? failureLabel = null)
    {
        var willNavigate = !IsCurrentRoute(page, route);
        var routeVisibleTimeoutMs = willNavigate
            ? BrowserTestConstants.Timing.RuntimeWarmupVisibleTimeoutMs
            : BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs;

        if (await IsCurrentRouteReadyAsync(page, route, pageTestId))
        {
            return;
        }

        var forceNavigation = !IsCurrentRoute(page, route);

        for (var attempt = 1; attempt <= RouteBootstrapAttemptCount; attempt++)
        {
            try
            {
                // Route readiness is validated by explicit URL and page-level sentinels below.
                // NetworkIdle is too strict for pages that keep long-lived browser activity alive on CI.
                if (forceNavigation || !IsCurrentRoute(page, route))
                {
                    await page.GotoAsync(route, new() { WaitUntil = RouteNavigationReadyState });
                }

                forceNavigation = false;
                await WaitForRouteAsync(page, route);
                if (await IsPageVisibleAsync(page, pageTestId, routeVisibleTimeoutMs))
                {
                    return;
                }
            }
            catch (TimeoutException)
            {
                if (attempt >= RouteBootstrapAttemptCount)
                {
                    throw;
                }

                forceNavigation = true;
                continue;
            }
            catch (PlaywrightException exception) when (IsRetryableRouteOpenFailure(exception))
            {
                if (IsEquivalentNavigationInterruption(exception, route))
                {
                    await WaitForRouteAsync(page, route);
                    if (await IsPageVisibleAsync(page, pageTestId, routeVisibleTimeoutMs))
                    {
                        return;
                    }
                }

                if (attempt >= RouteBootstrapAttemptCount)
                {
                    throw;
                }

                forceNavigation = true;
                continue;
            }
        }

        await TryCaptureFailurePageAsync(page, failureLabel ?? $"{RouteFailurePrefix}-{pageTestId}");
        await Expect(page.GetByTestId(pageTestId)).ToBeVisibleAsync(
            new() { Timeout = routeVisibleTimeoutMs });
    }

    internal static async Task ReloadPageAsync(
        IPage page,
        string route,
        string pageTestId,
        string? failureLabel = null)
    {
        for (var attempt = 1; attempt <= RouteBootstrapAttemptCount; attempt++)
        {
            try
            {
                await page.ReloadAsync(new() { WaitUntil = RouteNavigationReadyState });
                await WaitForRouteAsync(page, route);
                if (await IsPageVisibleAsync(page, pageTestId, BrowserTestConstants.Timing.RuntimeWarmupVisibleTimeoutMs))
                {
                    return;
                }
            }
            catch (TimeoutException) when (attempt < RouteBootstrapAttemptCount)
            {
            }
            catch (PlaywrightException exception) when (
                attempt < RouteBootstrapAttemptCount &&
                IsRetryableRouteOpenFailure(exception))
            {
            }
        }

        await TryCaptureFailurePageAsync(page, failureLabel ?? $"{RouteReloadFailurePrefix}-{pageTestId}");
        await WaitForRouteAsync(page, route);
        await Expect(page.GetByTestId(pageTestId)).ToBeVisibleAsync(
            new() { Timeout = BrowserTestConstants.Timing.RuntimeWarmupVisibleTimeoutMs });
    }

    internal static Task WaitForRouteAsync(IPage page, string route)
    {
        var routePattern = new Regex($"{Regex.Escape(route)}$");

        return Expect(page).ToHaveURLAsync(
            routePattern,
            new()
            {
                Timeout = BrowserTestConstants.Timing.DefaultNavigationTimeoutMs
            });
    }

    private static async Task<bool> IsCurrentRouteReadyAsync(IPage page, string route, string pageTestId)
    {
        if (!IsCurrentRoute(page, route))
        {
            return false;
        }

        return await IsPageVisibleAsync(page, pageTestId, BrowserTestConstants.Timing.DefaultVisibleTimeoutMs);
    }

    private static bool IsCurrentRoute(IPage page, string route)
    {
        if (!Uri.TryCreate(page.Url, UriKind.Absolute, out var pageUri))
        {
            return false;
        }

        return string.Equals(pageUri.PathAndQuery, route, StringComparison.Ordinal);
    }

    private static async Task<bool> IsPageVisibleAsync(IPage page, string pageTestId, int timeoutMs)
    {
        try
        {
            await page.GetByTestId(pageTestId).WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }

    private static bool IsRetryableRouteOpenFailure(PlaywrightException exception)
    {
        return !exception.Message.Contains("Target page, context or browser has been closed", StringComparison.Ordinal)
               && !exception.Message.Contains("Process exited", StringComparison.Ordinal);
    }

    private static bool IsEquivalentNavigationInterruption(PlaywrightException exception, string route)
    {
        return exception.Message.Contains("interrupted by another navigation to", StringComparison.Ordinal)
               && exception.Message.Contains(route, StringComparison.Ordinal);
    }

    private static async Task TryCaptureFailurePageAsync(IPage page, string failureLabel)
    {
        if (page.IsClosed)
        {
            return;
        }

        try
        {
            await UiScenarioArtifacts.CaptureFailurePageAsync(page, failureLabel);
        }
        catch
        {
        }
    }
}
