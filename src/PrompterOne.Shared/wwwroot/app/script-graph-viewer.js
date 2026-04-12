const g6AssetPath = "/_content/PrompterOne.Shared/vendor/antv-g6/v5.1.0/g6.min.js";
const defaultLayoutMode = "story";
const compactLayoutMode = "compact";

let g6LoadPromise = null;

function ensureG6() {
    if (window.G6?.Graph) {
        return Promise.resolve(window.G6);
    }

    g6LoadPromise ??= import(g6AssetPath).then(() => {
        if (!window.G6?.Graph) {
            throw new Error("AntV G6 loaded without exposing Graph.");
        }

        return window.G6;
    });

    return g6LoadPromise;
}

function read(value, camelName, pascalName, fallback) {
    if (!value) {
        return fallback;
    }

    return value[camelName] ?? value[pascalName] ?? fallback;
}

function readColor(name, fallback) {
    const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return value || fallback;
}

function readAttributes(node) {
    return read(node, "attributes", "Attributes", {}) ?? {};
}

function readAttribute(attributes, name) {
    return attributes?.[name] ?? attributes?.[name[0]?.toUpperCase() + name.slice(1)] ?? "";
}

function readLineNumber(node) {
    const value = Number.parseInt(readAttribute(node.data.attributes, "line"), 10);
    return Number.isFinite(value) ? value : Number.MAX_SAFE_INTEGER;
}

function readNodeFill(kind) {
    if (kind === "Document") {
        return "rgba(255, 226, 170, 0.96)";
    }

    if (kind === "Section" || kind === "Heading" || kind === "TpsSegment" || kind === "TpsBlock") {
        return readColor("--accent", "rgba(232, 92, 92, 0.92)");
    }

    if (kind === "Character") {
        return "rgba(126, 185, 232, 0.92)";
    }

    if (kind === "Theme" || kind === "Archetype") {
        return "rgba(105, 213, 152, 0.92)";
    }

    if (kind === "Pace" || kind === "Timing" || kind === "Cue") {
        return "rgba(255, 196, 106, 0.94)";
    }

    if (kind === "Line") {
        return "rgba(248, 241, 226, 0.90)";
    }

    return "rgba(180, 162, 245, 0.90)";
}

function readNodeStroke(kind) {
    if (kind === "Document") {
        return "rgba(255, 196, 106, 0.98)";
    }

    if (kind === "Line") {
        return "rgba(255, 255, 255, 0.36)";
    }

    return "rgba(224, 72, 72, 0.74)";
}

function readLane(kind) {
    if (kind === "Document") {
        return "main";
    }

    if (kind === "Section" || kind === "Heading" || kind === "TpsSegment") {
        return "structure";
    }

    if (kind === "TpsBlock") {
        return "block";
    }

    if (kind === "Character" || kind === "Theme" || kind === "Archetype" || kind === "Pace" || kind === "Timing" || kind === "Cue") {
        return "attribute";
    }

    if (kind === "Line") {
        return "source";
    }

    return "knowledge";
}

function readLanePosition(lane, mode) {
    const compact = mode === compactLayoutMode;
    const positions = compact
        ? { main: 0, structure: -280, block: 280, attribute: 560, source: 840, knowledge: -560 }
        : { main: 0, structure: -420, block: 340, attribute: 760, source: 1140, knowledge: -780 };

    return positions[lane] ?? positions.knowledge;
}

function compareNodes(left, right) {
    const lineDelta = readLineNumber(left) - readLineNumber(right);
    if (lineDelta !== 0) {
        return lineDelta;
    }

    const kindDelta = left.data.kind.localeCompare(right.data.kind);
    return kindDelta || left.data.label.localeCompare(right.data.label);
}

function assignNodePositions(nodes, mode) {
    const lanes = new Map();
    nodes.forEach(node => {
        const lane = readLane(node.data.kind);
        node.data.lane = lane;
        if (!lanes.has(lane)) {
            lanes.set(lane, []);
        }

        lanes.get(lane).push(node);
    });

    lanes.forEach((laneNodes, lane) => {
        laneNodes.sort(compareNodes);
        const columns = lane === "source" && laneNodes.length > 14 ? 2 : 1;
        const x = readLanePosition(lane, mode);
        const gap = mode === compactLayoutMode ? 62 : 72;
        const columnGap = mode === compactLayoutMode ? 210 : 250;
        const startY = -Math.round(((Math.ceil(laneNodes.length / columns) - 1) * gap) / 2);

        laneNodes.forEach((node, index) => {
            const column = columns === 1 ? 0 : index % columns;
            const row = columns === 1 ? index : Math.floor(index / columns);
            node.style.x = x + column * columnGap;
            node.style.y = startY + row * gap;
        });
    });
}

