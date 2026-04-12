using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Web.UITests;

public sealed partial class StandaloneAppFixture
{
    private const WaitUntilState BlankPageNavigationReadyState = WaitUntilState.Commit;
    private static readonly (string Route, string PageTestId)[] RuntimeWarmupRoutes =
    [
        (BrowserTestConstants.Routes.Library, UiTestIds.Library.Page),
        (BrowserTestConstants.Routes.Settings, UiTestIds.Settings.Page),
        (BrowserTestConstants.Routes.LearnDemo, UiTestIds.Learn.Page),
        (BrowserTestConstants.Routes.TeleprompterDemo, UiTestIds.Teleprompter.Page),
        (BrowserTestConstants.Routes.GoLiveDemo, UiTestIds.GoLive.Page),
        (BrowserTestConstants.Routes.EditorDemo, UiTestIds.Editor.Page)
    ];

    private static async Task InitializeContextAsync(IBrowserContext context, string baseAddress)
    {
        await context.AddInitScriptAsync(UiTestHostConstants.RuntimeTelemetryHarnessInitializationScript);
        await context.GrantPermissionsAsync(UiTestHostConstants.GrantedPermissions, new BrowserContextGrantPermissionsOptions
        {
            Origin = baseAddress
        });
        await ConfigureMediaHarnessAsync(context);
    }

    private static Task<IBrowserContext> CreateBrowserContextAsync(IBrowser browser, string baseAddress) =>
        browser.NewContextAsync(CreateBrowserContextOptions(baseAddress));

    private static BrowserNewContextOptions CreateBrowserContextOptions(string baseAddress) => new()
    {
        BaseURL = baseAddress,
        ViewportSize = new()
        {
            Width = BrowserTestConstants.Viewport.DefaultWidth,
            Height = BrowserTestConstants.Viewport.DefaultHeight
        }
    };

    private static async Task PrimeIsolatedBrowserStorageAsync(IPage page, string baseAddress)
    {
        await NavigateToBlankPageAsync(page, baseAddress);
        await page.EvaluateAsync(
            UiTestHostConstants.ResetBrowserStorageScript,
            UiTestHostConstants.BrowserStorageDatabaseName);
        await page.EvaluateAsync(BrowserTestLibrarySeedData.CreateInitializationScript());
    }

    private static Task NavigateToBlankPageAsync(IPage page, string baseAddress) =>
        page.GotoAsync($"{baseAddress}{UiTestHostConstants.BlankPagePath}", new()
        {
            WaitUntil = BlankPageNavigationReadyState,
            Timeout = BrowserTestConstants.Timing.DefaultNavigationTimeoutMs
        });

    private static async Task WarmUpRuntimeAsync(IBrowser browser, string baseAddress)
    {
        var context = await CreateBrowserContextAsync(browser, baseAddress);
        await InitializeContextAsync(context, baseAddress);

        var page = await context.NewPageAsync();
        var browserErrors = BrowserErrorCollector.Attach(page);

        try
        {
            PreparePage(page);
            await PrimeIsolatedBrowserStorageAsync(page, baseAddress);

            foreach (var (route, pageTestId) in RuntimeWarmupRoutes)
            {
                await WarmUpRouteAsync(page, route, pageTestId);
            }

            await browserErrors.AssertNoCriticalUiErrorsAsync();
        }
        catch (Exception exception)
        {
            throw BuildWarmupFailure(exception, browserErrors.Describe());
        }
        finally
        {
            await DisposeContextAsync(context);
        }
    }

    private static async Task WarmUpRouteAsync(IPage page, string route, string pageTestId)
    {
        await BrowserRouteDriver.OpenPageAsync(
            page,
            route,
            pageTestId,
            $"runtime-warmup-{pageTestId}");
    }

    private static InvalidOperationException BuildWarmupFailure(Exception exception, string browserDiagnostics) =>
        new(
            "Shared UI test runtime warmup failed." + Environment.NewLine +
            "Captured browser errors:" + Environment.NewLine +
            browserDiagnostics + Environment.NewLine +
            exception,
            exception);

    private static async Task DisposeContextAsync(IBrowserContext context)
    {
        try
        {
            await context.DisposeAsync();
        }
        catch
        {
        }
    }
}
