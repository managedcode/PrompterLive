const g6AssetPath = "/_content/PrompterOne.Shared/vendor/antv-g6/v5.1.0/g6.min.js";
const defaultLayoutMode = "story";
const compactLayoutMode = "compact";
const defaultNodeStyleMode = "compact";
const colorFallbacks = {
    documentFill: "#111A1F", documentStroke: "#C4A060",
    sectionFill: "#162028", sectionStroke: "rgba(196, 160, 96, .40)",
    blockFill: "#111A1F", blockStroke: "rgba(196, 160, 96, .22)",
    ideaFill: "#162028", ideaStroke: "rgba(196, 160, 96, .35)",
    characterFill: "#162028", characterStroke: "rgba(142, 207, 255, .64)",
    themeFill: "#162028", themeStroke: "rgba(111, 232, 154, .58)",
    defaultFill: "#162028", defaultStroke: "rgba(196, 160, 96, .28)",
    activeStroke: "rgba(196, 160, 96, .40)", edgeStroke: "rgba(196, 160, 96, .40)",
    edgeStructural: "rgba(196, 160, 96, .28)", edgeSemantic: "rgba(111, 232, 154, .52)",
    edgeMention: "rgba(196, 160, 96, .42)", edgeReference: "rgba(142, 207, 255, .50)",
    edgeCharacter: "rgba(142, 207, 255, .58)", edgeClaim: "rgba(111, 232, 154, .62)",
    labelLight: "#E8D5B0", labelDark: "#E8D5B0", labelDim: "#8A7E6E",
    glassBg: "rgba(6, 8, 16, .35)", laneHeaderBorder: "rgba(196, 160, 96, .12)",
    laneHeaderLabel: "#B0A08A", haloStroke: "rgba(255, 196, 106, 0.34)"
};
const defaultKindStyle = {
    fill: "defaultFill",
    stroke: "defaultStroke",
    labelFill: "labelLight",
    fontSize: 13,
    fontWeight: 600,
    maxLines: 4,
    compactSize: [220, 60],
    size: [250, 68]
};
const manualLayoutModes = new Set([
    "knowledge", "compact", "structure", "delivery"
]);
const fitViewPadding = 36;

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

function cfgColor(config, key, fallback) {
    const token = config?.colors?.[key];
    if (token) {
        const host = config?.colorHost instanceof Element ? config.colorHost : document.documentElement;
        const hostValue = getComputedStyle(host).getPropertyValue(token).trim();
        if (hostValue) return hostValue;

        const rootValue = getComputedStyle(document.documentElement).getPropertyValue(token).trim();
        if (rootValue) return rootValue;
    }
    return fallback;
}

function cfgArray(config, key) {
    const value = config?.[key];
    return Array.isArray(value) ? value : [];
}

function cfgSet(config, key) {
    return new Set(cfgArray(config, key));
}

function cfgObject(config, key) {
    const value = config?.[key];
    return value && typeof value === "object" ? value : {};
}

function cfgNodeStyleMode(config) {
    return config?.nodeStyleMode ?? config?.NodeStyleMode ?? defaultNodeStyleMode;
}

function cfgNodeVisualStyle(config) {
    const presets = cfgObject(config, "nodeStylePresets");
    const mode = cfgNodeStyleMode(config);
    return presets[mode] ?? presets[defaultNodeStyleMode] ?? {
        widthScale: 0.66,
        heightScale: 0.72,
        fontScale: 0.9,
        fontSizeDelta: -1,
        maxLineDelta: -2,
        labelWidthScale: 0.74,
        radius: 8,
        showLabels: true,
        compactBase: true,
        defaultShape: "ellipse",
        kindShapes: {}
    };
}

function readNodeType(kind, config) {
    const visualStyle = cfgNodeVisualStyle(config);
    const kindShapes = visualStyle.kindShapes ?? visualStyle.KindShapes ?? {};
    return kindShapes[kind] ?? visualStyle.defaultShape ?? visualStyle.DefaultShape ?? "ellipse";
}

function cfgLaneOrder(config, lanes) {
    const configured = cfgArray(config, "laneOrder");
    const extras = [...(lanes?.keys?.() ?? [])].filter(lane => !configured.includes(lane));
    return configured.length ? [...configured, ...extras] : extras;
}

function cfgModeLaneFilter(config, mode) {
    const lanes = cfgObject(config, "modeLaneFilters")?.[mode];
    return Array.isArray(lanes) ? new Set(lanes) : null;
}

function readKindStyle(kind, config) {
    const styles = cfgObject(config, "kindStyles");
    return styles[kind] ?? styles.default ?? defaultKindStyle;
}

function cfgLaneTitle(config, lane) {
    return config?.laneTitles?.[lane] ?? lane;
}

function readAttributes(node) {
    return read(node, "attributes", "Attributes", {}) ?? {};
}

function readSourceRanges(artifact) {
    return read(artifact, "sourceRanges", "SourceRanges", []) ?? [];
}

function createSourceRangeIds(artifact) {
    return new Set(readSourceRanges(artifact)
        .map(range => read(range, "nodeId", "NodeId", ""))
        .filter(Boolean));
}

function readNodeFill(kind, config) {
    const style = readKindStyle(kind, config);
    return cfgColor(config, style.fill ?? defaultKindStyle.fill, colorFallbacks.defaultFill);
}

function readNodeStroke(kind, config) {
    const style = readKindStyle(kind, config);
    return cfgColor(config, style.stroke ?? defaultKindStyle.stroke, colorFallbacks.defaultStroke);
}

function readLane(kind, config) {
    return cfgObject(config, "laneByKind")[kind] ?? "knowledge";
}

function readLaneIndex(kind, config) {
    const order = cfgLaneOrder(config);
    const index = order.indexOf(readLane(kind, config));
    return index < 0 ? order.length : index;
}

function compareNodes(left, right, config) {
    const laneDelta = readLaneIndex(left.data.kind, config) - readLaneIndex(right.data.kind, config);
    if (laneDelta !== 0) {
        return laneDelta;
    }

    const kindDelta = left.data.kind.localeCompare(right.data.kind);
    return kindDelta || left.data.label.localeCompare(right.data.label);
}

function readLaneGap(lane, mode, laneNodes) {
    const largestNodeHeight = laneNodes?.reduce((height, node) => {
        const size = node.style?.size ?? [0, 0];
        return Math.max(height, size[1] ?? 0);
    }, 0) ?? 0;
    const readableGap = largestNodeHeight + (mode === compactLayoutMode ? 14 : 18);

    if (lane === "delivery") {
        return Math.max(mode === compactLayoutMode ? 38 : 44, readableGap);
    }

    if (lane === "structure" || lane === "block") {
        return Math.max(mode === compactLayoutMode ? 52 : 62, readableGap);
    }

    return Math.max(mode === compactLayoutMode ? 48 : 56, readableGap);
}

