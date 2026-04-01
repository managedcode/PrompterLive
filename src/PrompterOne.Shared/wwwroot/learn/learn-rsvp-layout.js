(function () {
    const interopNamespace = "LearnRsvpLayoutInterop";
    const focusSelector = ".rsvp-focus";
    const orpSelector = ".rsvp-focus-orp";
    const focusShiftPropertyName = "--rsvp-focus-shift";
    const fontSyncReadyAttributeName = "data-rsvp-layout-font-sync-ready";
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
    }

    function syncLayoutNow(rowElement) {
        const focusElement = rowElement.querySelector(focusSelector);
        const orpElement = focusElement?.querySelector(orpSelector);

        if (!(focusElement instanceof HTMLElement) || !(orpElement instanceof HTMLElement)) {
            applyDefaultLayout(rowElement);
            return;
        }

        applyDefaultLayout(rowElement);
        void rowElement.offsetWidth;

        const focusRect = focusElement.getBoundingClientRect();
        const orpRect = orpElement.getBoundingClientRect();
        const focusShiftPx = readCenter(focusRect) - readCenter(orpRect);

        setPixelProperty(rowElement, focusShiftPropertyName, focusShiftPx);
    }

    function scheduleFontReadySync(rowElement) {
        if (rowElement.getAttribute(fontSyncReadyAttributeName) === "true") {
            return;
        }

        rowElement.setAttribute(fontSyncReadyAttributeName, "true");

        if (!document.fonts?.ready) {
            return;
        }

        void document.fonts.ready.then(() => {
            if (rowElement.isConnected) {
                syncLayoutNow(rowElement);
            }
        });
    }

    window[interopNamespace] = {
        syncLayout(rowElement) {
            if (!(rowElement instanceof HTMLElement)) {
                return;
            }

            syncLayoutNow(rowElement);
            scheduleFontReadySync(rowElement);
        }
    };
})();
