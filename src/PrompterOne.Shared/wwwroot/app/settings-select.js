(function () {
    const viewportMargin = 8;
    const panelGap = 6;

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    function getViewportSize() {
        const root = document.documentElement;
        return {
            width: root?.clientWidth || window.innerWidth || 0,
            height: root?.clientHeight || window.innerHeight || 0
        };
    }

    function position(trigger, panel) {
        if (!trigger || !panel || typeof trigger.getBoundingClientRect !== "function") {
            return;
        }

        panel.classList.remove("ss-panel-positioned");
        panel.style.removeProperty("--ss-panel-top");
        panel.style.removeProperty("--ss-panel-left");
        panel.style.removeProperty("--ss-panel-width");
        panel.style.removeProperty("--ss-panel-max-height");

        const triggerRect = trigger.getBoundingClientRect();
        const viewport = getViewportSize();
        const maxPanelWidth = Math.max(80, viewport.width - (viewportMargin * 2));
        const measuredWidth = Math.max(triggerRect.width, panel.scrollWidth || triggerRect.width);
        const panelWidth = Math.min(measuredWidth, maxPanelWidth);
        const left = clamp(
            triggerRect.left,
            viewportMargin,
            Math.max(viewportMargin, viewport.width - viewportMargin - panelWidth));

        panel.style.setProperty("--ss-panel-width", `${panelWidth}px`);
        panel.classList.add("ss-panel-positioned");

        const measuredHeight = panel.offsetHeight || panel.scrollHeight || 0;
        const spaceAbove = Math.max(0, triggerRect.top - viewportMargin - panelGap);
        const spaceBelow = Math.max(0, viewport.height - triggerRect.bottom - viewportMargin - panelGap);
        const opensUp = spaceBelow < measuredHeight && spaceAbove > spaceBelow;
        const availableHeight = Math.max(80, opensUp ? spaceAbove : spaceBelow);
        const panelHeight = Math.min(measuredHeight, availableHeight);
        const top = opensUp
            ? clamp(triggerRect.top - panelGap - panelHeight, viewportMargin, Math.max(viewportMargin, viewport.height - viewportMargin - panelHeight))
            : clamp(triggerRect.bottom + panelGap, viewportMargin, Math.max(viewportMargin, viewport.height - viewportMargin - panelHeight));

        panel.dataset.placement = opensUp ? "top" : "bottom";
        panel.style.setProperty("--ss-panel-top", `${top}px`);
        panel.style.setProperty("--ss-panel-left", `${left}px`);
        panel.style.setProperty("--ss-panel-max-height", `${availableHeight}px`);

        const actualRect = panel.getBoundingClientRect();
        const correctedTop = top - (actualRect.top - top);
        const correctedLeft = left - (actualRect.left - left);
        panel.style.setProperty("--ss-panel-top", `${correctedTop}px`);
        panel.style.setProperty("--ss-panel-left", `${correctedLeft}px`);
    }

    window.PrompterOneSettingsSelect = {
        position
    };
}());
