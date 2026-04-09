using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class RuntimeTelemetryFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private const string WaitForTelemetryInitializationScript =
        $$"""
        () => (window["{{BrowserTestConstants.Telemetry.HarnessGlobal}}"]?.["{{BrowserTestConstants.Telemetry.InitializationsCollection}}"]?.length ?? 0) >= 1
        """;
    private const string WaitForTelemetryCollectionsScript =
        $$"""
        ([minimumInitializations, minimumPageViews, minimumEvents, minimumVendorLoads]) => {
            const harness = window["{{BrowserTestConstants.Telemetry.HarnessGlobal}}"];
            return (harness?.["{{BrowserTestConstants.Telemetry.InitializationsCollection}}"]?.length ?? 0) >= minimumInitializations
                && (harness?.["{{BrowserTestConstants.Telemetry.PageViewsCollection}}"]?.length ?? 0) >= minimumPageViews
                && (harness?.["{{BrowserTestConstants.Telemetry.EventsCollection}}"]?.length ?? 0) >= minimumEvents
                && (harness?.["{{BrowserTestConstants.Telemetry.VendorLoadsCollection}}"]?.length ?? 0) >= minimumVendorLoads;
        }
        """;
    private const string ReadTelemetrySnapshotScript =
        $$"""
        () => JSON.stringify({
            events: window["{{BrowserTestConstants.Telemetry.HarnessGlobal}}"]?.["{{BrowserTestConstants.Telemetry.EventsCollection}}"] ?? [],
            initializations: window["{{BrowserTestConstants.Telemetry.HarnessGlobal}}"]?.["{{BrowserTestConstants.Telemetry.InitializationsCollection}}"] ?? [],
            pageViews: window["{{BrowserTestConstants.Telemetry.HarnessGlobal}}"]?.["{{BrowserTestConstants.Telemetry.PageViewsCollection}}"] ?? [],
            vendorLoads: window["{{BrowserTestConstants.Telemetry.HarnessGlobal}}"]?.["{{BrowserTestConstants.Telemetry.VendorLoadsCollection}}"] ?? []
        })
        """;
    private const string ClarityVendorScriptBody =
        """
        window.clarity = window.clarity || function () {
            window.clarity.q = window.clarity.q || [];
            window.clarity.q.push(arguments);
        };
        """;
    private const string GoogleAnalyticsVendorScriptBody = "window.dataLayer = window.dataLayer || [];";
    private const string JavaScriptContentType = "text/javascript";

    [Test]
    public async Task RuntimeTelemetry_RequestsRealVendorScripts_WhenHarnessAllowsVendorLoads()
    {
        var page = await _fixture.NewPageAsync();
        var googleAnalyticsRequests = new ConcurrentQueue<string>();
        var clarityRequests = new ConcurrentQueue<string>();

        try
        {
            await page.Context.RouteAsync(
                BrowserTestConstants.Telemetry.GoogleAnalyticsScriptRequestPattern,
                async route =>
                {
                    googleAnalyticsRequests.Enqueue(route.Request.Url);
                    await route.FulfillAsync(new()
                    {
                        Body = GoogleAnalyticsVendorScriptBody,
                        ContentType = JavaScriptContentType,
                        Status = 200
                    });
                });

            await page.Context.RouteAsync(
                BrowserTestConstants.Telemetry.ClarityScriptRequestPattern,
                async route =>
                {
                    clarityRequests.Enqueue(route.Request.Url);
                    await route.FulfillAsync(new()
                    {
                        Body = ClarityVendorScriptBody,
                        ContentType = JavaScriptContentType,
                        Status = 200
                    });
                });

            await page.AddInitScriptAsync(UiTestHostConstants.RuntimeTelemetryAllowVendorLoadsScript);

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await WaitForTelemetryCollectionsAsync(
                page,
                BrowserTestConstants.Telemetry.MinimumInitializationCount,
                BrowserTestConstants.Telemetry.MinimumPageViewCount,
                0,
                BrowserTestConstants.Telemetry.ExpectedVendorLoadCount);

            var snapshot = await ReadSnapshotAsync(page);

            await Assert.That(snapshot.Initializations).Contains(entry =>
                entry.ClarityConfigured
                && !entry.DebugEnabled
                && entry.GoogleAnalyticsConfigured
                && entry.HostEnabled
                && entry.RuntimeEnabled
                && entry.SentryConfigured
                && entry.SentryRuntimeEnabled);
            await Assert.That(snapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.Library, StringComparison.Ordinal));
            await Assert.That(snapshot.VendorLoads).Contains(entry =>
                !entry.Blocked
                && string.Equals(entry.Provider, BrowserTestConstants.Telemetry.GoogleAnalyticsProvider, StringComparison.Ordinal)
                && entry.Url.StartsWith(BrowserTestConstants.Telemetry.GoogleAnalyticsScriptUrlPrefix, StringComparison.Ordinal));
            await Assert.That(snapshot.VendorLoads).Contains(entry =>
                !entry.Blocked
                && string.Equals(entry.Provider, BrowserTestConstants.Telemetry.ClarityProvider, StringComparison.Ordinal)
                && entry.Url.StartsWith(BrowserTestConstants.Telemetry.ClarityScriptUrlPrefix, StringComparison.Ordinal));
            await Assert.That(googleAnalyticsRequests.ToArray()).Contains(url => url.StartsWith(BrowserTestConstants.Telemetry.GoogleAnalyticsScriptUrlPrefix, StringComparison.Ordinal));
            await Assert.That(clarityRequests.ToArray()).Contains(url => url.StartsWith(BrowserTestConstants.Telemetry.ClarityScriptUrlPrefix, StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static Task WaitForTelemetryInitializationAsync(Microsoft.Playwright.IPage page) =>
        page.WaitForFunctionAsync(
            WaitForTelemetryInitializationScript,
            null,
            new() { Timeout = BrowserTestConstants.Telemetry.TelemetryWaitTimeoutMs });

    private static Task WaitForTelemetryCollectionsAsync(
        Microsoft.Playwright.IPage page,
        int initializationCount,
        int pageViewCount,
        int eventCount,
        int vendorLoadCount) =>
        page.WaitForFunctionAsync(
            WaitForTelemetryCollectionsScript,
            new object[] { initializationCount, pageViewCount, eventCount, vendorLoadCount },
            new() { Timeout = BrowserTestConstants.Telemetry.TelemetryWaitTimeoutMs });

    private static async Task<TelemetryHarnessSnapshot> ReadSnapshotAsync(Microsoft.Playwright.IPage page)
    {
        var json = await page.EvaluateAsync<string>(ReadTelemetrySnapshotScript);

        return JsonSerializer.Deserialize<TelemetryHarnessSnapshot>(json, SnapshotJsonOptions)
            ?? new TelemetryHarnessSnapshot();
    }

    private static async Task AssertHarnessBlockedVendorLoads(TelemetryHarnessSnapshot snapshot)
    {
        await Assert.That(snapshot.VendorLoads).Contains(entry => string.Equals(entry.Provider, BrowserTestConstants.Telemetry.GoogleAnalyticsProvider, StringComparison.Ordinal) && entry.Blocked);
        await Assert.That(snapshot.VendorLoads).Contains(entry => string.Equals(entry.Provider, BrowserTestConstants.Telemetry.ClarityProvider, StringComparison.Ordinal) && entry.Blocked);
    }

    public sealed class TelemetryHarnessSnapshot
    {
        public TelemetryEventEntry[] Events { get; set; } = [];
        public TelemetryInitializationEntry[] Initializations { get; set; } = [];
        public TelemetryPageViewEntry[] PageViews { get; set; } = [];
        public TelemetryVendorLoadEntry[] VendorLoads { get; set; } = [];
    }

    public sealed class TelemetryEventEntry
    {
        [JsonPropertyName("eventName")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("script_loaded")]
        public bool ScriptLoaded { get; set; }

        [JsonPropertyName("source_screen")]
        public string SourceScreen { get; set; } = string.Empty;

        [JsonPropertyName("target_screen")]
        public string TargetScreen { get; set; } = string.Empty;
    }

    public sealed class TelemetryInitializationEntry
    {
        [JsonPropertyName("clarityConfigured")]
        public bool ClarityConfigured { get; set; }

        [JsonPropertyName("debugEnabled")]
        public bool DebugEnabled { get; set; }

        [JsonPropertyName("googleAnalyticsConfigured")]
        public bool GoogleAnalyticsConfigured { get; set; }

        [JsonPropertyName("hostEnabled")]
        public bool HostEnabled { get; set; }

        [JsonPropertyName("runtimeEnabled")]
        public bool RuntimeEnabled { get; set; }

        [JsonPropertyName("sentryConfigured")]
        public bool SentryConfigured { get; set; }

        [JsonPropertyName("sentryRuntimeEnabled")]
        public bool SentryRuntimeEnabled { get; set; }
    }

    public sealed class TelemetryPageViewEntry
    {
        [JsonPropertyName("eventName")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("page_path")]
        public string PagePath { get; set; } = string.Empty;

        [JsonPropertyName("page_title")]
        public string PageTitle { get; set; } = string.Empty;

        [JsonPropertyName("screen_name")]
        public string ScreenName { get; set; } = string.Empty;

        [JsonPropertyName("script_loaded")]
        public bool ScriptLoaded { get; set; }
    }

    public sealed class TelemetryVendorLoadEntry
    {
        [JsonPropertyName("blocked")]
        public bool Blocked { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
