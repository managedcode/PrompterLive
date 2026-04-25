using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorRenderedViewFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const string RawHeadingMarker = "##";
    private const string RawTagOpen = "[";
    private const string RawTagClose = "]";
    private const int IntroSegmentIndex = 0;
    private const int OpeningBlockIndex = 0;
    private const int ClosingBlockIndex = 2;
    private const string ReorderScript = """
        # Reorder Probe
        ## [Scene|140WPM|neutral]
        ### [Opening|Speaker:Host|140WPM|warm]
        Alpha opening block.

        ### [Middle|Speaker:Host|140WPM|focused]
        Bravo middle block.

        ### [Closing|Speaker:Host|140WPM|calm]
        Charlie closing block.
        """;
    private const string OpeningHeading = "### [Opening|Speaker:Host|140WPM|warm]";
    private const string MiddleHeading = "### [Middle|Speaker:Host|140WPM|focused]";
    private const string ClosingHeading = "### [Closing|Speaker:Host|140WPM|calm]";

    [Test]
    public Task EditorScreen_RenderedCardsViewHidesSyntaxAndWritesBackToSource() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);

            await page.GetByTestId(UiTestIds.Editor.RenderedTab).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedView))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var renderedBlock = page.GetByTestId(UiTestIds.Editor.RenderedBlockText(IntroSegmentIndex, OpeningBlockIndex));
            await Expect(renderedBlock)
                .ToHaveValueAsync(
                    new Regex(Regex.Escape(BrowserTestConstants.Editor.RenderedOpeningProbe)),
                    new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            var renderedValue = await renderedBlock.InputValueAsync();
            await Assert.That(renderedValue).DoesNotContain(RawHeadingMarker);
            await Assert.That(renderedValue).DoesNotContain(RawTagOpen);
            await Assert.That(renderedValue).DoesNotContain(RawTagClose);

            await renderedBlock.FillAsync(BrowserTestConstants.Editor.RenderedOpeningRewrite);
            await Expect(renderedBlock).ToHaveValueAsync(BrowserTestConstants.Editor.RenderedOpeningRewrite);

            await page.GetByTestId(UiTestIds.Editor.SourceTab).ClickAsync();
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            await Expect(sourceInput)
                .ToHaveValueAsync(
                    new Regex(Regex.Escape(BrowserTestConstants.Editor.RenderedOpeningRewrite)),
                    new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            var sourceValue = await sourceInput.InputValueAsync();
            await Assert.That(sourceValue).Contains(BrowserTestConstants.Editor.OpeningBlockHeading);
            await Assert.That(sourceValue).Contains(BrowserTestConstants.Editor.BodyHeading);
        });

    [Test]
    public Task EditorScreen_RenderedCardsView_ReordersBlocksWithFallbackAndDragDrop() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, "Rendered reorder probe");
            await EditorMonacoDriver.SetTextAsync(page, ReorderScript);
            await page.GetByTestId(UiTestIds.Editor.RenderedTab).ClickAsync();

            await page.GetByTestId(UiTestIds.Editor.RenderedBlockMoveDown(IntroSegmentIndex, OpeningBlockIndex)).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.SourceTab).ClickAsync();
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            await Expect(sourceInput)
                .ToHaveValueAsync(
                    new Regex($"{Regex.Escape(MiddleHeading)}[\\s\\S]+{Regex.Escape(OpeningHeading)}[\\s\\S]+{Regex.Escape(ClosingHeading)}"),
                    new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            await page.GetByTestId(UiTestIds.Editor.RenderedTab).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.RenderedBlock(IntroSegmentIndex, ClosingBlockIndex))
                .DragToAsync(page.GetByTestId(UiTestIds.Editor.RenderedBlock(IntroSegmentIndex, OpeningBlockIndex)));
            await page.GetByTestId(UiTestIds.Editor.SourceTab).ClickAsync();
            await Expect(sourceInput)
                .ToHaveValueAsync(
                    new Regex($"{Regex.Escape(ClosingHeading)}[\\s\\S]+{Regex.Escape(MiddleHeading)}[\\s\\S]+{Regex.Escape(OpeningHeading)}"),
                    new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            var sourceValue = await sourceInput.InputValueAsync();
            await Assert.That(sourceValue).Contains("Alpha opening block.");
            await Assert.That(sourceValue).Contains("Bravo middle block.");
            await Assert.That(sourceValue).Contains("Charlie closing block.");
        });
}