function readLaneWidth(lane, laneNodes, mode, config) {
    return laneNodes.reduce((width, node) => {
        const size = node.style?.size ?? readNodeSize(node.data.kind, mode, node.style?.labelText ?? node.data.label, config);
        return Math.max(width, size[0] ?? 0);
    }, 0);
}

function assignLaneCenters(lanes, mode, config) {
    const activeLanes = cfgLaneOrder(config, lanes).filter(lane => lanes.has(lane));
    const lanePadding = mode === compactLayoutMode ? 46 : 60;
    const widths = activeLanes.map(lane => readLaneWidth(lane, lanes.get(lane), mode, config));
    const totalWidth = widths.reduce((total, width) => total + width, 0) +
        Math.max(0, activeLanes.length - 1) * lanePadding;
    let cursor = -totalWidth / 2;
    const centers = new Map();

    activeLanes.forEach((lane, index) => {
        const width = widths[index];
        centers.set(lane, cursor + width / 2);
        cursor += width + lanePadding;
    });

    return centers;
}

function assignNodePositions(nodes, mode, config, element, edges) {
    if (mode === "knowledge") {
        return assignKnowledgeNodePositions(nodes);
    }

    if (mode === "grid") {
        return assignGridNodePositions(nodes, element);
    }

    if (mode === "radial") {
        return assignRadialNodePositions(nodes);
    }

    if (mode === "circular") {
        return assignCircularNodePositions(nodes);
    }

    if (mode === "concentric") {
        return assignConcentricNodePositions(nodes);
    }

    if (isSemanticFallbackMode(mode)) {
        return assignSemanticMapNodePositions(nodes, mode, element, edges);
    }

    const lanes = new Map();
    const laneHeaders = [];
    nodes.forEach(node => {
        const lane = readLane(node.data.kind, config);
        node.data.lane = lane;
        if (!lanes.has(lane)) {
            lanes.set(lane, []);
        }

        lanes.get(lane).push(node);
    });

    const laneCenters = assignLaneCenters(lanes, mode, config);

    lanes.forEach((laneNodes, lane) => {
        laneNodes.sort((left, right) => compareNodes(left, right, config));
        const laneCenter = laneCenters.get(lane) ?? 0;
        const gap = readLaneGap(lane, mode, laneNodes);
        const startY = -Math.round(((laneNodes.length - 1) * gap) / 2);
        laneHeaders.push({ lane, x: laneCenter, y: startY - gap });

        laneNodes.forEach((node, index) => {
            node.style.x = laneCenter;
            node.style.y = startY + index * gap;
        });
    });

    return laneHeaders;
}

function assignKnowledgeNodePositions(nodes) {
    const documentNodes = nodes.filter(node => node.data.kind === "Document");
    const ideaNodes = nodes
        .filter(node => node.data.kind === "Claim" || node.data.kind === "Idea")
        .sort((left, right) => left.data.kind.localeCompare(right.data.kind) || left.data.label.localeCompare(right.data.label));
    const tagNodes = nodes
        .filter(node => node.data.kind !== "Document" && node.data.kind !== "Claim" && node.data.kind !== "Idea")
        .sort((left, right) => left.data.kind.localeCompare(right.data.kind) || left.data.label.localeCompare(right.data.label));

    positionNodeColumn(documentNodes, 95, 0, 120);
    positionNodeGrid(ideaNodes, -710, 0, 430, 230);
    positionNodeGrid(tagNodes, 390, 0, 270, 90);
    return [];
}

function isSemanticFallbackMode(mode) {
    return mode === "mds" ||
        mode === "relationship" ||
        mode === "force" ||
        mode === "fruchterman" ||
        mode === "force-atlas2" ||
        mode === "d3-force";
}

function sortByGraphImportance(left, right) {
    return (right.data.degree ?? 0) - (left.data.degree ?? 0) ||
        readKindWeight(left.data.kind) - readKindWeight(right.data.kind) ||
        left.data.displayLabel.localeCompare(right.data.displayLabel);
}

function readKindWeight(kind) {
    switch (kind) {
        case "Document":
            return 0;
        case "Idea":
        case "Claim":
            return 1;
        case "Section":
        case "Heading":
        case "TpsSegment":
            return 2;
        case "TpsBlock":
            return 3;
        case "Theme":
        case "Term":
        case "Character":
            return 4;
        default:
            return 5;
    }
}

function readMaxNodeSize(nodes) {
    return nodes.reduce((maxSize, node) => {
        const size = node.style?.size ?? [160, 48];
        return [
            Math.max(maxSize[0], size[0] ?? 0),
            Math.max(maxSize[1], size[1] ?? 0)
        ];
    }, [160, 48]);
}

function assignGridNodePositions(nodes, element) {
    if (!nodes.length) {
        return [];
    }

    const bounds = readLayoutBounds(element);
    const [maxWidth, maxHeight] = readMaxNodeSize(nodes);
    const aspect = Math.max(.65, Math.min(1.45, bounds.width / bounds.height));
    const columnCount = Math.max(3, Math.min(5, Math.ceil(Math.sqrt(nodes.length * aspect * .72))));
    const rowCount = Math.ceil(nodes.length / columnCount);
    const columnGap = Math.max(164, maxWidth + 64);
    const rowGap = Math.max(74, maxHeight + 34);
    const startX = -((columnCount - 1) * columnGap) / 2;
    const startY = -((rowCount - 1) * rowGap) / 2;

    nodes
        .sort(sortByGraphImportance)
        .forEach((node, index) => {
            node.style.x = startX + (index % columnCount) * columnGap;
            node.style.y = startY + Math.floor(index / columnCount) * rowGap;
        });

    return [];
}

function assignRadialNodePositions(nodes) {
    const groups = [
        nodes.filter(node => node.data.kind === "Document"),
        nodes.filter(node => node.data.kind === "Idea" || node.data.kind === "Claim"),
        nodes.filter(node => node.data.kind === "Section" || node.data.kind === "Heading" || node.data.kind === "TpsSegment"),
        nodes.filter(node => node.data.kind === "Theme" || node.data.kind === "Term" || node.data.kind === "Character"),
        nodes.filter(node => !["Document", "Idea", "Claim", "Section", "Heading", "TpsSegment", "Theme", "Term", "Character"].includes(node.data.kind))
    ];
    positionNodeColumn(groups[0].sort(sortByGraphImportance), 0, 0, 96);
    positionNodeRings(groups.slice(1), 260);
    return [];
}

function assignCircularNodePositions(nodes) {
    const sorted = [...nodes].sort(sortByGraphImportance);
    positionNodeRings(splitChunks(sorted, 16), 320);
    return [];
}

