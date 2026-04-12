using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorScriptGraphViewTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_GraphTabRendersScriptKnowledgeGraphControls()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);

            await page.GetByTestId(UiTestIds.Editor.GraphTab).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Editor.GraphPanel))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSummary))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphControls))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphZoomIn))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphZoomOut))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphFit))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphLayoutMode))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphReadyAttributeName,
                    BrowserTestConstants.Editor.GraphReadyAttributeValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphTooltipsAttributeName,
                    BrowserTestConstants.Editor.GraphTooltipsAttributeValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Assert.That(await page.GetByTestId(UiTestIds.Editor.GraphNodeList).CountAsync()).IsEqualTo(0);

            await page.GetByTestId(UiTestIds.Editor.GraphZoomIn).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.GraphZoomOut).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.GraphFit).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.GraphLayoutMode).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphLayoutAttributeName,
                    BrowserTestConstants.Editor.GraphLayoutCompactValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
