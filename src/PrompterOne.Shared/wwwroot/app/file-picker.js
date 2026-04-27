export function openFilePicker(inputId) {
    const input = typeof inputId === "string"
        ? document.getElementById(inputId)
        : null;

    if (!(input instanceof HTMLInputElement)) {
        return;
    }

    input.click();
}

const abortErrorName = "AbortError";
const defaultMimeType = "text/plain";
const defaultSuggestedFileName = "untitled-script.tps";
const downloadMode = "download";
const fileSystemMode = "file-system";
const cancelledMode = "cancelled";

function buildFileType(description, mimeType, extensions) {
    const supportedExtensions = Array.isArray(extensions)
        ? extensions.filter(extension => typeof extension === "string" && extension.length > 0)
        : [];

    if (supportedExtensions.length === 0) {
        return null;
    }

    return {
        accept: {
            [typeof mimeType === "string" && mimeType.length > 0 ? mimeType : defaultMimeType]: supportedExtensions
        },
        description: typeof description === "string" && description.length > 0
            ? description
            : "PrompterOne file"
    };
}

function normalizeSuggestedFileName(suggestedFileName) {
    if (typeof suggestedFileName !== "string") {
        return defaultSuggestedFileName;
    }

    const trimmedFileName = suggestedFileName.trim();
    return trimmedFileName.length > 0
        ? trimmedFileName
        : defaultSuggestedFileName;
}

function triggerFileDownload(blob, fileName) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    link.rel = "noopener";
    link.hidden = true;
    document.body.appendChild(link);
    link.click();
    window.setTimeout(() => {
        link.remove();
        URL.revokeObjectURL(url);
    }, 0);
}

export async function saveTextFile(suggestedFileName, text, mimeType, description, extensions, preferSavePicker) {
    const fileName = normalizeSuggestedFileName(suggestedFileName);
    const blob = new Blob([typeof text === "string" ? text : ""], {
        type: typeof mimeType === "string" && mimeType.length > 0 ? mimeType : defaultMimeType
    });

    return saveBlob(fileName, blob, mimeType, description, extensions, preferSavePicker);
}

export async function saveBinaryFile(suggestedFileName, content, mimeType, description, extensions, preferSavePicker) {
    const fileName = normalizeSuggestedFileName(suggestedFileName);
    const bytes = content instanceof Uint8Array
        ? content
        : Array.isArray(content)
            ? new Uint8Array(content)
            : new Uint8Array();
    const blob = new Blob([bytes], {
        type: typeof mimeType === "string" && mimeType.length > 0 ? mimeType : "application/octet-stream"
    });

    return saveBlob(fileName, blob, mimeType, description, extensions, preferSavePicker);
}

async function saveBlob(fileName, blob, mimeType, description, extensions, preferSavePicker) {
    if (preferSavePicker !== false && typeof window.showSaveFilePicker === "function") {
        try {
            const fileType = buildFileType(description, mimeType, extensions);
            const pickerOptions = { suggestedName: fileName };

            if (fileType !== null) {
                pickerOptions.types = [fileType];
            }

            const handle = await window.showSaveFilePicker(pickerOptions);
            const writable = await handle.createWritable();
            await writable.write(blob);
            await writable.close();
            return fileSystemMode;
        }
        catch (error) {
            if (error?.name === abortErrorName) {
                return cancelledMode;
            }
        }
    }

    triggerFileDownload(blob, fileName);
    return downloadMode;
}