function readNodeSize(kind, mode) {
    const compact = mode === compactLayoutMode;
    if (kind === "Document") {
        return compact ? [168, 56] : [200, 64];
    }

    if (kind === "Line") {
        return compact ? [170, 44] : [220, 46];
    }

    if (kind === "Pace" || kind === "Timing" || kind === "Cue") {
        return compact ? [142, 40] : [170, 42];
    }

    return compact ? [158, 44] : [190, 48];
}

function createNodeData(node, mode) {
    const id = read(node, "id", "Id", "");
    const kind = read(node, "kind", "Kind", "");
    const label = read(node, "label", "Label", id);
    const detail = read(node, "detail", "Detail", "");
    const attributes = readAttributes(node);
    const size = readNodeSize(kind, mode);
    return {
        id,
        type: "rect",
        data: {
            attributes,
            detail,
            group: read(node, "group", "Group", ""),
            kind,
            label
        },
        style: {
            fill: readNodeFill(kind),
            labelFill: kind === "Line" ? "rgba(18, 18, 22, 0.86)" : "rgba(255, 252, 246, 0.94)",
            labelMaxWidth: Math.max(80, size[0] - 22),
            labelText: label,
            radius: 8,
            size,
            stroke: readNodeStroke(kind)
        }
    };
}

function createEdgeData(edge) {
    const id = read(edge, "id", "Id", "");
    const source = read(edge, "sourceId", "SourceId", "");
    const target = read(edge, "targetId", "TargetId", "");
    const label = read(edge, "label", "Label", "");
    return {
        id,
        source,
        target,
        data: {
            label
        },
        style: {
            labelText: label
        }
    };
}

function createGraphData(artifact, mode) {
    const nodes = read(artifact, "nodes", "Nodes", []);
    const edges = read(artifact, "edges", "Edges", []);
    const graphNodes = nodes.map(node => createNodeData(node, mode)).filter(node => node.id);
    const nodeIds = new Set(graphNodes.map(node => node.id));
    const graphEdges = edges
        .map(createEdgeData)
        .filter(edge => edge.id && nodeIds.has(edge.source) && nodeIds.has(edge.target));
    const graphData = {
        nodes: graphNodes,
        edges: graphEdges
    };
    assignNodePositions(graphData.nodes, mode);
    return graphData;
}

function createGraph(g6, element, graphData) {
    return new g6.Graph({
        container: element,
        autoFit: "view",
        data: graphData,
        animation: false,
        node: {
            style: {
                halo: true,
                haloStroke: "rgba(255, 196, 106, 0.34)",
                labelFontSize: 11,
                labelLineHeight: 14,
                labelTextAlign: "center",
                labelTextBaseline: "middle",
                labelWordWrap: true,
                lineWidth: 2
            }
        },
        edge: {
            style: {
                endArrow: true,
                labelFill: "rgba(255, 252, 246, 0.66)",
                labelFontSize: 9,
                lineWidth: 1.4,
                stroke: "rgba(255, 255, 255, 0.28)"
            }
        },
        behaviors: ["drag-canvas", "zoom-canvas", "drag-element"],
        autoResize: true,
        background: "transparent"
    });
}

function createTooltip(element) {
    element.prompterOneGraphTooltip?.remove();
    const tooltip = document.createElement("div");
    tooltip.className = "script-graph-tooltip";
    tooltip.hidden = true;
    element.appendChild(tooltip);
    element.prompterOneGraphTooltip = tooltip;
    return tooltip;
}

function formatAttributes(attributes) {
    return Object.entries(attributes ?? {})
        .filter(([, value]) => value !== null && value !== undefined && `${value}`.trim())
        .map(([key, value]) => `${key}: ${value}`)
        .slice(0, 8);
}

function buildNodeTooltip(node) {
    const attributes = formatAttributes(node.data.attributes);
    return [
        `<strong>${escapeHtml(node.data.label)}</strong>`,
        `<span>${escapeHtml(node.data.kind)}</span>`,
        node.data.detail ? `<p>${escapeHtml(node.data.detail)}</p>` : "",
        attributes.length ? `<small>${attributes.map(escapeHtml).join("<br>")}</small>` : ""
    ].filter(Boolean).join("");
}

function buildEdgeTooltip(edge, graphData) {
    const source = graphData.nodes.find(node => node.id === edge.source)?.data.label ?? edge.source;
    const target = graphData.nodes.find(node => node.id === edge.target)?.data.label ?? edge.target;
    return [
        `<strong>${escapeHtml(edge.data.label || "related")}</strong>`,
        `<p>${escapeHtml(source)} -> ${escapeHtml(target)}</p>`
    ].join("");
}

function escapeHtml(value) {
    return `${value}`.replace(/[&<>"']/g, character => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        "\"": "&quot;",
        "'": "&#39;"
    }[character]));
}

