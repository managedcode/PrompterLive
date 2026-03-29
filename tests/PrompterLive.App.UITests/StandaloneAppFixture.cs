using System.Diagnostics;
using System.Net;
using Microsoft.Playwright;

namespace PrompterLive.App.UITests;

[CollectionDefinition(Name)]
public sealed class StandaloneAppCollection : ICollectionFixture<StandaloneAppFixture>
{
    public const string Name = "standalone-app";
}

public sealed class StandaloneAppFixture : IAsyncLifetime
{
    private const string BaseAddressValue = "http://localhost:5051";
    private StaticSpaServer? _server;

    public string BaseAddress => BaseAddressValue;
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await StopListeningProcessAsync();
        _server = new StaticSpaServer(
            GetAppWwwrootDirectory(),
            GetFrameworkDirectory(),
            GetSharedWwwrootDirectory(),
            GetHotReloadStaticAssetsDirectory(),
            BaseAddressValue);
        await _server.StartAsync();
        await WaitForServerAsync();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        Playwright?.Dispose();
        if (_server is not null)
        {
            await _server.DisposeAsync();
        }
    }

    public async Task<IPage> NewPageAsync()
    {
        return await Browser.NewPageAsync(new BrowserNewPageOptions
        {
            BaseURL = BaseAddress
        });
    }

    private static async Task WaitForServerAsync()
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await GetServerStateAsync() == ServerState.Valid)
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"PrompterLive.App did not start listening on {BaseAddressValue} within the timeout.");
    }

    private static async Task<ServerState> GetServerStateAsync()
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        try
        {
            using var response = await client.GetAsync(BaseAddressValue);
            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return ServerState.NotRunning;
            }

            var body = await response.Content.ReadAsStringAsync();
            if (!body.Contains("Prompter.live", StringComparison.Ordinal))
            {
                return ServerState.Invalid;
            }

            var assetName = GetExpectedFrameworkAssetName();
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return ServerState.Valid;
            }

            using var assetResponse = await client.GetAsync($"{BaseAddressValue}/_framework/{assetName}");
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
            .EnumerateFiles(frameworkDirectory, "PrompterLive.App*.wasm", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(Path.GetFileName)
            .FirstOrDefault();
    }

    private static async Task StopListeningProcessAsync()
    {
        var listenerPids = await GetListeningPidsAsync();
        foreach (var pid in listenerPids.Distinct())
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            catch
            {
            }
        }
    }

    private static async Task<IReadOnlyList<int>> GetListeningPidsAsync()
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "lsof",
                Arguments = "-t -iTCP:5051 -sTCP:LISTEN",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (!process.Start())
        {
            return Array.Empty<int>();
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var pid) ? pid : 0)
            .Where(pid => pid > 0)
            .ToArray();
    }

    private static string GetAppWwwrootDirectory() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterLive.App/wwwroot"));

    private static string GetFrameworkDirectory() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterLive.App/bin/Debug/net10.0/wwwroot/_framework"));

    private static string GetSharedWwwrootDirectory() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterLive.Shared/wwwroot"));

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
