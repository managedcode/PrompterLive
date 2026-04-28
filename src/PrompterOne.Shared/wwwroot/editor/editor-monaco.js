import { ensureTpsLanguage, getTpsLanguageSupport } from "./editor-monaco-tps-language.js";

const cssClassPrefix = "po";
const largeDraftDecorationCharacterThreshold = 16000;
const largeDraftDecorationViewportLinePadding = 24;
const largeDraftTextNotificationDelayMs = 750;
const sourceMirrorLineRenderCharacterThreshold = 8000;
const frontMatterDelimiter = "---";
const emptyValue = "";
const findMatchActiveClassName = `${cssClassPrefix}-find-match-active`;
const findMatchClassName = `${cssClassPrefix}-find-match`;
const floatingToolbarMinimumTop = 44;
const floatingToolbarSelectionOffset = 5;
const hostStates = new WeakMap();
const gutterSelector = ".margin-view-overlays";
const minimapSelector = ".minimap";
const numericWpmRegex = /^(?<wpm>\d+)\s*WPM$/i;
const tagPattern = /\[[^[\]]+\]/g;
const testIdAttributeName = "data-test";
const scopeKindRoot = "root";
const scopeKindNeutral = "neutral";
const scopeKindStyle = "style";
const scopeKindWpm = "wpm";
const scopeKindPronunciation = "pronunciation";
const styledTextAuthoringMode = "styled-text";
const monacoMinimapDefaults = Object.freeze({
    autohide: "none",
    enabled: true,
    maxColumn: 80,
    renderCharacters: false,
    scale: 1,
    showSlider: "mouseover",
    side: "right",
    size: "fit"
});
const darkThemeMinimapColors = Object.freeze({
    "minimap.background": "#0B101880",
    "minimapSlider.activeBackground": "#D4B07052",
    "minimapSlider.background": "#D4B07026",
    "minimapSlider.hoverBackground": "#D4B0703D"
});
const lightThemeMinimapColors = Object.freeze({
    "minimap.background": "#F7F0E180",
    "minimapSlider.activeBackground": "#8B735552",
    "minimapSlider.background": "#8B735526",
    "minimapSlider.hoverBackground": "#8B73553D"
});
const tagSemanticClassNames = Object.freeze({
    breath: `${cssClassPrefix}-tag-breath`,
    editPoint: `${cssClassPrefix}-tag-editpoint`,
    emphasis: `${cssClassPrefix}-tag-emphasis`,
    energy: `${cssClassPrefix}-tag-energy`,
    highlight: `${cssClassPrefix}-tag-highlight`,
    melody: `${cssClassPrefix}-tag-melody`,
    pause: `${cssClassPrefix}-tag-pause`,
    pronunciation: `${cssClassPrefix}-tag-pronunciation`,
    stress: `${cssClassPrefix}-tag-stress`,
    wpm: `${cssClassPrefix}-tag-wpm`
});
const monacoDefaults = Object.freeze({
    automaticLayout: true,
    contextmenu: false,
    cursorBlinking: "smooth",
    editContext: false,
    fixedOverflowWidgets: true,
    folding: false,
    fontFamily: "var(--mono)",
    fontLigatures: false,
    fontSize: 16,
    glyphMargin: false,
    letterSpacing: 0,
    lineDecorationsWidth: 0,
    lineNumbers: "on",
    lineNumbersMinChars: 4,
    lineHeight: 32,
    minimap: monacoMinimapDefaults,
    overviewRulerBorder: false,
    overviewRulerLanes: 0,
    padding: { top: 40, bottom: 40 },
    quickSuggestions: true,
    renderLineHighlight: "none",
    roundedSelection: true,
    scrollBeyondLastLine: false,
    scrollbar: {
        alwaysConsumeMouseWheel: false,
        horizontal: "hidden",
        vertical: "auto",
        useShadows: false
    },
    suggestOnTriggerCharacters: true,
    tabSize: 4,
    trimAutoWhitespace: false,
    wordWrap: "on",
    wrappingIndent: "same"
});
const emptyTpsCatalog = Object.freeze({
    articulationStyles: [],
    deliveryModes: [],
    emotions: [],
    relativeSpeedTags: [],
    volumeLevels: []
});

let stylesheetPromise;
let loaderPromise;
let monacoPromise;
let themeObserverRegistered = false;

function createDecorationCatalog(options) {
    const runtimeCatalog = options?.tpsCatalog ?? emptyTpsCatalog;
    const emotionNames = normalizeCatalogNames(runtimeCatalog.emotions);
    const volumeNames = normalizeCatalogNames(runtimeCatalog.volumeLevels);
    const deliveryNames = normalizeCatalogNames(runtimeCatalog.deliveryModes);
    const articulationNames = normalizeCatalogNames(runtimeCatalog.articulationStyles);
    const speedNames = normalizeCatalogNames(runtimeCatalog.relativeSpeedTags);
    return {
        articulationClasses: new Map(articulationNames.map(name => [name, `${cssClassPrefix}-inline-articulation-${name}`])),
        articulationTagClasses: createSemanticTagClassMap(articulationNames, "articulation"),
        deliveryClasses: new Map(deliveryNames.map(name => [name, `${cssClassPrefix}-inline-delivery-${name}`])),
        deliveryTagClasses: createSemanticTagClassMap(deliveryNames, "delivery"),
        emotionClasses: new Map(emotionNames.map(name => [name, `${cssClassPrefix}-inline-emotion-${name}`])),
        emotionTagClasses: createSemanticTagClassMap(emotionNames, "emotion"),
        headerEmotionTags: new Set(emotionNames),
        speedClasses: new Map(speedNames.map(name => [name, name === "normal" ? null : `${cssClassPrefix}-inline-speed-${name}`])),
        speedTagClasses: createSemanticTagClassMap(speedNames.filter(name => name !== "normal"), "speed"),
        volumeClasses: new Map(volumeNames.map(name => [name, `${cssClassPrefix}-inline-${name}`])),
        volumeTagClasses: createSemanticTagClassMap(volumeNames, "volume")
    };
}

function normalizeCatalogNames(values) {
    return Array.isArray(values)
        ? values
            .map(value => String(value ?? emptyValue).trim().toLowerCase())
            .filter(Boolean)
        : [];
}

function createSemanticTagClassMap(names, category) {
    return new Map(names.map(name => [name, `${cssClassPrefix}-tag-${category}-${name}`]));
}

function normalizeTagName(value) {
    const normalizedValue = String(value ?? emptyValue).trim();
    const separatorIndex = normalizedValue.indexOf(":");
    return (separatorIndex >= 0 ? normalizedValue.slice(0, separatorIndex) : normalizedValue)
        .trim()
        .toLowerCase();
}

function createTagClassName(semanticTagClassName) {
    return semanticTagClassName
        ? `${cssClassPrefix}-tag ${semanticTagClassName}`
        : `${cssClassPrefix}-tag`;
}

function resolveSemanticTagClassName(normalizedName, tpsCatalog) {
    if (!normalizedName) {
        return null;
    }

    if (tpsCatalog.emotionTagClasses.has(normalizedName)) {
        return tpsCatalog.emotionTagClasses.get(normalizedName);
    }

    if (tpsCatalog.volumeTagClasses.has(normalizedName)) {
        return tpsCatalog.volumeTagClasses.get(normalizedName);
    }

    if (tpsCatalog.deliveryTagClasses.has(normalizedName)) {
        return tpsCatalog.deliveryTagClasses.get(normalizedName);
    }

    if (tpsCatalog.articulationTagClasses.has(normalizedName)) {
        return tpsCatalog.articulationTagClasses.get(normalizedName);
    }

    if (tpsCatalog.speedTagClasses.has(normalizedName)) {
        return tpsCatalog.speedTagClasses.get(normalizedName);
    }

    switch (normalizedName) {
        case "highlight":
            return tagSemanticClassNames.highlight;
        case "emphasis":
        case "strong":
        case "bold":
            return tagSemanticClassNames.emphasis;
        case "energy":
            return tagSemanticClassNames.energy;
        case "melody":
            return tagSemanticClassNames.melody;
        case "stress":
            return tagSemanticClassNames.stress;
        case "phonetic":
        case "pronunciation":
            return tagSemanticClassNames.pronunciation;
        case "pause":
            return tagSemanticClassNames.pause;
        case "breath":
            return tagSemanticClassNames.breath;
        case "edit_point":
        case "editpoint":
            return tagSemanticClassNames.editPoint;
        default:
            return null;
    }
}

