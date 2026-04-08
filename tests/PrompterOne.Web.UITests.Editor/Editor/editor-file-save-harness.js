(() => {
    const defaultMode = "";
    const downloadMode = "download";
    const fileSystemMode = "file-system";
    const harnessGlobalName = "__prompterOneEditorFileSaveHarness";

    if (typeof window[harnessGlobalName] === "object" && window[harnessGlobalName] !== null) {
        return;
    }

    let downloadCallCount = 0;
    let pickerCallCount = 0;
    let savedBlob = null;
    let savedFileName = "";
    let savedMode = defaultMode;

    const objectUrls = new Map();
    const originalAnchorClick = HTMLAnchorElement.prototype.click;
    const originalCreateObjectUrl = URL.createObjectURL.bind(URL);
    const originalRevokeObjectUrl = URL.revokeObjectURL.bind(URL);

    function recordSavedFile(mode, fileName, blob) {
        savedBlob = blob instanceof Blob
            ? blob
            : new Blob([blob]);
        savedFileName = typeof fileName === "string" ? fileName : "";
        savedMode = typeof mode === "string" ? mode : defaultMode;

        if (savedMode === downloadMode) {
            downloadCallCount += 1;
        }
    }

    const savePicker = async options => {
        pickerCallCount += 1;
        const chunks = [];
        const suggestedName = typeof options?.suggestedName === "string"
            ? options.suggestedName
            : "";

        return {
            createWritable: async () => ({
                write: async data => {
                    chunks.push(data instanceof Blob ? data : new Blob([data]));
                },
                close: async () => {
                    recordSavedFile(fileSystemMode, suggestedName, new Blob(chunks));
                }
            })
        };
    };

    function enableSavePicker() {
        window.showSaveFilePicker = savePicker;
    }

    function disableSavePicker() {
        try {
            delete window.showSaveFilePicker;
        }
        catch {
            window.showSaveFilePicker = undefined;
        }
    }

    URL.createObjectURL = object => {
        const url = originalCreateObjectUrl(object);
        if (object instanceof Blob) {
            objectUrls.set(url, object);
        }

        return url;
    };

    URL.revokeObjectURL = url => {
        objectUrls.delete(url);
        return originalRevokeObjectUrl(url);
    };

    HTMLAnchorElement.prototype.click = function () {
        if (typeof this.download === "string"
            && this.download.length > 0
            && typeof this.href === "string"
            && objectUrls.has(this.href)) {
            recordSavedFile(downloadMode, this.download, objectUrls.get(this.href));
            return;
        }

        return originalAnchorClick.apply(this, arguments);
    };

    enableSavePicker();

    window[harnessGlobalName] = {
        disableSavePicker,
        enableSavePicker,
        async getSavedFileState() {
            return {
                downloadCallCount,
                fileName: savedFileName,
                hasBlob: savedBlob instanceof Blob,
                mode: savedMode,
                pickerCallCount,
                text: savedBlob instanceof Blob ? await savedBlob.text() : ""
            };
        },
        reset() {
            downloadCallCount = 0;
            pickerCallCount = 0;
            savedBlob = null;
            savedFileName = "";
            savedMode = defaultMode;
            enableSavePicker();
        }
    };
})();
