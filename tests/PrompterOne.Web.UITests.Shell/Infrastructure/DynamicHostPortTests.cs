using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class DynamicHostPortTests(StandaloneAppFixture fixture)
{
    private const int RepeatedBootstrapPageCount = 10;
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task NewPageAsync_UsesDynamicLoopbackBaseAddress()
    {
        var baseAddress = new Uri(_fixture.BaseAddress);

        await Assert.That(baseAddress.Scheme).IsEqualTo(Uri.UriSchemeHttp);
        await Assert.That(baseAddress.IsLoopback).IsTrue();
        await Assert.That(baseAddress.Port).IsBetween(UiTestHostConstants.MinimumDynamicPort, UiTestHostConstants.MaximumTcpPort);

        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task NewPageAsync_RepeatedBootstrap_DoesNotTripShellDiagnostics()
    {
        var pages = new List<IPage>(RepeatedBootstrapPageCount);

        try
        {
            for (var pageIndex = 0; pageIndex < RepeatedBootstrapPageCount; pageIndex++)
            {
                var page = await _fixture.NewPageAsync();
                pages.Add(page);

                await page.GotoAsync(BrowserTestConstants.Routes.Library);
                await Expect(page.GetByTestId(UiTestIds.Library.Page))
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
                await Expect(page.GetByTestId(UiTestIds.Diagnostics.Bootstrap)).ToBeHiddenAsync();
            }
        }
        finally
        {
            foreach (var page in pages)
            {
                await page.Context.CloseAsync();
            }
        }
    }
}
