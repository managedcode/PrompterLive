using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class EditorLayoutTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_MetadataRailStaysDockedToRightOfMainPanel()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);

            var mainPanel = page.GetByTestId(UiTestIds.Editor.MainPanel);
            var metadataRail = page.GetByTestId(UiTestIds.Editor.MetadataRail);

            await Expect(mainPanel)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(metadataRail)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var mainBounds = await GetRequiredBoundingBoxAsync(mainPanel);
            var railBounds = await GetRequiredBoundingBoxAsync(metadataRail);
            var dockGap = railBounds.X - (mainBounds.X + mainBounds.Width);
            var bottomEdgeDrift = Math.Abs((railBounds.Y + railBounds.Height) - (mainBounds.Y + mainBounds.Height));

            Assert.InRange(
                Math.Abs(dockGap - BrowserTestConstants.Editor.MetadataRailDockGapPx),
                0,
                BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
            Assert.InRange(
                Math.Abs(railBounds.Y - mainBounds.Y),
                0,
                BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
            Assert.InRange(
                bottomEdgeDrift,
                0,
                BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_SourceEditorUsesSingleVerticalScrollSurface()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Editor);

            var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);
            var sourceScrollHost = page.GetByTestId(UiTestIds.Editor.SourceScrollHost);

            await Expect(sourceInput)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(sourceScrollHost)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await sourceInput.EvaluateAsync(
                """
                (element, lineCount) => {
                    element.value = Array.from({ length: lineCount }, (_, index) => `Scroll probe line ${index + 1}`).join('\n');
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                    element.scrollTop = element.scrollHeight;
                    element.dispatchEvent(new Event('scroll', { bubbles: true }));
                }
                """,
                BrowserTestConstants.Editor.ScrollProbeLineCount);

            var scrollState = await sourceInput.EvaluateAsync<EditorScrollState>(
                """
                element => {
                    const host = element.closest('[data-testid="editor-source-scroll-host"]');
                    return {
                        inputScrollTop: element.scrollTop,
                        hostScrollTop: host ? host.scrollTop : -1,
                        hostOverflowY: host ? getComputedStyle(host).overflowY : ''
                    };
                }
                """);

            Assert.True(scrollState.InputScrollTop > 0);
            Assert.Equal(BrowserTestConstants.Editor.MaxSourceScrollHostTopPx, scrollState.HostScrollTop);
            Assert.Equal("hidden", scrollState.HostOverflowY);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task<LayoutBounds> GetRequiredBoundingBoxAsync(ILocator locator) =>
        await locator.EvaluateAsync<LayoutBounds>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return {
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                };
            }
            """);

    private readonly record struct EditorScrollState(double InputScrollTop, double HostScrollTop, string HostOverflowY);
    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);
}
