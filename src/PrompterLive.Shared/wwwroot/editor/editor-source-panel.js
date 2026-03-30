(function () {
    const offscreenMirrorLeftPx = -99999;
    const floatingToolbarMinimumTopCssVariable = "--ed-floatbar-min-top";
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
    const blockHeaderRegex = /^(?<hash>###)\s+\[(?<content>.+)\]\s*$/;
    const frontMatterEntryRegex = /^(?<key>[A-Za-z0-9_]+):\s*(?<value>.+)$/;
    const segmentHeaderRegex = /^(?<hash>##)\s+\[(?<content>.+)\]\s*$/;
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
    const colorClasses = new Map([
        ["red", "mk-color-red"],
        ["green", "mk-color-green"],
        ["blue", "mk-color-blue"],
        ["yellow", "mk-color-yellow"],
        ["orange", "mk-color-orange"],
        ["purple", "mk-color-purple"],
        ["cyan", "mk-color-cyan"],
        ["magenta", "mk-color-magenta"],
        ["pink", "mk-color-pink"],
        ["teal", "mk-color-teal"],
        ["white", "mk-color-white"],
        ["gray", "mk-color-gray"]
    ]);
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
    const speedClasses = new Map([
        ["xslow", "mk-xslow"],
        ["slow", "mk-slow"],
        ["normal", null],
        ["fast", "mk-fast"],
        ["xfast", "mk-xfast"]
    ]);

    window[editorSurfaceNamespace] = {
        initialize(textarea, overlay) {
            if (!textarea || !overlay) {
                return false;
            }

            const existingState = editorSurfaceStates.get(textarea);
            if (existingState) {
                existingState.overlay = overlay;
                renderSurfaceOverlay(overlay, textarea.value);
                return true;
            }

            const state = {
                overlay,
                onInput() {
                    renderSurfaceOverlay(state.overlay, textarea.value);
                    window[editorSurfaceNamespace].syncScroll(textarea, state.overlay);
                }
            };

            textarea.addEventListener("input", state.onInput);
            editorSurfaceStates.set(textarea, state);
            renderSurfaceOverlay(overlay, textarea.value);
            return true;
        },

        renderOverlay(overlay, text) {
            renderSurfaceOverlay(overlay, text);
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

            textarea.focus();
            textarea.setSelectionRange(start, end);
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

    function getLineTopInsetPx(style) {
        const lineHeight = Number.parseFloat(style.lineHeight);
        const fontSize = Number.parseFloat(style.fontSize);
        if (!Number.isFinite(lineHeight) || !Number.isFinite(fontSize)) {
            return 0;
        }

        return Math.max(0, lineHeight - fontSize);
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

    function renderSurfaceOverlay(overlay, text) {
        if (!overlay) {
            return;
        }

        overlay.innerHTML = renderSourceHighlight(text);
        updateRenderedLengthMarker(overlay, text);
    }

    function updateRenderedLengthMarker(overlay, text) {
        if (!overlay) {
            return;
        }

        overlay.dataset[renderedLengthDataAttribute] = String((text || "").length);
    }

    function renderSourceHighlight(text) {
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

            parts.push(renderBodyLine(line));
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

    function renderBodyLine(line) {
        if (!line.trim()) {
            return wrapLine("ed-src-line ed-src-line-empty", "&nbsp;");
        }

        const segmentMatch = segmentHeaderRegex.exec(line);
        if (segmentMatch && segmentMatch.groups) {
            return wrapLine(
                "ed-src-line ed-src-line-segment",
                buildHeaderMarkup(segmentMatch.groups.hash, segmentMatch.groups.content, true));
        }

        const blockMatch = blockHeaderRegex.exec(line);
        if (blockMatch && blockMatch.groups) {
            return wrapLine(
                "ed-src-line ed-src-line-block",
                buildHeaderMarkup(blockMatch.groups.hash, blockMatch.groups.content, false));
        }

        return wrapLine("ed-src-line", renderInlineMarkup(line));
    }

    function buildHeaderMarkup(hashToken, content, isSegment) {
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

        fragments.push("<span class=\"h-br\">]</span>");
        return fragments.join("");
    }

    function renderInlineMarkup(content) {
        if (!content || !content.trim()) {
            return "<span class=\"mk-color-gray\">Add script content here.</span>";
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
            const cssClass = buildCssClass(getCurrentScope().state);
            if (!cssClass) {
                builder.push(encoded);
                return;
            }

            builder.push(`<span class="${cssClass}">${encoded}</span>`);
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

            const speedClass = speedClasses.get(name.toLowerCase());
            if (speedClasses.has(name.toLowerCase())) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, { ...currentState, speedClass });
                return;
            }

            const colorClass = colorClasses.get(name.toLowerCase());
            if (colorClass) {
                appendTag(rawTag);
                pushScope(name, scopeKindStyle, { ...currentState, colorClass });
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
            const cssClass = buildCssClass(parentState, ["mk-phonetic-word"]);

            builder.push(`<span class="mk-phonetic">${guide}</span> `);
            builder.push(`<span class="${cssClass}">${spokenText}</span>`);
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
            colorClass: null,
            emotionClass: null,
            speedClass: null,
            isEmphasis: false,
            isHighlighted: false
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
        if (state.colorClass) {
            classes.push(state.colorClass);
        }
        if (state.emotionClass) {
            classes.push(state.emotionClass);
        }
        if (state.speedClass) {
            classes.push(state.speedClass);
        }
        for (const extraClass of extraClasses || []) {
            if (extraClass) {
                classes.push(extraClass);
            }
        }

        return classes.join(" ");
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