export async function initializeEditor(host, proxy, semanticSnapshot, dotNetRef, options) {
    if (!host || !proxy || !dotNetRef || !options) {
        return false;
    }

    const monaco = await loadMonacoAsync(options);
    const existingState = hostStates.get(host);
    if (existingState) {
        existingState.proxy = proxy;
        existingState.semanticSnapshot = semanticSnapshot;
        existingState.dotNetRef = dotNetRef;
        existingState.options = options;
        existingState.tpsCatalog = createDecorationCatalog(options);
        existingState.findDecorationCollection ??= existingState.editor.createDecorationsCollection();
        existingState.findMatches ??= [];
        existingState.activeFindMatchIndex ??= -1;
        existingState.host.dataset.authoringMode = normalizeAuthoringMode(options.authoringMode);
        await syncEditorState(host, proxy.value ?? emptyValue, proxy.selectionStart ?? 0, proxy.selectionEnd ?? 0);
        updateFindDecorations(existingState);
        return true;
    }

    const editor = monaco.editor.create(host, {
        ...monacoDefaults,
        language: options.languageId,
        placeholder: options.placeholder,
        theme: resolveThemeName(options),
        value: proxy.value ?? emptyValue
    });

    const state = {
        activeFindMatchIndex: -1,
        decorationCollection: editor.createDecorationsCollection(),
        decorationFrameId: 0,
        dotNetRef,
        editor,
        findDecorationCollection: editor.createDecorationsCollection(),
        findMatches: [],
        host,
        lastDecorationClasses: [],
        lastMeaningfulScrollPosition: {
            scrollLeft: editor.getScrollLeft(),
            scrollTop: editor.getScrollTop()
        },
        monaco,
        options,
        proxy,
        semanticSnapshot,
        pendingTextNotificationTimeoutId: 0,
        suppressProxySelection: false,
        suppressSelectionNotification: false,
        suppressTextNotification: false,
        subscriptions: [],
        tpsCatalog: createDecorationCatalog(options)
    };

    state.subscriptions.push(
        editor.onDidChangeModelContent(() => onEditorContentChanged(state)),
        editor.onDidChangeCursorSelection(() => notifySelectionChanged(state, false)),
        editor.onDidLayoutChange(() => syncEditorChromeContracts(state)),
        editor.onDidScrollChange(() => {
            const scrollPosition = captureEditorScrollPosition(state);
            if (scrollPosition.scrollLeft > 0 || scrollPosition.scrollTop > 0) {
                state.lastMeaningfulScrollPosition = scrollPosition;
            }

            if (shouldUseVisibleRangeDecorations(state)) {
                scheduleDecorations(state);
            }

            if (!editor.getSelection()?.isEmpty()) {
                notifySelectionChanged(state, false);
            }
        }),
        editor.onMouseUp(() => notifySelectionChanged(state, true))
    );

    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyZ, () => notifyHistoryRequested(state, "undo"));
    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyY, () => notifyHistoryRequested(state, "redo"));
    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyZ, () => notifyHistoryRequested(state, "redo"));

    const hostDragOverHandler = event => handleEditorDragOver(state, event);
    const hostDropHandler = event => {
        void handleEditorDropAsync(state, event);
    };
    host.addEventListener("dragover", hostDragOverHandler);
    host.addEventListener("drop", hostDropHandler);
    state.hostDragOverHandler = hostDragOverHandler;
    state.hostDropHandler = hostDropHandler;

    hostStates.set(host, state);
    host.dataset.authoringMode = normalizeAuthoringMode(options.authoringMode);
    host.setAttribute(options.editorEngineAttributeName, options.editorEngineAttributeValue);
    host.setAttribute(options.editorReadyAttributeName, "true");

    syncProxyFromEditor(state);
    syncEditorChromeContracts(state);
    renderSemanticSnapshot(state, editor.getValue());
    scheduleDecorations(state);
    updateFindDecorations(state);
    ensureHarness(options);
    ensureThemeObserver(monaco, options);
    return true;
}

export async function syncEditorState(host, text, selectionStart, selectionEnd) {
    const state = hostStates.get(host);
    if (!state) {
        return;
    }

    const nextText = text ?? emptyValue;
    const preservedScrollPosition = resolvePreservedScrollPosition(state);
    cancelPendingTextNotification(state);
    state.suppressTextNotification = true;
    state.suppressSelectionNotification = true;

    const model = state.editor.getModel();
    if (model) {
        replaceModelTextPreservingViewport(state, nextText, preservedScrollPosition);
    }

    applySelection(state, selectionStart ?? 0, selectionEnd ?? 0, false, undefined, preservedScrollPosition);
    restoreEditorScrollPosition(state, preservedScrollPosition);
    requestAnimationFrame(() => {
        const currentState = hostStates.get(state.host);
        if (!currentState) {
            return;
        }

        restoreEditorScrollPosition(currentState, preservedScrollPosition);
    });
    state.suppressTextNotification = false;
    state.suppressSelectionNotification = false;
    syncProxyFromEditor(state);
    renderSemanticSnapshot(state, nextText);
    scheduleDecorations(state);
}

export function getSelectionState(host) {
    const state = hostStates.get(host);
    return state ? createSelectionState(state) : createEmptySelectionState();
}

export async function setSelection(host, start, end, revealSelection = true, focusEditor = true, selectionDirection = undefined) {
    const state = hostStates.get(host);
    if (!state) {
        return createEmptySelectionState();
    }

    applySelection(state, start ?? 0, end ?? 0, revealSelection !== false, selectionDirection, undefined, focusEditor !== false);
    await waitForSelectionState(state, start ?? 0, end ?? 0, selectionDirection);
    await notifySelectionChangedAsync(state, false);
    return await waitForSelectionState(state, start ?? 0, end ?? 0, selectionDirection);
}

export function setFindMatches(host, matches, activeMatchIndex = -1) {
    const state = hostStates.get(host);
    if (!state) {
        return;
    }

    state.findMatches = normalizeFindMatches(matches);
    state.activeFindMatchIndex = Number.isInteger(activeMatchIndex) ? activeMatchIndex : -1;
    updateFindDecorations(state);
}

export function disposeEditor(host) {
    const state = hostStates.get(host);
    if (!state) {
        return;
    }

    if (state.decorationFrameId) {
        cancelAnimationFrame(state.decorationFrameId);
    }

    state.host.removeEventListener("dragover", state.hostDragOverHandler);
    state.host.removeEventListener("drop", state.hostDropHandler);
    cancelPendingTextNotification(state);
    state.decorationCollection.clear();
    state.findDecorationCollection.clear();
    for (const subscription of state.subscriptions) {
        subscription.dispose();
    }

    state.editor.dispose();
    host.removeAttribute(state.options.editorReadyAttributeName);
    hostStates.delete(host);
}

async function loadMonacoAsync(options) {
    if (monacoPromise) {
        return monacoPromise;
    }

    await ensureStylesheetAsync(options.monacoStylesheetPath);
    await ensureLoaderAsync(options.monacoLoaderPath);
    window.require.config({ paths: { vs: options.monacoVsPath } });
    window.MonacoEnvironment = { baseUrl: options.monacoVsPath };

    monacoPromise = new Promise((resolve, reject) => {
        window.require(["vs/editor/editor.main"], monaco => {
            ensureTpsLanguage(monaco, options);
            applyResolvedTheme(monaco, options);
            resolve(monaco);
        }, reject);
    });

    return monacoPromise;
}

