const emptyValue = "";
const titleHeaderMarker = "#";
const segmentHeaderMarker = "##";
const blockHeaderMarker = "###";
const headerTimingPattern = /^\d+:\d{2}(?:-\d+:\d{2})?$/;
const archetypePrefix = "Archetype:";
const speakerPrefix = "Speaker:";
const numericWpmPattern = /^\d+\s*WPM$/i;
const inlineTagPattern = /\[[^[\]]+\]/g;
const escapedSequencePattern = /\\(?:[\\/\[\]\|\*])/;
const markdownBoldPattern = /\*\*([^*\n]+)\*\*/g;
const markdownItalicPattern = /\*(?!\*)([^*\n]+)\*(?!\*)/g;
const defaultRuntimeCatalog = Object.freeze({
    archetypeDescriptors: [],
    archetypes: [],
    articulationStyles: [],
    deliveryModes: [],
    editPointPriorities: [],
    emotions: [],
    relativeSpeedTags: [],
    volumeLevels: []
});
const speedDocumentationByName = Object.freeze({
    xslow: "Slow the wrapped text down significantly.",
    slow: "Open the wrapped text up a little and slow it down.",
    normal: "Restore the inherited or default pace inside a nested speed scope.",
    fast: "Tighten the wrapped text and deliver it faster.",
    xfast: "Push the wrapped text to the fastest delivery preset."
});

