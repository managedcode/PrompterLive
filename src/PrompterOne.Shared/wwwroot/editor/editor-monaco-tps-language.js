import { createTpsLanguageSpec } from "./editor-monaco-tps-language-spec.js";

const supportByLanguageId = new Map();

export function ensureTpsLanguage(monaco, options) {
    if (supportByLanguageId.has(options.languageId)) {
        return;
    }

    const spec = createTpsLanguageSpec(options?.tpsCatalog);
    if (!monaco.languages.getLanguages().some(language => language.id === options.languageId)) {
        monaco.languages.register({ id: options.languageId });
    }

    monaco.languages.setLanguageConfiguration(options.languageId, {
        autoClosingPairs: [{ open: "[", close: "]" }],
        brackets: [["[", "]"]],
        surroundingPairs: [{ open: "[", close: "]" }]
    });
    monaco.languages.setMonarchTokensProvider(options.languageId, createTokenizer(spec));

    const completionProvider = createCompletionProvider(monaco, spec);
    const hoverProvider = createHoverProvider(monaco, spec);
    monaco.languages.registerCompletionItemProvider(options.languageId, completionProvider);
    monaco.languages.registerHoverProvider(options.languageId, hoverProvider);
    supportByLanguageId.set(options.languageId, { completionProvider, hoverProvider });
}

export function getTpsLanguageSupport(languageId) {
    return supportByLanguageId.get(languageId) ?? null;
}