function ensureStylesheetAsync(stylesheetPath) {
    if (!stylesheetPromise) {
        stylesheetPromise = new Promise((resolve, reject) => {
            const existing = document.querySelector(`link[data-monaco-stylesheet="${stylesheetPath}"]`);
            if (existing) {
                resolve();
                return;
            }

            const link = document.createElement("link");
            link.rel = "stylesheet";
            link.href = stylesheetPath;
            link.dataset.monacoStylesheet = stylesheetPath;
            link.onload = () => resolve();
            link.onerror = () => reject(new Error(`Unable to load Monaco stylesheet: ${stylesheetPath}`));
            document.head.appendChild(link);
        });
    }

    return stylesheetPromise;
}

function ensureLoaderAsync(loaderPath) {
    if (!loaderPromise) {
        loaderPromise = new Promise((resolve, reject) => {
            if (window.require) {
                resolve();
                return;
            }

            const script = document.createElement("script");
            script.src = loaderPath;
            script.async = true;
            script.dataset.monacoLoader = loaderPath;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error(`Unable to load Monaco loader: ${loaderPath}`));
            document.head.appendChild(script);
        });
    }

    return loaderPromise;
}

function ensureHarness(options) {
    if (window[options.browserHarnessGlobalName]) {
        return;
    }

    window[options.browserHarnessGlobalName] = {
        focus: testId => {
            const state = getRequiredHarnessState(testId);
            state.editor.focus();
            return createHarnessState(state, options);
        },
        getState: testId => {
            const state = getOptionalHarnessState(testId);
            return state ? createHarnessState(state, options) : null;
        },
        getCompletions: async (testId, lineNumber, column) => {
            const state = getRequiredHarnessState(testId);
            const support = getRequiredLanguageSupport(state);
            const model = state.editor.getModel();
            if (!model) {
                return { suggestions: [] };
            }

            const result = await support.completionProvider.provideCompletionItems(
                model,
                new state.monaco.Position(lineNumber ?? 1, column ?? 1),
                { triggerKind: state.monaco.languages.CompletionTriggerKind.Invoke });
            return {
                suggestions: (result?.suggestions ?? []).map(suggestion => ({
                    detail: suggestion.detail ?? emptyValue,
                    documentation: normalizeMarkdownValue(suggestion.documentation),
                    insertText: typeof suggestion.insertText === "string" ? suggestion.insertText : suggestion.insertText?.value ?? emptyValue,
                    label: typeof suggestion.label === "string" ? suggestion.label : suggestion.label?.label ?? emptyValue
                }))
            };
        },
        getHover: async (testId, lineNumber, column) => {
            const state = getRequiredHarnessState(testId);
            const support = getRequiredLanguageSupport(state);
            const model = state.editor.getModel();
            if (!model) {
                return null;
            }

            const result = await support.hoverProvider.provideHover(
                model,
                new state.monaco.Position(lineNumber ?? 1, column ?? 1));
            return result
                ? {
                    contents: (result.contents ?? []).map(normalizeMarkdownValue),
                    range: result.range
                        ? {
                            endColumn: result.range.endColumn,
                            endLineNumber: result.range.endLineNumber,
                            startColumn: result.range.startColumn,
                            startLineNumber: result.range.startLineNumber
                        }
                        : null
                }
                : null;
        },
        setSelection: async (testId, start, end, revealSelection = true, selectionDirection = undefined) => {
            const state = getRequiredHarnessState(testId);
            applySelection(state, start ?? 0, end ?? 0, revealSelection !== false, selectionDirection);
            await waitForSelectionState(state, start ?? 0, end ?? 0, selectionDirection);
            await notifySelectionChangedAsync(state, false);
            return await waitForSelectionState(state, start ?? 0, end ?? 0, selectionDirection);
        },
        centerSelectionLine: testId => {
            const state = getRequiredHarnessState(testId);
            centerSelectionLineInViewport(state);
            requestAnimationFrame(() => {
                const currentState = getOptionalHarnessState(testId);
                if (!currentState) {
                    return;
                }

                centerSelectionLineInViewport(currentState);
                currentState.editor.render();
            });
            return createHarnessState(state, options);
        },
        setText: async (testId, text) => {
            const state = getRequiredHarnessState(testId);
            return await applyTextForHarnessAsync(state, text ?? emptyValue, options);
        },
        tokenizeLine: (testId, lineNumber) => {
            const state = getRequiredHarnessState(testId);
            const model = state.editor.getModel();
            if (!model) {
                return { lineText: emptyValue, tokens: [] };
            }

            const safeLineNumber = Math.max(1, Math.min(lineNumber ?? 1, model.getLineCount()));
            const lineText = model.getLineContent(safeLineNumber);
            const tokenizedLine = state.monaco.editor.tokenize(lineText, model.getLanguageId())[0] ?? [];
            return {
                lineText,
                tokens: tokenizedLine.map(token => ({
                    offset: token.offset ?? 0,
                    type: token.type ?? emptyValue
                }))
            };
        }
    };
}

function getRequiredLanguageSupport(state) {
    const model = state.editor.getModel();
    const support = getTpsLanguageSupport(model?.getLanguageId() ?? emptyValue);
    if (!support) {
        throw new Error(`Unable to resolve Monaco TPS language support for "${model?.getLanguageId() ?? emptyValue}".`);
    }

    return support;
}

function normalizeMarkdownValue(value) {
    if (typeof value === "string") {
        return value;
    }

    return value?.value ?? emptyValue;
}

function createHarnessState(state, options) {
    const model = state.editor.getModel();
    const layoutInfo = state.editor.getLayoutInfo();
    const minimapMetrics = measureMinimapMetrics(state);
    const visibleRanges = state.editor.getVisibleRanges();
    const primaryVisibleRange = visibleRanges.length
        ? {
            endLineNumber: visibleRanges[0].endLineNumber,
            startLineNumber: visibleRanges[0].startLineNumber
        }
        : null;
    return {
        decorationClasses: state.lastDecorationClasses?.length
            ? state.lastDecorationClasses
            : (model?.getAllDecorations().map(decoration =>
                decoration.options.inlineClassName || decoration.options.lineClassName || "").filter(Boolean) ?? []),
        engine: state.host.getAttribute(options.editorEngineAttributeName) ?? emptyValue,
        authoringMode: normalizeAuthoringMode(state.options.authoringMode),
        layout: {
            contentLeft: layoutInfo.contentLeft ?? 0,
            contentWidth: layoutInfo.contentWidth ?? 0,
            editorWidth: layoutInfo.width ?? 0,
            minimapLeft: minimapMetrics.left,
            minimapWidth: minimapMetrics.width
        },
        languageId: model?.getLanguageId() ?? emptyValue,
        lineCount: model?.getLineCount() ?? 0,
        ready: state.host.getAttribute(options.editorReadyAttributeName) === "true",
        scrollTop: state.editor.getScrollTop(),
        selection: createSelectionState(state),
        text: state.editor.getValue(),
        visibleRange: primaryVisibleRange
    };
}

function getOptionalHarnessState(testId) {
    const host = document.querySelector(`[data-test="${testId}"]`);
    return host ? hostStates.get(host) : null;
}

function getRequiredHarnessState(testId) {
    const state = getOptionalHarnessState(testId);
    if (!state) {
        throw new Error(`Unable to resolve Monaco editor state for test id "${testId}".`);
    }

    return state;
}

function ensureThemeObserver(monaco, options) {
    if (themeObserverRegistered) {
        return;
    }

    themeObserverRegistered = true;
    const observer = new MutationObserver(() => applyResolvedTheme(monaco, options));
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ["class", "data-theme"] });
    if (document.body) {
        observer.observe(document.body, { attributes: true, attributeFilter: ["class"] });
    }
}

function syncEditorChromeContracts(state) {
    const gutter = state.host.querySelector(gutterSelector);
    if (gutter instanceof HTMLElement && state.options.sourceGutterTestId) {
        gutter.setAttribute(testIdAttributeName, state.options.sourceGutterTestId);
    }

    const minimap = state.host.querySelector(minimapSelector);
    if (minimap instanceof HTMLElement && state.options.sourceMinimapTestId) {
        minimap.setAttribute(testIdAttributeName, state.options.sourceMinimapTestId);
    }
}