export function createTpsLanguageSpec(runtimeCatalog) {
    const catalog = normalizeCatalog(runtimeCatalog);
    const archetypeNames = catalog.archetypes;
    const emotionTagNames = catalog.emotions;
    const volumeTagNames = catalog.volumeLevels;
    const deliveryTagNames = catalog.deliveryModes;
    const articulationTagNames = catalog.articulationStyles;
    const speedTagNames = catalog.relativeSpeedTags;
    const editPointPriorityNames = catalog.editPointPriorities;
    const archetypeDescriptorByName = new Map(
        catalog.archetypeDescriptors.map(descriptor => [descriptor.name.toLowerCase(), descriptor]));

    const simpleWrapperSpecs = [
        createWrapperCompletion("emphasis", "[emphasis]${1:text}[/emphasis]", "Emphasis wrapper", "Wrap words that should land harder."),
        createWrapperCompletion("highlight", "[highlight]${1:text}[/highlight]", "Highlight wrapper", "Mark words that should read as visually highlighted."),
        createWrapperCompletion("stress", "[stress]${1:me}[/stress]", "Stress wrapper", "Wrap the stressed syllable or letters inside a word."),
        ...speedTagNames.map(name => createWrapperCompletion(name, "[" + name + "]${1:text}[/" + name + "]", "Speed wrapper", speedDocumentationByName[name] ?? "Adjust the delivery pace for the wrapped text.")),
        ...volumeTagNames.map(name => createWrapperCompletion(name, "[" + name + "]${1:text}[/" + name + "]", "Volume wrapper", `Wrap text that should be delivered as ${name}.`)),
        ...deliveryTagNames.map(name => createWrapperCompletion(name, "[" + name + "]${1:text}[/" + name + "]", "Delivery wrapper", `Wrap text that should be delivered as ${name}.`)),
        ...articulationTagNames.map(name => createWrapperCompletion(name, "[" + name + "]${1:text}[/" + name + "]", "Articulation wrapper", `Apply a ${name} articulation contour to the wrapped text.`)),
        ...emotionTagNames.map(name => createWrapperCompletion(name, "[" + name + "]${1:text}[/" + name + "]", "Emotion wrapper", `Apply the ${name} delivery color and feel to the wrapped text.`))
    ];

    const markdownWrapperSpecs = [
        createCompletionSpec("**text**", "**${1:text}**", "Markdown bold", "Strong emphasis level 2 using markdown bold syntax.", "tag"),
        createCompletionSpec("*text*", "*${1:text}*", "Markdown italic", "Emphasis level 1 using markdown italic syntax.", "tag")
    ];

    const parameterizedWrapperSpecs = [
        createParameterizedCompletion("phonetic", "[phonetic:${1:ˈkæməl}]${2:camel}[/phonetic]", "IPA pronunciation guide", "Attach an IPA pronunciation guide to the wrapped word."),
        createParameterizedCompletion("pronunciation", "[pronunciation:${1:KAM-uhl}]${2:camel}[/pronunciation]", "Simple pronunciation guide", "Attach a readable pronunciation guide to the wrapped word."),
        createParameterizedCompletion("stress", "[stress:${1:de-VE-lop-ment}]${2:development}[/stress]", "Syllable guide", "Attach a full syllable guide; renderers should show it as a tooltip or overlay."),
        createParameterizedCompletion("energy", "[energy:${1:8}]${2:text}[/energy]", "Energy contour", "Attach an explicit delivery energy level from 1 to 10 to the wrapped phrase.", "[energy:8]text[/energy]"),
        createParameterizedCompletion("melody", "[melody:${1:4}]${2:text}[/melody]", "Melody contour", "Attach an explicit melody and pitch variation level from 1 to 10 to the wrapped phrase.", "[melody:4]text[/melody]")
    ];

    const standaloneCompletionSpecs = [
        createCompletionSpec("[pause:2s]", "[pause:${1:2s}]", "Timed pause", "Pause for an explicit number of seconds or milliseconds.", "pause"),
        createCompletionSpec("[pause:1000ms]", "[pause:${1:1000ms}]", "Timed pause (ms)", "Pause for an explicit number of milliseconds.", "pause"),
        createCompletionSpec("[breath]", "[breath]", "Breath mark", "Mark a natural breath point without adding time.", "tag"),
        createCompletionSpec("[edit_point]", "[edit_point]", "Edit point", "Mark a standard edit point.", "tag"),
        ...editPointPriorityNames.map(priority => createCompletionSpec(
            `[edit_point:${priority}]`,
            `[edit_point:${priority}]`,
            "Priority edit point",
            `Mark an edit point with ${priority} priority.`,
            "tag")),
        createCompletionSpec("[140WPM]", "[${1:140}WPM]", "Inline WPM override", "Override speaking pace inline for the wrapped scope.", "tag")
    ];

    const headerCompletionSpecs = [
        createCompletionSpec("# Title", "# ${1:Document Title}", "Document title", "Top-level TPS document title.", "header"),
        createCompletionSpec("## [Segment Name|Speaker:Host|140WPM|neutral|0:00-0:30]", "## [${1:Segment Name}|Speaker:${2:Host}|${3:140}WPM|${4:neutral}|${5:0:00-0:30}]", "Segment header", "Create a structured TPS segment header.", "header"),
        ...archetypeNames.map(name => createArchetypeHeaderCompletion(segmentHeaderMarker, name, archetypeDescriptorByName.get(name.toLowerCase()), "neutral")),
        createCompletionSpec("### [Block Name|Speaker:Host|140WPM|focused]", "### [${1:Block Name}|Speaker:${2:Host}|${3:140}WPM|${4:focused}]", "Block header", "Create a structured TPS block header.", "header"),
        ...archetypeNames.map(name => createArchetypeHeaderCompletion(blockHeaderMarker, name, archetypeDescriptorByName.get(name.toLowerCase()), "focused")),
        createCompletionSpec("## Segment Title", "## ${1:Segment Title}", "Simple segment header", "Plain markdown segment header with inherited metadata.", "header"),
        createCompletionSpec("### Block Title", "### ${1:Block Title}", "Simple block header", "Plain markdown block header with inherited metadata.", "header")
    ];

    const pauseCompletionSpecs = [
        createCompletionSpec("/", " / ", "Short pause", "Insert a short 300ms pause.", "pause"),
        createCompletionSpec("//", " //", "Medium pause", "Insert a medium 600ms pause.", "pause")
    ];

    return Object.freeze({
        archetypeDescriptorByName,
        archetypeNames,
        archetypePrefix,
        articulationTagNames,
        blockHeaderMarker,
        deliveryTagNames,
        editPointPriorityNames,
        emotionTagNames,
        emptyValue,
        escapedSequencePattern,
        headerCompletionSpecs,
        headerDocumentationByMarker: Object.freeze({
            [titleHeaderMarker]: "Top-level TPS title. Use it for the document name above the front matter and body.",
            [segmentHeaderMarker]: "TPS segment header. Segments break a large script into major sections and can carry WPM, emotion, timing, speaker, and archetype metadata.",
            [blockHeaderMarker]: "TPS block header. Blocks live inside a segment and carry narrower structure and metadata."
        }),
        headerTimingPattern,
        inlineTagPattern,
        markdownBoldPattern,
        markdownItalicPattern,
        markdownWrapperSpecs,
        numericWpmPattern,
        parameterizedWrapperLookup: new Map(parameterizedWrapperSpecs.map(spec => [spec.name, spec])),
        parameterizedWrapperSpecs,
        pauseCompletionSpecs,
        plainWrapperLookup: new Map(simpleWrapperSpecs.map(spec => [spec.name, spec])),
        segmentHeaderMarker,
        simpleWrapperSpecs,
        speakerPrefix,
        speedTagNames,
        standaloneCompletionSpecs,
        titleHeaderMarker
    });
}