function moveTooltip(element, tooltip, event) {
    const bounds = element.getBoundingClientRect();
    const pointerX = event?.clientX ?? bounds.left + 18;
    const pointerY = event?.clientY ?? bounds.top + 18;
    const nextX = Math.min(Math.max(pointerX - bounds.left + 16, 12), Math.max(12, bounds.width - 260));
    const nextY = Math.min(Math.max(pointerY - bounds.top + 16, 12), Math.max(12, bounds.height - 140));
    tooltip.style.transform = `translate(${Math.round(nextX)}px, ${Math.round(nextY)}px)`;
}

function readEventElementId(event) {
    return event?.target?.id
        ?? event?.item?.id
        ?? event?.item?.getID?.()
        ?? event?.datum?.id
        ?? event?.data?.id
        ?? "";
}

function bindGraphTooltips(graph, element, graphData) {
    const tooltip = createTooltip(element);
    const nodeById = new Map(graphData.nodes.map(node => [node.id, node]));
    const edgeById = new Map(graphData.edges.map(edge => [edge.id, edge]));

    graph.on?.("node:pointerenter", event => {
        const node = nodeById.get(readEventElementId(event));
        if (!node) {
            return;
        }

        tooltip.innerHTML = buildNodeTooltip(node);
        tooltip.hidden = false;
        moveTooltip(element, tooltip, event);
    });
    graph.on?.("node:pointermove", event => moveTooltip(element, tooltip, event));
    graph.on?.("node:pointerleave", () => tooltip.hidden = true);
    graph.on?.("edge:pointerenter", event => {
        const edge = edgeById.get(readEventElementId(event));
        if (!edge) {
            return;
        }

        tooltip.innerHTML = buildEdgeTooltip(edge, graphData);
        tooltip.hidden = false;
        moveTooltip(element, tooltip, event);
    });
    graph.on?.("edge:pointermove", event => moveTooltip(element, tooltip, event));
    graph.on?.("edge:pointerleave", () => tooltip.hidden = true);
    element.dataset.graphTooltips = "true";
}

function findControls(element) {
    return element.closest(".script-graph-panel")?.querySelectorAll("[data-graph-control]") ?? [];
}

async function updateGraphLayout(element, mode) {
    const graph = element.prompterOneGraph;
    const artifact = element.prompterOneGraphArtifact;
    if (!graph || !artifact) {
        return;
    }

    const graphData = createGraphData(artifact, mode);
    element.prompterOneGraphData = graphData;
    element.dataset.graphLayout = mode;
    graph.setData(graphData);
    await graph.draw?.();
    await graph.fitView?.({ padding: 36 }, { duration: 120 });
}

function bindControls(element) {
    element.prompterOneGraphControlsAbort?.abort();
    const abort = new AbortController();
    element.prompterOneGraphControlsAbort = abort;

    findControls(element).forEach(control => {
        control.addEventListener("click", async () => {
            const action = control.getAttribute("data-graph-control");
            const graph = element.prompterOneGraph;
            if (!graph) {
                return;
            }

            if (action === "zoom-in") {
                await graph.zoomBy?.(1.18, { duration: 120 });
            }
            else if (action === "zoom-out") {
                await graph.zoomBy?.(0.84, { duration: 120 });
            }
            else if (action === "fit") {
                await graph.fitView?.({ padding: 36 }, { duration: 120 });
            }
            else if (action === "layout") {
                const nextMode = element.dataset.graphLayout === compactLayoutMode
                    ? defaultLayoutMode
                    : compactLayoutMode;
                await updateGraphLayout(element, nextMode);
            }
        }, { signal: abort.signal });
    });
}

function destroyExistingGraph(element) {
    element.prompterOneGraphControlsAbort?.abort();
    element.prompterOneGraphTooltip?.remove();
    element.prompterOneGraphTooltip = null;

    if (element.prompterOneGraph) {
        element.prompterOneGraph.destroy();
        element.prompterOneGraph = null;
    }
}

export async function render(element, artifact) {
    if (!element || !artifact) {
        return;
    }

    try {
        element.dataset.graphState = "processing";
        const g6 = await ensureG6();
        const mode = element.dataset.graphLayout || defaultLayoutMode;
        const graphData = createGraphData(artifact, mode);

        destroyExistingGraph(element);
        delete element.dataset.graphReady;
        delete element.dataset.graphError;
        delete element.dataset.graphTooltips;

        element.dataset.graphState = "rendering";
        element.dataset.graphLayout = mode;
        element.prompterOneGraphArtifact = artifact;
        element.prompterOneGraphData = graphData;
        element.prompterOneGraph = createGraph(g6, element, graphData);
        bindControls(element);
        await element.prompterOneGraph.render();
        bindGraphTooltips(element.prompterOneGraph, element, graphData);
        await element.prompterOneGraph.fitView?.({ padding: 36 });
        element.dataset.graphReady = "true";
        element.dataset.graphState = "ready";
    }
    catch (error) {
        element.dataset.graphError = error?.message ?? "Graph render failed.";
        element.dataset.graphState = "error";
        throw error;
    }
}