function applyResolvedTheme(monaco, options) {
    monaco.editor.defineTheme(options.darkThemeName, createThemeData(false));
    monaco.editor.defineTheme(options.lightThemeName, createThemeData(true));
    monaco.editor.setTheme(resolveThemeName(options));
}

function resolveThemeName(options) {
    return isLightThemeActive() ? options.lightThemeName : options.darkThemeName;
}

function normalizeAuthoringMode(authoringMode) {
    return authoringMode === styledTextAuthoringMode ? styledTextAuthoringMode : "raw";
}

function isLightThemeActive() {
    return document.documentElement.dataset.theme === "light" ||
        document.documentElement.classList.contains("theme-light") ||
        document.body?.classList.contains("theme-light") === true;
}

function createThemeData(isLight) {
    const minimapColors = isLight
        ? lightThemeMinimapColors
        : darkThemeMinimapColors;

    return {
        base: isLight ? "vs" : "vs-dark",
        inherit: true,
        rules: [
            { token: "frontmatter.key", foreground: isLight ? "5F57D6" : "A0AAFF" },
            { token: "frontmatter.value", foreground: isLight ? "23784B" : "6FE89A" },
            { token: "frontmatter.delimiter", foreground: isLight ? "7F8A8A" : "8A9A94" },
            { token: "header.title.hash", foreground: isLight ? "7D4A2C" : "F3C7A5" },
            { token: "header.title.body", foreground: isLight ? "7D4A2C" : "F3C7A5" },
            { token: "header.segment.hash", foreground: isLight ? "8B6A33" : "F2E1AA" },
            { token: "header.segment.body", foreground: isLight ? "8B6A33" : "F2E1AA" },
            { token: "header.block.hash", foreground: isLight ? "3B6E9A" : "8ECFFF" },
            { token: "header.block.body", foreground: isLight ? "3B6E9A" : "8ECFFF" },
            { token: "pause.timed", foreground: isLight ? "8E6A00" : "E0C070" },
            { token: "pause.long", foreground: isLight ? "9A7A19" : "EEDB96" },
            { token: "pause.short", foreground: isLight ? "8E6A00" : "E0C070" },
            { token: "cue.open", foreground: isLight ? "8A7B6B" : "8A9E98" },
            { token: "cue.close", foreground: isLight ? "8A7B6B" : "8A9E98" },
            { token: "cue.breath", foreground: isLight ? "7A6B4D" : "D7C79C" },
            { token: "cue.editpoint", foreground: isLight ? "9A5A63" : "FFB0BD" },
            { token: "cue.energy", foreground: isLight ? "357D62" : "5EECC2" },
            { token: "cue.melody", foreground: isLight ? "A4572A" : "FF9F6E" },
            { token: "cue.pronunciation", foreground: isLight ? "5C6AA0" : "AFC2FF" },
            { token: "markdown.bold", foreground: isLight ? "4F3C2A" : "FFE8B2", fontStyle: "bold" },
            { token: "markdown.italic", foreground: isLight ? "7A5A36" : "F3D39B", fontStyle: "italic" },
            { token: "wpm.badge", foreground: isLight ? "936F00" : "FFE066", fontStyle: "bold" },
            { token: "meta.tag", foreground: isLight ? "6E7781" : "B8C0C8" },
            { token: "escape.sequence", foreground: isLight ? "62708A" : "9FB2CC" }
        ],
        colors: {
            "editor.background": isLight ? "#00000000" : "#00000000",
            "editor.foreground": isLight ? "#32281E" : "#ECF0EE",
            "editorLineNumber.activeForeground": isLight ? "#5F4E38" : "#F2E1AA",
            "editorLineNumber.foreground": isLight ? "#9A8A74" : "#8A9A94",
            "editor.selectionBackground": isLight ? "#FFE06633" : "#FFE0662E",
            "editorCursor.foreground": isLight ? "#5C4D3D" : "#ECF0EE",
            ...minimapColors
        }
    };
}

function measureMinimapMetrics(state) {
    const minimap = state.host.querySelector(minimapSelector);
    if (!(minimap instanceof HTMLElement)) {
        return { left: 0, width: 0 };
    }

    const hostBounds = state.host.getBoundingClientRect();
    const minimapBounds = minimap.getBoundingClientRect();
    return {
        left: Math.max(0, minimapBounds.left - hostBounds.left),
        width: minimapBounds.width
    };
}

function onEditorContentChanged(state) {
    syncProxyFromEditor(state);
    dispatchProxyChangedEvent(state);
    renderSemanticSnapshot(state, state.editor.getValue());
    scheduleDecorations(state);
    queueTextChangedNotification(state);
}

function notifySelectionChanged(state, dismissMenus) {
    void notifySelectionChangedAsync(state, dismissMenus);
}

async function notifySelectionChangedAsync(state, dismissMenus) {
    syncProxyFromEditor(state);
    if (!state.suppressSelectionNotification) {
        const selectionState = createSelectionState(state);
        await state.dotNetRef.invokeMethodAsync(
            state.options.selectionChangedCallbackName,
            selectionState.start,
            selectionState.end,
            selectionState.line,
            selectionState.column,
            selectionState.toolbarTop,
            selectionState.toolbarLeft,
            dismissMenus);
    }
}

async function notifyTextChangedAsync(state, text) {
    if (state.suppressTextNotification) {
        return;
    }

    await state.dotNetRef.invokeMethodAsync(
        state.options.textChangedCallbackName,
        text ?? state.editor.getValue());
}

function cancelPendingTextNotification(state) {
    if (!state?.pendingTextNotificationTimeoutId) {
        return;
    }

    window.clearTimeout(state.pendingTextNotificationTimeoutId);
    state.pendingTextNotificationTimeoutId = 0;
}

function queueTextChangedNotification(state) {
    if (state.suppressTextNotification) {
        return;
    }

    const currentText = state.editor.getValue();
    if (currentText.length < largeDraftDecorationCharacterThreshold) {
        cancelPendingTextNotification(state);
        void notifyTextChangedAsync(state, currentText);
        return;
    }

    cancelPendingTextNotification(state);
    state.pendingTextNotificationTimeoutId = window.setTimeout(() => {
        state.pendingTextNotificationTimeoutId = 0;
        if (!hostStates.has(state.host) || state.suppressTextNotification) {
            return;
        }

        void notifyTextChangedAsync(state, state.editor.getValue());
    }, largeDraftTextNotificationDelayMs);
}

function waitForAnimationFrames(frameCount = 1) {
    return new Promise(resolve => {
        const pump = remaining => {
            if (remaining <= 0) {
                resolve();
                return;
            }

            requestAnimationFrame(() => pump(remaining - 1));
        };

        pump(frameCount);
    });
}

function textMatchesState(state, expectedText) {
    const currentText = state.editor.getValue();
    return currentText === expectedText && state.proxy.value === expectedText;
}

async function waitForTextState(state, expectedText) {
    const attempts = 12;
    for (let attempt = 0; attempt < attempts; attempt++) {
        if (textMatchesState(state, expectedText)) {
            return createHarnessState(state, state.options);
        }

        await waitForAnimationFrames(2);
    }

    return createHarnessState(state, state.options);
}

function selectionMatchesOffsets(state, start, end, expectedDirection) {
    const selection = createSelectionState(state);
    const orderedStart = Math.min(start, end);
    const orderedEnd = Math.max(start, end);
    const directionMatches = !expectedDirection || selection.direction === expectedDirection;
    return Math.min(selection.start, selection.end) === orderedStart &&
        Math.max(selection.start, selection.end) === orderedEnd &&
        directionMatches;
}

async function waitForSelectionState(state, start, end, expectedDirection) {
    const attempts = 24;
    for (let attempt = 0; attempt < attempts; attempt++) {
        if (selectionMatchesOffsets(state, start, end, expectedDirection)) {
            return createSelectionState(state);
        }

        await waitForAnimationFrames(2);
    }

    return createSelectionState(state);
}

