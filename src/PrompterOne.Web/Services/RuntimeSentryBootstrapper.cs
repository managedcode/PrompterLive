using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Services;

namespace PrompterOne.Web.Services;

internal static class RuntimeSentryBootstrapper
{
    private const char QueryPairSeparator = '&';
    private const char QueryPrefix = '?';
    private const char QueryValueSeparator = '=';
    private const string WasmDebugEnabledValue = "1";
    private const string WasmDebugQueryKey = "wasm-debug";

    public static void Configure(WebAssemblyHostBuilder builder, RuntimeTelemetryOptions telemetryOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(telemetryOptions);

        if (!ShouldEnable(telemetryOptions))
        {
            return;
        }

        var release = AppVersionProviderFactory
            .CreateFromAssembly(typeof(RuntimeSentryBootstrapper).Assembly)
            .Current
            .Version;

        builder.UseSentry(options =>
        {
            options.Dsn = telemetryOptions.SentryDsn;
            options.AutoSessionTracking = true;
            options.Environment = builder.HostEnvironment.Environment;
            options.Release = release;
            options.SendDefaultPii = true;
            options.SetBeforeSend(sentryEvent =>
                RuntimeTelemetrySuppressionPolicy.ShouldSuppressOutboundSentry(sentryEvent)
                    ? null
                    : sentryEvent);
#if DEBUG
            options.Debug = true;
#endif
        });

        builder.Logging.AddSentry(options => options.InitializeSdk = false);
    }

    public static void DisableForWasmDebug(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var navigationManager = services.GetRequiredService<NavigationManager>();
        if (IsWasmDebugEnabled(navigationManager.Uri))
        {

            SentrySdk.Close();
        }
    }

    private static bool ShouldEnable(RuntimeTelemetryOptions telemetryOptions) =>
        telemetryOptions.HostEnabled
        && telemetryOptions.SentryConfigured;

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
