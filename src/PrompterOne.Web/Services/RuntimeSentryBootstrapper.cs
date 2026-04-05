using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrompterOne.Shared.Settings.Services;

namespace PrompterOne.Web.Services;

internal static class RuntimeSentryBootstrapper
{
    private const string Dsn =
        "https://cc172c8c1921c7a979dfbb12ca80379f@o4511168317030400.ingest.de.sentry.io/4511168321749072";
    private const char QueryPairSeparator = '&';
    private const char QueryPrefix = '?';
    private const char QueryValueSeparator = '=';
    private const string WasmDebugEnabledValue = "1";
    private const string WasmDebugQueryKey = "wasm-debug";

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(Dsn);

    public static IDisposable? Initialize(IServiceProvider services, bool hostEnabled)
    {
        if (!hostEnabled || !IsConfigured)
        {
            return null;
        }

        var hostEnvironment = services.GetRequiredService<IWebAssemblyHostEnvironment>();
        if (hostEnvironment.IsDevelopment())
        {
            return null;
        }

        var navigationManager = services.GetRequiredService<NavigationManager>();
        if (IsWasmDebugEnabled(navigationManager.Uri))
        {
            return null;
        }

        var appVersionProvider = services.GetRequiredService<IAppVersionProvider>();
        return SentrySdk.Init(options =>
        {
            options.Dsn = Dsn;
            options.Debug = false;
            options.AutoSessionTracking = true;
            options.Environment = hostEnvironment.Environment;
            options.Release = appVersionProvider.Current.Version;
        });
    }

    private static bool IsWasmDebugEnabled(string uri) =>
        string.Equals(ResolveQueryValue(uri, WasmDebugQueryKey), WasmDebugEnabledValue, StringComparison.Ordinal);

    private static string ResolveQueryValue(string uri, string key)
    {
        var parsedUri = new Uri(uri, UriKind.Absolute);
        var query = parsedUri.Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        foreach (var pair in query.TrimStart(QueryPrefix).Split(QueryPairSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split(QueryValueSeparator, 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || !string.Equals(parts[0], key, StringComparison.Ordinal))
            {
                continue;
            }

            return parts.Length > 1
                ? Uri.UnescapeDataString(parts[1])
                : string.Empty;
        }

        return string.Empty;
    }
}