async function applyTextForHarnessAsync(state, nextText, options) {
    const model = state.editor.getModel();
    if (!model) {
        return createHarnessState(state, options);
    }

    state.suppressTextNotification = true;
    replaceModelTextPreservingViewport(state, nextText);
    syncProxyFromEditor(state);
    state.suppressTextNotification = false;

    queueTextChangedNotification(state);
    return await waitForTextState(state, nextText);
}

function notifyHistoryRequested(state, command) {
    void state.dotNetRef.invokeMethodAsync(state.options.historyRequestedCallbackName, command);
}

function notifyFilesDropped(state, request) {
    void state.dotNetRef.invokeMethodAsync(state.options.filesDroppedCallbackName, request);
}

function handleEditorDragOver(state, event) {
    if (!hasDraggedFiles(event.dataTransfer)) {
        return;
    }

    event.preventDefault();
    event.dataTransfer.dropEffect = "copy";
}

async function handleEditorDropAsync(state, event) {
    if (!hasDraggedFiles(event.dataTransfer)) {
        return;
    }

    event.preventDefault();
    const request = await readDroppedFilesAsync(
        event.dataTransfer.files,
        state.options.supportedFileNameSuffixes);
    if (!request.files.length && !request.rejectedFileNames.length) {
        return;
    }

    notifyFilesDropped(state, request);
}

function hasDraggedFiles(dataTransfer) {
    return Boolean(dataTransfer?.files && dataTransfer.files.length > 0);
}

async function readDroppedFilesAsync(fileList, supportedFileNameSuffixes) {
    const request = {
        files: [],
        rejectedFileNames: []
    };
    const supportedSuffixes = Array.isArray(supportedFileNameSuffixes)
        ? supportedFileNameSuffixes
        : [];

    for (const file of Array.from(fileList ?? [])) {
        if (!isSupportedFileName(file.name, supportedSuffixes)) {
            request.rejectedFileNames.push(file.name ?? emptyValue);
            continue;
        }

        try {
            request.files.push({
                fileName: file.name ?? emptyValue,
                text: await file.text()
            });
        }
        catch {
            request.rejectedFileNames.push(file.name ?? emptyValue);
        }
    }

    return request;
}

function isSupportedFileName(fileName, supportedSuffixes) {
    const normalizedFileName = String(fileName ?? emptyValue).toLowerCase();
    return normalizedFileName.length > 0 &&
        supportedSuffixes.some(suffix => normalizedFileName.endsWith(String(suffix ?? emptyValue).toLowerCase()));
}

function renderSemanticSnapshot(state, text) {
    const mirror = state.semanticSnapshot;
    if (!(mirror instanceof HTMLElement)) {
        return;
    }

    const nextText = text ?? emptyValue;
    mirror.dataset.renderedLength = String(nextText.length);
    const renderLines = nextText.length <= sourceMirrorLineRenderCharacterThreshold;
    if (state.lastMirrorText === nextText && state.lastMirrorRenderedLines === renderLines) {
        return;
    }

    state.lastMirrorText = nextText;
    state.lastMirrorRenderedLines = renderLines;

    if (!renderLines) {
        mirror.replaceChildren();
        return;
    }

    const fragment = document.createDocumentFragment();
    const lines = nextText.split(/\r\n|\n|\r/);
    for (const line of lines) {
        const element = document.createElement("div");
        element.className = "ed-src-line";
        element.textContent = line;
        fragment.appendChild(element);
    }

    mirror.replaceChildren(fragment);
}

function scheduleDecorations(state) {
    if (state.decorationFrameId) {
        cancelAnimationFrame(state.decorationFrameId);
    }

    state.decorationFrameId = requestAnimationFrame(() => {
        const decorations = buildDecorations(state);
        state.lastDecorationClasses = decorations
            .map(decoration => decoration?.options?.inlineClassName || decoration?.options?.lineClassName || emptyValue)
            .filter(Boolean);
        state.decorationCollection.set(decorations);
        state.decorationFrameId = 0;
    });
}

function buildDecorations(state) {
    const { editor, monaco } = state;
    const model = editor.getModel();
    if (!monaco || !model) {
        return [];
    }

    const decorations = [];
    const frontMatterEndLine = resolveFrontMatterEndLine(model);
    const lineRanges = getDecorationLineRanges(state, model);

    for (const lineRange of lineRanges) {
        for (let lineNumber = lineRange.startLineNumber; lineNumber <= lineRange.endLineNumber; lineNumber++) {
            const line = model.getLineContent(lineNumber);
            if (frontMatterEndLine > 0 && lineNumber <= frontMatterEndLine) {
                decorateFrontMatterLine(monaco, decorations, lineNumber, line);
                continue;
            }

            if (decorateHeaderLine(monaco, decorations, lineNumber, line, "##", `${cssClassPrefix}-line-segment`, state.tpsCatalog)) {
                continue;
            }

            if (decorateHeaderLine(monaco, decorations, lineNumber, line, "###", `${cssClassPrefix}-line-block`, state.tpsCatalog)) {
                continue;
            }

            decorateBodyLine(monaco, decorations, lineNumber, line, state.tpsCatalog);
        }
    }

    return decorations;
}

function normalizeFindMatches(matches) {
    if (!Array.isArray(matches)) {
        return [];
    }

    return matches
        .map(match => ({
            end: Number.isFinite(match?.end) ? Number(match.end) : 0,
            start: Number.isFinite(match?.start) ? Number(match.start) : 0
        }))
        .filter(match => match.end > match.start);
}

function updateFindDecorations(state) {
    state.findDecorationCollection.set(buildFindMatchDecorations(state));
}

function buildFindMatchDecorations(state) {
    const { editor, findMatches, monaco } = state;
    const model = editor.getModel();
    if (!model || !Array.isArray(findMatches) || findMatches.length === 0) {
        return [];
    }

    const maxOffset = model.getValueLength();
    const activeMatchIndex = state.activeFindMatchIndex >= 0 && state.activeFindMatchIndex < findMatches.length
        ? state.activeFindMatchIndex
        : -1;

    return findMatches
        .map((match, index) => {
            const safeStart = Math.max(0, Math.min(match.start, maxOffset));
            const safeEnd = Math.max(safeStart, Math.min(match.end, maxOffset));
            if (safeEnd <= safeStart) {
                return null;
            }

            const startPosition = model.getPositionAt(safeStart);
            const endPosition = model.getPositionAt(safeEnd);
            const inlineClassName = index === activeMatchIndex
                ? `${findMatchClassName} ${findMatchActiveClassName}`
                : findMatchClassName;

            return {
                options: {
                    inlineClassName,
                    inlineClassNameAffectsLetterSpacing: false
                },
                range: new monaco.Range(
                    startPosition.lineNumber,
                    startPosition.column,
                    endPosition.lineNumber,
                    endPosition.column)
            };
        })
        .filter(Boolean);
}

function getDecorationLineRanges(state, model) {
    const lineCount = model.getLineCount();
    if (!shouldUseVisibleRangeDecorations(state)) {
        return [{
            endLineNumber: lineCount,
            startLineNumber: 1
        }];
    }

    const visibleRanges = state.editor.getVisibleRanges();
    if (!visibleRanges.length) {
        return [{
            endLineNumber: Math.min(lineCount, largeDraftDecorationViewportLinePadding * 2),
            startLineNumber: 1
        }];
    }

    const mergedRanges = [];
    for (const visibleRange of visibleRanges) {
        const nextRange = {
            endLineNumber: Math.min(lineCount, visibleRange.endLineNumber + largeDraftDecorationViewportLinePadding),
            startLineNumber: Math.max(1, visibleRange.startLineNumber - largeDraftDecorationViewportLinePadding)
        };
        const previousRange = mergedRanges[mergedRanges.length - 1];
        if (previousRange && nextRange.startLineNumber <= previousRange.endLineNumber + 1) {
            previousRange.endLineNumber = Math.max(previousRange.endLineNumber, nextRange.endLineNumber);
            continue;
        }

        mergedRanges.push(nextRange);
    }

    return mergedRanges;
}

