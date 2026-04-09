using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using PrompterOne.Testing;

namespace PrompterOne.Web.UITests;

public sealed partial class StandaloneAppFixture : IAsyncInitializer, IAsyncDisposable
{
    private const int MinimumPageCount = 1;
    private const int ContextBootstrapAttemptCount = 3;
    private const int ServerStartupTimeoutSeconds = 60;
    private const int ServerProbeDelayMilliseconds = 500;
    private readonly ConcurrentDictionary<IBrowserContext, byte> _contexts = [];
    private readonly ConcurrentDictionary<string, IBrowserContext> _sharedContexts = new(StringComparer.Ordinal);
    private SharedRuntimeHandle? _runtimeHandle;

    public string BaseAddress => _runtimeHandle?.BaseAddress ?? throw new InvalidOperationException("UI test runtime is not initialized.");
    public IPlaywright Playwright => _runtimeHandle?.Playwright ?? throw new InvalidOperationException("UI test runtime is not initialized.");
    public IBrowser Browser => _runtimeHandle?.Browser ?? throw new InvalidOperationException("UI test runtime is not initialized.");

    public async Task InitializeAsync()
    {
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);
        _runtimeHandle = await SharedRuntime.AcquireAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTrackedContextsAsync();
        _sharedContexts.Clear();

