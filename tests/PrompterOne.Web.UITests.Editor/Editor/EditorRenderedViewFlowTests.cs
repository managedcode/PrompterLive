using System.Text.RegularExpressions;
using Microsoft.Playwright;
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
    private const string BackgroundColorProperty = "backgroundColor";
    private const double MaximumDarkRenderedSurfaceChannel = 170;

    [Test]
    public Task EditorScreen_RenderedStyledTextViewHidesSyntaxAndWritesBackToSource() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);

            await page.GetByTestId(UiTestIds.Editor.WorkspaceEditorTab).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedView))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceTab)).ToHaveTextAsync("Raw");
            await Expect(page.GetByTestId(UiTestIds.Editor.WorkspaceEditorTab)).ToHaveTextAsync("Editor");
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedView))
                .ToHaveAttributeAsync("data-rendered-authoring-mode", "styled-text");
            var renderedViewColor = await ReadCssColorAsync(page.GetByTestId(UiTestIds.Editor.RenderedView), BackgroundColorProperty);
            await Assert.That(HasMaximumChannels(renderedViewColor, MaximumDarkRenderedSurfaceChannel)).IsTrue().Because($"Expected dark-theme rendered text editor to avoid a white paper surface, but got rgba({renderedViewColor.R:0.##}, {renderedViewColor.G:0.##}, {renderedViewColor.B:0.##}, {renderedViewColor.A:0.##}).");
            await Assert.That(await page.Locator(".editor-rendered-card").CountAsync()).IsEqualTo(0);
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedView))
                .Not.ToHaveAttributeAsync("data-rendered-cards-drag-ready", "true");
            var renderedBlock = page.GetByTestId(UiTestIds.Editor.RenderedBlockText(IntroSegmentIndex, OpeningBlockIndex));
            await Expect(renderedBlock)
                .ToHaveValueAsync(
                    new Regex(Regex.Escape(BrowserTestConstants.Editor.RenderedOpeningProbe)),
                    new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            var renderedValue = await renderedBlock.InputValueAsync();
            await Assert.That(renderedValue).DoesNotContain(RawHeadingMarker);
            await Assert.That(renderedValue).DoesNotContain(RawTagOpen);
            await Assert.That(renderedValue).DoesNotContain(RawTagClose);
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedSegmentCues(IntroSegmentIndex)))
                .ToContainTextAsync("WPM");
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedBlockCues(IntroSegmentIndex, OpeningBlockIndex)))
                .ToContainTextAsync("WPM");
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedBlockCues(IntroSegmentIndex, OpeningBlockIndex)).Locator("[data-cue-kind='pace']"))
                .ToBeVisibleAsync();

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

    private static bool HasMaximumChannels(CssColor color, double maximum) =>
        color.R <= maximum && color.G <= maximum && color.B <= maximum;

    private static async Task<CssColor> ReadCssColorAsync(ILocator locator, string propertyName) =>
        await locator.EvaluateAsync<CssColor>(
            """
            (element, propertyName) => {
                const value = getComputedStyle(element)[propertyName];
                const rgbMatch = value.match(/rgba?\(([^)]+)\)/);
                if (rgbMatch) {
                    const parts = rgbMatch[1].split(',').map(part => Number.parseFloat(part.trim()));
                    return {
                        r: parts[0] ?? 0,
                        g: parts[1] ?? 0,
                        b: parts[2] ?? 0,
                        a: parts[3] ?? 1
                    };
                }

                const srgbMatch = value.match(/color\(srgb\s+([0-9.]+)\s+([0-9.]+)\s+([0-9.]+)(?:\s*\/\s*([0-9.]+))?\)/);
                if (srgbMatch) {
                    return {
                        r: Number.parseFloat(srgbMatch[1]) * 255,
                        g: Number.parseFloat(srgbMatch[2]) * 255,
                        b: Number.parseFloat(srgbMatch[3]) * 255,
                        a: srgbMatch[4] ? Number.parseFloat(srgbMatch[4]) : 1
                    };
                }

                return { r: 0, g: 0, b: 0, a: 0 };
            }
            """,
            propertyName);

    private readonly record struct CssColor(double R, double G, double B, double A);
}