function normalizeCatalog(runtimeCatalog) {
    const catalog = runtimeCatalog ?? defaultRuntimeCatalog;
    return {
        archetypeDescriptors: normalizeArchetypeDescriptors(catalog.archetypeDescriptors),
        archetypes: normalizeStringList(catalog.archetypes),
        articulationStyles: normalizeStringList(catalog.articulationStyles),
        deliveryModes: normalizeStringList(catalog.deliveryModes),
        editPointPriorities: normalizeStringList(catalog.editPointPriorities),
        emotions: normalizeStringList(catalog.emotions),
        relativeSpeedTags: normalizeStringList(catalog.relativeSpeedTags),
        volumeLevels: normalizeStringList(catalog.volumeLevels)
    };
}

function normalizeStringList(values) {
    return Array.isArray(values)
        ? values
            .map(value => String(value ?? emptyValue).trim())
            .filter(Boolean)
        : [];
}

function normalizeArchetypeDescriptors(values) {
    return Array.isArray(values)
        ? values
            .filter(candidate => candidate && typeof candidate === "object")
            .map(candidate => ({
                articulation: String(candidate.articulation ?? emptyValue).trim(),
                energyMax: Number(candidate.energyMax ?? 0),
                energyMin: Number(candidate.energyMin ?? 0),
                label: String(candidate.label ?? emptyValue).trim(),
                melodyMax: Number(candidate.melodyMax ?? 0),
                melodyMin: Number(candidate.melodyMin ?? 0),
                name: String(candidate.name ?? emptyValue).trim(),
                recommendedWpm: Number(candidate.recommendedWpm ?? 0),
                speedMax: Number(candidate.speedMax ?? 0),
                speedMin: Number(candidate.speedMin ?? 0),
                volume: String(candidate.volume ?? emptyValue).trim()
            }))
            .filter(candidate => candidate.name)
        : [];
}

function createArchetypeHeaderCompletion(marker, name, descriptor, emotionName) {
    const displayName = descriptor?.label || toDisplayLabel(name);
    const detail = marker === segmentHeaderMarker
        ? "Segment header (archetype)"
        : "Block header (archetype)";
    const documentation = createArchetypeHeaderDocumentation(marker, displayName, descriptor);
    if (marker === segmentHeaderMarker) {
        return createCompletionSpec(
            `## [Segment Name|Speaker:Host|Archetype:${displayName}|${emotionName}|0:00-0:30]`,
            `## [\${1:Segment Name}|Speaker:\${2:Host}|Archetype:\${3:${displayName}}|\${4:${emotionName}}|\${5:0:00-0:30}]`,
            detail,
            documentation,
            "header",
            name);
    }

    return createCompletionSpec(
        `### [Block Name|Speaker:Host|Archetype:${displayName}|${emotionName}]`,
        `### [\${1:Block Name}|Speaker:\${2:Host}|Archetype:\${3:${displayName}}|\${4:${emotionName}}]`,
        detail,
        documentation,
        "header",
        name);
}

function createArchetypeHeaderDocumentation(marker, displayName, descriptor) {
    const scopeName = marker === segmentHeaderMarker ? "segment" : "block";
    const profileSummary = descriptor
        ? ` Recommended WPM ${descriptor.recommendedWpm}; energy ${descriptor.energyMin}-${descriptor.energyMax}; melody ${descriptor.melodyMin}-${descriptor.melodyMax}; speed ${descriptor.speedMin}-${descriptor.speedMax} WPM; articulation ${normalizeDescriptorValue(descriptor.articulation)}.`
        : emptyValue;
    return `Create a structured TPS ${scopeName} header that inherits recommended pacing and phrasing from the ${displayName} archetype.${profileSummary}`;
}

function normalizeDescriptorValue(value) {
    return String(value ?? emptyValue).trim() || "default";
}

function toDisplayLabel(value) {
    const normalized = String(value ?? emptyValue).trim();
    return normalized
        ? normalized.charAt(0).toUpperCase() + normalized.slice(1)
        : emptyValue;
}

function createWrapperCompletion(name, insertText, detail, documentation) {
    return createCompletionSpec(`[${name}]text[/${name}]`, insertText, detail, documentation, "tag", name);
}

function createParameterizedCompletion(name, insertText, detail, documentation, label) {
    return createCompletionSpec(label ?? `[${name}:guide]text[/${name}]`, insertText, detail, documentation, "tag", name);
}

function createCompletionSpec(label, insertText, detail, documentation, group, name) {
    return { detail, documentation, group, insertText, label, name: name ?? emptyValue };
}
