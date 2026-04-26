const blockSelector = ".editor-rendered-block[data-rendered-segment-index][data-rendered-block-index]";
const handleSelector = ".editor-rendered-block-drag-handle";
const draggingClass = "editor-rendered-block--dragging";
const dropTargetClass = "editor-rendered-block--drop-target";

export function attach(root, dotNetReference) {
    if (!(root instanceof HTMLElement)) {
        return { dispose() {} };
    }

    root.dataset.renderedCardsDragReady = "true";

    let sourceBlock = null;
    let targetBlock = null;
    let pointerId = null;
    let mouseDragging = false;

    const clearState = () => {
        sourceBlock?.classList.remove(draggingClass);
        targetBlock?.classList.remove(dropTargetClass);
        sourceBlock = null;
        targetBlock = null;
        pointerId = null;
        mouseDragging = false;
    };

    const readBlockAddress = block => ({
        segmentIndex: Number.parseInt(block.dataset.renderedSegmentIndex ?? "", 10),
        blockIndex: Number.parseInt(block.dataset.renderedBlockIndex ?? "", 10)
    });

    const isValidAddress = address =>
        Number.isInteger(address.segmentIndex) && Number.isInteger(address.blockIndex);

    const resolveBlockAt = event => {
        const element = document.elementFromPoint(event.clientX, event.clientY);
        return element instanceof Element ? element.closest(blockSelector) : null;
    };

    const updateTarget = event => {
        if (!sourceBlock || (pointerId !== null && event.pointerId !== pointerId)) {
            return;
        }

        const nextTarget = resolveBlockAt(event);
        if (!(nextTarget instanceof HTMLElement) || nextTarget === sourceBlock) {
            targetBlock?.classList.remove(dropTargetClass);
            targetBlock = null;
            return;
        }

        if (targetBlock !== nextTarget) {
            targetBlock?.classList.remove(dropTargetClass);
            targetBlock = nextTarget;
            targetBlock.classList.add(dropTargetClass);
        }
    };

    const onPointerDown = event => {
        const handle = event.target instanceof Element ? event.target.closest(handleSelector) : null;
        if (!(handle instanceof HTMLElement) || event.button !== 0) {
            return;
        }

        const block = handle.closest(blockSelector);
        if (!(block instanceof HTMLElement)) {
            return;
        }

        event.preventDefault();
        clearState();
        sourceBlock = block;
        pointerId = event.pointerId;
        sourceBlock.classList.add(draggingClass);
        try {
            root.setPointerCapture?.(event.pointerId);
        } catch {
        }
    };

    const onPointerMove = event => {
        updateTarget(event);
    };

    const onPointerUp = event => {
        if (!sourceBlock || event.pointerId !== pointerId) {
            return;
        }

        updateTarget(event);
        const source = readBlockAddress(sourceBlock);
        const target = targetBlock ? readBlockAddress(targetBlock) : null;
        clearState();
        try {
            root.releasePointerCapture?.(event.pointerId);
        } catch {
        }

        if (!target || !isValidAddress(source) || !isValidAddress(target)) {
            return;
        }

        dotNetReference.invokeMethodAsync(
            "HandleRenderedCardDropAsync",
            source.segmentIndex,
            source.blockIndex,
            target.segmentIndex,
            target.blockIndex);
    };

    const onPointerCancel = event => {
        if (event.pointerId === pointerId) {
            clearState();
        }
    };

    const startMouseDrag = event => {
        const handle = event.target instanceof Element ? event.target.closest(handleSelector) : null;
        if (!(handle instanceof HTMLElement) || event.button !== 0) {
            return;
        }

        const block = handle.closest(blockSelector);
        if (!(block instanceof HTMLElement)) {
            return;
        }

        event.preventDefault();
        clearState();
        sourceBlock = block;
        mouseDragging = true;
        sourceBlock.classList.add(draggingClass);
    };

    const moveMouseDrag = event => {
        if (!mouseDragging) {
            return;
        }

        updateTarget(event);
    };

    const endMouseDrag = event => {
        if (!mouseDragging || !sourceBlock) {
            return;
        }

        updateTarget(event);
        const source = readBlockAddress(sourceBlock);
        const target = targetBlock ? readBlockAddress(targetBlock) : null;
        clearState();

        if (!target || !isValidAddress(source) || !isValidAddress(target)) {
            return;
        }

        dotNetReference.invokeMethodAsync(
            "HandleRenderedCardDropAsync",
            source.segmentIndex,
            source.blockIndex,
            target.segmentIndex,
            target.blockIndex);
    };

    root.addEventListener("pointerdown", onPointerDown);
    root.addEventListener("pointermove", onPointerMove);
    root.addEventListener("pointerup", onPointerUp);
    root.addEventListener("pointercancel", onPointerCancel);
    root.addEventListener("mousedown", startMouseDrag);
    root.addEventListener("mousemove", moveMouseDrag);
    root.addEventListener("mouseup", endMouseDrag);

    return {
        dispose() {
            root.removeEventListener("pointerdown", onPointerDown);
            root.removeEventListener("pointermove", onPointerMove);
            root.removeEventListener("pointerup", onPointerUp);
            root.removeEventListener("pointercancel", onPointerCancel);
            root.removeEventListener("mousedown", startMouseDrag);
            root.removeEventListener("mousemove", moveMouseDrag);
            root.removeEventListener("mouseup", endMouseDrag);
            delete root.dataset.renderedCardsDragReady;
            clearState();
        }
    };
}
