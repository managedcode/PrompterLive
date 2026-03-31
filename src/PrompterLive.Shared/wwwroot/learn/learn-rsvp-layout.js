(function () {
    const interopNamespace = "LearnRsvpLayoutInterop";
    const focusSelector = ".rsvp-focus";
    const orpSelector = ".rsvp-focus-orp";
    const focusShiftPropertyName = "--rsvp-focus-shift";
    const leftOffsetPropertyName = "--rsvp-context-left-offset";
    const rightOffsetPropertyName = "--rsvp-context-right-offset";
    const defaultContextOffsetPx = 220;
    const railGapPx = 28;
    const pixelUnitSuffix = "px";
    const zeroPixels = "0px";

    function setPixelProperty(target, propertyName, value) {
        const roundedValue = Math.round(value * 100) / 100;
        target.style.setProperty(propertyName, `${roundedValue}${pixelUnitSuffix}`);
    }

    function readCenter(rect) {
        return rect.left + (rect.width / 2);
    }

    function applyDefaultLayout(rowElement) {
        rowElement.style.setProperty(focusShiftPropertyName, zeroPixels);
        setPixelProperty(rowElement, leftOffsetPropertyName, defaultContextOffsetPx);
        setPixelProperty(rowElement, rightOffsetPropertyName, defaultContextOffsetPx);
    }

    window[interopNamespace] = {
        syncLayout(rowElement) {
            if (!(rowElement instanceof HTMLElement)) {
                return;
            }

            const focusElement = rowElement.querySelector(focusSelector);
            const orpElement = focusElement?.querySelector(orpSelector);

            if (!(focusElement instanceof HTMLElement) || !(orpElement instanceof HTMLElement)) {
                applyDefaultLayout(rowElement);
                return;
            }

            applyDefaultLayout(rowElement);
            void rowElement.offsetWidth;

            const rowRect = rowElement.getBoundingClientRect();
            const focusRect = focusElement.getBoundingClientRect();
            const orpRect = orpElement.getBoundingClientRect();
            const focusShiftPx = readCenter(focusRect) - readCenter(orpRect);

            setPixelProperty(rowElement, focusShiftPropertyName, focusShiftPx);
            void rowElement.offsetWidth;

            const shiftedFocusRect = focusElement.getBoundingClientRect();
            const rowCenterPx = readCenter(rowRect);
            const leftOffsetPx = Math.max(rowCenterPx - shiftedFocusRect.left + railGapPx, railGapPx);
            const rightOffsetPx = Math.max(shiftedFocusRect.right - rowCenterPx + railGapPx, railGapPx);

            setPixelProperty(rowElement, leftOffsetPropertyName, leftOffsetPx);
            setPixelProperty(rowElement, rightOffsetPropertyName, rightOffsetPx);
        }
    };
})();