function assignConcentricNodePositions(nodes) {
    const high = nodes.filter(node => (node.data.degree ?? 0) >= 8).sort(sortByGraphImportance);
    const medium = nodes.filter(node => (node.data.degree ?? 0) >= 4 && (node.data.degree ?? 0) < 8).sort(sortByGraphImportance);
    const low = nodes.filter(node => (node.data.degree ?? 0) < 4).sort(sortByGraphImportance);
    positionNodeColumn(high.slice(0, 2), 0, 0, 96);
    positionNodeRings([high.slice(2), ...splitChunks(medium, 14), ...splitChunks(low, 16)], 300);
    return [];
}

function assignSemanticMapNodePositions(nodes, mode, element, edges) {
    const [maxWidth, maxHeight] = readMaxNodeSize(nodes);
    const columnGap = Math.max(250, maxWidth + 92);
    const rowGap = Math.max(88, maxHeight + 42);
    const documentNodes = nodes.filter(node => node.data.kind === "Document").sort(sortByGraphImportance);
    const ideas = nodes
        .filter(node => node.data.kind === "Idea" || node.data.kind === "Claim")
        .sort((left, right) => readEdgeBridgeScore(right, edges) - readEdgeBridgeScore(left, edges) || sortByGraphImportance(left, right));
    const structure = nodes
        .filter(node => node.data.kind === "Section" || node.data.kind === "Heading" || node.data.kind === "TpsSegment" || node.data.kind === "TpsBlock")
        .sort(sortByGraphImportance);
    const references = nodes
        .filter(node => !documentNodes.includes(node) && !ideas.includes(node) && !structure.includes(node))
        .sort(sortByGraphImportance);
    const forceBias = mode === "relationship" || mode === "force-atlas2" || mode === "d3-force" ? .34 : 0;
    positionNodeColumn(documentNodes, 0, -rowGap * (2.1 - forceBias), rowGap);
    positionNodeGridAt(ideas, 0, rowGap * (1.1 + forceBias), 2, columnGap, rowGap);
    positionNodeGridAt(structure, -columnGap * 1.55, 0, 2, columnGap, rowGap);
    positionNodeGridAt(references, columnGap * 1.75, 0, readReferenceColumnCount(references, element), columnGap, rowGap);
    return [];
}

function readReferenceColumnCount(nodes, element) {
    const bounds = readLayoutBounds(element);
    if (nodes.length <= 8) {
        return 1;
    }

    return bounds.width > 920 ? 3 : 2;
}

function readEdgeBridgeScore(node, edges) {
    return edges.filter(edge => edge.source === node.id || edge.target === node.id).length;
}

function positionNodeGridAt(nodes, centerX, centerY, maxColumns, columnGap, rowGap) {
    if (!nodes.length) {
        return;
    }

    const columns = Math.max(1, Math.min(maxColumns, Math.ceil(Math.sqrt(nodes.length))));
    const rows = Math.ceil(nodes.length / columns);
    const startX = centerX - ((columns - 1) * columnGap) / 2;
    const startY = centerY - ((rows - 1) * rowGap) / 2;
    nodes.forEach((node, index) => {
        node.style.x = startX + (index % columns) * columnGap;
        node.style.y = startY + Math.floor(index / columns) * rowGap;
    });
}

function positionNodeRings(groups, startRadius) {
    const visibleGroups = groups.filter(group => group.length);
    let radius = startRadius;
    visibleGroups.forEach((group, index) => {
        splitChunks(group.sort(sortByGraphImportance), 16).forEach(chunk => {
            const [maxWidth, maxHeight] = readMaxNodeSize(chunk);
            const requiredRadius = Math.ceil((chunk.length * (maxWidth + 82)) / (2 * Math.PI));
            radius = Math.max(radius, requiredRadius);
            positionNodeRing(chunk, radius, (-Math.PI / 2) + index * .24);
            radius += maxHeight + 112;
        });
    });
}

function positionNodeRing(nodes, radius, angleOffset) {
    nodes.forEach((node, index) => {
        const angle = angleOffset + (index / nodes.length) * Math.PI * 2;
        node.style.x = Math.round(Math.cos(angle) * radius);
        node.style.y = Math.round(Math.sin(angle) * radius);
    });
}

function splitChunks(nodes, chunkSize) {
    const chunks = [];
    for (let index = 0; index < nodes.length; index += chunkSize) {
        chunks.push(nodes.slice(index, index + chunkSize));
    }
    return chunks;
}

function positionNodeColumn(nodes, x, centerY, gap) {
    const totalHeight = nodes.reduce((height, node) => height + (node.style?.size?.[1] ?? 56), 0) +
        Math.max(0, nodes.length - 1) * gap;
    let cursor = centerY - totalHeight / 2;
    nodes.forEach(node => {
        const size = node.style?.size ?? [160, 56];
        node.style.x = x;
        node.style.y = cursor + size[1] / 2;
        cursor += size[1] + gap;
    });
}

function positionNodeGrid(nodes, startX, centerY, columnGap, rowGap) {
    if (!nodes.length) {
        return;
    }

    const columnCount = nodes.length > 18 ? 3 : 2;
    const rows = Math.ceil(nodes.length / columnCount);
    const startY = centerY - ((rows - 1) * rowGap) / 2;
    nodes.forEach((node, index) => {
        const column = index % columnCount;
        const row = Math.floor(index / columnCount);
        node.style.x = startX + column * columnGap;
        node.style.y = startY + row * rowGap;
    });
}

function readNodeSize(kind, mode, label = "", config) {
    const style = readKindStyle(kind, config);
    const visualStyle = cfgNodeVisualStyle(config);
    const compact = mode === compactLayoutMode || (visualStyle.compactBase ?? visualStyle.CompactBase ?? false);
    const lineBudget = compact ? (style.compactLineBudget ?? 24) : (style.lineBudget ?? 30);
    const maxLineCount = Math.max(1, (style.maxLines ?? defaultKindStyle.maxLines) +
        (visualStyle.maxLineDelta ?? visualStyle.MaxLineDelta ?? 0));
    const lineCount = Math.max(1, Math.min(maxLineCount, Math.ceil((label?.trim().length ?? 0) / lineBudget)));
    const extraHeight = (lineCount - 1) * Math.max(12, 18 * (visualStyle.fontScale ?? visualStyle.FontScale ?? 1));
    const size = compact ? (style.compactSize ?? defaultKindStyle.compactSize) : (style.size ?? defaultKindStyle.size);
    const widthScale = visualStyle.widthScale ?? visualStyle.WidthScale ?? 1;
    const heightScale = visualStyle.heightScale ?? visualStyle.HeightScale ?? 1;
    return [
        Math.max(38, Math.round((size[0] ?? defaultKindStyle.size[0]) * widthScale)),
        Math.max(30, Math.round(((size[1] ?? defaultKindStyle.size[1]) + extraHeight) * heightScale))
    ];
}

function readNodeDegree(nodeId, degreeByNode) {
    return degreeByNode?.get(nodeId) ?? 0;
}

