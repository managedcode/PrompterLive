using Microsoft.Playwright;

namespace PrompterOne.Web.UITests;

internal static class RapidControlDriver
{
    public static Task BurstClickAsync(IPage page, string testId, int count) =>
        page.EvaluateAsync(
            """
            args => {
                const selector = `[data-test="${args.testId}"]`;
                const element = document.querySelector(selector);
                if (!(element instanceof HTMLElement)) {
                    throw new Error(`Unable to find rapid-click target: ${args.testId}`);
                }

                for (let clickIndex = 0; clickIndex < args.count; clickIndex += 1) {
                    element.click();
                }
            }
            """,
            new
            {
                count,
                testId
            });
}
