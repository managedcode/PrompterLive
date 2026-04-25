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
}
