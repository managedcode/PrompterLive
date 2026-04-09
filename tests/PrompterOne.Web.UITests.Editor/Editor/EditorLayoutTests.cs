using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorLayoutTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_MetadataRailStaysDockedToRightOfMainPanel()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

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

            await Assert.That(Math.Abs(dockGap - BrowserTestConstants.Editor.MetadataRailDockGapPx)).IsBetween(0, BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
            await Assert.That(Math.Abs(railBounds.Y - mainBounds.Y)).IsBetween(0, BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
            await Assert.That(bottomEdgeDrift).IsBetween(0, BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_SourceEditorUsesSingleVerticalScrollSurface()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Editor);

            var sourceScrollHost = page.GetByTestId(UiTestIds.Editor.SourceScrollHost);

            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(sourceScrollHost)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await EditorMonacoDriver.SetTextAsync(
                page,
                string.Join(
                    '\n',
                    Enumerable.Range(1, BrowserTestConstants.Editor.ScrollProbeLineCount)
                        .Select(index => $"Scroll probe line {index}")));

            await EditorMonacoDriver.SetCaretAtEndAsync(page);
            await EditorMonacoDriver.CenterSelectionLineAsync(page);
            await EditorMonacoDriver.WaitForSelectionScrollAsync(
                page,
                BrowserTestConstants.Editor.AiScrollJumpMinimumScrollTopPx,
                BrowserTestConstants.Timing.DefaultVisibleTimeoutMs);

            var stageState = await EditorMonacoDriver.GetStateAsync(page);
            var scrollState = await sourceScrollHost.EvaluateAsync<EditorScrollState>(
                """
                element => {
                    return {
                        hostOverflow: getComputedStyle(element).overflow,
                        hostScrollTop: element.scrollTop,
                        hostOverflowY: getComputedStyle(element).overflowY
                    };
                }
                """);

            await Assert.That(stageState.VisibleRange).IsNotNull();
            await Assert.That(
                stageState.VisibleRange!.StartLineNumber > 1 &&
                stageState.Selection.Line >= stageState.VisibleRange.StartLineNumber &&
                stageState.Selection.Line <= stageState.VisibleRange.EndLineNumber).IsTrue().Because($"Expected Monaco to own the vertical scroll surface after centering the selection line, but the visible range did not move off the first line or did not contain the caret line. ScrollTop={stageState.ScrollTop}; SelectionLine={stageState.Selection.Line}; VisibleStart={stageState.VisibleRange.StartLineNumber}; VisibleEnd={stageState.VisibleRange.EndLineNumber}; LineCount={stageState.LineCount}.");
            await Assert.That(scrollState.HostScrollTop).IsEqualTo(BrowserTestConstants.Editor.MaxSourceScrollHostTopPx);
            await Assert.That(
                string.Equals(scrollState.HostOverflow, BrowserTestConstants.Editor.HiddenOverflowValue, StringComparison.Ordinal) ||
                string.Equals(scrollState.HostOverflowY, BrowserTestConstants.Editor.HiddenOverflowValue, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(scrollState.HostOverflowY)).IsTrue().Because($"Expected the outer source host to stay visually non-scrollable while Monaco owns the vertical scroll surface, but computed overflow was '{scrollState.HostOverflow}' / overflow-y '{scrollState.HostOverflowY}'.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_CreatedDateFieldShowsVisibleCalendarIcon()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);

            var createdInput = page.GetByTestId(UiTestIds.Editor.Created);
            var createdIcon = page.GetByTestId(UiTestIds.Editor.CreatedIcon);

            await Expect(createdInput)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(createdIcon)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var iconColor = await createdIcon.EvaluateAsync<string>(
                """
                element => getComputedStyle(element).color
                """);

            await Assert.That(iconColor).IsNotEqualTo(BrowserTestConstants.Editor.CalendarIconUnexpectedColor);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_ToolbarKeepsFarActionsReachableOnPhoneLandscape()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.ResponsiveLayout.IphoneMediumHeight,
                BrowserTestConstants.ResponsiveLayout.IphoneMediumWidth);
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var toolbar = page.GetByTestId(UiTestIds.Editor.Toolbar);
            var toolbarTools = page.GetByTestId(UiTestIds.Editor.ToolbarTools);
            var findBar = page.GetByTestId(UiTestIds.Editor.FindBar);
            var aiButton = page.GetByTestId(UiTestIds.Editor.Ai);
            var scrollNext = page.GetByTestId(UiTestIds.Editor.ToolbarScrollNext);
            var scrollPrevious = page.GetByTestId(UiTestIds.Editor.ToolbarScrollPrevious);

            await Expect(toolbar)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(toolbarTools)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(findBar)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(aiButton)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(scrollNext)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(scrollPrevious)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var initialToolbarState = await ReadToolbarOverflowStateAsync(page);

            await Assert.That(new[] { "auto", "scroll" }.Contains(initialToolbarState.OverflowX, StringComparer.Ordinal)).IsTrue().Because($"Unexpected toolbar overflow-x value: {initialToolbarState.OverflowX}");
            await Assert.That(initialToolbarState.ScrollWidth - initialToolbarState.ClientWidth)
                .IsBetween(BrowserTestConstants.EditorFlow.MinimumToolbarScrollAdvancePx, double.MaxValue);
            await Assert.That(initialToolbarState.FindLeft - initialToolbarState.ToolsLeft)
                .IsBetween(-BrowserTestConstants.EditorFlow.ToolbarOverflowTolerancePx, double.MaxValue);
            await Assert.That(initialToolbarState.ToolsRight - initialToolbarState.FindRight)
                .IsBetween(-BrowserTestConstants.EditorFlow.ToolbarOverflowTolerancePx, double.MaxValue);

            await scrollNext.ClickAsync();
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const toolbar = document.querySelector(`[data-test="${args.toolbarTestId}"]`);
                    return toolbar instanceof HTMLElement && toolbar.scrollLeft >= args.minimumScrollLeft;
                }
                """,
                new
                {
                    minimumScrollLeft = initialToolbarState.ScrollLeft + BrowserTestConstants.EditorFlow.MinimumToolbarScrollAdvancePx,
                    toolbarTestId = UiTestIds.Editor.Toolbar
                },
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var scrolledToolbarState = await ReadToolbarOverflowStateAsync(page);

            await Assert.That(scrolledToolbarState.ScrollLeft - initialToolbarState.ScrollLeft)
                .IsBetween(BrowserTestConstants.EditorFlow.MinimumToolbarScrollAdvancePx, double.MaxValue);
            await Assert.That(scrolledToolbarState.FindLeft - scrolledToolbarState.ToolsLeft)
                .IsBetween(-BrowserTestConstants.EditorFlow.ToolbarOverflowTolerancePx, double.MaxValue);
            await Assert.That(scrolledToolbarState.ToolsRight - scrolledToolbarState.FindRight)
                .IsBetween(-BrowserTestConstants.EditorFlow.ToolbarOverflowTolerancePx, double.MaxValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_UsesAvailableWidthAndMetadataRailCanCollapse()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.LayoutScenario);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);

            var layout = page.GetByTestId(UiTestIds.Editor.Layout);
            var mainPanel = page.GetByTestId(UiTestIds.Editor.MainPanel);
            var metadataRail = page.GetByTestId(UiTestIds.Editor.MetadataRail);
            var metadataToggle = page.GetByTestId(UiTestIds.Editor.MetadataRailToggle);

            await Expect(layout)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(mainPanel)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(metadataRail)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(metadataToggle)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var expandedMetrics = await ReadLayoutMetricsAsync(page);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.LayoutScenario,
                BrowserTestConstants.EditorFlow.LayoutExpandedStep);

            await Assert.That(expandedMetrics.LayoutViewportRightGap).IsBetween(0, BrowserTestConstants.Editor.MaximumLayoutViewportRightGapPx);
            await Assert.That(expandedMetrics.MetadataRailCollapsed).IsFalse();
            await Assert.That(expandedMetrics.MetadataToggleChevronDirection).IsEqualTo(BrowserTestConstants.EditorFlow.MetadataRailExpandedChevronDirection);

            await metadataToggle.ClickAsync();
            await Expect(metadataToggle).ToHaveAttributeAsync("aria-expanded", "false");
            await WaitForMetadataRailCollapsedAsync(page);

            var collapsedMetrics = await ReadLayoutMetricsAsync(page);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.LayoutScenario,
                BrowserTestConstants.EditorFlow.LayoutCollapsedStep);

            await Assert.That(collapsedMetrics.LayoutViewportRightGap).IsBetween(0, BrowserTestConstants.Editor.MaximumLayoutViewportRightGapPx);
            await Assert.That(collapsedMetrics.MetadataRailCollapsed).IsTrue();
            await Assert.That(collapsedMetrics.MetadataToggleChevronDirection).IsEqualTo(BrowserTestConstants.EditorFlow.MetadataRailCollapsedChevronDirection);
            await Assert.That(collapsedMetrics.MetadataRailWidth).IsBetween(0, BrowserTestConstants.Editor.MaximumCollapsedMetadataRailWidthPx);

            var reclaimedMainWidth = collapsedMetrics.MainWidth - expandedMetrics.MainWidth;

            await Assert.That(reclaimedMainWidth > 0).IsTrue().Because($"Expected the main editor panel to grow after collapsing metadata, but it changed from {expandedMetrics.MainWidth:0.##} to {collapsedMetrics.MainWidth:0.##}.");
            await Assert.That(reclaimedMainWidth >= collapsedMetrics.MetadataRailWidth).IsTrue().Because($"Expected the main editor panel to reclaim at least the remaining collapsed rail width ({collapsedMetrics.MetadataRailWidth:0.##}px), but it only grew by {reclaimedMainWidth:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static Task WaitForMetadataRailCollapsedAsync(IPage page) =>
        page.WaitForFunctionAsync(
            """
            (args) => {
                const rail = document.querySelector(`[data-test="${args.railTestId}"]`);
                const toggle = document.querySelector(`[data-test="${args.toggleTestId}"]`);
                if (!(rail instanceof HTMLElement) || !(toggle instanceof HTMLElement)) {
                    return false;
                }

                const railWidth = rail.getBoundingClientRect().width;
                return rail.getAttribute('data-collapsed') === 'true' &&
                    toggle.getAttribute('aria-expanded') === 'false' &&
                    railWidth <= args.maximumCollapsedWidth;
            }
            """,
            new
            {
                maximumCollapsedWidth = BrowserTestConstants.Editor.MaximumCollapsedMetadataRailWidthPx,
                railTestId = UiTestIds.Editor.MetadataRail,
                toggleTestId = UiTestIds.Editor.MetadataRailToggle
            },
            new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

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

    private static async Task<EditorLayoutMetrics> ReadLayoutMetricsAsync(IPage page) =>
        await page.EvaluateAsync<EditorLayoutMetrics>(
            """
            ({ layoutTestId, mainTestId, railTestId, toggleTestId }) => {
                const layout = document.querySelector(`[data-test="${layoutTestId}"]`);
                const main = document.querySelector(`[data-test="${mainTestId}"]`);
                const rail = document.querySelector(`[data-test="${railTestId}"]`);
                const toggle = document.querySelector(`[data-test="${toggleTestId}"]`);

                if (!(layout instanceof HTMLElement) || !(main instanceof HTMLElement) || !(rail instanceof HTMLElement) || !(toggle instanceof HTMLElement)) {
                    throw new Error("Editor layout metrics target is unavailable.");
                }

                const layoutRect = layout.getBoundingClientRect();
                const mainRect = main.getBoundingClientRect();
                const railRect = rail.getBoundingClientRect();

                return {
                    layoutViewportRightGap: window.innerWidth - layoutRect.right,
                    mainWidth: mainRect.width,
                    metadataRailWidth: railRect.width,
                    metadataRailCollapsed: rail.getAttribute('data-collapsed') === 'true',
                    metadataToggleChevronDirection: toggle.getAttribute('data-chevron-direction') ?? ''
                };
            }
            """,
            new
            {
                layoutTestId = UiTestIds.Editor.Layout,
                mainTestId = UiTestIds.Editor.MainPanel,
                railTestId = UiTestIds.Editor.MetadataRail,
                toggleTestId = UiTestIds.Editor.MetadataRailToggle
            });

    private static Task<ToolbarOverflowState> ReadToolbarOverflowStateAsync(IPage page) =>
        page.EvaluateAsync<ToolbarOverflowState>(
            """
            args => {
                const toolbar = document.querySelector(`[data-test="${args.toolbarTestId}"]`);
                const tools = document.querySelector(`[data-test="${args.toolbarToolsTestId}"]`);
                const findBar = document.querySelector(`[data-test="${args.findBarTestId}"]`);
                if (!(toolbar instanceof HTMLElement) ||
                    !(tools instanceof HTMLElement) ||
                    !(findBar instanceof HTMLElement)) {
                    throw new Error("Toolbar reachability probe targets are unavailable.");
                }

                const toolsRect = tools.getBoundingClientRect();
                const findRect = findBar.getBoundingClientRect();

                return {
                    clientWidth: toolbar.clientWidth,
                    findLeft: findRect.left,
                    findRight: findRect.right,
                    overflowX: getComputedStyle(toolbar).overflowX,
                    scrollLeft: toolbar.scrollLeft,
                    scrollWidth: toolbar.scrollWidth,
                    toolsLeft: toolsRect.left,
                    toolsRight: toolsRect.right
                };
            }
            """,
            new
            {
                findBarTestId = UiTestIds.Editor.FindBar,
                toolbarTestId = UiTestIds.Editor.Toolbar,
                toolbarToolsTestId = UiTestIds.Editor.ToolbarTools
            });

    private readonly record struct ToolbarOverflowState(
        double ClientWidth,
        double FindLeft,
        double FindRight,
        string OverflowX,
        double ScrollLeft,
        double ScrollWidth,
        double ToolsLeft,
        double ToolsRight);
    private readonly record struct EditorScrollState(string HostOverflow, double HostScrollTop, string HostOverflowY);
    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);
    private readonly record struct EditorLayoutMetrics(
        double LayoutViewportRightGap,
        double MainWidth,
        double MetadataRailWidth,
        bool MetadataRailCollapsed,
        string MetadataToggleChevronDirection);
}
