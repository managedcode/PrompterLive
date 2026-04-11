const g6AssetPath = "/_content/PrompterOne.Shared/vendor/antv-g6/v5.1.0/g6.min.js";

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

function readNodeFill(kind) {
    if (kind === "Section" || kind === "Heading" || kind === "TpsSegment" || kind === "TpsBlock") {
        return readColor("--accent", "rgba(232, 92, 92, 0.92)");
    }

    if (kind === "Entity" || kind === "Character") {
        return "rgba(126, 185, 232, 0.92)";
    }

    if (kind === "Theme" || kind === "Archetype") {
        return "rgba(105, 213, 152, 0.92)";
    }

    return readColor("--gold-text", "rgba(255, 196, 106, 0.94)");
}

function readRing(kind) {
    if (kind === "Document") {
        return 0;
    }

    if (kind === "Section" || kind === "Heading" || kind === "TpsSegment") {
        return 1;
    }

    if (kind === "TpsBlock" || kind === "Entity" || kind === "Character") {
        return 2;
    }

    if (kind === "Theme" || kind === "Archetype" || kind === "Line") {
        return 3;
    }

    return 2;
}

function assignNodePositions(nodes, edges) {
    const degree = new Map(nodes.map(node => [node.id, 0]));
    edges.forEach(edge => {
        degree.set(edge.source, (degree.get(edge.source) ?? 0) + 1);
        degree.set(edge.target, (degree.get(edge.target) ?? 0) + 1);
    });

    const rings = new Map();
    nodes.forEach(node => {
        const ring = readRing(node.data.kind);
        if (!rings.has(ring)) {
            rings.set(ring, []);
        }

        rings.get(ring).push(node);
    });

    [...rings.entries()]
        .sort(([left], [right]) => left - right)
        .forEach(([ring, ringNodes]) => {
            ringNodes.sort((left, right) => {
                const degreeDelta = (degree.get(right.id) ?? 0) - (degree.get(left.id) ?? 0);
                return degreeDelta || left.data.label.localeCompare(right.data.label);
            });

            const radius = ring === 0 ? 0 : 150 + (ring - 1) * 130;
            ringNodes.forEach((node, index) => {
                const angle = ringNodes.length === 1
                    ? -Math.PI / 2
                    : (Math.PI * 2 * index) / ringNodes.length - Math.PI / 2;
                node.style.x = Math.round(Math.cos(angle) * radius);
                node.style.y = Math.round(Math.sin(angle) * radius);
                node.style.size = ring === 0 ? 48 : node.style.size;
            });
        });
}

function createNodeData(node) {
    const id = read(node, "id", "Id", "");
    const kind = read(node, "kind", "Kind", "");
    const label = read(node, "label", "Label", id);
    return {
        id,
        data: {
            group: read(node, "group", "Group", ""),
            kind,
            label
        },
        style: {
            fill: readNodeFill(kind),
            labelText: label
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

function createGraphData(artifact) {
    const nodes = read(artifact, "nodes", "Nodes", []);
    const edges = read(artifact, "edges", "Edges", []);
    const graphNodes = nodes.map(createNodeData).filter(node => node.id);
    const nodeIds = new Set(graphNodes.map(node => node.id));
    const graphEdges = edges
        .map(createEdgeData)
        .filter(edge => edge.id && nodeIds.has(edge.source) && nodeIds.has(edge.target));
    const graphData = {
        nodes: graphNodes,
        edges: graphEdges
    };
    assignNodePositions(graphData.nodes, graphData.edges);
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
                haloStroke: "rgba(255, 196, 106, 0.35)",
                labelFill: "rgba(255, 252, 246, 0.92)",
                labelFontSize: 11,
                labelMaxWidth: 132,
                labelWordWrap: true,
                lineWidth: 2,
                size: 34,
                stroke: "rgba(224, 72, 72, 0.82)"
            }
        },
        edge: {
            style: {
                endArrow: true,
                labelFill: "rgba(255, 252, 246, 0.68)",
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

export async function render(element, artifact) {
    if (!element || !artifact) {
        return;
    }

    try {
        element.dataset.graphState = "processing";
        const g6 = await ensureG6();
        const graphData = createGraphData(artifact);

        if (element.prompterOneGraph) {
            element.prompterOneGraph.destroy();
        }

        delete element.dataset.graphReady;
        delete element.dataset.graphError;

        element.dataset.graphState = "rendering";
        element.prompterOneGraph = createGraph(g6, element, graphData);
        await element.prompterOneGraph.render();
        await element.prompterOneGraph.fitView?.({ padding: 24 });
        element.dataset.graphReady = "true";
        element.dataset.graphState = "ready";
    }
    catch (error) {
        element.dataset.graphError = error?.message ?? "Graph render failed.";
        element.dataset.graphState = "error";
        throw error;
    }
}
