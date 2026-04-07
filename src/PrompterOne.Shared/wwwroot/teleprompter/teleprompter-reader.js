(function () {
    const computedMatrixPrefix = "matrix(";
    const computedMatrix3dPrefix = "matrix3d(";
    const noneTransformValue = "none";
    const teleprompterReaderNamespace = "TeleprompterReaderInterop";

    function parseTransformTranslateY(transformValue) {
        if (!transformValue || transformValue === noneTransformValue) {
            return 0;
        }

        try {
            return new DOMMatrixReadOnly(transformValue).m42;
        } catch {
            const numericParts = transformValue.match(/-?\d+(?:\.\d+)?/g);
            if (!numericParts) {
                return 0;
            }

            if (transformValue.startsWith(computedMatrix3dPrefix) && numericParts.length >= 14) {
                return Number(numericParts[13]) || 0;
            }

            if (transformValue.startsWith(computedMatrixPrefix) && numericParts.length >= 6) {
                return Number(numericParts[5]) || 0;
            }

            return 0;
        }
    }

    function getCurrentTranslateY(element) {
        if (!(element instanceof HTMLElement)) {
            return 0;
        }

        return parseTransformTranslateY(window.getComputedStyle(element).transform);
    }

    function measureOffset(stage, targetWord, text) {
        const stageRect = stage.getBoundingClientRect();
        const wordRect = targetWord.getBoundingClientRect();
        const focalPoint = stageRect.top + (stageRect.height * (Number(this.focalPointPercent) / 100));
        const currentWordCenter = wordRect.top + (wordRect.height / 2);
        const currentTextTranslateY = getCurrentTranslateY(text);
        const offset = focalPoint - currentWordCenter + currentTextTranslateY;

        return Number.isFinite(offset) ? offset : null;
    }

    function withNeutralizedCard(card, text, callback) {
        if (!(card instanceof HTMLElement) || !(text instanceof HTMLElement)) {
            return callback();
        }

        const previousCardTransform = card.style.transform;
        const previousCardTransition = card.style.transition;
        const previousCardVisibility = card.style.visibility;
        const previousTextTransform = text.style.transform;
        const previousTextTransition = text.style.transition;

        card.style.visibility = "hidden";
        card.style.transition = "none";
        card.style.transform = "translateY(0)";
        text.style.transition = "none";
        text.style.transform = "none";
        void text.offsetHeight;

        try {
            return callback();
        } finally {
            text.style.transform = previousTextTransform;
            text.style.transition = previousTextTransition;
            card.style.transform = previousCardTransform;
            card.style.transition = previousCardTransition;
            card.style.visibility = previousCardVisibility;
            void card.offsetHeight;
        }
    }

    function resolveFullscreenElement(elementId) {
        const element = document.getElementById(elementId);
        return element instanceof HTMLElement ? element : null;
    }

    function resolveReaderCard(targetWord, cardStateAttributeName) {
        if (!(targetWord instanceof HTMLElement) || typeof cardStateAttributeName !== "string" || cardStateAttributeName.length === 0) {
            return null;
        }

        return targetWord.closest(`[${cardStateAttributeName}]`);
    }

    window[teleprompterReaderNamespace] = {
        measureClusterOffset(stageId, textId, targetWordId, focalPointPercent, neutralizeCard, cardStateAttributeName) {
            const stage = document.getElementById(stageId);
            const text = document.getElementById(textId);
            const targetWord = document.getElementById(targetWordId);
            const card = Boolean(neutralizeCard)
                ? resolveReaderCard(targetWord, cardStateAttributeName)
                : null;

            if (!(stage instanceof HTMLElement) || !(text instanceof HTMLElement) || !(targetWord instanceof HTMLElement)) {
                return null;
            }

            const context = { focalPointPercent };
            const readOffset = () => measureOffset.call(context, stage, targetWord, text);

            return Boolean(neutralizeCard)
                ? withNeutralizedCard(card, text, readOffset)
                : readOffset();
        },
        isFullscreenActive(elementId) {
            const element = resolveFullscreenElement(elementId);
            return element !== null && document.fullscreenElement === element;
        },
        async toggleFullscreen(elementId) {
            const element = resolveFullscreenElement(elementId);
            if (element === null || !document.fullscreenEnabled || typeof element.requestFullscreen !== "function") {
                return false;
            }

            if (document.fullscreenElement === element) {
                await document.exitFullscreen();
                return false;
            }

            await element.requestFullscreen();
            return document.fullscreenElement === element;
        }
    };
})();
