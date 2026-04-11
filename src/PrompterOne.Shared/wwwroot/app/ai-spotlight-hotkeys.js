let dotNetReference = null;

function isAssistantShortcut(event) {
    return (event.metaKey || event.ctrlKey)
        && !event.altKey
        && !event.shiftKey
        && event.key?.toLowerCase() === "k";
}

function handleKeydown(event) {
    if (!dotNetReference) {
        return;
    }

    if (isAssistantShortcut(event)) {
        event.preventDefault();
        dotNetReference.invokeMethodAsync("Toggle");
        return;
    }

    if (event.key === "Escape") {
        dotNetReference.invokeMethodAsync("Close");
    }
}

export function initialize(reference) {
    if (dotNetReference) {
        return;
    }

    dotNetReference = reference;
    document.addEventListener("keydown", handleKeydown, true);
}

export function dispose() {
    document.removeEventListener("keydown", handleKeydown, true);
    dotNetReference = null;
}