function scaleNodeSize(size, degree, kind) {
    if (kind === "Document") {
        return size;
    }

    const widthScale = 1 + Math.min(degree, 8) * 0.035;
    const heightScale = 1 + Math.min(degree, 8) * 0.025;
    return [
        Math.round(size[0] * Math.min(widthScale, 1.24)),
        Math.round(size[1] * Math.min(heightScale, 1.18))
    ];
}

function cleanDisplayText(value) {
    const text = removeInlineGraphMarkup(`${value ?? ""}`)
        .replace(/^\s*#{1,6}\s+/, "")
        .replace(/^Line\s+\d+\s*:\s*/i, "")
        .trim();
    if (!text) {
        return "";
    }

    const primary = text.split("|").map(part => part.trim()).filter(Boolean)[0] ?? text;
    return primary.trim();
}

function removeInlineGraphMarkup(value) {
    let text = `${value ?? ""}`;
    text = text.replace(/\[([^\]\r\n]+)\]\([^)]+\)/g, "$1");
    text = text.replace(/\[[^\]\r\n]+\]/g, " ");
    return text.replace(/\s+/g, " ");
}

function readDisplayLabel(kind, label, detail) {
    return cleanDisplayText(label) || cleanDisplayText(detail) || kind;
}

function readLabelFill(kind, config) {
    const style = readKindStyle(kind, config);
    return cfgColor(config, style.labelFill ?? defaultKindStyle.labelFill, colorFallbacks.labelLight);
}

function readLabelFontSize(kind, config) {
    const visualStyle = cfgNodeVisualStyle(config);
    const base = readKindStyle(kind, config).fontSize ?? defaultKindStyle.fontSize;
    return Math.max(8, Math.round((base + (visualStyle.fontSizeDelta ?? visualStyle.FontSizeDelta ?? 0)) *
        (visualStyle.fontScale ?? visualStyle.FontScale ?? 1)));
}

function readLabelFontWeight(kind, config) {
    return readKindStyle(kind, config).fontWeight ?? defaultKindStyle.fontWeight;
}

function readLabelLineHeight(kind, config) {
    return Math.max(18, readLabelFontSize(kind, config) + 4);
}

function readLabelMaxWidth(kind, size, config) {
    const visualStyle = cfgNodeVisualStyle(config);
    return Math.max(34, Math.round((size[0] - (readKindStyle(kind, config).labelPadding ?? 32)) *
        (visualStyle.labelWidthScale ?? visualStyle.LabelWidthScale ?? 1)));
}

function createNodeData(node, mode, config, sourceRangeIds, degreeByNode) {
    const id = read(node, "id", "Id", "");
    const kind = read(node, "kind", "Kind", "");
    const label = read(node, "label", "Label", id);
    const detail = read(node, "detail", "Detail", "");
    const attributes = readAttributes(node);
    const visualStyle = cfgNodeVisualStyle(config);
    const displayLabel = readDisplayLabel(kind, label, detail);
    const degree = readNodeDegree(id, degreeByNode);
    const size = scaleNodeSize(readNodeSize(kind, mode, displayLabel, config), degree, kind);
    const hasSourceRange = sourceRangeIds.has(id);
    const labelMaxLines = Math.max(1, (readKindStyle(kind, config).maxLines ?? defaultKindStyle.maxLines) +
        (visualStyle.maxLineDelta ?? visualStyle.MaxLineDelta ?? 0));
    const showLabels = visualStyle.showLabels ?? visualStyle.ShowLabels ?? true;
    return {
        id,
        type: readNodeType(kind, config),
        data: {
            attributes,
            detail,
            group: read(node, "group", "Group", ""),
            hasSourceRange,
            kind,
            label,
            displayLabel,
            degree
        },
        style: {
            cursor: hasSourceRange ? "pointer" : "default",
            fill: readNodeFill(kind, config),
            labelFill: readLabelFill(kind, config),
            labelFontSize: readLabelFontSize(kind, config),
            labelFontWeight: readLabelFontWeight(kind, config),
            labelLineHeight: readLabelLineHeight(kind, config),
            labelMaxWidth: readLabelMaxWidth(kind, size, config),
            labelMaxLines,
            labelPlacement: "center",
            labelText: showLabels ? displayLabel : "",
            labelTextAlign: "center",
            labelTextBaseline: "middle",
            labelWordWrap: true,
            labelWordWrapWidth: readLabelMaxWidth(kind, size, config),
            radius: visualStyle.radius ?? visualStyle.Radius ?? 8,
            size,
            stroke: readNodeStroke(kind, config)
        }
    };
}

function createLaneHeaderNode(lane, x, y, config) {
    const label = cfgLaneTitle(config, lane);
    return {
        id: `prompterone:graph:lane:${lane}`,
        type: "rect",
        data: {
            attributes: {},
            detail: "",
            group: "graph",
            kind: "LaneHeader",
            label
        },
        style: {
            fill: cfgColor(config, "glassBg", colorFallbacks.glassBg),
            labelFill: cfgColor(config, "laneHeaderLabel", colorFallbacks.laneHeaderLabel),
            labelFontSize: 13,
            labelFontWeight: 700,
            labelMaxWidth: 150,
            labelPlacement: "center",
            labelText: label,
            labelTextAlign: "center",
            labelTextBaseline: "middle",
            lineWidth: 1,
            radius: 8,
            size: [168, 30],
            stroke: cfgColor(config, "laneHeaderBorder", colorFallbacks.laneHeaderBorder),
            x,
            y
        }
    };
}

function readEdgeStyle(label, config) {
    const styles = cfgObject(config, "edgeStyles");
    return styles[label] ?? styles.default ?? {};
}

function readEdgeLineDash(edgeStyle) {
    const dash = edgeStyle.lineDash ?? edgeStyle.LineDash;
    return Array.isArray(dash) ? dash : [];
}

function createEdgeData(edge, config) {
    const id = read(edge, "id", "Id", "");
    const source = read(edge, "sourceId", "SourceId", "");
    const target = read(edge, "targetId", "TargetId", "");
    const label = read(edge, "label", "Label", "");
    const edgeStyle = readEdgeStyle(label, config);
    const stroke = edgeStyle.stroke ?? edgeStyle.Stroke ?? "edgeStroke";
    const edgeStroke = cfgColor(config, stroke, colorFallbacks[stroke] ?? colorFallbacks.edgeStroke);
    return {
        id,
        source,
        target,
        data: { label },
        style: {
            endArrow: true,
            labelText: "",
            lineDash: readEdgeLineDash(edgeStyle),
            lineWidth: edgeStyle.lineWidth ?? edgeStyle.LineWidth ?? 1.7,
            opacity: edgeStyle.opacity ?? edgeStyle.Opacity ?? 0.72,
            stroke: edgeStroke,
            endArrowFill: edgeStroke,
            endArrowStroke: edgeStroke
        }
    };
}

