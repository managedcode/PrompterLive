using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class DynamicHostPortTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task NewPageAsync_UsesDynamicLoopbackBaseAddress()
    {
        var baseAddress = new Uri(_fixture.BaseAddress);

        await Assert.That(baseAddress.Scheme).IsEqualTo(Uri.UriSchemeHttp);
        await Assert.That(baseAddress.IsLoopback).IsTrue();
        await Assert.That(baseAddress.Port).IsBetween(UiTestHostConstants.MinimumDynamicPort,UiTestHostConstants.MaximumTcpPort);

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
}