// Thin Blazor bridge for design-time browser behavior.

const DESIGN_EDITABLE_TARGET_SELECTOR = 'input, textarea, select, [contenteditable="true"]';
const ORP_CLASS_NAME = 'orp';
const RSVP_TEXT_RESET_VALUE = 'translateX(0px)';
const RSVP_WORD_SHIFT_RESET_VALUE = '0px';
const SCREEN_TELEPROMPTER_ID = 'screen-teleprompter';

const designKeyboardHandlers = new Map();

let readerStateBridge = null;
let readerStateMethodName = '';

function isEditableTarget(target) {
    return target instanceof HTMLElement &&
        (target.matches(DESIGN_EDITABLE_TARGET_SELECTOR) || target.isContentEditable);
}

function attachDesignKeyboard(screenId, dotNetRef, methodName, handledKeys) {
    detachDesignKeyboard(screenId);

    if (!screenId || !dotNetRef || !methodName) {
        return;
    }

    const keySet = new Set(Array.isArray(handledKeys) ? handledKeys : []);
    const handler = event => {
        if (!keySet.has(event.key) || !document.getElementById(screenId)) {
            return;
        }

        const editableTarget = isEditableTarget(event.target);
        if (!editableTarget) {
            event.preventDefault();
        }

        void dotNetRef.invokeMethodAsync(methodName, event.key, editableTarget);
    };

    designKeyboardHandlers.set(screenId, handler);
    document.addEventListener('keydown', handler);
}

function detachDesignKeyboard(screenId) {
    const handler = designKeyboardHandlers.get(screenId);
    if (!handler) {
        return;
    }

    document.removeEventListener('keydown', handler);
    designKeyboardHandlers.delete(screenId);
}

function attachReaderStateBridge(dotNetRef, methodName) {
    readerStateBridge = dotNetRef || null;
    readerStateMethodName = methodName || '';
}

function detachReaderStateBridge() {
    readerStateBridge = null;
    readerStateMethodName = '';
}

function reportReaderSegmentChanged(activeCardIndex, segmentName, emotionKey) {
    if (!readerStateBridge || !readerStateMethodName) {
        return;
    }

    void readerStateBridge.invokeMethodAsync(
        readerStateMethodName,
        activeCardIndex,
        segmentName || '',
        emotionKey || '');
}

function alignRsvpFocusWord(container) {
    if (!container) {
        return;
    }

    const orpElement = container.querySelector(`.${ORP_CLASS_NAME}`);
    if (!orpElement || !container.parentElement) {
        container.style.transform = RSVP_TEXT_RESET_VALUE;
        return;
    }

    const containerRect = container.parentElement.getBoundingClientRect();
    const orpRect = orpElement.getBoundingClientRect();
    const containerCenter = containerRect.left + containerRect.width / 2;
    const orpCenter = orpRect.left + orpRect.width / 2;
    container.style.transform = `translateX(${containerCenter - orpCenter}px)`;
}

function resetRsvpContextShift(element, propertyName) {
    if (element) {
        element.style.setProperty(propertyName, RSVP_WORD_SHIFT_RESET_VALUE);
    }
}

function applyRsvpContextShift(element, propertyName, gap, direction, maxGapPixels) {
    if (!element || gap <= maxGapPixels) {
        return;
    }

    element.style.setProperty(propertyName, `${(gap - maxGapPixels) * direction}px`);
}

function tightenRsvpContextWordGaps(container, leftElement, rightElement, propertyName, maxGapPixels) {
    if (!container) {
        return;
    }

    const focusRect = container.getBoundingClientRect();
    const leftWord = leftElement?.lastElementChild;
    const rightWord = rightElement?.firstElementChild;

    if (leftWord) {
        const leftGap = focusRect.left - leftWord.getBoundingClientRect().right;
        applyRsvpContextShift(leftElement, propertyName, leftGap, 1, maxGapPixels);
    }

    if (rightWord) {
        const rightGap = rightWord.getBoundingClientRect().left - focusRect.right;
        applyRsvpContextShift(rightElement, propertyName, rightGap, -1, maxGapPixels);
    }
}

function animateRsvpPause(pauseFillId, pauseDurationMilliseconds) {
    const pauseFill = document.getElementById(pauseFillId);
    if (!pauseFill) {
        return;
    }

    pauseFill.style.transition = 'none';
    pauseFill.style.width = '0%';
    void pauseFill.offsetWidth;

    if (!Number.isFinite(pauseDurationMilliseconds) || pauseDurationMilliseconds <= 0) {
        return;
    }

    pauseFill.style.transition = `width ${pauseDurationMilliseconds}ms linear`;
    pauseFill.style.width = '100%';
}

function syncRsvpLayout(wordId, leftContextId, rightContextId, pauseFillId, contextShiftPropertyName, maxGapPixels, pauseDurationMilliseconds) {
    const wordElement = document.getElementById(wordId);
    const leftContextElement = document.getElementById(leftContextId);
    const rightContextElement = document.getElementById(rightContextId);

    alignRsvpFocusWord(wordElement);
    resetRsvpContextShift(leftContextElement, contextShiftPropertyName);
    resetRsvpContextShift(rightContextElement, contextShiftPropertyName);
    tightenRsvpContextWordGaps(
        wordElement,
        leftContextElement,
        rightContextElement,
        contextShiftPropertyName,
        maxGapPixels);
    animateRsvpPause(pauseFillId, pauseDurationMilliseconds);
}

window.PrompterLiveDesign = {
    attachDesignKeyboard,
    attachReaderStateBridge,
    detachDesignKeyboard,
    detachReaderStateBridge,
    initialize(screenId) {
        if (screenId === SCREEN_TELEPROMPTER_ID) {
            resetReader();
            void initializeReaderCamera();
        }
    },
    jumpReaderCard(direction) {
        jumpCard(direction);
    },
    reportReaderSegmentChanged,
    setReaderFontSize(value) {
        setReaderFontSize(value);
    },
    setReaderFocalPoint(value) {
        setReaderFocalPoint(value);
    },
    setReaderTextWidth(value) {
        setReaderTextWidth(value);
    },
    stepReaderWord(direction) {
        stepWord(direction);
    },
    syncRsvpLayout,
    toggleReaderCamera() {
        return toggleReaderCamera();
    },
    toggleReaderPlayback() {
        toggleTp();
    }
};