function createDegreeMap(edges) {
    const degreeByNode = new Map();
    edges.forEach(edge => {
        if (edge.source) {
            degreeByNode.set(edge.source, (degreeByNode.get(edge.source) ?? 0) + 1);
        }

        if (edge.target) {
            degreeByNode.set(edge.target, (degreeByNode.get(edge.target) ?? 0) + 1);
        }
    });
    return degreeByNode;
}

function applyVisibleDegreeStyles(nodes, degreeByNode, config) {
    nodes.forEach(node => {
        const degree = readNodeDegree(node.id, degreeByNode);
        const size = scaleNodeSize(node.style.size, degree, node.data.kind);
        node.data.degree = degree;
        node.style.size = size;
        node.style.labelMaxWidth = readLabelMaxWidth(node.data.kind, size, config);
        node.style.labelWordWrapWidth = readLabelMaxWidth(node.data.kind, size, config);
    });
}

function createGraphData(artifact, mode, config, element) {
    const nodes = read(artifact, "nodes", "Nodes", []);
    const edges = read(artifact, "edges", "Edges", []);
    const edgeData = edges.map(edge => createEdgeData(edge, config));
    const sourceRangeIds = createSourceRangeIds(artifact);
    const laneFilter = cfgModeLaneFilter(config, mode);
    const nonVisualNodeKinds = cfgSet(config, "nonVisualKinds");
    const storyNodeKinds = cfgSet(config, "storyKinds");
    const graphNodes = nodes
        .map(node => createNodeData(node, mode, config, sourceRangeIds))
        .filter(node => {
            if (!node.id || nonVisualNodeKinds.has(node.data.kind)) return false;
            if (mode === defaultLayoutMode && !storyNodeKinds.has(node.data.kind)) return false;
            if (laneFilter) {
                const lane = readLane(node.data.kind, config);
                return laneFilter.has(lane);
            }
            return true;
        });

    const nodeIds = new Set(graphNodes.map(node => node.id));
    const graphEdges = mode === defaultLayoutMode
        ? createStoryEdges(edgeData, nodeIds)
        : edgeData.filter(edge => edge.id && nodeIds.has(edge.source) && nodeIds.has(edge.target));
    applyVisibleDegreeStyles(graphNodes, createDegreeMap(graphEdges), config);

    if (manualLayoutModes.has(mode)) {
        const laneHeaders = assignNodePositions(graphNodes, mode, config, element, graphEdges)
            .filter(header => header.lane !== "main")
            .map(header => createLaneHeaderNode(header.lane, header.x, header.y, config));
        graphNodes.push(...laneHeaders);
    }

    return { nodes: graphNodes, edges: graphEdges };
}

function createStoryEdges(edges, visibleNodeIds) {
    const parentByNode = new Map();
    edges.forEach(edge => {
        if (edge.source && edge.target && !parentByNode.has(edge.target)) {
            parentByNode.set(edge.target, edge.source);
        }
    });

    const edgeKeys = new Set();
    const graphEdges = [];
    edges.forEach(edge => {
        const source = resolveVisibleAncestor(edge.source, visibleNodeIds, parentByNode);
        const target = visibleNodeIds.has(edge.target) ? edge.target : "";
        if (!edge.id || !source || !target || source === target) {
            return;
        }

        const key = `${source}->${target}:${edge.data?.label ?? ""}`;
        if (edgeKeys.has(key)) {
            return;
        }

        edgeKeys.add(key);
        graphEdges.push({ ...edge, id: `story:${edge.id}`, source, target });
    });
    return graphEdges;
}

function resolveVisibleAncestor(nodeId, visibleNodeIds, parentByNode) {
    let current = nodeId;
    const seen = new Set();
    while (current && !seen.has(current)) {
        if (visibleNodeIds.has(current)) {
            return current;
        }

        seen.add(current);
        current = parentByNode.get(current);
    }
    return "";
}

function readLayoutBounds(element) {
    const rect = element.getBoundingClientRect?.() ?? {};
    return {
        width: Math.max(1, Math.round(rect.width || element.clientWidth || 1024)),
        height: Math.max(1, Math.round(rect.height || element.clientHeight || 640))
    };
}

function readLayoutMetrics(graphData, element) {
    const bounds = readLayoutBounds(element);
    const sizes = graphData.nodes.map(node => node.style?.size ?? [160, 48]);
    const nodeCount = Math.max(1, sizes.length);
    const avgWidth = sizes.reduce((total, size) => total + (size[0] ?? 160), 0) / nodeCount;
    const avgHeight = sizes.reduce((total, size) => total + (size[1] ?? 48), 0) / nodeCount;
    const maxWidth = sizes.reduce((value, size) => Math.max(value, size[0] ?? 0), 0);
    const maxHeight = sizes.reduce((value, size) => Math.max(value, size[1] ?? 0), 0);
    const collisionRadius = Math.ceil(Math.hypot(avgWidth, avgHeight) / 2 + 28);
    const columnCount = Math.max(2, Math.ceil(Math.sqrt(nodeCount * bounds.width / bounds.height)));
    const rowCount = Math.max(1, Math.ceil(nodeCount / columnCount));
    return {
        avgHeight,
        avgWidth,
        bounds,
        collisionRadius,
        graphHeight: Math.max(bounds.height * 1.4, rowCount * (maxHeight + 76)),
        graphWidth: Math.max(bounds.width * 1.4, columnCount * (maxWidth + 96)),
        maxHeight,
        maxWidth,
        nodeCount
    };
}

function createLayoutNodeSize(graphData, paddingX = 34, paddingY = 26) {
    const sizeByNode = new Map(graphData.nodes.map(node => [node.id, node.style?.size ?? [160, 48]]));
    return node => {
        const size = node?.style?.size ?? sizeByNode.get(node?.id) ?? [160, 48];
        return [
            Math.max(38, Math.ceil((size[0] ?? 160) + paddingX)),
            Math.max(30, Math.ceil((size[1] ?? 48) + paddingY))
        ];
    };
}

function createLayoutNodeDiameter(metrics, padding = 48) {
    return Math.ceil(Math.hypot(metrics.maxWidth, metrics.maxHeight) + padding);
}

