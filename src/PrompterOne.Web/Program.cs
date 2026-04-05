using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Services;
using PrompterOne.Web;
using PrompterOne.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("PrompterOne", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer", LogLevel.Warning);

var runtimeTelemetryHostEnabled =
    builder.Configuration.GetValue<bool?>(RuntimeTelemetryOptions.HostEnabledPath)
    ?? !builder.HostEnvironment.IsDevelopment();

var runtimeTelemetryOptions = new RuntimeTelemetryOptions(
    builder.Configuration[RuntimeTelemetryOptions.GoogleAnalyticsMeasurementIdPath] ?? string.Empty,
    builder.Configuration[RuntimeTelemetryOptions.ClarityProjectIdPath] ?? string.Empty,
    HostEnabled: runtimeTelemetryHostEnabled,
    SentryConfigured: RuntimeSentryBootstrapper.IsConfigured);

builder.Services.AddLocalization();
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IAppVersionProvider>(_ => AppVersionProviderFactory.CreateFromAssembly(typeof(Program).Assembly));
builder.Services.AddPrompterOneShared(runtimeTelemetryOptions);

var host = builder.Build();
using var sentry = RuntimeSentryBootstrapper.Initialize(host.Services, runtimeTelemetryHostEnabled);
await host.Services.GetRequiredService<AppCulturePreferenceService>().InitializeAsync();
await host.RunAsync();
