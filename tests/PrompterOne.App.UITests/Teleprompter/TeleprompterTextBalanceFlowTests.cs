using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class TeleprompterTextBalanceFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private const int OpeningCardIndex = 0;

    [Fact]
    public Task TeleprompterScreen_OpeningBlock_KeepsMultiLineTextVisuallyBalancedWithinStage() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var balance = await page.GetByTestId(UiTestIds.Teleprompter.CardText(OpeningCardIndex))
                .EvaluateAsync<ReaderLineBalanceProbe?>(
                    """
                    (element, stageId) => {
                        if (!(element instanceof HTMLElement)) {
                            return null;
                        }

                        const stage = document.getElementById(stageId);
                        if (!(stage instanceof HTMLElement)) {
                            return null;
                        }

                        const range = document.createRange();
                        range.selectNodeContents(element);

                        const stageRect = stage.getBoundingClientRect();
                        const wordRects = Array
                            .from(range.getClientRects())
                            .filter(rect => rect.width > 0 && rect.height > 0);

                        if (wordRects.length === 0) {
                            return {
                                lineCount: 0,
                                maxAsymmetryPx: 0,
                                averageAsymmetryPx: 0
                            };
                        }

                        const lineRectMap = new Map();
                        for (const rect of wordRects) {
                            const key = Math.round(rect.top);
                            const existing = lineRectMap.get(key);
                            if (existing) {
                                existing.left = Math.min(existing.left, rect.left);
                                existing.right = Math.max(existing.right, rect.right);
                            } else {
                                lineRectMap.set(key, { left: rect.left, right: rect.right });
                            }
                        }

                        const lineRects = Array.from(lineRectMap.values());
                        const asymmetries = lineRects.map(rect => {
                            const leftGap = rect.left - stageRect.left;
                            const rightGap = stageRect.right - rect.right;
                            return Math.abs(leftGap - rightGap);
                        });

                        const totalAsymmetry = asymmetries.reduce((sum, value) => sum + value, 0);
                        return {
                            lineCount: lineRects.length,
                            maxAsymmetryPx: Math.max(...asymmetries),
                            averageAsymmetryPx: totalAsymmetry / asymmetries.length
                        };
                    }
                    """,
                    UiDomIds.Teleprompter.ClusterWrap);

            Assert.NotNull(balance);
            Assert.True(
                balance.LineCount >= BrowserTestConstants.TeleprompterFlow.MinimumBalancedTextLineCount,
                $"Expected at least {BrowserTestConstants.TeleprompterFlow.MinimumBalancedTextLineCount} visible text lines, but found {balance.LineCount}.");
            Assert.InRange(
                balance.MaxAsymmetryPx,
                0,
                BrowserTestConstants.TeleprompterFlow.MaximumTextLineAsymmetryPx);
            Assert.InRange(
                balance.AverageAsymmetryPx,
                0,
                BrowserTestConstants.TeleprompterFlow.MaximumAverageTextLineAsymmetryPx);
        });

    private sealed class ReaderLineBalanceProbe
    {
        public int LineCount { get; init; }

        public double MaxAsymmetryPx { get; init; }

        public double AverageAsymmetryPx { get; init; }
    }
}