function createLayoutConfig(mode, graphData, element) {
    const metrics = readLayoutMetrics(graphData, element);
    const nodeSize = createLayoutNodeSize(graphData);
    const storyNodeSize = createLayoutNodeSize(graphData, 18, 18);
    const nodeDiameter = createLayoutNodeDiameter(metrics);
    const linkDistance = Math.max(180, Math.ceil(metrics.avgWidth * 1.8));

    switch (mode) {
        case "story":
            return { type: "dagre", rankdir: "LR", nodesep: 56, ranksep: 76, nodeSize: storyNodeSize };
        case "relationship":
            return { type: "force-atlas2", kr: 110, kg: 10, nodeSize, preventOverlap: true };
        case "dagre":
            return { type: "dagre", rankdir: "TB", nodesep: 70, ranksep: 120, nodeSize };
        case "radial":
            return {
                type: "radial",
                unitRadius: Math.max(260, Math.ceil(metrics.maxWidth + metrics.maxHeight + 120)),
                linkDistance,
                maxIteration: 1000,
                nodeSize: nodeDiameter,
                nodeSpacing: 64,
                preventOverlap: true,
                preventOverlapPadding: 36,
                strictRadial: false
            };
        case "grid":
            return {
                type: "grid",
                width: metrics.graphWidth,
                height: metrics.graphHeight,
                nodeSize,
                nodeSpacing: 72,
                preventOverlap: true
            };
        case "concentric":
            return {
                type: "concentric",
                minNodeSpacing: Math.max(96, Math.ceil(metrics.maxWidth * .8)),
                nodeSize,
                preventOverlap: true
            };
        case "force":
            return { type: "force", preventOverlap: true, linkDistance, nodeSize, nodeStrength: -620 };
        case "circular":
            return {
                type: "circular",
                radius: Math.max(480, Math.ceil((metrics.nodeCount * (metrics.avgWidth + 34)) / (2 * Math.PI))),
                ordering: "topology",
                nodeSize,
                preventOverlap: true
            };
        case "mds":
            return {
                type: "mds",
                center: [metrics.bounds.width / 2, metrics.bounds.height / 2],
                linkDistance: Math.max(360, Math.ceil(metrics.avgWidth * 3.4)),
                maxIteration: 1000,
                nodeSize: createLayoutNodeDiameter(metrics, 96),
                nodeSpacing: 140,
                preventOverlap: true,
                preventOverlapPadding: 72
            };
        case "fruchterman":
            return {
                type: "fruchterman",
                width: metrics.graphWidth,
                height: metrics.graphHeight,
                gravity: 8,
                speed: 4,
                clustering: true,
                nodeSize
            };
        case "force-atlas2":
            return { type: "force-atlas2", kr: 140, kg: 8, nodeSize, preventOverlap: true };
        case "d3-force":
            return {
                type: "d3-force",
                link: { distance: linkDistance },
                collide: { radius: metrics.collisionRadius, strength: 0.86 },
                manyBody: { strength: -720 }
            };
        case "antv-dagre":
            return { type: "antv-dagre", rankdir: "TB", nodesep: 82, ranksep: 130, nodeSize, edgeLabelSpace: false };
        case "indented":
            return { type: "dagre", rankdir: "LR", nodesep: 66, ranksep: 124, nodeSize };
        default:
            return null;
    }
}

function createGraph(g6, element, graphData, mode, config) {
    const graphConfig = {
        container: element,
        autoFit: "center",
        data: graphData,
        animation: false,
        node: {
            style: {
                halo: true,
                haloStroke: cfgColor(config, "haloStroke", colorFallbacks.haloStroke),
                labelFontSize: 13,
                labelLineHeight: 17,
                labelPlacement: "center",
                labelTextAlign: "center",
                labelTextBaseline: "middle",
                labelWordWrap: true,
                lineWidth: 2
            }
        },
        edge: {
            style: {
                endArrow: true,
                labelFill: cfgColor(config, "labelDim", colorFallbacks.labelDim),
                labelFontSize: 9,
                lineWidth: 1.4,
                stroke: cfgColor(config, "edgeStroke", colorFallbacks.edgeStroke)
            }
        },
        behaviors: ["drag-canvas", "zoom-canvas", "drag-element"],
        autoResize: true,
        background: "transparent"
    };

    const g6Layout = manualLayoutModes.has(mode) ? null : createLayoutConfig(mode, graphData, element);
    if (g6Layout) {
        graphConfig.layout = g6Layout;
    }

    return new g6.Graph(graphConfig);
}

function syncRenderedGraphData(element, graphData) {
    try {
        const renderedData = element.prompterOneGraph?.getData?.();
        const nodes = read(renderedData, "nodes", "Nodes", []);
        if (Array.isArray(nodes) && nodes.length > 0) {
            const edges = read(renderedData, "edges", "Edges", graphData.edges);
            element.prompterOneGraphData = { nodes, edges };
            return;
        }
    }
    catch {
        // Keep the deterministic Blazor-authored graph data when G6 does not expose rendered data.
    }

    element.prompterOneGraphData = graphData;
}

async function fitGraphView(graph, mode, duration = 120) {
    await graph.fitView?.({ padding: fitViewPadding }, { duration });

    if (mode === "grid" || mode === "radial" || mode === "mds") {
        return;
    }

    const minimumZoom = readMinimumZoom(mode);
    const currentZoom = graph.getZoom?.() ?? 1;
    if (Number.isFinite(currentZoom) && currentZoom < minimumZoom) {
        await graph.zoomTo?.(minimumZoom, { duration: 0 });
    }
}

function syncGraphViewportSize(element) {
    const graph = element.prompterOneGraph;
    if (!graph) {
        return false;
    }

    const bounds = readLayoutBounds(element);
    graph.resize?.(bounds.width, bounds.height);
    element.dataset.graphViewportWidth = `${bounds.width}`;
    element.dataset.graphViewportHeight = `${bounds.height}`;
    return true;
}

async function syncAndFitGraphView(element, mode, duration = 120) {
    if (!syncGraphViewportSize(element)) {
        return;
    }

    await fitGraphView(element.prompterOneGraph, mode, duration);
}

function bindGraphResize(element) {
    element.prompterOneGraphResizeObserver?.disconnect();
    if (element.prompterOneGraphResizeFrame) {
        cancelAnimationFrame(element.prompterOneGraphResizeFrame);
        element.prompterOneGraphResizeFrame = 0;
    }

    if (!window.ResizeObserver) {
        return;
    }

    const resizeObserver = new ResizeObserver(() => {
        if (element.prompterOneGraphResizeFrame) {
            cancelAnimationFrame(element.prompterOneGraphResizeFrame);
        }

        element.prompterOneGraphResizeFrame = requestAnimationFrame(() => {
            element.prompterOneGraphResizeFrame = 0;
            const mode = element.dataset.graphLayout ?? defaultLayoutMode;
            syncAndFitGraphView(element, mode, 80).catch(error => {
                console.warn("PrompterOne graph resize failed", error);
            });
        });
    });
    resizeObserver.observe(element);
    element.prompterOneGraphResizeObserver = resizeObserver;
}