function resolveFrontMatterEndLine(model) {
    if (model.getLineCount() < 2 || model.getLineContent(1) !== frontMatterDelimiter) {
        return 0;
    }

    for (let lineNumber = 2; lineNumber <= model.getLineCount(); lineNumber++) {
        if (model.getLineContent(lineNumber) === frontMatterDelimiter) {
            return lineNumber;
        }
    }

    return 1;
}

function shouldUseVisibleRangeDecorations(state) {
    const model = state.editor.getModel();
    return (model?.getValueLength() ?? 0) >= largeDraftDecorationCharacterThreshold;
}

function decorateFrontMatterLine(monaco, decorations, lineNumber, line) {
    if (line === frontMatterDelimiter) {
        decorations.push(createInlineDecoration(monaco, lineNumber, 1, line.length + 1, `${cssClassPrefix}-frontmatter-delimiter`, `${cssClassPrefix}-line-frontmatter`));
        return;
    }

    const match = /^(\s*)([A-Za-z0-9_]+):\s*(.*)$/.exec(line);
    if (!match) {
        decorations.push(createLineDecoration(monaco, lineNumber, `${cssClassPrefix}-line-frontmatter`));
        return;
    }

    decorations.push(createLineDecoration(monaco, lineNumber, `${cssClassPrefix}-line-frontmatter`));
    const keyStart = match[1].length + 1;
    const keyEnd = keyStart + match[2].length;
    const valueStart = keyEnd + 2;
    decorations.push(createInlineDecoration(monaco, lineNumber, keyStart, keyEnd + 1, `${cssClassPrefix}-frontmatter-key`));
    decorations.push(createInlineDecoration(monaco, lineNumber, valueStart, line.length + 1, `${cssClassPrefix}-frontmatter-value`));
}

function decorateHeaderLine(monaco, decorations, lineNumber, line, prefix, lineClassName, tpsCatalog) {
    if (!line.startsWith(prefix + " [") || !line.endsWith("]")) {
        return false;
    }

    decorations.push(createLineDecoration(monaco, lineNumber, lineClassName));
    decorations.push(createInlineDecoration(monaco, lineNumber, 1, prefix.length + 1, `${cssClassPrefix}-header-mark`));
    decorations.push(createInlineDecoration(monaco, lineNumber, prefix.length + 2, prefix.length + 3, `${cssClassPrefix}-header-bracket`));
    decorations.push(createInlineDecoration(monaco, lineNumber, line.length, line.length + 1, `${cssClassPrefix}-header-bracket`));

    const contentStart = prefix.length + 3;
    const content = line.slice(contentStart - 1, -1);
    const parts = content.split("|");
    let segmentStart = contentStart;
    parts.forEach((part, index) => {
        const className = index === 0
            ? `${cssClassPrefix}-header-name`
            : part.startsWith("Speaker:")
                ? `${cssClassPrefix}-header-speaker`
                : /\d+\s*WPM$/i.test(part)
                    ? `${cssClassPrefix}-header-wpm`
                    : tpsCatalog.headerEmotionTags.has(part.toLowerCase())
                        ? `${cssClassPrefix}-header-emotion`
                        : `${cssClassPrefix}-header-meta`;
        decorations.push(createInlineDecoration(monaco, lineNumber, segmentStart, segmentStart + part.length, className));
        if (index < parts.length - 1) {
            decorations.push(createInlineDecoration(
                monaco,
                lineNumber,
                segmentStart + part.length,
                segmentStart + part.length + 1,
                `${cssClassPrefix}-header-separator`));
        }
        segmentStart += part.length + 1;
    });

    return true;
}

function decorateBodyLine(monaco, decorations, lineNumber, line, tpsCatalog) {
    const scopes = [createInlineScopeFrame("root", scopeKindRoot, createInlineRenderState())];
    let index = 0;

    for (const match of line.matchAll(new RegExp(tagPattern.source, "g"))) {
        const tagIndex = match.index ?? 0;
        if (tagIndex > index) {
            decorateInlineTextSegment(monaco, decorations, lineNumber, line.slice(index, tagIndex), index, scopes);
        }

        decorateInlineTag(monaco, decorations, lineNumber, match[0], tagIndex, scopes, tpsCatalog);
        index = tagIndex + match[0].length;
    }

    if (index < line.length) {
        decorateInlineTextSegment(monaco, decorations, lineNumber, line.slice(index), index, scopes);
    }
}

function decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, className) {
    decorations.push(createInlineDecoration(monaco, lineNumber, startColumn, endColumn, className));
}

function decorateInlineTextSegment(monaco, decorations, lineNumber, text, lineOffset, scopes) {
    if (!text) {
        return;
    }

    const currentScope = scopes[scopes.length - 1];
    if (currentScope.kind === scopeKindPronunciation) {
        currentScope.buffer.push({
            endColumn: lineOffset + text.length + 1,
            startColumn: lineOffset + 1
        });
        return;
    }

    let chunkStart = 0;
    for (let index = 0; index < text.length; index += 1) {
        const pauseLength = getPauseLength(text, index);
        if (pauseLength === 0) {
            continue;
        }

        decorateStyledTextSegment(
            monaco,
            decorations,
            lineNumber,
            lineOffset + chunkStart + 1,
            lineOffset + index + 1,
            currentScope.state);

        const pauseClassName = pauseLength === 2
            ? `${cssClassPrefix}-pause-long`
            : `${cssClassPrefix}-pause-short`;
        decorateRawRange(
            monaco,
            decorations,
            lineNumber,
            lineOffset + index + 1,
            lineOffset + index + pauseLength + 1,
            pauseClassName);

        index += pauseLength - 1;
        chunkStart = index + 1;
    }

    decorateStyledTextSegment(
        monaco,
        decorations,
        lineNumber,
        lineOffset + chunkStart + 1,
        lineOffset + text.length + 1,
        currentScope.state);
}

function decorateStyledTextSegment(monaco, decorations, lineNumber, startColumn, endColumn, state, extraClasses) {
    if (startColumn >= endColumn) {
        return;
    }

    const className = buildInlineStateClassName(state, extraClasses);
    if (!className) {
        return;
    }

    decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, className);
}

