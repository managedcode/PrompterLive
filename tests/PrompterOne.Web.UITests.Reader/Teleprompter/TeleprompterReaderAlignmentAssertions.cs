using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class TeleprompterReaderAlignmentAssertions
{
    internal static string BuildDataTestSelector(string testId) =>
        $"[{BrowserTestConstants.Html.DataTestAttribute}='{testId}']";

    internal static async Task AssertWordAlignedToGuideAsync(IPage page, string wordSelector)
    {
        var word = page.Locator(wordSelector);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.FocalGuide)).ToBeVisibleAsync();
        await Expect(word).ToBeVisibleAsync(new()
        {
            Timeout = BrowserTestConstants.Timing.ReaderPlaybackAdvanceTimeoutMs
        });

        var attemptCount = BrowserTestConstants.Timing.ReaderPlaybackAdvanceTimeoutMs /
            BrowserTestConstants.Teleprompter.AlignmentPollDelayMs;
        ReaderAlignmentProbe lastProbe = new();

        for (var attempt = 0; attempt < attemptCount; attempt++)
        {
            lastProbe = await CaptureAlignmentProbeAsync(page, wordSelector);
            if (lastProbe.IsMeasurable &&
                lastProbe.IsActiveCard &&
                Math.Abs(lastProbe.CenterDelta) <= BrowserTestConstants.Teleprompter.AlignmentTolerancePx)
            {
                return;
            }

            await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.AlignmentPollDelayMs);
        }

        await Assert.That(lastProbe.IsMeasurable).IsTrue();
        await Assert.That(lastProbe.IsActiveCard).IsTrue();
        await Assert.That(Math.Abs(lastProbe.CenterDelta))
            .IsBetween(0d, BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
    }

    private static Task<ReaderAlignmentProbe> CaptureAlignmentProbeAsync(IPage page, string wordSelector) =>
        page.EvaluateAsync<ReaderAlignmentProbe>(
            """
            args => {
                const focalGuide = document.querySelector(`[data-test="${args.focalGuideTestId}"]`);
                const word = document.querySelector(args.wordSelector);

                if (!(focalGuide instanceof HTMLElement) || !(word instanceof HTMLElement)) {
                    return { isMeasurable: false, isActiveCard: false, centerDelta: Number.MAX_VALUE };
                }

                const card = word.closest(`[${args.cardStateAttributeName}]`);
                const focalGuideRect = focalGuide.getBoundingClientRect();
                const wordRect = word.getBoundingClientRect();

                return {
                    isMeasurable: true,
                    isActiveCard: card instanceof HTMLElement &&
                        card.getAttribute(args.cardStateAttributeName) === args.activeState,
                    centerDelta:
                        (focalGuideRect.top + focalGuideRect.height / 2) -
                        (wordRect.top + wordRect.height / 2)
                };
            }
            """,
            new
            {
                activeState = UiDataAttributes.Teleprompter.ActiveState,
                cardStateAttributeName = UiDataAttributes.Teleprompter.CardState,
                focalGuideTestId = UiTestIds.Teleprompter.FocalGuide,
                wordSelector
            });

    private sealed class ReaderAlignmentProbe
    {
        public bool IsMeasurable { get; set; }
        public bool IsActiveCard { get; set; }
        public double CenterDelta { get; set; } = double.MaxValue;
    }
}
