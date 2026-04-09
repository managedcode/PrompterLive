using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class EditorTooltipDriver
{
    internal static ILocator GetToolbarTooltip(IPage page, string tooltipText) =>
        page.GetByTestId(UiTestIds.Editor.ToolbarTooltip)
            .Filter(new() { HasTextString = tooltipText });

    internal static async Task WaitUntilFullyVisibleAsync(IPage page, ILocator tooltip, string tooltipText)
    {
        await Expect(tooltip).ToBeVisibleAsync();

        await page.WaitForFunctionAsync(
            """
            args => Array.from(document.querySelectorAll(`[data-test="${args.testId}"]`))
                .filter(node => (node.textContent ?? '').includes(args.text))
                .some(node => {
                    const styles = getComputedStyle(node);
                    return styles.visibility !== "hidden"
                        && Number.parseFloat(styles.opacity || "0") >= args.minimumOpacity;
                })
            """,
            new
            {
                minimumOpacity = BrowserTestConstants.EditorFlow.MinimumVisibleTooltipOpacity,
                testId = UiTestIds.Editor.ToolbarTooltip,
                text = tooltipText
            },
            new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

        await Expect(tooltip).ToHaveTextAsync(tooltipText);
    }

    internal static async Task<double> ReadOpacityAsync(ILocator locator) =>
        await locator.EvaluateAsync<double>(
            """
            element => Number.parseFloat(getComputedStyle(element).opacity)
            """);
}
