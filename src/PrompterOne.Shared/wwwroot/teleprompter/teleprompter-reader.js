(function () {
    const computedMatrixPrefix = "matrix(";
    const computedMatrix3dPrefix = "matrix3d(";
    const noneTransformValue = "none";
    const readerCardSelector = ".rd-card";
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

    window[teleprompterReaderNamespace] = {
        measureClusterOffset(stageId, textId, targetWordId, focalPointPercent, neutralizeCard) {
            const stage = document.getElementById(stageId);
            const text = document.getElementById(textId);
            const targetWord = document.getElementById(targetWordId);
            const card = Boolean(neutralizeCard)
                ? targetWord instanceof HTMLElement
                    ? targetWord.closest(readerCardSelector)
                    : null
                : null;

            if (!(stage instanceof HTMLElement) || !(text instanceof HTMLElement) || !(targetWord instanceof HTMLElement)) {
                return null;
            }

            const context = { focalPointPercent };
            const readOffset = () => measureOffset.call(context, stage, targetWord, text);

            return Boolean(neutralizeCard)
                ? withNeutralizedCard(card, text, readOffset)
                : readOffset();
        }
    };
})();