function readMinimumZoom(mode) {
    switch (mode) {
        case "story":
            return 0.36;
        case "compact":
        case "knowledge":
        case "structure":
        case "delivery":
            return 0.3;
        case "grid":
            return 0.46;
        case "mds":
            return 0.34;
        case "radial":
        case "circular":
            return 0.26;
        default:
            return 0.24;
    }
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
    const hiddenAttributeNames = new Set([
        "category", "line", "scope", "scopeLabel", "source",
        "wpm", "pace", "timing", "cue", "emotion", "archetype", "value", "valueType"
    ]);
    return Object.entries(attributes ?? {})
        .filter(([key]) => !hiddenAttributeNames.has(key))
        .filter(([, value]) => value !== null && value !== undefined && `${value}`.trim())
        .map(([key, value]) => `${key}: ${value}`)
        .slice(0, 8);
}

function buildNodeTooltip(node, config) {
    const attributes = formatAttributes(node.data.attributes);
    const label = cleanDisplayText(node.data.displayLabel || node.style?.labelText || node.data.label);
    const detail = cleanDisplayText(node.data.detail);
    const visibleDetail = detail && detail !== label ? detail : "";
    const sourceHint = node.data.hasSourceRange ? (config?.sourceHint ?? config?.SourceHint ?? "") : "";
    return [
        `<strong>${escapeHtml(label)}</strong>`,
        `<span>${escapeHtml(node.data.kind)}</span>`,
        visibleDetail ? `<p>${escapeHtml(visibleDetail)}</p>` : "",
        attributes.length ? `<small>${attributes.map(escapeHtml).join("<br>")}</small>` : "",
        sourceHint ? `<em>${escapeHtml(sourceHint)}</em>` : ""
    ].filter(Boolean).join("");
}