function createTokenizer(spec) {
    const simpleWrapperPattern = createAlternationPattern(spec.simpleWrapperSpecs.map(candidate => candidate.name));
    const parameterizedPattern = createAlternationPattern(spec.parameterizedWrapperSpecs.map(candidate => candidate.name));
    const closingPattern = createAlternationPattern([...spec.simpleWrapperSpecs, ...spec.parameterizedWrapperSpecs].map(candidate => candidate.name));
    return {
        tokenizer: {
            root: [
                [/^---$/, "frontmatter.delimiter"],
                [/^([A-Za-z0-9_]+)(:)(\s*)(.+)$/, ["frontmatter.key", "delimiter", "white", "frontmatter.value"]],
                [/^(###)(\s+)(\[.*\]|.+)$/, ["header.block.hash", "white", "header.block.body"]],
                [/^(##)(\s+)(\[.*\]|.+)$/, ["header.segment.hash", "white", "header.segment.body"]],
                [/^(#)(\s+)(.+)$/, ["header.title.hash", "white", "header.title.body"]],
                [spec.escapedSequencePattern, "escape.sequence"],
                [spec.markdownBoldPattern, "markdown.bold"],
                [spec.markdownItalicPattern, "markdown.italic"],
                [/\[pause:[^\]]+\]/i, "pause.timed"],
                [/\[(?:edit_point|editpoint)(?::[^\]]+)?\]/i, "cue.editpoint"],
                [/\[breath\]/i, "cue.breath"],
                [/\[energy:[^\]]+\]/i, "cue.energy"],
                [/\[melody:[^\]]+\]/i, "cue.melody"],
                [new RegExp(`\\[(?:${parameterizedPattern}):[^\\]]+\\]`, "i"), "cue.pronunciation"],
                [new RegExp(`\\[\\/(?:${closingPattern})(?::[^\\]]+)?\\]`, "i"), "cue.close"],
                [new RegExp(`\\[(?:${simpleWrapperPattern})\\]`, "i"), "cue.open"],
                [/\[\d+\s*WPM\]/i, "wpm.badge"],
                [/(?<!\\)(?<!\S)\/\/(?=\s|$)/, "pause.long"],
                [/(?<!\\)(?<!\S)\/(?=\s|$)/, "pause.short"],
                [/\[[^\]]+\]/, "meta.tag"]
            ]
        }
    };
}

function createCompletionProvider(monaco, spec) {
    return {
        triggerCharacters: ["#", "[", "/"],
        provideCompletionItems(model, position) {
            const range = resolveCompletionRange(monaco, model, position);
            return {
                suggestions: resolveCompletionSpecs(model, position, spec).map(candidate => ({
                    detail: candidate.detail,
                    documentation: candidate.documentation,
                    insertText: candidate.insertText,
                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                    kind: resolveCompletionKind(monaco, candidate.group),
                    label: candidate.label,
                    range
                }))
            };
        }
    };
}

function createHoverProvider(monaco, spec) {
    return {
        provideHover(model, position) {
            const line = model.getLineContent(position.lineNumber);
            return findWrappedGuideHover(monaco, line, position, spec) ??
                findMarkdownHover(monaco, line, position, spec) ??
                findTagHover(monaco, line, position, spec) ??
                findPauseHover(monaco, line, position) ??
                findHeaderHover(monaco, line, position, spec);
        }
    };
}

function resolveCompletionRange(monaco, model, position) {
    const line = model.getLineContent(position.lineNumber);
    const prefix = line.slice(0, position.column - 1);
    if (/^\s*#{1,3}$/.test(prefix)) {
        const markerStart = prefix.indexOf("#") + 1;
        return new monaco.Range(position.lineNumber, markerStart, position.lineNumber, position.column);
    }

    const lastOpenBracket = prefix.lastIndexOf("[");
    if (lastOpenBracket > prefix.lastIndexOf("]")) {
        return new monaco.Range(position.lineNumber, lastOpenBracket + 1, position.lineNumber, position.column);
    }

    const pauseMatch = /\/{1,2}$/.exec(prefix);
    if (pauseMatch) {
        return new monaco.Range(position.lineNumber, position.column - pauseMatch[0].length, position.lineNumber, position.column);
    }

    const word = model.getWordUntilPosition(position);
    return new monaco.Range(position.lineNumber, word.startColumn, position.lineNumber, word.endColumn);
}

function resolveCompletionSpecs(model, position, spec) {
    const prefix = model.getLineContent(position.lineNumber).slice(0, position.column - 1);
    const insideTag = prefix.lastIndexOf("[") > prefix.lastIndexOf("]");
    if (/^\s*#{1,3}$/.test(prefix)) {
        return [...spec.headerCompletionSpecs, ...spec.standaloneCompletionSpecs, ...spec.markdownWrapperSpecs, ...spec.simpleWrapperSpecs, ...spec.parameterizedWrapperSpecs];
    }

    if (prefix.endsWith("//") || prefix.endsWith("/")) {
        return [...spec.pauseCompletionSpecs, ...spec.standaloneCompletionSpecs, ...spec.markdownWrapperSpecs, ...spec.simpleWrapperSpecs];
    }

    if (insideTag) {
        return [...spec.standaloneCompletionSpecs, ...spec.markdownWrapperSpecs, ...spec.simpleWrapperSpecs, ...spec.parameterizedWrapperSpecs, ...spec.headerCompletionSpecs, ...spec.pauseCompletionSpecs];
    }

    return [...spec.headerCompletionSpecs, ...spec.standaloneCompletionSpecs, ...spec.markdownWrapperSpecs, ...spec.simpleWrapperSpecs, ...spec.parameterizedWrapperSpecs, ...spec.pauseCompletionSpecs];
}

function resolveCompletionKind(monaco, group) {
    if (group === "header") {
        return monaco.languages.CompletionItemKind.Snippet;
    }

    if (group === "pause") {
        return monaco.languages.CompletionItemKind.Operator;
    }

    return monaco.languages.CompletionItemKind.Keyword;
}

function findWrappedGuideHover(monaco, line, position, spec) {
    const wrappedGuidePattern = /\[(phonetic|pronunciation|stress|energy|melody):([^\]]+)\]([^[]+?)\[\/\1(?::[^\]]+)?\]/ig;
    for (const match of line.matchAll(wrappedGuidePattern)) {
        const openTag = match[0].slice(0, match[0].indexOf(match[3], 0));
        const startColumn = (match.index ?? 0) + openTag.length + 1;
        const endColumn = startColumn + match[3].length;
        if (position.column < startColumn || position.column > endColumn) {
            continue;
        }

        const normalizedName = match[1].toLowerCase();
        const candidate = spec.parameterizedWrapperLookup.get(normalizedName);
        return createHover(
            monaco,
            position.lineNumber,
            startColumn,
            endColumn,
            resolveParameterizedTitle(normalizedName),
            `${candidate?.documentation ?? spec.emptyValue}\n\nGuide: \`${match[2]}\``.trim());
    }

    return null;
}

function findMarkdownHover(monaco, line, position, spec) {
    for (const match of line.matchAll(spec.markdownBoldPattern)) {
        const startColumn = (match.index ?? 0) + 1;
        const endColumn = startColumn + match[0].length;
        if (position.column < startColumn || position.column > endColumn) {
            continue;
        }

        return createHover(
            monaco,
            position.lineNumber,
            startColumn,
            endColumn,
            "Markdown bold",
            "Strong emphasis level 2 using markdown bold syntax such as `**text**`.");
    }

    for (const match of line.matchAll(spec.markdownItalicPattern)) {
        const startColumn = (match.index ?? 0) + 1;
        const endColumn = startColumn + match[0].length;
        if (position.column < startColumn || position.column > endColumn) {
            continue;
        }

        return createHover(
            monaco,
            position.lineNumber,
            startColumn,
            endColumn,
            "Markdown italic",
            "Emphasis level 1 using markdown italic syntax such as `*text*`.");
    }

    return null;
}

