(function () {
    const deferredOverlayHighlightDelayMs = 180;
    const deferredOverlayHighlightMaximumLength = 100000;
    const defaultMeasuredLineHeightPx = 32;
    const offscreenMirrorLeftPx = -99999;
    const interactiveOverlayLargeDraftThreshold = 16000;
    const plainTextInputCssClass = "ed-source-input-plain";
    const plainTextOverlayCssClass = "ed-source-highlight-plain";
    const selectionViewportMarginLineCount = 2;
    const floatingToolbarMinimumTopCssVariable = "--ed-floatbar-min-top";
    const textareaMirrorCaretMarkerWidthPx = 1;
    const textareaMirrorCaretMarkerText = "\u200b";
    const textareaMirrorStyleProperties = [
        "whiteSpace",
        "wordBreak",
        "overflowWrap",
        "fontFamily",
        "fontSize",
        "fontWeight",
        "lineHeight",
        "letterSpacing",
        "fontVariantLigatures",
        "tabSize",
        "padding",
        "border",
        "boxSizing"
    ];
    const editorSurfaceNamespace = "EditorSurfaceInterop";
    const frontMatterEntryRegex = /^(?<key>[A-Za-z0-9_]+):\s*(?<value>.+)$/;
    const numericWpmRegex = /^(?<wpm>\d+)\s*WPM$/i;
    const tagPattern = /\[[^[\]]+\]/g;
    const emptySourceMarkup = "<div class=\"ed-src-line ed-src-line-empty\">Start writing in TPS.</div>";
    const scopeKindRoot = "root";
    const scopeKindNeutral = "neutral";
    const scopeKindStyle = "style";
    const scopeKindWpm = "wpm";
    const scopeKindPronunciation = "pronunciation";
    const renderedLengthDataAttribute = "renderedLength";
    const editorSurfaceStates = new WeakMap();
    const overlaySurfaceStates = new WeakMap();
    const emotionClasses = new Map([
        ["warm", "mk-emo-warm"],
        ["concerned", "mk-emo-concerned"],
        ["focused", "mk-emo-focused"],
        ["motivational", "mk-emo-motivational"],
        ["neutral", "mk-emo-neutral"],
        ["urgent", "mk-emo-urgent"],
        ["happy", "mk-emo-happy"],
        ["excited", "mk-emo-excited"],
        ["sad", "mk-emo-sad"],
        ["calm", "mk-emo-calm"],
        ["energetic", "mk-emo-energetic"],
        ["professional", "mk-emo-professional"]
    ]);
    const volumeClasses = new Map([
        ["loud", "mk-vol-loud"],
        ["soft", "mk-vol-soft"],
        ["whisper", "mk-vol-whisper"]
    ]);
    const deliveryClasses = new Map([
        ["aside", "mk-del-aside"],
        ["rhetorical", "mk-del-rhetorical"],
        ["sarcasm", "mk-del-sarcasm"],
        ["building", "mk-del-building"]
    ]);
    const speedClasses = new Map([
        ["xslow", "mk-xslow"],
        ["slow", "mk-slow"],
        ["normal", null],
        ["fast", "mk-fast"],
        ["xfast", "mk-xfast"]
    ]);

    window[editorSurfaceNamespace] = {
        initialize(textarea, overlay, cueContracts) {
            if (!textarea || !overlay) {
                return false;
            }

            const normalizedCueContracts = normalizeCueContracts(cueContracts);
            const existingState = editorSurfaceStates.get(textarea);
            if (existingState) {
                existingState.overlay = overlay;
                existingState.cueContracts = normalizedCueContracts;
                overlaySurfaceStates.set(overlay, existingState);
                cancelScheduledOverlayRender(existingState);
                applyInteractivePlainTextMode(textarea, overlay, shouldUseInteractivePlainTextRender(textarea.value));
                renderSurfaceOverlay(overlay, textarea.value, false, normalizedCueContracts);
                return true;
            }

            const state = {
                overlay,
                cueContracts: normalizedCueContracts,
                pendingFullRenderTimeoutId: 0,
                pendingRenderFrameId: 0,
                onInput() {
                    scheduleOverlayRender(textarea, state);
                }
            };

            textarea.addEventListener("input", state.onInput);
            editorSurfaceStates.set(textarea, state);
            overlaySurfaceStates.set(overlay, state);
            applyInteractivePlainTextMode(textarea, overlay, shouldUseInteractivePlainTextRender(textarea.value));
            renderSurfaceOverlay(overlay, textarea.value, false, normalizedCueContracts);
            return true;
        },

        renderOverlay(overlay, text, cueContracts) {
            const state = overlaySurfaceStates.get(overlay);
            if (state) {
                state.cueContracts = normalizeCueContracts(cueContracts) || state.cueContracts;
                cancelScheduledOverlayRender(state);
            }

            renderSurfaceOverlay(overlay, text, false, state?.cueContracts);
        },

        syncScroll(textarea, overlay) {
            if (!textarea || !overlay) {
                return;
            }

            overlay.scrollTop = textarea.scrollTop;
            overlay.scrollLeft = textarea.scrollLeft;
        },

        getSelectionState(textarea) {
            return createEditorSelectionState(textarea);
        },

        setSelection(textarea, start, end) {
            if (!textarea) {
                return createEmptyEditorSelectionState();
            }

            focusTextareaWithoutScroll(textarea);
            textarea.setSelectionRange(start, end);
            ensureSelectionRangeVisible(textarea, start, end);
            syncOverlayForTextarea(textarea);
            return createEditorSelectionState(textarea);
        }
    };

    function createEmptyEditorSelectionState() {
        return {
            start: 0,
            end: 0,
            line: 1,
            column: 1,
            toolbarTop: 0,
            toolbarLeft: 0
        };
    }

    function createEditorSelectionState(textarea) {
        if (!textarea) {
            return createEmptyEditorSelectionState();
        }

        const start = textarea.selectionStart || 0;
        const end = textarea.selectionEnd || start;
        const location = getTextLocation(textarea.value, start);
        const hasSelection = start !== end;
        const coords = hasSelection
            ? measureTextareaRangeGeometry(textarea, start, end)
            : createCollapsedToolbarGeometry(textarea);

        return {
            start,
            end,
            line: location.line,
            column: location.column,
            toolbarTop: Math.max(getFloatingToolbarMinimumTopPx(textarea), coords.top),
            toolbarLeft: coords.left
        };
    }

    function createCollapsedToolbarGeometry(textarea) {
        return {
            top: getFloatingToolbarMinimumTopPx(textarea),
            left: textarea ? textarea.offsetLeft : 0
        };
    }

    function focusTextareaWithoutScroll(textarea) {
        if (!textarea || typeof textarea.focus !== "function") {
            return;
        }

        try {
            textarea.focus({ preventScroll: true });
        }
        catch {
            textarea.focus();
        }
    }

    function getTextLocation(text, index) {
        const prefix = (text || "").slice(0, index);
        const prefixLines = prefix.split("\n");
        return {
            line: prefixLines.length,
            column: (prefixLines[prefixLines.length - 1] || "").length + 1
        };
    }

    function measureTextareaRangeGeometry(textarea, start, end) {
        const value = textarea.value;
        return measureWithTextareaMirror(textarea, (mirror, style) => {
            const selection = document.createElement("span");

            mirror.textContent = value.slice(0, start);
            selection.textContent = value.slice(start, end) || " ";
            mirror.appendChild(selection);

            const mirrorRect = mirror.getBoundingClientRect();
            const selectionRect = selection.getBoundingClientRect();
            const selectionRects = selection.getClientRects();
            const firstRect = selectionRects[0] || selectionRect;

            return {
                top: textarea.offsetTop + firstRect.top - mirrorRect.top - textarea.scrollTop - getLineTopInsetPx(style),
                left: textarea.offsetLeft + selectionRect.left - mirrorRect.left - textarea.scrollLeft + (selectionRect.width / 2)
            };
        });
    }

    function measureTextareaContentGeometry(textarea, start, end) {
        if (!textarea) {
            return null;
        }

        const value = textarea.value || "";
        const normalizedStart = Math.max(0, Math.min(start, value.length));
        const normalizedEnd = Math.max(normalizedStart, Math.min(end, value.length));

        return measureWithTextareaMirror(textarea, mirror => {
            const marker = document.createElement("span");
            const selectedText = value.slice(normalizedStart, normalizedEnd);

            mirror.textContent = value.slice(0, normalizedStart);
            if (selectedText.length > 0) {
                marker.textContent = selectedText;
            }
            else {
                marker.textContent = textareaMirrorCaretMarkerText;
                marker.style.display = "inline-block";
                marker.style.width = `${textareaMirrorCaretMarkerWidthPx}px`;
            }

            mirror.appendChild(marker);

            const mirrorRect = mirror.getBoundingClientRect();
            const markerRects = marker.getClientRects();
            const markerRect = marker.getBoundingClientRect();
            const firstRect = markerRects[0] || markerRect;
            const lastRect = markerRects[markerRects.length - 1] || markerRect;

            return {
                top: firstRect.top - mirrorRect.top,
                bottom: lastRect.bottom - mirrorRect.top
            };
        });
    }

    function getLineTopInsetPx(style) {
        const lineHeight = Number.parseFloat(style.lineHeight);
        const fontSize = Number.parseFloat(style.fontSize);
        if (!Number.isFinite(lineHeight) || !Number.isFinite(fontSize)) {
            return 0;
        }

        return Math.max(0, lineHeight - fontSize);
    }

    function getSelectionViewportMarginPx(textarea) {
        const lineHeight = textarea
            ? Number.parseFloat(window.getComputedStyle(textarea).lineHeight)
            : Number.NaN;
        const measuredLineHeight = Number.isFinite(lineHeight)
            ? lineHeight
            : defaultMeasuredLineHeightPx;

        return measuredLineHeight * selectionViewportMarginLineCount;
    }

    function ensureSelectionRangeVisible(textarea, start, end) {
        if (!textarea) {
            return;
        }

        const geometry = measureTextareaContentGeometry(textarea, start, end);
        if (!geometry) {
            return;
        }

        const margin = getSelectionViewportMarginPx(textarea);
        const viewportTop = textarea.scrollTop;
        const viewportBottom = viewportTop + textarea.clientHeight;
        const requiredTop = Math.max(0, geometry.top - margin);
        const requiredBottom = geometry.bottom + margin;

        if (requiredTop < viewportTop) {
            textarea.scrollTop = requiredTop;
            return;
        }

        if (requiredBottom > viewportBottom) {
            textarea.scrollTop = Math.max(0, requiredBottom - textarea.clientHeight);
        }
    }

    function getFloatingToolbarMinimumTopPx(textarea) {
        const value = Number.parseFloat(
            window.getComputedStyle(textarea).getPropertyValue(floatingToolbarMinimumTopCssVariable));

        return Number.isFinite(value)
            ? value
            : 0;
    }

    function measureWithTextareaMirror(textarea, measure) {
        const style = window.getComputedStyle(textarea);
        const mirror = createTextareaMirror(textarea, style);
        document.body.appendChild(mirror);

        try {
            return measure(mirror, style);
        }
        finally {
            if (mirror.parentNode) {
                mirror.parentNode.removeChild(mirror);
            }
        }
    }

    function createTextareaMirror(textarea, style) {
        const mirror = document.createElement("div");

        mirror.style.position = "absolute";
        mirror.style.visibility = "hidden";
        mirror.style.width = `${textarea.clientWidth}px`;
        mirror.style.left = `${offscreenMirrorLeftPx}px`;
        mirror.style.top = "0";

        for (const property of textareaMirrorStyleProperties) {
            mirror.style[property] = style[property];
        }

        return mirror;
    }

    function syncOverlayForTextarea(textarea) {
        const state = editorSurfaceStates.get(textarea);
        if (!state || !state.overlay) {
            return;
        }

        window[editorSurfaceNamespace].syncScroll(textarea, state.overlay);
    }

    function renderSurfaceOverlay(overlay, text, interactivePlainTextMode, cueContracts) {
        if (!overlay) {
            return;
        }

        overlay.innerHTML = interactivePlainTextMode
            ? renderPlainTextOverlay(text)
            : renderSourceHighlight(text, cueContracts);
        updateRenderedLengthMarker(overlay, text);
    }

    function updateRenderedLengthMarker(overlay, text) {
        if (!overlay) {
            return;
        }

        overlay.dataset[renderedLengthDataAttribute] = String((text || "").length);
    }

    function cancelScheduledOverlayRender(state) {
        if (!state) {
            return;
        }

        if (state.pendingRenderFrameId) {
            window.cancelAnimationFrame(state.pendingRenderFrameId);
            state.pendingRenderFrameId = 0;
        }

        if (state.pendingFullRenderTimeoutId) {
            window.clearTimeout(state.pendingFullRenderTimeoutId);
            state.pendingFullRenderTimeoutId = 0;
        }
    }

    function renderScheduledOverlay(textarea, state) {
        if (!textarea || !state || !state.overlay) {
            return;
        }

        const text = textarea.value;
        const useInteractivePlainTextMode = shouldUseInteractivePlainTextRender(text);
        applyInteractivePlainTextMode(textarea, state.overlay, useInteractivePlainTextMode);

        if (useInteractivePlainTextMode) {
            cancelDeferredHighlightRender(state);
            updateRenderedLengthMarker(state.overlay, text);
            window[editorSurfaceNamespace].syncScroll(textarea, state.overlay);
            return;
        }

        renderSurfaceOverlay(state.overlay, text, false, state.cueContracts);
        window[editorSurfaceNamespace].syncScroll(textarea, state.overlay);
        scheduleDeferredHighlightRender(textarea, state, false);
    }

    function scheduleOverlayRender(textarea, state) {
        if (!textarea || !state || state.pendingRenderFrameId) {
            return;
        }

        state.pendingRenderFrameId = window.requestAnimationFrame(() => {
            state.pendingRenderFrameId = 0;
            renderScheduledOverlay(textarea, state);
        });
    }

    function scheduleDeferredHighlightRender(textarea, state, useInteractivePlainTextMode) {
        if (!state) {
            return;
        }

        const text = textarea?.value || "";
        if (!shouldUseDeferredHighlightRender(text, useInteractivePlainTextMode)) {
            if (state.pendingFullRenderTimeoutId) {
                window.clearTimeout(state.pendingFullRenderTimeoutId);
                state.pendingFullRenderTimeoutId = 0;
            }

            return;
        }

        if (state.pendingFullRenderTimeoutId) {
            window.clearTimeout(state.pendingFullRenderTimeoutId);
        }

        state.pendingFullRenderTimeoutId = window.setTimeout(() => {
            state.pendingFullRenderTimeoutId = 0;

            if (!textarea || !state.overlay) {
                return;
            }

            renderSurfaceOverlay(state.overlay, textarea.value, false, state.cueContracts);
            window[editorSurfaceNamespace].syncScroll(textarea, state.overlay);
        }, deferredOverlayHighlightDelayMs);
    }

    function cancelDeferredHighlightRender(state) {
        if (!state || !state.pendingFullRenderTimeoutId) {
            return;
        }

        window.clearTimeout(state.pendingFullRenderTimeoutId);
        state.pendingFullRenderTimeoutId = 0;
    }

    function shouldUseInteractivePlainTextRender(text) {
        return (text || "").length >= interactiveOverlayLargeDraftThreshold;
    }

    function shouldUseDeferredHighlightRender(text, useInteractivePlainTextMode) {
        return useInteractivePlainTextMode
            && (text || "").length <= deferredOverlayHighlightMaximumLength;
    }

    function applyInteractivePlainTextMode(textarea, overlay, enabled) {
        if (textarea) {
            textarea.classList.toggle(plainTextInputCssClass, enabled);
        }

        if (overlay) {
            overlay.classList.toggle(plainTextOverlayCssClass, enabled);
        }
    }

    function renderPlainTextOverlay(text) {
        if (!text || !text.trim()) {
            return emptySourceMarkup;
        }

        const normalizedText = text.replace(/\r\n/g, "\n");
        return normalizedText
            .split("\n")
            .map(line => line.trim()
                ? wrapLine("ed-src-line", encodeOrSpace(line))
                : wrapLine("ed-src-line ed-src-line-empty", "&nbsp;"))
            .join("");
    }

    function renderSourceHighlight(text, cueContracts) {
        if (!text || !text.trim()) {
            return emptySourceMarkup;
        }

        const normalizedText = text.replace(/\r\n/g, "\n");
        const lines = normalizedText.split("\n");
        let inFrontMatter = lines.length > 0 && lines[0] === "---";
        const parts = [];

        for (let index = 0; index < lines.length; index += 1) {
            const line = lines[index];
            if (inFrontMatter) {
                parts.push(renderFrontMatterLine(line));
                if (index > 0 && line === "---") {
                    inFrontMatter = false;
                }
                continue;
            }

            parts.push(renderBodyLine(line, cueContracts));
        }

        return parts.join("");
    }

    function renderFrontMatterLine(line) {
        if (line === "---") {
            return wrapLine("ed-src-line ed-src-line-frontmatter", "<span class=\"ed-src-frontmatter-delimiter\">---</span>");
        }

        const match = frontMatterEntryRegex.exec(line);
        if (!match || !match.groups) {
            return wrapLine("ed-src-line ed-src-line-frontmatter", encodeOrSpace(line));
        }

        return wrapLine(
            "ed-src-line ed-src-line-frontmatter",
            `<span class="ed-src-frontmatter-key">${encodeHtml(match.groups.key)}</span>: <span class="ed-src-frontmatter-value">${encodeHtml(match.groups.value)}</span>`);
    }

    function renderBodyLine(line, cueContracts) {
        if (!line.trim()) {
            return wrapLine("ed-src-line ed-src-line-empty", "&nbsp;");
        }

        const header = tryParseHeaderLine(line);
        if (header) {
            return wrapLine(
                header.isSegment
                    ? "ed-src-line ed-src-line-segment"
                    : "ed-src-line ed-src-line-block",
                buildHeaderMarkup(header.hashToken, header.content, header.isSegment, header.hasClosingBracket) +
                renderHeaderTrailingText(header.trailingText, cueContracts));
        }

        return wrapLine("ed-src-line", renderInlineMarkup(line, cueContracts));
    }

    function tryParseHeaderLine(line) {
        if (!line) {
            return null;
        }

        const hashToken = line.startsWith("###")
            ? "###"
            : line.startsWith("##")
                ? "##"
                : null;

        if (!hashToken) {
            return null;
        }

        const body = line.slice(hashToken.length).trimStart();
        if (!body.startsWith("[")) {
            return null;
        }

        const closingBracketIndex = body.indexOf("]");
        const hasClosingBracket = closingBracketIndex >= 0;
        const content = hasClosingBracket
            ? body.slice(1, closingBracketIndex)
            : body.slice(1);
        const trailingText = hasClosingBracket
            ? body.slice(closingBracketIndex + 1)
            : "";

        return {
            content,
            hashToken,
            hasClosingBracket,
            isSegment: hashToken === "##",
            trailingText
        };
    }

    function buildHeaderMarkup(hashToken, content, isSegment, hasClosingBracket) {
        const parts = content.split("|").map(part => part.trim());
        const fragments = [
            `<span class="h-mark">${encodeHtml(hashToken)} </span><span class="h-br">[</span>`
        ];

        for (let index = 0; index < parts.length; index += 1) {
            if (index > 0) {
                fragments.push("<span class=\"h-sep\">|</span>");
            }

            const cssClass = index === 0
                ? "h-name"
                : index === 1
                    ? "h-wpm"
                    : index === 2
                        ? "h-emo"
                        : isSegment
                            ? "h-wpm"
                            : "h-emo";

            fragments.push(`<span class="${cssClass}">${encodeHtml(parts[index])}</span>`);
        }

        if (hasClosingBracket) {
            fragments.push("<span class=\"h-br\">]</span>");
        }

        return fragments.join("");
    }

    function renderHeaderTrailingText(text, cueContracts) {
        if (!text) {
            return "";
        }

        return text.trim()
            ? renderInlineMarkup(text, cueContracts)
            : encodeOrSpace(text);
    }

    function renderInlineMarkup(content, cueContracts) {
        if (!content || !content.trim()) {
            return "<span class=\"mk-muted\">Add script content here.</span>";
        }

        const builder = [];
        const scopes = [createScopeFrame("root", scopeKindRoot, createRenderState())];
        let index = 0;

        for (const match of content.matchAll(new RegExp(tagPattern.source, "g"))) {
            const tagIndex = match.index || 0;
            const rawTag = match[0];
            if (tagIndex > index) {
                appendText(content.slice(index, tagIndex));
            }

            handleTag(rawTag);
            index = tagIndex + rawTag.length;
        }

        if (index < content.length) {
            appendText(content.slice(index));
        }

        return builder.join("");

        function appendText(text) {
            if (!text) {
                return;
            }

            const currentScope = getCurrentScope();
            if (currentScope.kind === scopeKindPronunciation) {
                currentScope.buffer.push(text);
                return;
            }

            let chunkStart = 0;
            for (let textIndex = 0; textIndex < text.length; textIndex += 1) {
                const pauseLength = getPauseLength(text, textIndex);
                if (pauseLength === 0) {
                    continue;
                }

                appendStyledText(text.slice(chunkStart, textIndex));
                builder.push(`<span class="mk-pause">${encodeHtml(text.slice(textIndex, textIndex + pauseLength))}</span>`);
                textIndex += pauseLength - 1;
                chunkStart = textIndex + 1;
            }

            if (chunkStart < text.length) {
                appendStyledText(text.slice(chunkStart));
            }
        }

        function appendStyledText(text) {
            if (!text) {
                return;
            }

            const encoded = encodeHtml(text).replace(/\n/g, "<br>");
            const currentState = getCurrentScope().state;
            if (!requiresStyledSpan(currentState)) {
                builder.push(encoded);
                return;
            }

            appendStyledSpan(encoded, currentState);
        }

        function handleTag(rawTag) {
            const inner = rawTag.slice(1, -1).trim();
            if (!inner) {
                return;
            }

            if (inner.startsWith("/")) {
                closeTag(rawTag, inner.slice(1).trim());
                return;
            }

            const separatorIndex = inner.indexOf(":");
            const name = (separatorIndex >= 0 ? inner.slice(0, separatorIndex) : inner).trim();
            const argument = separatorIndex >= 0 ? inner.slice(separatorIndex + 1).trim() : null;
            const numericMatch = numericWpmRegex.exec(name);
            const currentState = getCurrentScope().state;

            if (numericMatch && numericMatch.groups) {
                appendTag(rawTag);
                builder.push(`<span class="mk-wpm-badge">${encodeHtml(numericMatch.groups.wpm)}WPM</span> `);
                pushScope(name, scopeKindWpm, currentState);
                return;
            }

            if (equalsIgnoreCase(name, "pause")) {
                appendStandalone("mk-special", rawTag);
                return;
            }

            if (equalsIgnoreCase(name, "breath")) {
                appendStandalone("mk-breath", rawTag);
                return;
            }

            if (equalsIgnoreCase(name, "edit_point") || equalsIgnoreCase(name, "editpoint")) {
                appendStandalone("mk-edit", rawTag);
                return;
            }

            if (equalsIgnoreCase(name, "phonetic") || equalsIgnoreCase(name, "pronunciation")) {
                appendTag(rawTag);
                pushScope(name, scopeKindPronunciation, currentState, argument);
                return;
            }

            if (equalsIgnoreCase(name, "highlight")) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, { ...currentState, isHighlighted: true });
                return;
            }

            if (equalsIgnoreCase(name, "emphasis") || equalsIgnoreCase(name, "strong") || equalsIgnoreCase(name, "bold")) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, { ...currentState, isEmphasis: true });
                return;
            }

            if (equalsIgnoreCase(name, "stress")) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, { ...currentState, isStress: true });
                return;
            }

            const speedClass = speedClasses.get(name.toLowerCase());
            if (speedClasses.has(name.toLowerCase())) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, {
                    ...currentState,
                    speedClass,
                    speedValue: speedClass ? name.toLowerCase() : null
                });
                return;
            }

            const volumeClass = volumeClasses.get(name.toLowerCase());
            if (volumeClass) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, {
                    ...currentState,
                    volumeClass,
                    volumeValue: name.toLowerCase()
                });
                return;
            }

            const deliveryClass = deliveryClasses.get(name.toLowerCase());
            if (deliveryClass) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, {
                    ...currentState,
                    deliveryClass,
                    deliveryValue: name.toLowerCase()
                });
                return;
            }

            const emotionClass = emotionClasses.get(name.toLowerCase());
            if (emotionClass) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, { ...currentState, emotionClass });
                return;
            }

            appendTag(rawTag);
            pushScope(name, scopeKindNeutral, currentState);
        }

        function closeTag(rawTag, closingName) {
            if (scopes.length <= 1) {
                appendTag(rawTag);
                return;
            }

            const matchedScope = popScope(closingName);
            if (matchedScope && matchedScope.kind === scopeKindPronunciation) {
                appendPronunciationPayload(matchedScope, getCurrentScope().state);
            }

            appendTag(rawTag);
        }

        function popScope(closingName) {
            if (scopes.length <= 1) {
                return null;
            }

            const poppedScopes = [];
            let matchedScope = null;

            while (scopes.length > 1) {
                const currentScope = scopes.pop();
                poppedScopes.push(currentScope);
                if (currentScope && equalsIgnoreCase(currentScope.name, closingName)) {
                    matchedScope = currentScope;
                    break;
                }
            }

            if (!matchedScope) {
                for (let reverseIndex = poppedScopes.length - 1; reverseIndex >= 0; reverseIndex -= 1) {
                    scopes.push(poppedScopes[reverseIndex]);
                }
            }

            return matchedScope;
        }

        function appendPronunciationPayload(scope, parentState) {
            const guide = scope.argument ? encodeHtml(scope.argument) : "";
            const spokenText = scope.buffer.length === 0
                ? ""
                : encodeHtml(normalizePronunciationText(scope.buffer.join("")));

            builder.push(`<span class="mk-phonetic">${guide}</span> `);
            appendStyledSpan(spokenText, parentState, ["mk-phonetic-word"]);
        }

        function appendTag(value) {
            builder.push(`<span class="mk-tag">${encodeHtml(value)}</span>`);
        }

        function appendStandalone(cssClass, value) {
            builder.push(`<span class="${cssClass}">${encodeHtml(value)}</span>`);
        }

        function pushScope(name, kind, state, argument) {
            scopes.push(createScopeFrame(name, kind, state, argument));
        }

        function getCurrentScope() {
            return scopes[scopes.length - 1];
        }

        function appendStyledSpan(encodedText, state, extraClasses) {
            if (!requiresStyledSpan(state, extraClasses)) {
                builder.push(encodedText);
                return;
            }

            builder.push(`<span${buildHtmlAttributes(state, cueContracts, extraClasses)}>${encodedText}</span>`);
        }
    }

    function createScopeFrame(name, kind, state, argument) {
        return {
            name,
            kind,
            state,
            argument: argument || null,
            buffer: []
        };
    }

    function createRenderState() {
        return {
            emotionClass: null,
            volumeValue: null,
            volumeClass: null,
            deliveryValue: null,
            deliveryClass: null,
            speedValue: null,
            speedClass: null,
            isEmphasis: false,
            isHighlighted: false,
            isStress: false
        };
    }

    function normalizeCueContracts(rawContracts) {
        return {
            volumeAttributeName: rawContracts?.volumeAttributeName || "",
            deliveryAttributeName: rawContracts?.deliveryAttributeName || "",
            speedAttributeName: rawContracts?.speedAttributeName || "",
            stressAttributeName: rawContracts?.stressAttributeName || "",
            stressAttributeValue: rawContracts?.stressAttributeValue || ""
        };
    }

    function buildCssClass(state, extraClasses) {
        const classes = [];
        if (state.isEmphasis) {
            classes.push("mk-em");
        }
        if (state.isHighlighted) {
            classes.push("mk-hl");
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
        if (state.speedClass) {
            classes.push(state.speedClass);
        }
        if (state.isStress) {
            classes.push("mk-stress");
        }
        for (const extraClass of extraClasses || []) {
            if (extraClass) {
                classes.push(extraClass);
            }
        }

        return classes.join(" ");
    }

    function requiresStyledSpan(state, extraClasses) {
        return Boolean(
            state.isEmphasis ||
            state.isHighlighted ||
            state.isStress ||
            state.emotionClass ||
            state.volumeClass ||
            state.deliveryClass ||
            state.speedClass ||
            (extraClasses || []).some(value => Boolean(value)));
    }

    function buildHtmlAttributes(state, cueContracts, extraClasses) {
        const attributes = [];
        const cssClass = buildCssClass(state, extraClasses);
        appendHtmlAttribute(attributes, "class", cssClass);
        appendHtmlAttribute(attributes, cueContracts?.volumeAttributeName, state.volumeValue);
        appendHtmlAttribute(attributes, cueContracts?.deliveryAttributeName, state.deliveryValue);
        appendHtmlAttribute(attributes, cueContracts?.speedAttributeName, state.speedValue);
        if (state.isStress) {
            appendHtmlAttribute(attributes, cueContracts?.stressAttributeName, cueContracts?.stressAttributeValue);
        }

        return attributes.join("");
    }

    function appendHtmlAttribute(attributes, name, value) {
        if (!name || !value) {
            return;
        }

        attributes.push(` ${name}="${encodeHtml(value)}"`);
    }

    function getPauseLength(text, index) {
        if (text[index] !== "/") {
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

    function normalizePronunciationText(value) {
        return value.trim().replace(/\s+/g, " ");
    }

    function equalsIgnoreCase(left, right) {
        return left.localeCompare(right, undefined, { sensitivity: "accent" }) === 0;
    }

    function encodeOrSpace(line) {
        return line ? encodeHtml(line) : "&nbsp;";
    }

    function wrapLine(cssClass, markup) {
        return `<div class="${cssClass}">${markup}</div>`;
    }

    function encodeHtml(value) {
        return (value || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;");
    }
})();
