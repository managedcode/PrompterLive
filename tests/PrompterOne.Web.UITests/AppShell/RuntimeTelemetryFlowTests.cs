using System.Text.Json;
using System.Text.Json.Serialization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class RuntimeTelemetryFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public async Task RuntimeTelemetry_TracksPageViewsAndShellActions_InProductionMode()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await WaitForTelemetryInitializationAsync(page);

            await page.GetByTestId(UiTestIds.Header.LibraryNewScript).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Editor));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await WaitForTelemetryCollectionsAsync(
                page,
                BrowserTestConstants.Telemetry.MinimumInitializationCount,
                BrowserTestConstants.Telemetry.ExpectedActionPageViewCount,
                BrowserTestConstants.Telemetry.ExpectedActionEventCount,
                BrowserTestConstants.Telemetry.ExpectedVendorLoadCount);

            var createScriptSnapshot = await ReadSnapshotAsync(page);

            await Assert.That(createScriptSnapshot.Initializations).Contains(entry => entry.RuntimeEnabled && entry.HostEnabled && !entry.DebugEnabled && entry.SentryConfigured && entry.SentryRuntimeEnabled);
            await Assert.That(createScriptSnapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.Library, StringComparison.Ordinal));
            await Assert.That(createScriptSnapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.Editor, StringComparison.Ordinal));
            await Assert.That(createScriptSnapshot.Events).Contains(entry => string.Equals(entry.EventName, AppRuntimeTelemetry.Events.CreateScript, StringComparison.Ordinal));
            await AssertBlockedVendorLoads(createScriptSnapshot);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await WaitForTelemetryInitializationAsync(page);

            await page.GetByTestId(UiTestIds.Header.EditorLearn).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnQuantum));
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync();
            await WaitForTelemetryCollectionsAsync(
                page,
                BrowserTestConstants.Telemetry.MinimumInitializationCount,
                BrowserTestConstants.Telemetry.ExpectedActionPageViewCount,
                BrowserTestConstants.Telemetry.ExpectedActionEventCount,
                BrowserTestConstants.Telemetry.ExpectedVendorLoadCount);

            var learnSnapshot = await ReadSnapshotAsync(page);

            await Assert.That(learnSnapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.Editor, StringComparison.Ordinal));
            await Assert.That(learnSnapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.Learn, StringComparison.Ordinal));
            await Assert.That(learnSnapshot.Events).Contains(entry => string.Equals(entry.EventName, AppRuntimeTelemetry.Events.OpenLearn, StringComparison.Ordinal));
            await AssertBlockedVendorLoads(learnSnapshot);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await WaitForTelemetryInitializationAsync(page);

            await page.GetByTestId(UiTestIds.Header.EditorRead).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.TeleprompterQuantum));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
            await WaitForTelemetryCollectionsAsync(
                page,
                BrowserTestConstants.Telemetry.MinimumInitializationCount,
                BrowserTestConstants.Telemetry.ExpectedActionPageViewCount,
                BrowserTestConstants.Telemetry.ExpectedActionEventCount,
                BrowserTestConstants.Telemetry.ExpectedVendorLoadCount);

            var readSnapshot = await ReadSnapshotAsync(page);

            await Assert.That(readSnapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.Editor, StringComparison.Ordinal));
            await Assert.That(readSnapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.Teleprompter, StringComparison.Ordinal));
            await Assert.That(readSnapshot.Events).Contains(entry => string.Equals(entry.EventName, AppRuntimeTelemetry.Events.OpenRead, StringComparison.Ordinal));
            await AssertBlockedVendorLoads(readSnapshot);

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await WaitForTelemetryInitializationAsync(page);

            await page.GetByTestId(UiTestIds.Header.GoLive).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.GoLive));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await WaitForTelemetryCollectionsAsync(
                page,
                BrowserTestConstants.Telemetry.MinimumInitializationCount,
                BrowserTestConstants.Telemetry.ExpectedActionPageViewCount,
                BrowserTestConstants.Telemetry.ExpectedActionEventCount,
                BrowserTestConstants.Telemetry.ExpectedVendorLoadCount);

            var goLiveSnapshot = await ReadSnapshotAsync(page);

            await Assert.That(goLiveSnapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.Library, StringComparison.Ordinal));
            await Assert.That(goLiveSnapshot.PageViews).Contains(entry => string.Equals(entry.ScreenName, AppRuntimeTelemetry.Pages.GoLive, StringComparison.Ordinal));
            await Assert.That(goLiveSnapshot.Events).Contains(entry => string.Equals(entry.EventName, AppRuntimeTelemetry.Events.OpenGoLive, StringComparison.Ordinal));
            await AssertBlockedVendorLoads(goLiveSnapshot);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task RuntimeTelemetry_DoesNotTrack_WhenWasmDebugQueryIsEnabled()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.LibraryWithWasmDebug);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.DefaultNavigationTimeoutMs
            });
            await WaitForTelemetryInitializationAsync(page);

            await page.GetByTestId(UiTestIds.Header.GoLive).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.GoLive));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            var snapshot = await ReadSnapshotAsync(page);

            await Assert.That(snapshot.Initializations).Contains(entry => entry.DebugEnabled && !entry.RuntimeEnabled && entry.HostEnabled && entry.SentryConfigured && !entry.SentryRuntimeEnabled);
            await Assert.That(snapshot.PageViews).IsEmpty();
            await Assert.That(snapshot.Events).IsEmpty();
            await Assert.That(snapshot.VendorLoads).IsEmpty();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static Task WaitForTelemetryInitializationAsync(Microsoft.Playwright.IPage page) =>
        page.WaitForFunctionAsync(
            "() => (window.__prompterOneTelemetryHarness?.initializations?.length ?? 0) >= 1",
            null,
            new() { Timeout = BrowserTestConstants.Telemetry.TelemetryWaitTimeoutMs });

    private static Task WaitForTelemetryCollectionsAsync(
        Microsoft.Playwright.IPage page,
        int initializationCount,
        int pageViewCount,
        int eventCount,
        int vendorLoadCount) =>
        page.WaitForFunctionAsync(
            """
            ([minimumInitializations, minimumPageViews, minimumEvents, minimumVendorLoads]) => {
                const harness = window.__prompterOneTelemetryHarness;
                return (harness?.initializations?.length ?? 0) >= minimumInitializations
                    && (harness?.pageViews?.length ?? 0) >= minimumPageViews
                    && (harness?.events?.length ?? 0) >= minimumEvents
                    && (harness?.vendorLoads?.length ?? 0) >= minimumVendorLoads;
            }
            """,
            new object[] { initializationCount, pageViewCount, eventCount, vendorLoadCount },
            new() { Timeout = BrowserTestConstants.Telemetry.TelemetryWaitTimeoutMs });

    private static async Task<TelemetryHarnessSnapshot> ReadSnapshotAsync(Microsoft.Playwright.IPage page)
    {
        var json = await page.EvaluateAsync<string>(
            """
            () => JSON.stringify({
                events: window.__prompterOneTelemetryHarness?.events ?? [],
                initializations: window.__prompterOneTelemetryHarness?.initializations ?? [],
                pageViews: window.__prompterOneTelemetryHarness?.pageViews ?? [],
                vendorLoads: window.__prompterOneTelemetryHarness?.vendorLoads ?? []
            })
            """);

        return JsonSerializer.Deserialize<TelemetryHarnessSnapshot>(json, SnapshotJsonOptions)
            ?? new TelemetryHarnessSnapshot();
    }

    private static async Task AssertBlockedVendorLoads(TelemetryHarnessSnapshot snapshot)
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