function decorateInlineTag(monaco, decorations, lineNumber, rawTag, tagIndex, scopes, tpsCatalog) {
    const inner = rawTag.slice(1, -1).trim();
    if (!inner) {
        return;
    }

    const startColumn = tagIndex + 1;
    const endColumn = startColumn + rawTag.length;
    if (inner.startsWith("/")) {
        const normalizedClosingName = normalizeTagName(inner.slice(1));
        const matchedScope = popInlineScope(scopes, normalizedClosingName);
        if (matchedScope?.kind === scopeKindPronunciation) {
            decoratePronunciationBuffer(monaco, decorations, lineNumber, matchedScope, scopes[scopes.length - 1].state);
        }

        decorateRawRange(
            monaco,
            decorations,
            lineNumber,
            startColumn,
            endColumn,
            createTagClassName(matchedScope?.tagClassName ?? resolveSemanticTagClassName(normalizedClosingName, tpsCatalog)));
        return;
    }

    const separatorIndex = inner.indexOf(":");
    const name = (separatorIndex >= 0 ? inner.slice(0, separatorIndex) : inner).trim();
    const argument = separatorIndex >= 0 ? inner.slice(separatorIndex + 1).trim() : null;
    const normalizedName = normalizeTagName(name);
    const currentState = scopes[scopes.length - 1].state;
    const semanticTagClassName = numericWpmRegex.test(name)
        ? tagSemanticClassNames.wpm
        : resolveSemanticTagClassName(normalizedName, tpsCatalog);

    if (numericWpmRegex.test(name)) {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindWpm, currentState, null, semanticTagClassName));
        return;
    }

    if (normalizedName === "pause") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        return;
    }

    if (normalizedName === "breath") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        return;
    }

    if (normalizedName === "edit_point" || normalizedName === "editpoint") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        return;
    }

    if (normalizedName === "phonetic" || normalizedName === "pronunciation") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindPronunciation, currentState, argument, semanticTagClassName));
        return;
    }

    if (normalizedName === "highlight") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            isHighlighted: true
        }, null, semanticTagClassName));
        return;
    }

    if (normalizedName === "emphasis" || normalizedName === "strong" || normalizedName === "bold") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            isEmphasis: true
        }, null, semanticTagClassName));
        return;
    }

    if (normalizedName === "stress") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            isStress: true
        }, null, semanticTagClassName));
        return;
    }

    if (normalizedName === "energy") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            energyClass: `${cssClassPrefix}-inline-energy`,
            energyValue: argument ?? null
        }, null, semanticTagClassName));
        return;
    }

    if (normalizedName === "melody") {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            melodyClass: `${cssClassPrefix}-inline-melody`,
            melodyValue: argument ?? null
        }, null, semanticTagClassName));
        return;
    }

    if (tpsCatalog.speedClasses.has(normalizedName)) {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            speedClass: tpsCatalog.speedClasses.get(normalizedName),
            speedValue: tpsCatalog.speedClasses.get(normalizedName) ? normalizedName : null
        }, null, semanticTagClassName));
        return;
    }

    if (tpsCatalog.volumeClasses.has(normalizedName)) {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            volumeClass: tpsCatalog.volumeClasses.get(normalizedName),
            volumeValue: normalizedName
        }, null, semanticTagClassName));
        return;
    }

    if (tpsCatalog.deliveryClasses.has(normalizedName)) {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            deliveryClass: tpsCatalog.deliveryClasses.get(normalizedName),
            deliveryValue: normalizedName
        }, null, semanticTagClassName));
        return;
    }

    if (tpsCatalog.articulationClasses.has(normalizedName)) {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            articulationClass: tpsCatalog.articulationClasses.get(normalizedName),
            articulationValue: normalizedName
        }, null, semanticTagClassName));
        return;
    }

    if (tpsCatalog.emotionClasses.has(normalizedName)) {
        decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
        scopes.push(createInlineScopeFrame(normalizedName, scopeKindStyle, {
            ...currentState,
            emotionClass: tpsCatalog.emotionClasses.get(normalizedName)
        }, null, semanticTagClassName));
        return;
    }

    decorateRawRange(monaco, decorations, lineNumber, startColumn, endColumn, createTagClassName(semanticTagClassName));
    scopes.push(createInlineScopeFrame(normalizedName, scopeKindNeutral, currentState, null, semanticTagClassName));
}

function decoratePronunciationBuffer(monaco, decorations, lineNumber, scope, parentState) {
    for (const chunk of scope.buffer) {
        decorateStyledTextSegment(
            monaco,
            decorations,
            lineNumber,
            chunk.startColumn,
            chunk.endColumn,
            parentState,
            [`${cssClassPrefix}-inline-pronunciation-word`]);
    }
}

function createInlineScopeFrame(name, kind, state, argument, tagClassName) {
    return {
        argument: argument ?? null,
        buffer: [],
        kind,
        name,
        state,
        tagClassName: tagClassName ?? null
    };
}

function createInlineRenderState() {
    return {
        articulationClass: null,
        articulationValue: null,
        deliveryClass: null,
        deliveryValue: null,
        energyClass: null,
        energyValue: null,
        emotionClass: null,
        isEmphasis: false,
        isHighlighted: false,
        isStress: false,
        melodyClass: null,
        melodyValue: null,
        speedClass: null,
        speedValue: null,
        volumeClass: null,
        volumeValue: null
    };
}

function popInlineScope(scopes, closingName) {
    if (scopes.length <= 1) {
        return null;
    }

    const poppedScopes = [];
    let matchedScope = null;
    while (scopes.length > 1) {
        const currentScope = scopes.pop();
        poppedScopes.push(currentScope);
        if (currentScope?.name.localeCompare(closingName, undefined, { sensitivity: "accent" }) === 0) {
            matchedScope = currentScope;
            break;
        }
    }

    if (!matchedScope) {
        for (let index = poppedScopes.length - 1; index >= 0; index -= 1) {
            scopes.push(poppedScopes[index]);
        }
    }

    return matchedScope;
}

function buildInlineStateClassName(state, extraClasses) {
    const classes = [];
    if (state.isEmphasis) {
        classes.push(`${cssClassPrefix}-inline-emphasis`);
    }
    if (state.isHighlighted) {
        classes.push(`${cssClassPrefix}-inline-highlight`);
    }
    if (state.isStress) {
        classes.push(`${cssClassPrefix}-inline-stress`);
    }
    if (state.emotionClass) {
        classes.push(state.emotionClass);
    }
    if (state.volumeClass) {
        classes.push(state.volumeClass);
    }
    if (state.deliveryClass) {
        classes.push(state.deliveryClass);
    }
    if (state.articulationClass) {
        classes.push(state.articulationClass);
    }
    if (state.energyClass) {
        classes.push(state.energyClass);
    }
    if (state.melodyClass) {
        classes.push(state.melodyClass);
    }
    if (state.speedClass) {
        classes.push(state.speedClass);
    }
    for (const extraClass of extraClasses ?? []) {
        if (extraClass) {
            classes.push(extraClass);
        }
    }

    return classes.join(" ");
}

function getPauseLength(text, index) {
    if (text[index] !== "/" || (index > 0 && text[index - 1] === "\\")) {
        return 0;
    }

    const length = index + 1 < text.length && text[index + 1] === "/" ? 2 : 1;
    const previous = index === 0 ? "\0" : text[index - 1];
    const nextIndex = index + length;
    const next = nextIndex >= text.length ? "\0" : text[nextIndex];

    if ((previous !== "\0" && !/\s/.test(previous)) || (next !== "\0" && !/\s/.test(next))) {
        return 0;
    }

    return length;
}

function createInlineDecoration(monaco, lineNumber, startColumn, endColumn, inlineClassName, lineClassName) {
    return {
        options: {
            inlineClassName,
            lineClassName
        },
        range: new monaco.Range(lineNumber, startColumn, lineNumber, endColumn)
    };
}

function createLineDecoration(monaco, lineNumber, lineClassName) {
    return {
        options: { isWholeLine: true, lineClassName },
        range: new monaco.Range(lineNumber, 1, lineNumber, 1)
    };
}

function revealSelectionInViewport(state, scrollType) {
    const selection = state.editor.getSelection();
    if (!selection) {
        return;
    }

    const lineNumber = selection.startLineNumber;
    const position = {
        column: selection.endColumn,
        lineNumber: selection.endLineNumber
    };

    if (typeof state.editor.revealLineInCenterIfOutsideViewport === "function") {
        state.editor.revealLineInCenterIfOutsideViewport(lineNumber, scrollType);
    }
    else if (typeof state.editor.revealLineInCenter === "function") {
        state.editor.revealLineInCenter(lineNumber, scrollType);
    }

    if (typeof state.editor.revealPositionInCenterIfOutsideViewport === "function") {
        state.editor.revealPositionInCenterIfOutsideViewport(position, scrollType);
    }
    else if (typeof state.editor.revealPositionInCenter === "function") {
        state.editor.revealPositionInCenter(position, scrollType);
    }

    if (typeof state.editor.revealRangeInCenterIfOutsideViewport === "function") {
        state.editor.revealRangeInCenterIfOutsideViewport(selection, scrollType);
    }
    else {
        state.editor.revealRangeInCenter(selection, scrollType);
    }
}

function inferSelectionDirection(start, end) {
    if (start === end) {
        return "none";
    }

    return start > end ? "backward" : "forward";
}

function normalizeSelectionDirection(direction, start, end) {
    if (direction === "backward" || direction === "forward") {
        return direction;
    }

    return inferSelectionDirection(start, end);
}

function captureEditorScrollPosition(state) {
    return {
        scrollLeft: state.editor.getScrollLeft(),
        scrollTop: state.editor.getScrollTop()
    };
}

