const blockSelector = ".editor-rendered-block[data-rendered-segment-index][data-rendered-block-index]";
const handleSelector = ".editor-rendered-block-drag-handle";
const draggingClass = "editor-rendered-block--dragging";
const dropTargetClass = "editor-rendered-block--drop-target";
const dropBeforeClass = "editor-rendered-block--drop-before";
const dropAfterClass = "editor-rendered-block--drop-after";
const dragGhostClass = "editor-rendered-drag-ghost";

export function attach(root, dotNetReference) {
    if (!(root instanceof HTMLElement)) {
        return { dispose() {} };
    }

    root.dataset.renderedCardsDragReady = "true";

    let sourceBlock = null;
    let targetBlock = null;
    let pointerId = null;
    let mouseDragging = false;
    let dragGhost = null;

    const clearState = () => {
        sourceBlock?.classList.remove(draggingClass);
        targetBlock?.classList.remove(dropTargetClass, dropBeforeClass, dropAfterClass);
        dragGhost?.remove();
        sourceBlock = null;
        targetBlock = null;
        pointerId = null;
        mouseDragging = false;
        dragGhost = null;
        delete root.dataset.renderedCardsDragging;
        delete root.dataset.renderedCardsDropPosition;
    };

    const readBlockAddress = block => ({
        segmentIndex: Number.parseInt(block.dataset.renderedSegmentIndex ?? "", 10),
        blockIndex: Number.parseInt(block.dataset.renderedBlockIndex ?? "", 10)
    });

    const isValidAddress = address =>
        Number.isInteger(address.segmentIndex) && Number.isInteger(address.blockIndex);

    const resolveBlockAt = event => {
        const element = document.elementFromPoint(event.clientX, event.clientY);
        const block = element instanceof Element ? element.closest(blockSelector) : null;
        return block === dragGhost ? null : block;
    };

    const readDropPosition = (event, block) => {
        const rect = block.getBoundingClientRect();
        return event.clientY < rect.top + (rect.height / 2) ? "before" : "after";
    };

    const setTargetDropPosition = (block, position) => {
        block.classList.toggle(dropBeforeClass, position === "before");
        block.classList.toggle(dropAfterClass, position === "after");
        root.dataset.renderedCardsDropPosition = position;
    };

    const createDragGhost = (block, event) => {
        dragGhost?.remove();
        dragGhost = block.cloneNode(true);
        dragGhost.classList.remove(draggingClass, dropTargetClass, dropBeforeClass, dropAfterClass);
        dragGhost.classList.add(dragGhostClass);
        dragGhost.removeAttribute("data-test");
        dragGhost.setAttribute("aria-hidden", "true");
        dragGhost.style.width = `${Math.min(block.getBoundingClientRect().width, 330)}px`;
        document.body.append(dragGhost);
        moveDragGhost(event);
    };

    const moveDragGhost = event => {
        if (!dragGhost) {
            return;
        }

        dragGhost.style.left = `${event.clientX}px`;
        dragGhost.style.top = `${event.clientY}px`;
    };

    const updateTarget = event => {
        if (!sourceBlock || (pointerId !== null && event.pointerId !== pointerId)) {
            return;
        }

        moveDragGhost(event);
        const nextTarget = resolveBlockAt(event);
        if (!(nextTarget instanceof HTMLElement) || nextTarget === sourceBlock) {
            targetBlock?.classList.remove(dropTargetClass, dropBeforeClass, dropAfterClass);
            targetBlock = null;
            delete root.dataset.renderedCardsDropPosition;
            return;
        }

        const position = readDropPosition(event, nextTarget);
        if (targetBlock !== nextTarget) {
            targetBlock?.classList.remove(dropTargetClass, dropBeforeClass, dropAfterClass);
            targetBlock = nextTarget;
            targetBlock.classList.add(dropTargetClass);
        }
        setTargetDropPosition(targetBlock, position);
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
        root.dataset.renderedCardsDragging = "true";
        createDragGhost(sourceBlock, event);
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
        root.dataset.renderedCardsDragging = "true";
        createDragGhost(sourceBlock, event);
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