        if (_runtimeHandle is not null)
        {
            await SharedRuntime.ReleaseAsync();
            _runtimeHandle = null;
        }
    }

    public async Task ResetRuntimeAsync()
    {
        await DisposeTrackedContextsAsync();
        _sharedContexts.Clear();

        _runtimeHandle = null;
        await SharedRuntime.ResetAsync();
    }

    public Task<IPage> NewPageAsync(
        bool additionalContext = false,
        [CallerMemberName] string contextKey = "")
    {
        return additionalContext
            ? CreateAdditionalPageAsync()
            : CreateSharedPageAsync(contextKey);
    }

    public async Task<IReadOnlyList<IPage>> NewSharedPagesAsync(
        int pageCount,
        [CallerMemberName] string contextKey = "")
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageCount, MinimumPageCount);

        var pages = new List<IPage>(pageCount);

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            pages.Add(await NewPageAsync(contextKey: contextKey));
        }

        return pages;
    }

    private async Task<IPage> CreateAdditionalPageAsync()
    {
        for (var attempt = 1; ; attempt++)
        {
            var context = await CreateTrackedContextAsync();

            try
            {
                await WarmUpContextWithSacrificialPageAsync(context);
                var page = await CreatePrimedPageAsync(context);
                await WarmUpContextPageIfNeededAsync(page, BaseAddress);
                return page;
            }
            catch (PlaywrightException exception) when (attempt < ContextBootstrapAttemptCount && IsBrowserClosedException(exception))
            {
                await DisposeContextAsync(context);
            }
            catch (InvalidOperationException exception) when (attempt < ContextBootstrapAttemptCount && IsContextWarmupFailure(exception))
            {
                await DisposeContextAsync(context);
            }
        }
    }

    private async Task<IPage> CreateSharedPageAsync(string contextKey)
    {
        for (var attempt = 1; ; attempt++)
        {
            var (context, isNewSharedContext) = await GetOrCreateSharedContextAsync(contextKey);

            try
            {
                if (isNewSharedContext)
                {
                    await WarmUpContextWithSacrificialPageAsync(context, warmAllRuntimeRoutes: true);
                    return await CreatePrimedPageAsync(context);
                }

                return await CreateBlankPageAsync(context);
            }
            catch (PlaywrightException exception) when (attempt < ContextBootstrapAttemptCount && IsBrowserClosedException(exception))
            {
                RemoveSharedContext(context);
            }
            catch (InvalidOperationException exception) when (attempt < ContextBootstrapAttemptCount && IsContextWarmupFailure(exception))
            {
                RemoveSharedContext(context);
            }
        }
    }

    private async Task<(IBrowserContext Context, bool IsNew)> GetOrCreateSharedContextAsync(string contextKey)
    {
        if (_sharedContexts.TryGetValue(contextKey, out var existingContext))
        {
            EnsureContextTracked(existingContext);
            return (existingContext, false);
        }

        var newContext = await CreateTrackedContextAsync();

        if (_sharedContexts.TryAdd(contextKey, newContext))
        {
            return (newContext, true);
        }

        try
        {
            await newContext.DisposeAsync();
        }
        catch
        {
        }

        return (_sharedContexts[contextKey], false);
    }

    private async Task<IBrowserContext> CreateTrackedContextAsync()
    {
        var runtime = await EnsureRuntimeHandleAsync();
        var context = await CreateBrowserContextAsync(runtime.Browser);
        await InitializeContextAsync(context, BaseAddress);
        EnsureContextTracked(context);
        return context;
    }

    private async Task DisposeTrackedContextsAsync()
    {
        foreach (var context in _contexts.Keys)
        {
            if (!_contexts.TryRemove(context, out _))
            {
                continue;
            }

            try
            {
                await context.DisposeAsync();
            }
            catch
            {
            }
        }
    }

    private void EnsureContextTracked(IBrowserContext context)
    {
        if (!_contexts.TryAdd(context, 0))
        {
            return;
        }

        context.Close += (_, _) =>
        {
            _contexts.TryRemove(context, out _);
            RemoveSharedContext(context);
        };
    }

    private void RemoveSharedContext(IBrowserContext context)
    {
        foreach (var entry in _sharedContexts)
        {
            if (ReferenceEquals(entry.Value, context))
            {
                _sharedContexts.TryRemove(entry.Key, out _);
            }
        }
    }

    private async Task<SharedRuntimeHandle> EnsureRuntimeHandleAsync()
    {
        _runtimeHandle ??= await SharedRuntime.AcquireAsync();
        if (_runtimeHandle.Browser.IsConnected)
        {
            return _runtimeHandle;
        }

        _runtimeHandle = await SharedRuntime.AcquireAsync();
        return _runtimeHandle;
    }

    private async Task<IBrowserContext> CreateBrowserContextAsync(IBrowser browser)
    {
        try
        {
            return await CreateBrowserContextAsync(browser, BaseAddress);
        }
        catch (PlaywrightException exception) when (IsBrowserClosedException(exception))
        {
            _runtimeHandle = await SharedRuntime.AcquireAsync();
            return await CreateBrowserContextAsync(_runtimeHandle.Browser, _runtimeHandle.BaseAddress);
        }
    }

    private static bool IsBrowserClosedException(PlaywrightException exception) =>
        exception.Message.Contains("Target page, context or browser has been closed", StringComparison.Ordinal)
        || exception.Message.Contains("Process exited", StringComparison.Ordinal);

    private static bool IsContextWarmupFailure(InvalidOperationException exception) =>
        exception.Message.StartsWith("Browser context warmup failed.", StringComparison.Ordinal);

    private async Task<IPage> CreatePrimedPageAsync(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        PreparePage(page);
        await PrimeIsolatedBrowserStorageAsync(page);
        return page;
    }

    private async Task<IPage> CreateBlankPageAsync(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        PreparePage(page);
        await page.GotoAsync($"{BaseAddress}{UiTestHostConstants.BlankPagePath}");
        return page;
    }

    private async Task WarmUpContextWithSacrificialPageAsync(IBrowserContext context, bool warmAllRuntimeRoutes = false)
    {
        if (!TestEnvironment.IsCiEnvironment)
        {
            return;
        }

        var warmupPage = await context.NewPageAsync();

        try
        {
            // Warm a disposable page so the returned test page stays clean and isolated.
            PreparePage(warmupPage);
            await PrimeIsolatedBrowserStorageAsync(warmupPage);
            await WarmUpContextPageIfNeededAsync(warmupPage, BaseAddress, warmAllRuntimeRoutes);
        }
        finally
        {
            await ClosePageAsync(warmupPage);
        }
    }

    private Task PrimeIsolatedBrowserStorageAsync(IPage page) =>
        PrimeIsolatedBrowserStorageAsync(page, BaseAddress);

    private static async Task ClosePageAsync(IPage page)
    {
        try
        {
            if (!page.IsClosed)
            {
                await page.CloseAsync();
            }
        }
        catch
        {
        }
    }

    private static void PreparePage(IPage page)
    {
        page.SetDefaultNavigationTimeout(BrowserTestConstants.Timing.DefaultNavigationTimeoutMs);
        page.SetDefaultTimeout(BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);
    }

    private static class SharedRuntime
    {
        private static readonly SemaphoreSlim LifecycleGate = new(1, 1);
        private static IBrowser? _browser;
        private static IPlaywright? _playwright;
        private static StaticSpaServer? _server;

        public static async Task<SharedRuntimeHandle> AcquireAsync()
        {
            await LifecycleGate.WaitAsync();
            try
            {
                if (_server is null || _playwright is null || _browser is null || !_browser.IsConnected)
                {
                    try
                    {
                        await DisposeRuntimeAsync();
                        await StartRuntimeAsync();
                    }
                    catch
                    {
                        await DisposeRuntimeAsync();
                        throw;
                    }
                }

                return new SharedRuntimeHandle(_playwright!, _browser!, _server!.BaseAddress);
            }
            finally
            {
                LifecycleGate.Release();
            }
        }

        public static Task ReleaseAsync() => Task.CompletedTask;

        public static async Task ResetAsync()
        {
            await LifecycleGate.WaitAsync();
            try
            {
                await DisposeRuntimeAsync();
            }
            finally
            {
                LifecycleGate.Release();
            }
        }

        private static async Task StartRuntimeAsync()
        {
            _server = CreateServer();
            await _server.StartAsync();
            await WaitForServerAsync(_server.BaseAddress);
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _playwright.Selectors.SetTestIdAttribute(BrowserTestConstants.Html.DataTestAttribute);
            _browser = await CreateBrowserAsync(_playwright);
            await WarmUpRuntimeAsync(_browser, _server.BaseAddress);
        }

        private static StaticSpaServer CreateServer()
        {
            return new StaticSpaServer(
                GetAppWwwrootDirectory(),
                GetFrameworkDirectory(),
                GetSharedWwwrootDirectory(),
                GetAppScopedStylesheetPath(),
                GetSharedScopedStylesheetPath(),
                GetStaticWebAssetsManifestPath(),
                GetHotReloadStaticAssetsDirectory(),
                UiTestHostConstants.LoopbackBaseAddressTemplate);
        }

        private static Task<IBrowser> CreateBrowserAsync(IPlaywright playwright)
        {
            return playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args =
                [
                    "--use-fake-ui-for-media-stream",
                    "--use-fake-device-for-media-stream"
                ]
            });
        }

        private static async Task DisposeRuntimeAsync()
        {
            if (_browser is not null)
            {
                await _browser.DisposeAsync();
                _browser = null;
            }

            _playwright?.Dispose();
            _playwright = null;

            if (_server is not null)
            {
                await _server.DisposeAsync();
                _server = null;
            }
        }
    }

    private sealed record SharedRuntimeHandle(IPlaywright Playwright, IBrowser Browser, string BaseAddress);

    private static async Task WaitForServerAsync(string baseAddress)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(ServerStartupTimeoutSeconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await GetServerStateAsync(baseAddress) == ServerState.Valid)
            {
                return;
            }

            await Task.Delay(ServerProbeDelayMilliseconds);
        }

        throw new TimeoutException($"PrompterOne.Web did not start listening on {baseAddress} within the timeout.");
    }

    private static async Task<ServerState> GetServerStateAsync(string baseAddress)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        try
        {
            using var response = await client.GetAsync(baseAddress);
            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return ServerState.NotRunning;
            }

            var body = await response.Content.ReadAsStringAsync();
            if (!body.Contains(UiTestHostConstants.ApplicationMarker, StringComparison.Ordinal))
            {
                return ServerState.Invalid;
            }

            var assetName = GetExpectedFrameworkAssetName();
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return ServerState.Valid;
            }

            using var assetResponse = await client.GetAsync($"{baseAddress}/_framework/{assetName}");
            return assetResponse.StatusCode is HttpStatusCode.OK
                ? ServerState.Valid
                : ServerState.Invalid;
        }
        catch
        {
            return ServerState.NotRunning;
        }
    }

    private static string? GetExpectedFrameworkAssetName()
    {
        var frameworkDirectory = GetFrameworkDirectory();

        if (!Directory.Exists(frameworkDirectory))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(frameworkDirectory, "PrompterOne.Web*.wasm", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(Path.GetFileName)
            .FirstOrDefault();
    }

    private static string GetAppWwwrootDirectory() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterOne.Web/wwwroot"));

    private static string GetFrameworkDirectory() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterOne.Web/bin/Debug/net10.0/wwwroot/_framework"));

    private static string GetSharedWwwrootDirectory() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterOne.Shared/wwwroot"));

    private static string GetAppScopedStylesheetPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterOne.Web/obj/Debug/net10.0/scopedcss/bundle/PrompterOne.Web.styles.css"));

    private static string GetSharedScopedStylesheetPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterOne.Shared/obj/Debug/net10.0/scopedcss/projectbundle/PrompterOne.Shared.bundle.scp.css"));

    private static string GetStaticWebAssetsManifestPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterOne.Web/obj/Debug/net10.0/staticwebassets.development.json"));

    private static string? GetHotReloadStaticAssetsDirectory()
    {
        var packagesRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "packages",
            "microsoft.dotnet.hotreload.webassembly.browser");

        if (!Directory.Exists(packagesRoot))
        {
            return null;
        }

        return Directory
            .EnumerateDirectories(packagesRoot)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => Path.Combine(path, "staticwebassets"))
            .FirstOrDefault(Directory.Exists);
    }

    private enum ServerState
    {
        NotRunning,
        Valid,
        Invalid
    }
}