function findTagHover(monaco, line, position, spec) {
    for (const match of line.matchAll(spec.inlineTagPattern)) {
        const startColumn = (match.index ?? 0) + 1;
        const endColumn = startColumn + match[0].length;
        if (position.column < startColumn || position.column > endColumn) {
            continue;
        }

        const tagHelp = resolveTagHelp(match[0], spec);
        return tagHelp
            ? createHover(monaco, position.lineNumber, startColumn, endColumn, tagHelp.title, tagHelp.description)
            : null;
    }

    return null;
}

function findPauseHover(monaco, line, position) {
    for (let index = 0; index < line.length; index += 1) {
        const pauseLength = getPauseLength(line, index);
        if (pauseLength === 0) {
            continue;
        }

        const startColumn = index + 1;
        const endColumn = startColumn + pauseLength;
        if (position.column >= startColumn && position.column <= endColumn) {
            return createHover(
                monaco,
                position.lineNumber,
                startColumn,
                endColumn,
                pauseLength === 2 ? "Medium pause" : "Short pause",
                pauseLength === 2
                    ? "Insert a 600ms beat pause between phrases."
                    : "Insert a 300ms short pause between clauses.");
        }

        index += pauseLength - 1;
    }

    return null;
}

function findHeaderHover(monaco, line, position, spec) {
    const headerMatch = /^(#{1,3})(\s+)(.+)$/.exec(line);
    if (!headerMatch) {
        return null;
    }

    const marker = headerMatch[1];
    const bodyStartColumn = marker.length + headerMatch[2].length + 1;
    if (position.column <= marker.length) {
        return createHover(monaco, position.lineNumber, 1, marker.length + 1, resolveHeaderTitle(marker, spec), spec.headerDocumentationByMarker[marker]);
    }

    const body = headerMatch[3];
    if (body.startsWith("[") && body.endsWith("]")) {
        const contentStartColumn = bodyStartColumn + 1;
        let partStartColumn = contentStartColumn;
        for (const [index, part] of body.slice(1, -1).split("|").entries()) {
            const partEndColumn = partStartColumn + part.length;
            if (position.column >= partStartColumn && position.column <= partEndColumn) {
                const value = part.trim();
                return createHover(monaco, position.lineNumber, partStartColumn, partEndColumn, resolveHeaderPartTitle(marker, index, value, spec), resolveHeaderPartDescription(marker, index, value, spec));
            }

            partStartColumn = partEndColumn + 1;
        }
    }

    return createHover(monaco, position.lineNumber, bodyStartColumn, line.length + 1, resolveHeaderTitle(marker, spec), spec.headerDocumentationByMarker[marker]);
}

function resolveTagHelp(rawTag, spec) {
    const inner = rawTag.slice(1, -1).trim();
    const isClosing = inner.startsWith("/");
    const content = isClosing ? inner.slice(1).trim() : inner;
    const separatorIndex = content.indexOf(":");
    const name = (separatorIndex >= 0 ? content.slice(0, separatorIndex) : content).trim().toLowerCase();
    const argument = separatorIndex >= 0 ? content.slice(separatorIndex + 1).trim() : spec.emptyValue;
    if (spec.numericWpmPattern.test(content)) {
        return { title: "Inline WPM override", description: "Override speaking pace inline with a numeric WPM badge such as `[180WPM]`." };
    }

    if (name === "pause") {
        return {
            title: "Timed pause",
            description: argument
                ? `Pause for an explicit duration. Current value: \`${argument}\`. Use seconds or milliseconds such as \`[pause:2s]\` or \`[pause:1000ms]\`.`
                : "Pause for an explicit duration using seconds or milliseconds, for example `[pause:2s]` or `[pause:1000ms]`."
        };
    }

    if (name === "breath") {
        return { title: "Breath mark", description: "Mark a natural breath point without adding extra timing." };
    }

    if (name === "edit_point" || name === "editpoint") {
        return {
            title: "Edit point",
            description: argument
                ? `Mark an edit point with priority \`${argument}\`.`
                : "Mark a standard edit point in the script."
        };
    }

    if (argument && spec.parameterizedWrapperLookup.has(name)) {
        return {
            title: resolveParameterizedTitle(name),
            description: `${spec.parameterizedWrapperLookup.get(name).documentation}\n\nGuide: \`${argument}\``
        };
    }

    if (spec.plainWrapperLookup.has(name)) {
        return {
            title: spec.plainWrapperLookup.get(name).detail,
            description: spec.plainWrapperLookup.get(name).documentation
        };
    }

    return null;
}

function resolveHeaderTitle(marker, spec) {
    return marker === spec.titleHeaderMarker
        ? "Document title"
        : marker === spec.segmentHeaderMarker
            ? "Segment header"
            : "Block header";
}

function resolveHeaderPartTitle(marker, index, value, spec) {
    if (index === 0) {
        return marker === spec.segmentHeaderMarker ? "Segment name" : "Block name";
    }

    switch (resolveHeaderPartKind(value, spec)) {
        case "speaker":
            return "Speaker assignment";
        case "archetype":
            return "Archetype";
        case "pace":
            return "Pace";
        case "emotion":
            return "Emotion";
        case "timing":
            return "Timing";
        default:
            return "Header option";
    }
}

function resolveHeaderPartDescription(marker, index, value, spec) {
    if (index === 0) {
        return marker === spec.segmentHeaderMarker
            ? "Name of the major TPS segment."
            : "Name of the TPS block inside the current segment.";
    }

    const kind = resolveHeaderPartKind(value, spec);

    switch (kind) {
        case "speaker":
            return "Assigns this segment or block to a named speaker. Use `Speaker:Name` for multi-speaker scripts.";
        case "archetype": {
            const descriptor = resolveArchetypeDescriptor(value, spec);
            return descriptor
                ? `Applies the ${descriptor.label} performance preset. It guides pace, energy, melody, and ${normalizeArchetypeArticulation(descriptor.articulation)} articulation.`
                : "Applies a performance preset that can guide pace, energy, melody, and articulation.";
        }
        case "pace":
            return "Sets the target speaking pace for this scope, for example `140WPM`.";
        case "emotion":
            return `Sets the inherited delivery mood for this scope. \`${value}\` affects reader and editor cue styling.`;
        case "timing":
            return "Marks an optional planned time window for this segment or block.";
        default:
            return "Extra structured header option inherited by content inside this scope.";
    }
}

function resolveHeaderPartKind(value, spec) {
    const lowerValue = value.toLowerCase();
    if (lowerValue.startsWith(spec.speakerPrefix.toLowerCase())) {
        return "speaker";
    }

    if (lowerValue.startsWith(spec.archetypePrefix.toLowerCase())) {
        return "archetype";
    }

    if (spec.numericWpmPattern.test(value)) {
        return "pace";
    }

    if (spec.emotionTagNames.includes(lowerValue)) {
        return "emotion";
    }

    if (spec.headerTimingPattern.test(value)) {
        return "timing";
    }

    return "metadata";
}

function resolveArchetypeDescriptor(value, spec) {
    const prefixLength = spec.archetypePrefix.length;
    const candidate = value.slice(prefixLength).trim().toLowerCase();
    return spec.archetypeDescriptorByName.get(candidate) ?? null;
}

function normalizeArchetypeArticulation(value) {
    const normalized = String(value ?? "").trim().toLowerCase();
    return normalized || "default";
}

function resolveParameterizedTitle(name) {
    return name === "stress"
        ? "Syllable guide"
        : name === "phonetic"
            ? "IPA pronunciation guide"
            : name === "energy"
                ? "Energy contour"
                : name === "melody"
                    ? "Melody contour"
                    : "Pronunciation guide";
}

function createHover(monaco, lineNumber, startColumn, endColumn, title, description) {
    return {
        contents: [{ value: `**${title}**\n\n${description}` }],
        range: new monaco.Range(lineNumber, startColumn, lineNumber, endColumn)
    };
}

function getPauseLength(text, index) {
    if (text[index] !== "/" || (index > 0 && text[index - 1] === "\\")) {
        return 0;
    }

    const length = index + 1 < text.length && text[index + 1] === "/" ? 2 : 1;
    const previous = index === 0 ? "\0" : text[index - 1];
    const next = index + length >= text.length ? "\0" : text[index + length];
    return (previous === "\0" || /\s/.test(previous)) && (next === "\0" || /\s/.test(next))
        ? length
        : 0;
}

function createAlternationPattern(names) {
    return names.map(name => name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|");
}
