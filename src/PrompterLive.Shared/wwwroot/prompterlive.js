(function () {
    const storageKey = "prompterlive.library.v1";
    const seedVersionKey = "prompterlive.library.seed-version";
    const settingsPrefix = "prompterlive.settings.";
    const streamMap = new Map();
    const readerAnimations = new Map();

    function normalizeDocument(document) {
        if (!document) {
            return null;
        }

        if (Array.isArray(document)) {
            const [id, title, text, documentName, updatedAt] = document;
            return {
                id: id || "",
                title: title || "Untitled Script",
                text: text || "",
                documentName: documentName || "untitled-script.tps",
                updatedAt: updatedAt || new Date().toISOString()
            };
        }

        if (typeof document !== "object") {
            return null;
        }

        return {
            id: document.id ?? document.Id ?? "",
            title: document.title ?? document.Title ?? "Untitled Script",
            text: document.text ?? document.Text ?? "",
            documentName: document.documentName ?? document.DocumentName ?? "untitled-script.tps",
            updatedAt: document.updatedAt ?? document.UpdatedAt ?? new Date().toISOString()
        };
    }

    function normalizeDocuments(documents) {
        if (!documents) {
            return [];
        }

        if (Array.isArray(documents)) {
            if (documents.length === 0) {
                return [];
            }

            if (typeof documents[0] !== "object" || documents[0] === null) {
                return normalizeDocument(documents) ? [normalizeDocument(documents)] : [];
            }

            return documents.map(normalizeDocument).filter(Boolean);
        }

        const normalized = normalizeDocument(documents);
        return normalized ? [normalized] : [];
    }

    function readDocuments() {
        try {
            const raw = window.localStorage.getItem(storageKey);
            if (!raw) {
                return [];
            }

            const parsed = JSON.parse(raw);
            if (Array.isArray(parsed)) {
                return normalizeDocuments(parsed);
            }

            return normalizeDocuments(parsed);
        } catch {
            return [];
        }
    }

    function writeDocuments(documents) {
        window.localStorage.setItem(storageKey, JSON.stringify(documents));
    }

    async function stopStream(stream) {
        if (!stream) {
            return;
        }

        stream.getTracks().forEach(track => track.stop());
    }

    window.PrompterLive = {
        storage: {
            ensureSeedData(seedDocuments) {
                const current = readDocuments();
                const normalizedSeedDocuments = normalizeDocuments(seedDocuments);

                if (current.length === 0) {
                    writeDocuments(normalizedSeedDocuments);
                    return;
                }

                const existingIds = new Set(current.map(document => document.id));
                const merged = current.concat(
                    normalizedSeedDocuments.filter(document => !existingIds.has(document.id))
                );

                writeDocuments(merged);
            },
            getSeedVersion() {
                return window.localStorage.getItem(seedVersionKey);
            },
            setSeedVersion(version) {
                if (!version) {
                    window.localStorage.removeItem(seedVersionKey);
                    return;
                }

                window.localStorage.setItem(seedVersionKey, version);
            },
            listDocuments() {
                return readDocuments();
            },
            listDocumentsJson() {
                return JSON.stringify(readDocuments());
            },
            getDocument(id) {
                return readDocuments().find(document => document.id === id) ?? null;
            },
            getDocumentJson(id) {
                return JSON.stringify(window.PrompterLive.storage.getDocument(id));
            },
            saveDocument(document) {
                const normalizedDocument = normalizeDocument(document);
                if (!normalizedDocument) {
                    return null;
                }

                const documents = readDocuments();
                const index = documents.findIndex(item => item.id === normalizedDocument.id);
                if (index >= 0) {
                    documents[index] = normalizedDocument;
                } else {
                    documents.push(normalizedDocument);
                }

                writeDocuments(documents);
                return normalizedDocument;
            },
            saveDocumentJson(document) {
                return JSON.stringify(window.PrompterLive.storage.saveDocument(document));
            },
            deleteDocument(id) {
                const documents = readDocuments().filter(document => document.id !== id);
                writeDocuments(documents);
            }
        },

        settings: {
            load(key) {
                try {
                    const raw = window.localStorage.getItem(settingsPrefix + key);
                    return raw ? JSON.parse(raw) : null;
                } catch {
                    return null;
                }
            },
            save(key, value) {
                window.localStorage.setItem(settingsPrefix + key, JSON.stringify(value));
            }
        },

        downloads: {
            saveText(fileName, content) {
                const blob = new Blob([content], { type: "text/plain;charset=utf-8" });
                const url = URL.createObjectURL(blob);
                const anchor = document.createElement("a");
                anchor.href = url;
                anchor.download = fileName || "script.tps";
                anchor.click();
                URL.revokeObjectURL(url);
            }
        },

        media: {
            async queryPermissions() {
                const state = { cameraGranted: false, microphoneGranted: false };

                if (!navigator.permissions?.query) {
                    return state;
                }

                try {
                    const camera = await navigator.permissions.query({ name: "camera" });
                    state.cameraGranted = camera.state === "granted";
                } catch {
                }

                try {
                    const microphone = await navigator.permissions.query({ name: "microphone" });
                    state.microphoneGranted = microphone.state === "granted";
                } catch {
                }

                return state;
            },

            async requestPermissions() {
                try {
                    const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
                    await stopStream(stream);
                } catch {
                }

                return await window.PrompterLive.media.queryPermissions();
            },

            async listDevices() {
                if (!navigator.mediaDevices?.enumerateDevices) {
                    return [];
                }

                const devices = await navigator.mediaDevices.enumerateDevices();
                return devices.map((device, index) => ({
                    deviceId: device.deviceId,
                    label: device.label || `${device.kind} ${index + 1}`,
                    kind: device.kind,
                    isDefault: device.deviceId === "default"
                }));
            },

            async attachCamera(elementId, deviceId, muted) {
                const element = document.getElementById(elementId);
                if (!element || !navigator.mediaDevices?.getUserMedia) {
                    return;
                }

                if (streamMap.has(elementId)) {
                    await stopStream(streamMap.get(elementId));
                }

                const stream = await navigator.mediaDevices.getUserMedia({
                    video: deviceId ? { deviceId: { exact: deviceId } } : true,
                    audio: false
                });

                element.srcObject = stream;
                element.muted = muted !== false;
                element.playsInline = true;
                await element.play().catch(() => {});
                streamMap.set(elementId, stream);
            },

            async detachCamera(elementId) {
                const stream = streamMap.get(elementId);
                if (stream) {
                    await stopStream(stream);
                    streamMap.delete(elementId);
                }

                const element = document.getElementById(elementId);
                if (element) {
                    element.srcObject = null;
                }
            }
        },

        reader: {
            startAutoScroll(elementId, pixelsPerSecond) {
                const element = document.getElementById(elementId);
                if (!element) {
                    return;
                }

                window.PrompterLive.reader.stopAutoScroll(elementId);

                let lastTimestamp = 0;
                const step = timestamp => {
                    if (!readerAnimations.has(elementId)) {
                        return;
                    }

                    if (!lastTimestamp) {
                        lastTimestamp = timestamp;
                    }

                    const delta = (timestamp - lastTimestamp) / 1000;
                    lastTimestamp = timestamp;
                    element.scrollTop += pixelsPerSecond * delta;

                    if (element.scrollTop + element.clientHeight >= element.scrollHeight) {
                        readerAnimations.delete(elementId);
                        return;
                    }

                    readerAnimations.set(elementId, requestAnimationFrame(step));
                };

                readerAnimations.set(elementId, requestAnimationFrame(step));
            },

            stopAutoScroll(elementId) {
                const frame = readerAnimations.get(elementId);
                if (frame) {
                    cancelAnimationFrame(frame);
                    readerAnimations.delete(elementId);
                }
            }
        }
    };
})();
