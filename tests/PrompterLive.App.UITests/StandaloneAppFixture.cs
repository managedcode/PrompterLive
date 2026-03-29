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
    private const string BaseAddressValue = "http://localhost:5040";
    private Process? _process;

    public string BaseAddress => BaseAddressValue;
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var serverState = await GetServerStateAsync();
        if (serverState == ServerState.Invalid)
        {
            await StopListeningProcessAsync();
            serverState = ServerState.NotRunning;
        }

        if (serverState != ServerState.Valid)
        {
            _process = StartAppProcess();
            await WaitForServerAsync();
        }

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

        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
    }

    public async Task<IPage> NewPageAsync()
    {
        return await Browser.NewPageAsync(new BrowserNewPageOptions
        {
            BaseURL = BaseAddress
        });
    }

    private static Process StartAppProcess()
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/PrompterLive.App"));

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run",
                WorkingDirectory = projectDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += static (_, _) => { };
        process.ErrorDataReceived += static (_, _) => { };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start PrompterLive.App.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
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
        var frameworkDirectory = Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/PrompterLive.App")),
            "bin",
            "Debug",
            "net10.0",
            "wwwroot",
            "_framework");

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
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "lsof",
                Arguments = "-t -iTCP:5040 -sTCP:LISTEN",
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

    private enum ServerState
    {
        NotRunning,
        Valid,
        Invalid
    }
}
