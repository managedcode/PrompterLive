(function () {
    const interopNamespace = "LearnRsvpLayoutInterop";
    const pixelUnitSuffix = "px";
    const zeroPixels = "0px";

    function setPixelProperty(target, propertyName, value) {
        const roundedValue = Math.round(value * 100) / 100;
        target.style.setProperty(propertyName, `${roundedValue}${pixelUnitSuffix}`);
    }

    function applyDefaultLayout(rowElement, focusLeftExtentPropertyName, focusRightExtentPropertyName) {
        rowElement.style.setProperty(focusLeftExtentPropertyName, zeroPixels);
        rowElement.style.setProperty(focusRightExtentPropertyName, zeroPixels);
    }

    function syncLayoutNow(rowElement, focusElement, orpElement, focusLeftExtentPropertyName, focusRightExtentPropertyName) {
        if (!(focusElement instanceof HTMLElement) || !(orpElement instanceof HTMLElement)) {
            applyDefaultLayout(rowElement, focusLeftExtentPropertyName, focusRightExtentPropertyName);
            return;
        }

        applyDefaultLayout(rowElement, focusLeftExtentPropertyName, focusRightExtentPropertyName);
        void rowElement.offsetWidth;

        const focusRect = focusElement.getBoundingClientRect();
        const orpRect = orpElement.getBoundingClientRect();
        const orpCenterPx = orpRect.left + (orpRect.width / 2);
        const focusLeftExtentPx = Math.max(orpCenterPx - focusRect.left, 0);
        const focusRightExtentPx = Math.max(focusRect.right - orpCenterPx, 0);

        setPixelProperty(rowElement, focusLeftExtentPropertyName, focusLeftExtentPx);
        setPixelProperty(rowElement, focusRightExtentPropertyName, focusRightExtentPx);
    }

    function scheduleFontReadySync(rowElement, focusElement, orpElement, focusLeftExtentPropertyName, focusRightExtentPropertyName, fontSyncReadyAttributeName) {
        if (rowElement.getAttribute(fontSyncReadyAttributeName) === "true") {
            return;
        }

        rowElement.setAttribute(fontSyncReadyAttributeName, "true");

        if (!document.fonts?.ready) {
            return;
        }

        void document.fonts.ready.then(() => {
            if (rowElement.isConnected) {
                syncLayoutNow(rowElement, focusElement, orpElement, focusLeftExtentPropertyName, focusRightExtentPropertyName);
            }
        });
    }

    window[interopNamespace] = {
        syncLayout(rowElement, focusElement, orpElement, focusLeftExtentPropertyName, focusRightExtentPropertyName, fontSyncReadyAttributeName) {
            if (!(rowElement instanceof HTMLElement)) {
                return;
            }

            syncLayoutNow(rowElement, focusElement, orpElement, focusLeftExtentPropertyName, focusRightExtentPropertyName);
            scheduleFontReadySync(rowElement, focusElement, orpElement, focusLeftExtentPropertyName, focusRightExtentPropertyName, fontSyncReadyAttributeName);
        }
    };
})();
