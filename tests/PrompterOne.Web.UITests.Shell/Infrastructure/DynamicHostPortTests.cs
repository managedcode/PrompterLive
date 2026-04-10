using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class DynamicHostPortTests(StandaloneAppFixture fixture)
{
    private const string FixtureStorageProbeKey = "fixture-shared-context-probe";
    private const string FixtureStorageProbeValue = "shared-context-visible";
    private const string ReadLocalStorageScript = "(key) => window.localStorage.getItem(key) ?? ''";
    private const int RepeatedBootstrapPageCount = 10;
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task NewPageAsync_UsesDynamicLoopbackBaseAddress()
    {
        var baseAddress = new Uri(_fixture.BaseAddress);

        await Assert.That(baseAddress.Scheme).IsEqualTo(Uri.UriSchemeHttp);
        await Assert.That(baseAddress.IsLoopback).IsTrue();
        await Assert.That(baseAddress.Port).IsBetween(UiTestHostConstants.MinimumDynamicPort, UiTestHostConstants.MaximumTcpPort);

        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await BrowserRouteDriver.OpenPageAsync(
                page,
                BrowserTestConstants.Routes.Library,
                UiTestIds.Library.Page,
                nameof(NewPageAsync_UsesDynamicLoopbackBaseAddress));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task NewPageAsync_RepeatedBootstrap_DoesNotTripShellDiagnostics()
    {
        for (var pageIndex = 0; pageIndex < RepeatedBootstrapPageCount; pageIndex++)
        {
            var page = await _fixture.NewPageAsync(additionalContext: true);

            try
            {
                await BrowserRouteDriver.OpenPageAsync(
                    page,
                    BrowserTestConstants.Routes.Library,
                    UiTestIds.Library.Page,
                    nameof(NewPageAsync_RepeatedBootstrap_DoesNotTripShellDiagnostics));
                await Expect(page.GetByTestId(UiTestIds.Diagnostics.Bootstrap)).ToBeHiddenAsync();
            }
            finally
            {
                await page.Context.CloseAsync();
            }
        }
    }

    [Test]
    public async Task NewPageAsync_DefaultPath_ReusesSharedBrowserStorage()
    {
        var primaryPage = await _fixture.NewSharedPageAsync(nameof(NewPageAsync_DefaultPath_ReusesSharedBrowserStorage));
        var secondaryPage = await _fixture.NewSharedPageAsync(nameof(NewPageAsync_DefaultPath_ReusesSharedBrowserStorage));

        try
        {
            await primaryPage.EvaluateAsync(
                BrowserTestConstants.Localization.SetLocalStorageScript,
                new[] { FixtureStorageProbeKey, FixtureStorageProbeValue });

            var storedValue = await secondaryPage.EvaluateAsync<string>(ReadLocalStorageScript, FixtureStorageProbeKey);
            var seededLibrary = await secondaryPage.EvaluateAsync<string>(ReadLocalStorageScript, BrowserStorageKeys.DocumentLibrary);

            await Assert.That(storedValue).IsEqualTo(FixtureStorageProbeValue);
            await Assert.That(string.IsNullOrWhiteSpace(seededLibrary)).IsFalse();
        }
        finally
        {
            await primaryPage.Context.CloseAsync();
        }
    }

    [Test]
    public async Task NewPageAsync_AdditionalContext_KeepsBrowserStorageIsolated()
    {
        var sharedPage = await _fixture.NewSharedPageAsync(nameof(NewPageAsync_AdditionalContext_KeepsBrowserStorageIsolated));
        var isolatedPage = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await sharedPage.EvaluateAsync(
                BrowserTestConstants.Localization.SetLocalStorageScript,
                new[] { FixtureStorageProbeKey, FixtureStorageProbeValue });

            var storedValue = await isolatedPage.EvaluateAsync<string>(ReadLocalStorageScript, FixtureStorageProbeKey);
            var seededLibrary = await isolatedPage.EvaluateAsync<string>(ReadLocalStorageScript, BrowserStorageKeys.DocumentLibrary);

            await Assert.That(storedValue).IsEqualTo(string.Empty);
            await Assert.That(string.IsNullOrWhiteSpace(seededLibrary)).IsFalse();
        }
        finally
        {
            await sharedPage.Context.CloseAsync();
            await isolatedPage.Context.CloseAsync();
        }
    }
}
