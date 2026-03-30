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

    window[editorSurfaceNamespace] = {
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
        const prefix = textarea.value.slice(0, start);
        const prefixLines = prefix.split("\n");
        const line = prefixLines.length;
        const column = (prefixLines[prefixLines.length - 1] || "").length + 1;
        const coords = measureTextareaSelectionGeometry(textarea, start, end);

        return {
            start,
            end,
            line,
            column,
            toolbarTop: Math.max(getFloatingToolbarMinimumTopPx(textarea), coords.top),
            toolbarLeft: coords.left
        };
    }

    function measureTextareaSelectionGeometry(textarea, start, end) {
        if (start === end) {
            return measureTextareaCaretGeometry(textarea, end);
        }

        return measureTextareaRangeGeometry(textarea, start, end);
    }

    function measureTextareaCaretGeometry(textarea, index) {
        const value = textarea.value;
        return measureWithTextareaMirror(textarea, (mirror, style) => {
            const span = document.createElement("span");

            mirror.textContent = value.slice(0, index);
            span.textContent = value.slice(index, index + 1) || " ";
            mirror.appendChild(span);

            return {
                top: textarea.offsetTop + span.offsetTop - textarea.scrollTop - getLineTopInsetPx(style),
                left: textarea.offsetLeft + span.offsetLeft - textarea.scrollLeft
            };
        });
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
            document.body.removeChild(mirror);
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
})();
