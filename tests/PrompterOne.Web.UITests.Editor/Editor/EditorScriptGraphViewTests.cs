using Microsoft.Playwright;
using PrompterOne.Shared.Components.Editor;
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
        var browserErrors = BrowserErrorCollector.Attach(page);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);

            await page.GetByTestId(UiTestIds.Editor.GraphTab).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Editor.GraphPanel))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSummary))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSplit))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSplit))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphSplitModeAttributeName,
                    BrowserTestConstants.Editor.GraphSplitModeSplitValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSourcePane))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSplitResizer))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphControls))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphZoomIn))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphZoomOut))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphFit))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphAnalyze))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphAutoLayout))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await page.GetByTestId(UiTestIds.Editor.GraphAnalyze).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphReadyAttributeName,
                    BrowserTestConstants.Editor.GraphReadyAttributeValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphLayoutMode))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphOnlyToggle))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Html.AriaPressedAttribute,
                    BrowserTestConstants.Html.AriaPressedFalseValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var sourcePaneWidthBeforeResize = await page.GetByTestId(UiTestIds.Editor.GraphSourcePane)
                .EvaluateAsync<double>("element => element.getBoundingClientRect().width");
            var resizerBox = await page.GetByTestId(UiTestIds.Editor.GraphSplitResizer).BoundingBoxAsync();
            await Assert.That(resizerBox).IsNotNull();
            await page.Mouse.MoveAsync(resizerBox!.X + (resizerBox.Width / 2), resizerBox.Y + (resizerBox.Height / 2));
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(
                resizerBox.X + (resizerBox.Width / 2) + BrowserTestConstants.Editor.GraphSplitResizeDeltaPx,
                resizerBox.Y + (resizerBox.Height / 2));
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSplit))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphSplitResizingAttributeName,
                    BrowserTestConstants.Editor.GraphSplitResizingValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await page.Mouse.UpAsync();
            var sourcePaneWidthAfterResize = await page.GetByTestId(UiTestIds.Editor.GraphSourcePane)
                .EvaluateAsync<double>("element => element.getBoundingClientRect().width");
            await Assert.That(sourcePaneWidthAfterResize - sourcePaneWidthBeforeResize)
                .IsGreaterThan(BrowserTestConstants.Editor.GraphSplitResizeMinimumDeltaPx);

            await page.GetByTestId(UiTestIds.Editor.GraphOnlyToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSplit))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphSplitModeAttributeName,
                    BrowserTestConstants.Editor.GraphSplitModeGraphOnlyValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphOnlyToggle))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Html.AriaPressedAttribute,
                    BrowserTestConstants.Html.AriaPressedTrueValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Assert.That(await page.GetByTestId(UiTestIds.Editor.GraphSourcePane).CountAsync()).IsEqualTo(0);
            await Assert.That(await page.GetByTestId(UiTestIds.Editor.WorkspaceTabs).CountAsync()).IsEqualTo(0);
            await Assert.That(await page.GetByTestId(UiTestIds.Editor.MetadataRail).CountAsync()).IsEqualTo(0);
            await page.GetByTestId(UiTestIds.Editor.GraphOnlyToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSplit))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphSplitModeAttributeName,
                    BrowserTestConstants.Editor.GraphSplitModeSplitValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSourcePane))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var graphLayoutValues = await page.GetByTestId(UiTestIds.Editor.GraphLayoutMode)
                .EvaluateAsync<string[]>("select => Array.from(select.options).map(option => option.value)");
            await Assert.That(graphLayoutValues.Contains(ScriptGraphLayoutModes.Circular))
                .IsTrue()
                .Because("The graph layout selector should expose the G6 circular preset.");
            await Assert.That(graphLayoutValues.Contains(ScriptGraphLayoutModes.Mds))
                .IsTrue()
                .Because("The graph layout selector should expose the G6 MDS preset.");
            await Assert.That(graphLayoutValues.Contains(ScriptGraphLayoutModes.ForceAtlas2))
                .IsTrue()
                .Because("The graph layout selector should expose the G6 Force Atlas 2 preset.");
            await Expect(page.GetByTestId(UiTestIds.Editor.MetadataRail))
                .ToHaveAttributeAsync("data-collapsed", "true");
            await page.GetByTestId(UiTestIds.Editor.MetadataRailToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphRailTab))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphRailPanel))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphRailLayoutMode))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphRailNodeStyleMode))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphNodeStyleAttributeName,
                    BrowserTestConstants.Editor.GraphNodeStyleCompactValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var compactNodeTypes = await ReadNonLaneGraphNodeTypesAsync(page);
            await Assert.That(compactNodeTypes)
                .IsEquivalentTo([BrowserTestConstants.Editor.GraphNodeTypeEllipseValue]);
            await page.GetByTestId(UiTestIds.Editor.GraphRailNodeStyleMode)
                .SelectOptionAsync([ScriptGraphNodeStyleModes.Cards]);
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphNodeStyleAttributeName,
                    BrowserTestConstants.Editor.GraphNodeStyleCardsValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var cardNodeTypes = await ReadNonLaneGraphNodeTypesAsync(page);
            await Assert.That(cardNodeTypes)
                .IsEquivalentTo([BrowserTestConstants.Editor.GraphNodeTypeRectValue]);
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
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphNavigationAttributeName,
                    BrowserTestConstants.Editor.GraphNavigationAttributeValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var graphKinds = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<string[]>("element => Array.from(new Set(element.prompterOneGraphData.nodes.map(node => node.data.kind)))");
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSemanticStatus))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphSemanticStatusAttributeName,
                    BrowserTestConstants.Editor.GraphSemanticStatusModelUnavailableValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Assert.That(graphKinds.Contains("Idea")).IsFalse();
            await Assert.That(graphKinds.Contains("Claim")).IsFalse();
            await Assert.That(graphKinds.Contains("Term")).IsFalse();
            await Assert.That(graphKinds.Contains("Line")).IsFalse();
            await Assert.That(graphKinds.Contains("Pace")).IsFalse();
            await Assert.That(graphKinds.Contains("Timing")).IsFalse();
            await Assert.That(graphKinds.Contains("Cue")).IsFalse();
            await Assert.That(graphKinds.Contains("Literal")).IsFalse();
            await Assert.That(graphKinds.Contains("Uri")).IsFalse();

            await page.GetByTestId(UiTestIds.Editor.GraphTokenizerAnalyze).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSemanticStatus))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphSemanticStatusAttributeName,
                    BrowserTestConstants.Editor.GraphSemanticStatusTokenizerSimilarityValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphReadyAttributeName,
                    BrowserTestConstants.Editor.GraphReadyAttributeValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            graphKinds = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<string[]>("element => Array.from(new Set(element.prompterOneGraphData.nodes.map(node => node.data.kind)))");
            await Assert.That(graphKinds.Contains(BrowserTestConstants.Editor.GraphNodeKindSimilarityChunkValue)).IsTrue();
            var graphEdgeLabels = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<string[]>("element => Array.from(new Set(element.prompterOneGraphData.edges.map(edge => edge.data.label)))");
            await Assert.That(graphEdgeLabels.Contains(BrowserTestConstants.Editor.GraphEdgeLabelTokenSimilarityValue)).IsTrue();
            var navigableSimilarityNodeId = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<string>(
                    "(element, kind) => element.prompterOneGraphData.nodes.find(node => node.data.kind === kind && node.data.hasSourceRange)?.id || ''",
                    BrowserTestConstants.Editor.GraphNodeKindSimilarityChunkValue);
            await Assert.That(string.IsNullOrWhiteSpace(navigableSimilarityNodeId)).IsFalse();
            await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync(
                    "(element, nodeId) => element.dispatchEvent(new CustomEvent('prompterone:graph-node-request', { detail: { nodeId } }))",
                    navigableSimilarityNodeId);
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphSelectedNodeAttributeName,
                    navigableSimilarityNodeId,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Assert.That(await page.GetByTestId(UiTestIds.Editor.GraphNodeList).CountAsync()).IsEqualTo(0);

            await page.GetByTestId(UiTestIds.Editor.GraphZoomIn).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.GraphZoomOut).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.GraphFit).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.GraphAutoLayout).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphAutoLayoutRunsAttributeName,
                    BrowserTestConstants.Editor.GraphAutoLayoutFirstRunValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await page.GetByTestId(UiTestIds.Editor.GraphLayoutMode)
                .SelectOptionAsync([ScriptGraphLayoutModes.Compact]);

            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphLayoutAttributeName,
                    BrowserTestConstants.Editor.GraphLayoutCompactValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await page.GetByTestId(UiTestIds.Editor.GraphLayoutMode)
                .SelectOptionAsync([ScriptGraphLayoutModes.Circular]);
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphLayoutAttributeName,
                    BrowserTestConstants.Editor.GraphLayoutCircularValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await page.GetByTestId(UiTestIds.Editor.GraphLayoutMode)
                .SelectOptionAsync([ScriptGraphLayoutModes.Grid]);
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphLayoutAttributeName,
                    BrowserTestConstants.Editor.GraphLayoutGridValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var gridNodeOverlapCount = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<int>(
                    """
                    element => {
                        const nodes = element.prompterOneGraphData.nodes
                            .filter(node => Number.isFinite(node.style?.x) && Number.isFinite(node.style?.y));
                        let overlapCount = 0;
                        for (let outer = 0; outer < nodes.length; outer += 1) {
                            const left = nodes[outer];
                            const leftSize = left.style.size;
                            const leftBox = {
                                minX: left.style.x - leftSize[0] / 2,
                                maxX: left.style.x + leftSize[0] / 2,
                                minY: left.style.y - leftSize[1] / 2,
                                maxY: left.style.y + leftSize[1] / 2
                            };
                            for (let inner = outer + 1; inner < nodes.length; inner += 1) {
                                const right = nodes[inner];
                                const rightSize = right.style.size;
                                const rightBox = {
                                    minX: right.style.x - rightSize[0] / 2,
                                    maxX: right.style.x + rightSize[0] / 2,
                                    minY: right.style.y - rightSize[1] / 2,
                                    maxY: right.style.y + rightSize[1] / 2
                                };
                                if (leftBox.minX < rightBox.maxX &&
                                    leftBox.maxX > rightBox.minX &&
                                    leftBox.minY < rightBox.maxY &&
                                    leftBox.maxY > rightBox.minY) {
                                    overlapCount += 1;
                                }
                            }
                        }
                        return overlapCount;
                    }
                    """);
            await Assert.That(gridNodeOverlapCount)
                .IsEqualTo(BrowserTestConstants.Editor.GraphNodeOverlapExpectedCount);

            await page.GetByTestId(UiTestIds.Editor.GraphCanvas).HoverAsync();
            await page.Keyboard.DownAsync("Space");
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphGrabAttributeName,
                    BrowserTestConstants.Editor.GraphGrabAttributeValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var grabCursor = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<string>("element => getComputedStyle(element.querySelector('canvas') || element).cursor");
            await Assert.That(string.Equals(
                    grabCursor,
                    BrowserTestConstants.Editor.GraphGrabCursorValue,
                    StringComparison.Ordinal))
                .IsTrue()
                .Because("Holding Space over the graph should present a grab cursor for panning.");
            await page.Mouse.DownAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphPanningAttributeName,
                    BrowserTestConstants.Editor.GraphPanningAttributeValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await page.Mouse.UpAsync();
            await page.Keyboard.UpAsync("Space");
            var grabAttribute = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .GetAttributeAsync(BrowserTestConstants.Editor.GraphGrabAttributeName);
            await Assert.That(grabAttribute).IsNull();

            await page.GetByTestId(UiTestIds.Editor.GraphRailLayoutMode)
                .SelectOptionAsync([ScriptGraphLayoutModes.Mds]);
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphLayoutAttributeName,
                    BrowserTestConstants.Editor.GraphLayoutMdsValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await page.GetByTestId(UiTestIds.Editor.GraphAutoLayout).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphAutoLayoutRunsAttributeName,
                    BrowserTestConstants.Editor.GraphAutoLayoutSecondRunValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Editor.GraphRailLayoutMode)
                .SelectOptionAsync([ScriptGraphLayoutModes.Story]);
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphLayoutAttributeName,
                    BrowserTestConstants.Editor.GraphLayoutStoryValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
        }
        catch (Exception exception)
        {
            await UiScenarioArtifacts.CaptureFailurePageAsync(page, nameof(EditorScreen_GraphTabRendersScriptKnowledgeGraphControls));
            var bootstrapOverlayText = await ReadBootstrapOverlayTextAsync(page);
            var graphDiagnostics = await ReadGraphCanvasDiagnosticsAsync(page);
            throw new InvalidOperationException(
                string.Join(
                    Environment.NewLine,
                    "Editor graph browser diagnostics:",
                    bootstrapOverlayText,
                    graphDiagnostics,
                    browserErrors.Describe()),
                exception);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task<string> ReadBootstrapOverlayTextAsync(IPage page)
    {
        var overlay = page.GetByTestId(UiTestIds.Diagnostics.Bootstrap);
        if (await overlay.CountAsync() == 0 || !await overlay.IsVisibleAsync())
        {
            return "No bootstrap overlay was visible.";
        }

        var text = await overlay.TextContentAsync(new()
        {
            Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
        });
        return string.IsNullOrWhiteSpace(text)
            ? "Bootstrap overlay was visible without readable text."
            : $"Bootstrap overlay: {text.Trim()}";
    }

    private static async Task<string> ReadGraphCanvasDiagnosticsAsync(IPage page)
    {
        var canvas = page.GetByTestId(UiTestIds.Editor.GraphCanvas);
        if (await canvas.CountAsync() == 0)
        {
            return "Graph canvas was not in the DOM.";
        }

        var state = await canvas.GetAttributeAsync(BrowserTestConstants.Editor.GraphStateAttributeName);
        var error = await canvas.GetAttributeAsync(BrowserTestConstants.Editor.GraphErrorAttributeName);
        var ready = await canvas.GetAttributeAsync(BrowserTestConstants.Editor.GraphReadyAttributeName);
        var details = await canvas.EvaluateAsync<GraphCanvasDiagnostics>(
            """
            element => ({
                canvasCount: element.querySelectorAll("canvas").length,
                hasArtifact: Boolean(element.prompterOneGraphArtifact),
                hasConfig: Boolean(element.prompterOneGraphConfig),
                hasData: Boolean(element.prompterOneGraphData),
                hasGraph: Boolean(element.prompterOneGraph),
                layout: element.dataset.graphLayout || "",
                nodeStyle: element.dataset.graphNodeStyle || ""
            })
            """);
        return string.Join(
            Environment.NewLine,
            $"Graph state: {state ?? "<null>"}",
            $"Graph ready: {ready ?? "<null>"}",
            $"Graph error: {error ?? "<null>"}",
            $"Graph layout: {details.Layout}",
            $"Graph node style: {details.NodeStyle}",
            $"Graph canvas count: {details.CanvasCount}",
            $"Graph has artifact/config/data/graph: {details.HasArtifact}/{details.HasConfig}/{details.HasData}/{details.HasGraph}");
    }

    private sealed class GraphCanvasDiagnostics
    {
        public int CanvasCount { get; set; }

        public bool HasArtifact { get; set; }

        public bool HasConfig { get; set; }

        public bool HasData { get; set; }

        public bool HasGraph { get; set; }

        public string Layout { get; set; } = string.Empty;

        public string NodeStyle { get; set; } = string.Empty;
    }

    private static Task<string[]> ReadNonLaneGraphNodeTypesAsync(IPage page) =>
        page.GetByTestId(UiTestIds.Editor.GraphCanvas)
            .EvaluateAsync<string[]>(
                """
                (element, lanePrefix) => Array.from(new Set(
                    element.prompterOneGraphData.nodes
                        .filter(node => !node.id.startsWith(lanePrefix))
                        .map(node => node.type)))
                """,
                BrowserTestConstants.Editor.GraphLaneNodePrefix);
}
