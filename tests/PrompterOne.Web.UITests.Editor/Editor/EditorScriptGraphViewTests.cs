using Microsoft.Playwright;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorScriptGraphViewTests(StandaloneAppFixture fixture)
{
    private const string GraphVisibilityScenario = "editor-script-graph-visibility";
    private const string GraphStoryLayoutStep = "01-story-layout";
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
            var graphCanvasFit = await ReadGraphCanvasFitProbeAsync(page);
            await Assert.That(graphCanvasFit.HostWidth)
                .IsGreaterThanOrEqualTo(graphCanvasFit.PanelWidth - BrowserTestConstants.Editor.GraphCanvasFitTolerancePx)
                .Because("The graph host should span the full graph panel width.");
            await Assert.That(graphCanvasFit.HostHeight)
                .IsGreaterThanOrEqualTo(BrowserTestConstants.Editor.GraphCanvasMinimumVisibleHeightPx)
                .Because("The graph host should use the available editor workspace instead of collapsing to a clipped viewport.");
            await Assert.That(graphCanvasFit.HostBottomDelta)
                .IsBetween(0, BrowserTestConstants.Editor.GraphCanvasFitTolerancePx)
                .Because("The graph host should fill the panel's remaining vertical space to the bottom edge.");
            await Assert.That(graphCanvasFit.SurfaceWidthDelta)
                .IsBetween(0, BrowserTestConstants.Editor.GraphCanvasFitTolerancePx)
                .Because("The rendered G6 canvas should match the graph host width.");
            await Assert.That(graphCanvasFit.SurfaceHeightDelta)
                .IsBetween(0, BrowserTestConstants.Editor.GraphCanvasFitTolerancePx)
                .Because("The rendered G6 canvas should match the graph host height.");
            await Assert.That(graphCanvasFit.ViewportWidthDelta)
                .IsBetween(0, BrowserTestConstants.Editor.GraphCanvasFitTolerancePx)
                .Because("The G6 viewport width should be synchronized from the graph host.");
            await Assert.That(graphCanvasFit.ViewportHeightDelta)
                .IsBetween(0, BrowserTestConstants.Editor.GraphCanvasFitTolerancePx)
                .Because("The G6 viewport height should be synchronized from the graph host.");
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
                    BrowserTestConstants.Editor.GraphNodeStyleDotsValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var dotNodeTypes = await ReadNonLaneGraphNodeTypesAsync(page);
            await Assert.That(dotNodeTypes)
                .IsEquivalentTo([BrowserTestConstants.Editor.GraphNodeTypeCircleValue]);
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
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphMetadata))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var markdownLdNodeCount = await page.GetByTestId(UiTestIds.Editor.GraphMetadata)
                .EvaluateAsync<int>("element => Number(element.dataset.kbNodes || 0)");
            await Assert.That(markdownLdNodeCount)
                .IsGreaterThan(0)
                .Because("The baseline graph should surface deterministic Markdown-LD metadata before tokenizer similarity is requested.");
            await Assert.That(graphKinds.Contains("Idea")).IsFalse();
            await Assert.That(graphKinds.Contains("Claim")).IsFalse();
            await Assert.That(graphKinds.Contains("Term")).IsTrue();
            await Assert.That(graphKinds.Contains("Line")).IsFalse();
            await Assert.That(graphKinds.Contains("Pace")).IsFalse();
            await Assert.That(graphKinds.Contains("Timing")).IsFalse();
            await Assert.That(graphKinds.Contains("Cue")).IsFalse();
            await Assert.That(graphKinds.Contains("Literal")).IsFalse();
            await Assert.That(graphKinds.Contains("Uri")).IsFalse();

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.GraphTokenizerAnalyze),
                noWaitAfter: true);
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
            var edgeTooltipAnchor = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<GraphTooltipAnchorProbe>(
                    """
                    (element, edgeLabel) => {
                        const bounds = element.getBoundingClientRect();
                        const pointerX = bounds.left + (bounds.width * 0.62);
                        const pointerY = bounds.top + (bounds.height * 0.46);
                        element.dispatchEvent(new PointerEvent("pointerenter", {
                            bubbles: true,
                            clientX: pointerX,
                            clientY: pointerY
                        }));
                        element.dispatchEvent(new PointerEvent("pointermove", {
                            bubbles: true,
                            clientX: pointerX,
                            clientY: pointerY
                        }));
                        const edge = element.prompterOneGraphData.edges.find(edge => edge.data.label === edgeLabel) ??
                            element.prompterOneGraphData.edges[0];
                        element.prompterOneGraph.emit?.("edge:pointerenter", {
                            target: { id: edge.id },
                            canvas: { x: 1, y: 1 },
                            canvasPoint: { x: 1, y: 1 },
                            point: { x: 1, y: 1 },
                            x: 1,
                            y: 1
                        });
                        const tooltip = element.prompterOneGraphTooltip;
                        return {
                            visible: Boolean(tooltip && !tooltip.hidden),
                            expectedAnchorX: pointerX - bounds.left,
                            expectedAnchorY: pointerY - bounds.top,
                            anchorX: Number(tooltip?.dataset.anchorX ?? Number.NaN),
                            anchorY: Number(tooltip?.dataset.anchorY ?? Number.NaN),
                            text: tooltip?.textContent ?? ""
                        };
                    }
                    """,
                    BrowserTestConstants.Editor.GraphEdgeLabelTokenSimilarityValue);
            await Assert.That(edgeTooltipAnchor.Visible)
                .IsTrue()
                .Because("Hovering a graph edge should show the relationship tooltip.");
            await Assert.That(edgeTooltipAnchor.Text.Contains(
                    BrowserTestConstants.Editor.GraphEdgeLabelTokenSimilarityValue,
                    StringComparison.Ordinal))
                .IsTrue()
                .Because("The edge tooltip should describe the hovered relationship.");
            await Assert.That(Math.Abs(edgeTooltipAnchor.AnchorX - edgeTooltipAnchor.ExpectedAnchorX))
                .IsBetween(0, BrowserTestConstants.Editor.GraphTooltipAnchorTolerancePx)
                .Because("Edge tooltip positioning should anchor to the last DOM pointer, not the graph-space edge point.");
            await Assert.That(Math.Abs(edgeTooltipAnchor.AnchorY - edgeTooltipAnchor.ExpectedAnchorY))
                .IsBetween(0, BrowserTestConstants.Editor.GraphTooltipAnchorTolerancePx)
                .Because("Edge tooltip positioning should anchor to the last DOM pointer, not the graph-space edge point.");
            var visibleRawTpsMarkup = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<string[]>(
                    """
                    (element, rawPattern) => {
                        const pattern = new RegExp(rawPattern);
                        return element.prompterOneGraphData.nodes
                            .flatMap(node => [node.data?.displayLabel, node.data?.detail, node.style?.labelText])
                            .filter(value => pattern.test(value ?? ""));
                    }
                    """,
                    BrowserTestConstants.Editor.GraphRawTpsMarkupRegex);
            await Assert.That(visibleRawTpsMarkup).IsEmpty();
            var minimumReadableEdgeOpacity = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<double>(
                    """
                    element => {
                        const opacities = element.prompterOneGraphData.edges
                            .map(edge => Number(edge.style?.opacity ?? 1))
                            .filter(Number.isFinite);
                        return opacities.length === 0 ? 1 : Math.min(...opacities);
                    }
                    """);
            await Assert.That(minimumReadableEdgeOpacity)
                .IsGreaterThanOrEqualTo(BrowserTestConstants.Editor.GraphMinimumReadableEdgeOpacity);
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
            var gridNodePositionCount = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<int>(
                    """
                    element => element.prompterOneGraphData.nodes
                        .filter(node => Number.isFinite(node.style?.x ?? node.x) &&
                            Number.isFinite(node.style?.y ?? node.y))
                        .length
                    """);
            await Assert.That(gridNodePositionCount)
                .IsGreaterThanOrEqualTo(BrowserTestConstants.Editor.GraphRenderedNodePositionMinimumCount);
            var gridNodeOverlapCount = await page.GetByTestId(UiTestIds.Editor.GraphCanvas)
                .EvaluateAsync<int>(
                    """
                    element => {
                        const nodes = element.prompterOneGraphData.nodes
                            .map(node => ({
                                id: node.id,
                                x: Number(node.style?.x ?? node.x),
                                y: Number(node.style?.y ?? node.y),
                                size: node.style?.size ?? [160, 48]
                            }))
                            .filter(node => Number.isFinite(node.x) && Number.isFinite(node.y));
                        let overlapCount = 0;
                        for (let outer = 0; outer < nodes.length; outer += 1) {
                            const left = nodes[outer];
                            const leftSize = left.size;
                            const leftBox = {
                                minX: left.x - leftSize[0] / 2,
                                maxX: left.x + leftSize[0] / 2,
                                minY: left.y - leftSize[1] / 2,
                                maxY: left.y + leftSize[1] / 2
                            };
                            for (let inner = outer + 1; inner < nodes.length; inner += 1) {
                                const right = nodes[inner];
                                const rightSize = right.size;
                                const rightBox = {
                                    minX: right.x - rightSize[0] / 2,
                                    maxX: right.x + rightSize[0] / 2,
                                    minY: right.y - rightSize[1] / 2,
                                    maxY: right.y + rightSize[1] / 2
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
            await UiScenarioArtifacts.CaptureLocatorAsync(
                page.GetByTestId(UiTestIds.Editor.GraphCanvas),
                GraphVisibilityScenario,
                GraphStoryLayoutStep);
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

    private sealed class GraphCanvasFitProbe
    {
        public double PanelWidth { get; set; }

        public double HostWidth { get; set; }

        public double HostHeight { get; set; }

        public double HostBottomDelta { get; set; }

        public double SurfaceWidthDelta { get; set; }

        public double SurfaceHeightDelta { get; set; }

        public double ViewportWidthDelta { get; set; }

        public double ViewportHeightDelta { get; set; }
    }

    private sealed class GraphTooltipAnchorProbe
    {
        public bool Visible { get; set; }

        public double ExpectedAnchorX { get; set; }

        public double ExpectedAnchorY { get; set; }

        public double AnchorX { get; set; }

        public double AnchorY { get; set; }

        public string Text { get; set; } = string.Empty;
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

    private static Task<GraphCanvasFitProbe> ReadGraphCanvasFitProbeAsync(IPage page) =>
        page.GetByTestId(UiTestIds.Editor.GraphCanvas)
            .EvaluateAsync<GraphCanvasFitProbe>(
                """
                element => {
                    const panel = element.closest('[data-test="editor-graph-panel"]');
                    const surface = element.querySelector("canvas");
                    const panelRect = panel?.getBoundingClientRect() ?? new DOMRect();
                    const hostRect = element.getBoundingClientRect();
                    const surfaceRect = surface?.getBoundingClientRect() ?? new DOMRect();
                    const viewportWidth = Number(element.dataset.graphViewportWidth || 0);
                    const viewportHeight = Number(element.dataset.graphViewportHeight || 0);
                    return {
                        panelWidth: panelRect.width,
                        hostWidth: hostRect.width,
                        hostHeight: hostRect.height,
                        hostBottomDelta: Math.abs(panelRect.bottom - hostRect.bottom),
                        surfaceWidthDelta: Math.abs(surfaceRect.width - hostRect.width),
                        surfaceHeightDelta: Math.abs(surfaceRect.height - hostRect.height),
                        viewportWidthDelta: Math.abs(viewportWidth - hostRect.width),
                        viewportHeightDelta: Math.abs(viewportHeight - hostRect.height)
                    };
                }
                """);
}