function getPrimaryVisibleRange(state) {
    const visibleRanges = state.editor.getVisibleRanges();
    return visibleRanges.length ? visibleRanges[0] : null;
}

function resolvePreservedScrollPosition(state) {
    const currentScrollPosition = captureEditorScrollPosition(state);
    if (currentScrollPosition.scrollLeft > 0 || currentScrollPosition.scrollTop > 0) {
        return currentScrollPosition;
    }

    const fallbackScrollPosition = state.lastMeaningfulScrollPosition ?? currentScrollPosition;
    if (!fallbackScrollPosition || fallbackScrollPosition.scrollTop <= currentScrollPosition.scrollTop) {
        return currentScrollPosition;
    }

    const selection = createSelectionState(state);
    const visibleRange = getPrimaryVisibleRange(state);
    const selectionLine = Math.max(1, selection.line ?? 1);
    const selectionBelowVisibleRange = visibleRange
        ? selectionLine > visibleRange.endLineNumber
        : selectionLine > 1;

    return selectionBelowVisibleRange
        ? fallbackScrollPosition
        : currentScrollPosition;
}

function restoreEditorScrollPosition(state, scrollPosition) {
    state.editor.setScrollPosition(scrollPosition, state.monaco.editor.ScrollType.Immediate);
    if (scrollPosition.scrollLeft > 0 || scrollPosition.scrollTop > 0) {
        state.lastMeaningfulScrollPosition = scrollPosition;
    }
}

function replaceModelTextPreservingViewport(state, nextText, preservedScrollPosition) {
    const model = state.editor.getModel();
    if (!model || model.getValue() === nextText) {
        return false;
    }

    const stableScrollPosition = preservedScrollPosition ?? resolvePreservedScrollPosition(state);
    model.setValue(nextText);
    restoreEditorScrollPosition(state, stableScrollPosition);
    requestAnimationFrame(() => {
        const currentState = hostStates.get(state.host);
        if (!currentState) {
            return;
        }

        restoreEditorScrollPosition(currentState, stableScrollPosition);
    });
    return true;
}

function centerSelectionLineInViewport(state) {
    const selection = state.editor.getSelection();
    const scrollType = state.monaco.editor.ScrollType.Immediate;

    if (!selection) {
        state.editor.focus();
        return;
    }

    const lineNumber = Math.max(1, selection.positionLineNumber ?? selection.endLineNumber ?? selection.startLineNumber ?? 1);
    const position = {
        column: selection.positionColumn ?? selection.endColumn ?? selection.startColumn ?? 1,
        lineNumber
    };

    if (typeof state.editor.revealLineInCenter === "function") {
        state.editor.revealLineInCenter(lineNumber, scrollType);
    }
    else if (typeof state.editor.revealLineInCenterIfOutsideViewport === "function") {
        state.editor.revealLineInCenterIfOutsideViewport(lineNumber, scrollType);
    }

    if (typeof state.editor.revealPositionInCenter === "function") {
        state.editor.revealPositionInCenter(position, scrollType);
    }
    else if (typeof state.editor.revealPositionInCenterIfOutsideViewport === "function") {
        state.editor.revealPositionInCenterIfOutsideViewport(position, scrollType);
    }

    if (typeof state.editor.revealRangeInCenter === "function") {
        state.editor.revealRangeInCenter(selection, scrollType);
    }
    else if (typeof state.editor.revealRangeInCenterIfOutsideViewport === "function") {
        state.editor.revealRangeInCenterIfOutsideViewport(selection, scrollType);
    }

    state.editor.render();
    state.editor.focus();
}

function applySelection(state, start, end, revealSelection, selectionDirection, preservedScrollPositionOverride, focusEditor = true) {
    const model = state.editor.getModel();
    if (!model) {
        return;
    }

    const maxOffset = model.getValueLength();
    const safeStart = Math.max(0, Math.min(start, maxOffset));
    const safeEnd = Math.max(0, Math.min(end, maxOffset));
    const direction = normalizeSelectionDirection(selectionDirection, safeStart, safeEnd);
    const orderedStart = Math.min(safeStart, safeEnd);
    const orderedEnd = Math.max(safeStart, safeEnd);
    const anchorOffset = direction === "backward" ? orderedEnd : orderedStart;
    const focusOffset = direction === "backward" ? orderedStart : orderedEnd;
    const anchorPosition = model.getPositionAt(anchorOffset);
    const focusPosition = model.getPositionAt(focusOffset);
    const preservedScrollPosition = preservedScrollPositionOverride ?? resolvePreservedScrollPosition(state);

    state.editor.setSelection(new state.monaco.Selection(
        anchorPosition.lineNumber,
        anchorPosition.column,
        focusPosition.lineNumber,
        focusPosition.column));

    if (revealSelection) {
        const scrollType = state.monaco.editor.ScrollType.Immediate;
        if (focusEditor) {
            state.editor.focus();
        }
        revealSelectionInViewport(state, scrollType);
        state.editor.render();
        requestAnimationFrame(() => {
            const currentState = hostStates.get(state.host);
            if (!currentState) {
                return;
            }

            revealSelectionInViewport(currentState, scrollType);
            currentState.editor.render();
        });
    }
    else {
        if (focusEditor) {
            state.editor.focus();
        }
        restoreEditorScrollPosition(state, preservedScrollPosition);
    }

    syncProxyFromEditor(state);
}

function syncProxyFromEditor(state) {
    const selection = createSelectionState(state);
    const proxyStart = Math.min(selection.start, selection.end);
    const proxyEnd = Math.max(selection.start, selection.end);
    const proxyDirection = normalizeSelectionDirection(selection.direction, selection.start, selection.end);
    state.proxy.value = state.editor.getValue();
    state.suppressProxySelection = true;
    state.proxy.selectionStart = proxyStart;
    state.proxy.selectionEnd = proxyEnd;
    try {
        state.proxy.selectionDirection = proxyDirection;
    }
    catch {
    }
    state.suppressProxySelection = false;
}

function dispatchProxyChangedEvent(state) {
    state.proxy.dispatchEvent(new CustomEvent(state.options.proxyChangedEventName, {
        detail: {
            selectionEnd: state.proxy.selectionEnd ?? 0,
            selectionStart: state.proxy.selectionStart ?? 0,
            textLength: state.proxy.value.length
        }
    }));
}

function createSelectionState(state) {
    const selection = state.editor.getSelection();
    const model = state.editor.getModel();
    if (!selection || !model) {
        return createEmptySelectionState();
    }

    const anchorPosition = {
        column: selection.selectionStartColumn,
        lineNumber: selection.selectionStartLineNumber
    };
    const focusPosition = {
        column: selection.positionColumn,
        lineNumber: selection.positionLineNumber
    };
    const start = model.getOffsetAt(anchorPosition);
    const end = model.getOffsetAt(focusPosition);
    const direction = normalizeSelectionDirection(selection.getDirection?.(), start, end);
    const visiblePosition = state.editor.getScrolledVisiblePosition(selection.getStartPosition());
    const rawToolbarTop = visiblePosition
        ? Math.max(floatingToolbarMinimumTop, (visiblePosition.top ?? 0) + floatingToolbarSelectionOffset)
        : floatingToolbarMinimumTop;
    const rawToolbarLeft = visiblePosition ? (visiblePosition.left ?? 0) + ((visiblePosition.width ?? 0) / 2) : 0;
    const toolbarTop = Number.isFinite(rawToolbarTop) ? rawToolbarTop : floatingToolbarMinimumTop;
    const toolbarLeft = Number.isFinite(rawToolbarLeft) ? rawToolbarLeft : 0;

    return {
        column: focusPosition.column,
        direction,
        end,
        line: focusPosition.lineNumber,
        start,
        toolbarLeft,
        toolbarTop
    };
}

function createEmptySelectionState() {
    return {
        column: 1,
        direction: "none",
        end: 0,
        line: 1,
        start: 0,
        toolbarLeft: 0,
        toolbarTop: floatingToolbarMinimumTop
    };
}
