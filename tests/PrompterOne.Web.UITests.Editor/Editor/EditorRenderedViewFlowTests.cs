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
    private const string AttachmentFileName = "opening-context.png";
    private const string BackgroundColorProperty = "backgroundColor";
    private const double MaximumDarkCardsSurfaceChannel = 170;
    private static readonly byte[] AttachmentPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];

    [Test]
    public Task EditorScreen_RenderedCardsViewHidesSyntaxAndWritesBackToSource() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);

            await page.GetByTestId(UiTestIds.Editor.RenderedTab).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedView))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceTab)).ToHaveTextAsync("Raw");
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedTab)).ToHaveTextAsync("Rendered");
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedView))
                .ToHaveAttributeAsync("data-rendered-authoring-mode", "visual");
            var renderedViewColor = await ReadCssColorAsync(page.GetByTestId(UiTestIds.Editor.RenderedView), BackgroundColorProperty);
            await Assert.That(HasMaximumChannels(renderedViewColor, MaximumDarkCardsSurfaceChannel)).IsTrue().Because($"Expected dark-theme cards view to avoid a white paper surface, but got rgba({renderedViewColor.R:0.##}, {renderedViewColor.G:0.##}, {renderedViewColor.B:0.##}, {renderedViewColor.A:0.##}).");
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

            await EditorMonacoDriver.SetTextAsync(page, ReorderScript);
            await page.GetByTestId(UiTestIds.Editor.RenderedTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedBlockCues(IntroSegmentIndex, OpeningBlockIndex)))
                .ToContainTextAsync("Warm");
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedBlockCues(IntroSegmentIndex, OpeningBlockIndex)).Locator("[data-cue-kind='emotion']").First)
                .ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedView))
                .ToHaveAttributeAsync("data-rendered-cards-drag-ready", "true");
            var boardMetrics = await ReadRenderedBoardMetricsAsync(page);
            await Assert.That(boardMetrics.ColumnCount)
                .IsGreaterThanOrEqualTo(BrowserTestConstants.Editor.RenderedCardsMinimumBoardColumns)
                .Because("Cards view should use the available workspace as a multi-column board, not one narrow vertical list.");
            await Assert.That(boardMetrics.OverflowRightPx)
                .IsLessThanOrEqualTo(BrowserTestConstants.Editor.RenderedCardsColumnTolerancePx)
                .Because("The multi-column cards board should fit inside the visible segment lane.");
            var dragHandle = page.GetByTestId(UiTestIds.Editor.RenderedBlockDragHandle(IntroSegmentIndex, ClosingBlockIndex));
            var draggedBlock = page.GetByTestId(UiTestIds.Editor.RenderedBlock(IntroSegmentIndex, ClosingBlockIndex));
            var targetBlock = page.GetByTestId(UiTestIds.Editor.RenderedBlock(IntroSegmentIndex, OpeningBlockIndex));
            await Expect(draggedBlock)
                .ToHaveAttributeAsync("draggable", "false");
            await DragHandleToAsync(
                page,
                dragHandle,
                targetBlock,
                draggedBlock);
            await page.GetByTestId(UiTestIds.Editor.SourceTab).ClickAsync();
            await Expect(sourceInput)
                .ToHaveValueAsync(
                    new Regex($"{Regex.Escape(ClosingHeading)}[\\s\\S]+{Regex.Escape(OpeningHeading)}[\\s\\S]+{Regex.Escape(MiddleHeading)}"),
                    new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            var sourceValue = await sourceInput.InputValueAsync();
            await Assert.That(sourceValue).Contains("Alpha opening block.");
            await Assert.That(sourceValue).Contains("Bravo middle block.");
            await Assert.That(sourceValue).Contains("Charlie closing block.");
        });

    private static async Task DragHandleToAsync(IPage page, ILocator handle, ILocator target, ILocator dragged)
    {
        var handleBox = await handle.BoundingBoxAsync();
        var targetBox = await target.BoundingBoxAsync();
        if (handleBox is null || targetBox is null)
        {
            throw new InvalidOperationException("Expected rendered cards drag source and target to have layout boxes.");
        }

        var sourceX = Convert.ToInt32(Math.Round(handleBox.X + (handleBox.Width / 2)));
        var sourceY = Convert.ToInt32(Math.Round(handleBox.Y + (handleBox.Height / 2)));
        var targetX = Convert.ToInt32(Math.Round(targetBox.X + (targetBox.Width / 2)));
        var targetY = Convert.ToInt32(Math.Round(targetBox.Y + (targetBox.Height / 2)));
        var mouseDown = new { bubbles = true, button = 0, clientX = sourceX, clientY = sourceY };
        var mouseMove = new { bubbles = true, button = 0, clientX = targetX, clientY = targetY };
        var mouseUp = new { bubbles = true, button = 0, clientX = targetX, clientY = targetY };

        await handle.DispatchEventAsync("mousedown", mouseDown);
        await target.DispatchEventAsync("mousemove", mouseMove);
        await Expect(page.GetByTestId(UiTestIds.Editor.RenderedView))
            .ToHaveAttributeAsync("data-rendered-cards-dragging", "true");
        await Expect(dragged).ToHaveClassAsync(new Regex("editor-rendered-block--dragging"));
        await Expect(target).ToHaveClassAsync(new Regex("editor-rendered-block--drop-target"));
        await target.DispatchEventAsync("mouseup", mouseUp);
    }

    [Test]
    public Task EditorScreen_RenderedCardsView_AttachesMediaWithoutChangingSourceAndShowsItInReader() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, ReorderScript, "Rendered attachment probe");
            var scriptId = await ResolveCurrentScriptIdAsync(page);
            await page.GetByTestId(UiTestIds.Editor.RenderedTab).ClickAsync();

            await page.GetByTestId(UiTestIds.Editor.RenderedBlockAttachmentInput(IntroSegmentIndex, OpeningBlockIndex))
                .SetInputFilesAsync(new FilePayload
                {
                    Name = AttachmentFileName,
                    MimeType = "image/png",
                    Buffer = AttachmentPng
                });

            var attachment = page.GetByTestId(UiTestIds.Editor.RenderedBlockAttachment(IntroSegmentIndex, OpeningBlockIndex, 0));
            await Expect(attachment)
                .ToContainTextAsync(AttachmentFileName, new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            await page.GetByTestId(UiTestIds.Editor.SourceTab).ClickAsync();
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            await Expect(sourceInput)
                .ToHaveValueAsync(
                    new Regex(Regex.Escape(OpeningHeading)),
                    new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });
            var sourceValue = await sourceInput.InputValueAsync();
            await Assert.That(sourceValue).DoesNotContain(AttachmentFileName);

            await BrowserRouteDriver.ReloadPageAsync(
                page,
                AppRoutes.EditorWithId(scriptId),
                UiTestIds.Editor.Page,
                "rendered-attachment-editor-reload");
            await page.GetByTestId(UiTestIds.Editor.RenderedTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.RenderedBlockAttachment(IntroSegmentIndex, OpeningBlockIndex, 0)))
                .ToContainTextAsync(AttachmentFileName, new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            await PlaybackRouteDriver.OpenTeleprompterAsync(
                page,
                AppRoutes.TeleprompterWithId(scriptId),
                "rendered-attachment-teleprompter");
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardAttachment(0, 0)))
                .ToContainTextAsync(AttachmentFileName, new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
        });

    private static Task<string> ResolveCurrentScriptIdAsync(IPage page) =>
        page.EvaluateAsync<string>(
            "key => new URLSearchParams(window.location.search).get(key) || ''",
            AppRoutes.ScriptIdQueryKey);

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

    private static Task<RenderedBoardMetrics> ReadRenderedBoardMetricsAsync(IPage page) =>
        page.GetByTestId(UiTestIds.Editor.RenderedSegment(IntroSegmentIndex))
            .EvaluateAsync<RenderedBoardMetrics>(
                """
                segment => {
                    const segmentRect = segment.getBoundingClientRect();
                    const blocks = Array.from(segment.querySelectorAll(".editor-rendered-block"));
                    const lefts = [];
                    for (const block of blocks) {
                        const rect = block.getBoundingClientRect();
                        if (!lefts.some(left => Math.abs(left - rect.left) <= 4)) {
                            lefts.push(rect.left);
                        }
                    }

                    const overflowRightPx = blocks.reduce((overflow, block) => {
                        const rect = block.getBoundingClientRect();
                        return Math.max(overflow, rect.right - segmentRect.right);
                    }, 0);
                    return {
                        columnCount: lefts.length,
                        overflowRightPx
                    };
                }
                """);

    private readonly record struct RenderedBoardMetrics(int ColumnCount, double OverflowRightPx);

    private readonly record struct CssColor(double R, double G, double B, double A);
}