function buildEdgeTooltip(edge, graphData) {
    const sourceNode = graphData.nodes.find(node => node.id === edge.source);
    const targetNode = graphData.nodes.find(node => node.id === edge.target);
    const source = cleanDisplayText(sourceNode?.style?.labelText ?? sourceNode?.data.label ?? "Source");
    const target = cleanDisplayText(targetNode?.style?.labelText ?? targetNode?.data.label ?? "Target");
    return [
        `<strong>${escapeHtml(edge.data.label || "related")}</strong>`,
        `<p>${escapeHtml(source)}</p>`,
        `<p>${escapeHtml(target)}</p>`
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
    const point = readClientPoint(element, event, bounds);
    const tooltipWidth = tooltip.offsetWidth || 280;
    const tooltipHeight = tooltip.offsetHeight || 120;
    const localX = clampNumber(point.x - bounds.left, 0, bounds.width);
    const localY = clampNumber(point.y - bounds.top, 0, bounds.height);
    const offset = 18;
    const opensLeft = localX > bounds.width * 0.58;
    const opensAbove = localY > bounds.height * 0.62;
    const preferredX = opensLeft ? localX - tooltipWidth - offset : localX + offset;
    const preferredY = opensAbove ? localY - tooltipHeight - offset : localY + offset;
    const nextX = Math.min(Math.max(preferredX, 12), Math.max(12, bounds.width - tooltipWidth - 12));
    const nextY = Math.min(Math.max(preferredY, 12), Math.max(12, bounds.height - tooltipHeight - 12));
    tooltip.dataset.anchorX = `${Math.round(localX)}`;
    tooltip.dataset.anchorY = `${Math.round(localY)}`;
    tooltip.style.transform = `translate(${Math.round(nextX)}px, ${Math.round(nextY)}px)`;
}

function readClientPoint(element, event, bounds) {
    const nativeEvent = event?.nativeEvent ?? event?.originalEvent ?? event?.event;
    const nativePoint = readFinitePoint(nativeEvent?.clientX, nativeEvent?.clientY) ??
        readFinitePoint(event?.clientX, event?.clientY);
    if (nativePoint) {
        return nativePoint;
    }

    const storedPointer = readFinitePoint(
        element.prompterOneGraphPointer?.clientX,
        element.prompterOneGraphPointer?.clientY);
    if (storedPointer) {
        return storedPointer;
    }

    const graphPoint = readFinitePoint(event?.client?.x, event?.client?.y) ??
        readFinitePoint(event?.canvas?.x, event?.canvas?.y) ??
        readFinitePoint(event?.canvasPoint?.x, event?.canvasPoint?.y) ??
        readFinitePoint(event?.point?.x, event?.point?.y) ??
        readFinitePoint(event?.x, event?.y);
    if (graphPoint) {
        if (graphPoint.x >= bounds.left &&
            graphPoint.x <= bounds.right &&
            graphPoint.y >= bounds.top &&
            graphPoint.y <= bounds.bottom) {
            return graphPoint;
        }

        return {
            x: bounds.left + clampNumber(graphPoint.x, 0, bounds.width),
            y: bounds.top + clampNumber(graphPoint.y, 0, bounds.height)
        };
    }

    return { x: bounds.left + bounds.width / 2, y: bounds.top + bounds.height / 2 };
}

function readFinitePoint(x, y) {
    const pointX = Number(x);
    const pointY = Number(y);
    return Number.isFinite(pointX) && Number.isFinite(pointY)
        ? { x: pointX, y: pointY }
        : null;
}

function clampNumber(value, minimum, maximum) {
    return Math.min(Math.max(value, minimum), maximum);
}

function storeGraphPointer(element, event) {
    const point = readFinitePoint(event.clientX, event.clientY);
    if (!point) {
        return;
    }

    const bounds = element.getBoundingClientRect();
    element.prompterOneGraphPointer = {
        clientX: point.x,
        clientY: point.y,
        localX: clampNumber(point.x - bounds.left, 0, bounds.width),
        localY: clampNumber(point.y - bounds.top, 0, bounds.height)
    };
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
    element.prompterOneGraphTooltipAbort?.abort();
    const abort = new AbortController();
    element.prompterOneGraphTooltipAbort = abort;
    element.addEventListener("pointermove", event => storeGraphPointer(element, event), { signal: abort.signal });
    element.addEventListener("pointerenter", event => storeGraphPointer(element, event), { signal: abort.signal });

    const tooltip = createTooltip(element);
    const nodeById = new Map(graphData.nodes.map(node => [node.id, node]));
    const edgeById = new Map(graphData.edges.map(edge => [edge.id, edge]));

    graph.on?.("node:pointerenter", event => {
        const node = nodeById.get(readEventElementId(event));
        if (!node) {
            return;
        }

        tooltip.innerHTML = buildNodeTooltip(node, element.prompterOneGraphConfig);
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

async function requestGraphSourceRange(element, config, nodeId) {
    const callbackReference = config?.callbackReference ?? config?.CallbackReference;
    if (!nodeId || !callbackReference?.invokeMethodAsync) {
        return;
    }

    element.dataset.graphSelectedNode = nodeId;
    await callbackReference.invokeMethodAsync("OnGraphNodeRequestedAsync", nodeId);
}

function bindGraphNavigation(graph, element, graphData, config) {
    element.prompterOneGraphNavigationAbort?.abort();
    const abort = new AbortController();
    element.prompterOneGraphNavigationAbort = abort;
    const navigableNodeIds = new Set(graphData.nodes
        .filter(node => node.data.hasSourceRange)
        .map(node => node.id));
    const requestNode = async nodeId => {
        if (navigableNodeIds.has(nodeId)) {
            await requestGraphSourceRange(element, config, nodeId);
        }
    };

    graph.on?.("node:click", event => requestNode(readEventElementId(event)));
    element.addEventListener("prompterone:graph-node-request", event => {
        requestNode(event.detail?.nodeId ?? "");
    }, { signal: abort.signal });
    element.dataset.graphNavigation = "true";
}

function findControls(element) {
    return element.closest(".script-graph-panel")?.querySelectorAll("[data-graph-control]") ?? [];
}

async function updateGraphLayout(element, mode) {
    const artifact = element.prompterOneGraphArtifact;
    const config = element.prompterOneGraphConfig;
    if (!artifact) {
        return;
    }

    await renderGraphInstance(element, artifact, config, mode, 120);
}

async function autoLayoutGraph(element) {
    const mode = element.dataset.graphLayout ?? element.prompterOneGraphConfig?.layoutMode ?? defaultLayoutMode;
    await updateGraphLayout(element, mode);
    const runCount = Number.parseInt(element.dataset.graphAutoLayoutRuns || "0", 10) || 0;
    element.dataset.graphAutoLayoutRuns = `${runCount + 1}`;
}

async function renderGraphInstance(element, artifact, config, mode, fitDuration = 0) {
    const g6 = await ensureG6();
    config ??= {};
    config.colorHost = element;
    const graphData = createGraphData(artifact, mode, config, element);
    destroyExistingGraph(element);
    delete element.dataset.graphReady;
    delete element.dataset.graphError;
    delete element.dataset.graphTooltips;
    delete element.dataset.graphNavigation;
    delete element.dataset.graphSelectedNode;

    element.dataset.graphState = "rendering";
    element.prompterOneGraphData = graphData;
    element.prompterOneGraphArtifact = artifact;
    element.prompterOneGraphConfig = config;
    element.dataset.graphLayout = mode;
    element.dataset.graphNodeStyle = cfgNodeStyleMode(config);
    element.prompterOneGraph = createGraph(g6, element, graphData, mode, config);
    bindControls(element);
    bindSpacebarGrab(element);
    await element.prompterOneGraph.render();
    syncRenderedGraphData(element, graphData);
    bindGraphTooltips(element.prompterOneGraph, element, graphData);
    bindGraphNavigation(element.prompterOneGraph, element, graphData, config);
    bindGraphResize(element);
    await syncAndFitGraphView(element, mode, fitDuration);
    element.dataset.graphReady = "true";
    element.dataset.graphState = "ready";
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
                await syncAndFitGraphView(element, element.dataset.graphLayout ?? defaultLayoutMode, 120);
            }
            else if (action === "auto-layout") {
                await autoLayoutGraph(element);
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

function bindSpacebarGrab(element) {
    element.prompterOneGraphSpaceGrabAbort?.abort();
    const ac = new AbortController();
    element.prompterOneGraphSpaceGrabAbort = ac;
    const opt = { signal: ac.signal };
    let isPointerInside = false;
    let isSpaceDown = false;
    let isPointerDragging = false;
    const canvas = () => element.querySelector("canvas");
    const setCanvasCursor = value => {
        const graphCanvas = canvas();
        if (graphCanvas) {
            graphCanvas.style.setProperty("cursor", value, "important");
        }
    };
    const clearCanvasCursor = () => {
        const graphCanvas = canvas();
        if (graphCanvas) {
            graphCanvas.style.removeProperty("cursor");
        }
    };

    const canGrab = () => isPointerInside || element.contains(document.activeElement ?? document.body);
    const syncGrab = () => {
        if (isSpaceDown && canGrab()) {
            element.dataset.graphGrab = "true";
            element.dataset.graphPanning = isPointerDragging ? "true" : "false";
            setCanvasCursor(isPointerDragging ? "grabbing" : "grab");
        }
        else {
            delete element.dataset.graphGrab;
            delete element.dataset.graphPanning;
            clearCanvasCursor();
        }
    };

    element.addEventListener("pointerenter", () => {
        isPointerInside = true;
        syncGrab();
    }, opt);

    element.addEventListener("pointerleave", () => {
        isPointerInside = false;
        syncGrab();
    }, opt);

    element.addEventListener("pointerdown", () => {
        element.focus?.({ preventScroll: true });
        isPointerDragging = isSpaceDown;
        syncGrab();
    }, opt);

    document.addEventListener("pointerup", () => {
        isPointerDragging = false;
        syncGrab();
    }, opt);

    element.addEventListener("focusin", syncGrab, opt);
    element.addEventListener("focusout", syncGrab, opt);

    document.addEventListener("keydown", e => {
        if (e.code === "Space" && canGrab()) {
            e.preventDefault();
            isSpaceDown = true;
            syncGrab();
        }
    }, opt);

    document.addEventListener("keyup", e => {
        if (e.code === "Space") {
            isSpaceDown = false;
            isPointerDragging = false;
            syncGrab();
        }
    }, opt);
}

function destroyExistingGraph(element) {
    element.prompterOneGraphControlsAbort?.abort();
    element.prompterOneGraphNavigationAbort?.abort();
    element.prompterOneGraphTooltipAbort?.abort();
    element.prompterOneGraphSpaceGrabAbort?.abort();
    element.prompterOneGraphResizeObserver?.disconnect();
    element.prompterOneGraphResizeObserver = null;
    if (element.prompterOneGraphResizeFrame) {
        cancelAnimationFrame(element.prompterOneGraphResizeFrame);
        element.prompterOneGraphResizeFrame = 0;
    }
    element.prompterOneGraphTooltip?.remove();
    element.prompterOneGraphTooltip = null;

    if (element.prompterOneGraph) {
        element.prompterOneGraph.destroy();
        element.prompterOneGraph = null;
    }
}

export async function render(element, artifact, config) {
    if (!element || !artifact) {
        return;
    }

    try {
        element.dataset.graphState = "processing";
        const mode = config?.layoutMode ?? element.dataset.graphLayout ?? defaultLayoutMode;
        await renderGraphInstance(element, artifact, config, mode, 0);
    }
    catch (error) {
        element.dataset.graphError = error?.message ?? "Graph render failed.";
        element.dataset.graphState = "error";
        console.error("PrompterOne graph render failed", error?.stack ?? error);
        throw error;
    }
}

export function measureElement(element) {
    if (!element) {
        return null;
    }

    const rect = element.getBoundingClientRect();
    return {
        left: rect.left,
        top: rect.top,
        width: rect.width,
        height: rect.height
    };
}
