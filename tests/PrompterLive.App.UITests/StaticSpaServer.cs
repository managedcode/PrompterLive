using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace PrompterLive.App.UITests;

internal sealed class StaticSpaServer(
    string appWwwrootDirectory,
    string frameworkDirectory,
    string sharedWwwrootDirectory,
    string? hotReloadStaticAssetsDirectory,
    string baseAddress) : IAsyncDisposable
{
    private readonly string _appWwwrootDirectory = appWwwrootDirectory;
    private readonly string _frameworkDirectory = frameworkDirectory;
    private readonly string _sharedWwwrootDirectory = sharedWwwrootDirectory;
    private readonly string? _hotReloadStaticAssetsDirectory = hotReloadStaticAssetsDirectory;
    private readonly string _baseAddress = baseAddress;
    private WebApplication? _app;

    public async Task StartAsync()
    {
        if (!Directory.Exists(_appWwwrootDirectory))
        {
            throw new DirectoryNotFoundException($"App wwwroot directory not found: {_appWwwrootDirectory}");
        }

        if (!Directory.Exists(_frameworkDirectory))
        {
            throw new DirectoryNotFoundException($"Blazor framework directory not found: {_frameworkDirectory}");
        }

        if (!Directory.Exists(_sharedWwwrootDirectory))
        {
            throw new DirectoryNotFoundException($"Shared wwwroot directory not found: {_sharedWwwrootDirectory}");
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(_baseAddress);

        var provider = BuildContentTypeProvider();
        var app = builder.Build();
        MapStaticDirectory(app, "/_framework", _frameworkDirectory, provider);
        MapStaticDirectory(app, "/_content/PrompterLive.Shared", _sharedWwwrootDirectory, provider);
        if (!string.IsNullOrWhiteSpace(_hotReloadStaticAssetsDirectory) && Directory.Exists(_hotReloadStaticAssetsDirectory))
        {
            MapStaticDirectory(app, "/_content/Microsoft.DotNet.HotReload.WebAssembly.Browser", _hotReloadStaticAssetsDirectory, provider);
        }
        app.MapGet("/{**path}", async context =>
        {
            var requestPath = context.Request.Path.Value;
            if (!string.IsNullOrWhiteSpace(requestPath) && Path.HasExtension(requestPath))
            {
                var assetPath = requestPath.TrimStart('/');
                var physicalPath = Path.GetFullPath(Path.Combine(_appWwwrootDirectory, assetPath.Replace('/', Path.DirectorySeparatorChar)));
                if (!physicalPath.StartsWith(_appWwwrootDirectory, StringComparison.Ordinal) || !File.Exists(physicalPath))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                if (!provider.TryGetContentType(physicalPath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                context.Response.ContentType = contentType;
                context.Response.Headers.CacheControl = "no-store";
                await context.Response.SendFileAsync(physicalPath);
                return;
            }

            await context.Response.SendFileAsync(Path.Combine(_appWwwrootDirectory, "index.html"));
        });

        _app = app;
        await _app.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private static FileExtensionContentTypeProvider BuildContentTypeProvider()
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".dat"] = "application/octet-stream";
        provider.Mappings[".dll"] = "application/octet-stream";
        provider.Mappings[".gz"] = "application/gzip";
        provider.Mappings[".map"] = "application/json";
        provider.Mappings[".pdb"] = "application/octet-stream";
        provider.Mappings[".wasm"] = "application/wasm";
        provider.Mappings[".webmanifest"] = "application/manifest+json";
        return provider;
    }

    private static void MapStaticDirectory(
        WebApplication app,
        string requestPrefix,
        string physicalDirectory,
        FileExtensionContentTypeProvider contentTypeProvider)
    {
        app.MapGet($"{requestPrefix}/{{**assetPath}}", async context =>
        {
            var assetPath = context.Request.RouteValues["assetPath"]?.ToString();
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var physicalPath = Path.GetFullPath(Path.Combine(physicalDirectory, assetPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!physicalPath.StartsWith(physicalDirectory, StringComparison.Ordinal) || !File.Exists(physicalPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (!contentTypeProvider.TryGetContentType(physicalPath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            context.Response.ContentType = contentType;
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.SendFileAsync(physicalPath);
        });
    }
}
