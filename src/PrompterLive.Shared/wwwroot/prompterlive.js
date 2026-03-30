(function () {
    const settingsPrefix = "prompterlive.settings.";
    const cultureSettingKey = settingsPrefix + "culture";
    const defaultCultureName = "en";
    const streamMap = new Map();
    const audioMonitorMap = new Map();
    const readerAnimations = new Map();
    const microphoneMonitorFillSelector = "[data-mic-role='fill']";
    const microphoneMonitorValueSelector = "[data-mic-role='value']";
    const microphoneMonitorActiveState = "active";
    const microphoneMonitorIdleState = "idle";
    const microphoneMonitorLevelMultiplier = 2800;
    const shellAutoHideDelayMs = 2400;
    const shellStateOffline = "offline";
    const shellStateOnline = "online";
    const shellErrorUiId = "blazor-error-ui";
    const shellErrorDetailId = "app-shell-error-detail";
    const shellConnectivityUiId = "app-connectivity-ui";
    const shellConnectivityTitleId = "app-connectivity-title";
    const shellConnectivityMessageId = "app-connectivity-message";
    const shellConnectivityRetryId = "app-connectivity-retry";
    const shellConnectivityDismissId = "app-connectivity-dismiss";
    const shellBootstrapDismissSelector = "[data-testid='diagnostics-bootstrap-dismiss']";
    const shellConsoleErrorPrefix = "[PrompterLive.shell]";
    const shellDiagnosticsBridgeFailureSource = "logger-bridge-failure";
    const shellDiagnosticsReportMethodName = "ReportShellError";
    const shellErrorSourceManual = "manual";
    const shellErrorSourceUnhandledRejection = "unhandledrejection";
    const shellErrorSourceWindowError = "window.error";
    let connectivityHideTimer = 0;
    let shellDiagnosticsLogger = null;
    let pendingShellErrors = [];

    function getBrowserCultures() {
        if (Array.isArray(window.navigator.languages) && window.navigator.languages.length > 0) {
            return window.navigator.languages;
        }

        return [window.navigator.language || defaultCultureName];
    }

    function getStoredCulture() {
        return window.localStorage.getItem(cultureSettingKey) || "";
    }

    function applyDocumentCulture(cultureName) {
        const normalizedCulture = typeof cultureName === "string" && cultureName.trim()
            ? cultureName.trim()
            : defaultCultureName;
        if (document && document.documentElement) {
            document.documentElement.lang = normalizedCulture;
        }

        return normalizedCulture;
    }

    function setShellText(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = value;
        }
    }

    function showShellDetail(detail) {
        const detailElement = document.getElementById(shellErrorDetailId);
        if (!detailElement) {
            return;
        }

        if (!detail) {
            detailElement.hidden = true;
            detailElement.textContent = "";
            return;
        }

        detailElement.hidden = false;
        detailElement.textContent = detail;
    }

    function normalizeShellDetail(detail) {
        return typeof detail === "string"
            ? detail.trim()
            : "";
    }

    function normalizeShellSource(source) {
        return typeof source === "string" && source.trim()
            ? source.trim()
            : shellErrorSourceManual;
    }

    function queueShellError(source, detail) {
        pendingShellErrors.push({
            source: normalizeShellSource(source),
            detail: normalizeShellDetail(detail)
        });
    }

    function reportShellErrorToLogger(source, detail) {
        const normalizedSource = normalizeShellSource(source);
        const normalizedDetail = normalizeShellDetail(detail);
        if (!shellDiagnosticsLogger?.invokeMethodAsync) {
            queueShellError(normalizedSource, normalizedDetail);
            return;
        }

        shellDiagnosticsLogger
            .invokeMethodAsync(shellDiagnosticsReportMethodName, normalizedSource, normalizedDetail)
            .catch(error => {
                queueShellError(normalizedSource, normalizedDetail);
                console.error(
                    shellConsoleErrorPrefix,
                    shellDiagnosticsBridgeFailureSource,
                    error?.message || "");
            });
    }

    function flushPendingShellErrors() {
        if (!shellDiagnosticsLogger?.invokeMethodAsync || pendingShellErrors.length === 0) {
            return;
        }

        const pendingErrors = pendingShellErrors;
        pendingShellErrors = [];

        for (const pendingError of pendingErrors) {
            reportShellErrorToLogger(pendingError.source, pendingError.detail);
        }
    }

    function attachDiagnosticsLogger(logger) {
        shellDiagnosticsLogger = logger || null;
        flushPendingShellErrors();
    }

    function logShellError(source, detail) {
        const normalizedSource = normalizeShellSource(source);
        const normalizedDetail = normalizeShellDetail(detail);
        console.error(shellConsoleErrorPrefix, normalizedSource, normalizedDetail);
        reportShellErrorToLogger(normalizedSource, normalizedDetail);
    }

    function hideBootstrapError() {
        const errorUi = document.getElementById(shellErrorUiId);
        if (errorUi) {
            errorUi.style.display = "none";
        }
    }

    function hideConnectivityStatus() {
        const connectivityUi = document.getElementById(shellConnectivityUiId);
        if (!connectivityUi) {
            return;
        }

        window.clearTimeout(connectivityHideTimer);
        connectivityHideTimer = 0;
        connectivityUi.hidden = true;
        delete connectivityUi.dataset.state;
    }

    function showConnectivityStatus(state) {
        const connectivityUi = document.getElementById(shellConnectivityUiId);
        if (!connectivityUi) {
            return;
        }

        const isOnline = state === shellStateOnline;
        setShellText(shellConnectivityTitleId, isOnline ? "Connection restored" : "Connection lost");
        setShellText(
            shellConnectivityMessageId,
            isOnline
                ? "The browser connection is back. Continue working or reload if anything still looks stale."
                : "Prompter.live is offline. Live routing, cloud sync, and remote publishing will resume when the browser reconnects.");

        connectivityUi.hidden = false;
        connectivityUi.dataset.state = state;

        window.clearTimeout(connectivityHideTimer);
        connectivityHideTimer = 0;

        if (isOnline) {
            connectivityHideTimer = window.setTimeout(hideConnectivityStatus, shellAutoHideDelayMs);
        }
    }

    function showBootstrapError(detail, source = shellErrorSourceManual) {
        const errorUi = document.getElementById(shellErrorUiId);
        if (!errorUi) {
            return;
        }

        logShellError(source, detail);
        showShellDetail(detail);
        errorUi.style.display = "grid";
    }

    function initializeAppShell() {
        const errorUi = document.getElementById(shellErrorUiId);
        if (errorUi) {
            const errorUiObserver = new MutationObserver(() => {
                if (errorUi.style.display && errorUi.style.display !== "none") {
                    errorUi.style.display = "grid";
                }
            });

            errorUiObserver.observe(errorUi, {
                attributes: true,
                attributeFilter: ["style"]
            });
        }

        const bootstrapDismissButton = document.querySelector(shellBootstrapDismissSelector);
        if (bootstrapDismissButton) {
            bootstrapDismissButton.addEventListener("click", hideBootstrapError);
        }

        const connectivityRetryButton = document.getElementById(shellConnectivityRetryId);
        if (connectivityRetryButton) {
            connectivityRetryButton.addEventListener("click", () => window.location.reload());
        }

        const connectivityDismissButton = document.getElementById(shellConnectivityDismissId);
        if (connectivityDismissButton) {
            connectivityDismissButton.addEventListener("click", hideConnectivityStatus);
        }

        window.addEventListener("offline", () => showConnectivityStatus(shellStateOffline));
        window.addEventListener("online", () => showConnectivityStatus(shellStateOnline));
        window.addEventListener("error", event => {
            const detail = event?.message || event?.filename || "";
            if (detail) {
                showBootstrapError(detail, shellErrorSourceWindowError);
            }
        });
        window.addEventListener("unhandledrejection", event => {
            const reason = event?.reason;
            const detail = typeof reason === "string"
                ? reason
                : reason?.message || reason?.toString?.() || "";

            showBootstrapError(detail, shellErrorSourceUnhandledRejection);
        });

        if (window.navigator && window.navigator.onLine === false) {
            showConnectivityStatus(shellStateOffline);
        }
    }

    async function stopStream(stream) {
        if (!stream) {
            return;
        }

        stream.getTracks().forEach(track => track.stop());
    }

    function getMicrophoneMonitorElements(rootElementId) {
        const root = document.getElementById(rootElementId);
        if (!root) {
            return null;
        }

        return {
            root,
            fill: root.querySelector(microphoneMonitorFillSelector),
            value: root.querySelector(microphoneMonitorValueSelector)
        };
    }

    function updateMicrophoneMonitorUi(rootElementId, levelPercent) {
        const elements = getMicrophoneMonitorElements(rootElementId);
        if (!elements) {
            return;
        }

        const normalizedLevel = Number.isFinite(levelPercent)
            ? Math.max(0, Math.min(100, Math.round(levelPercent)))
            : 0;

        elements.root.dataset.liveLevel = normalizedLevel.toString();
        elements.root.dataset.liveState = normalizedLevel > 0
            ? microphoneMonitorActiveState
            : microphoneMonitorIdleState;

        if (elements.fill) {
            elements.fill.style.width = `${normalizedLevel}%`;
        }

        if (elements.value) {
            elements.value.textContent = `${normalizedLevel}%`;
        }
    }

    async function stopMicrophoneLevelMonitor(rootElementId) {
        const monitor = audioMonitorMap.get(rootElementId);
        if (!monitor) {
            updateMicrophoneMonitorUi(rootElementId, 0);
            return;
        }

        audioMonitorMap.delete(rootElementId);

        if (monitor.frameHandle) {
            window.cancelAnimationFrame(monitor.frameHandle);
        }

        monitor.sourceNode?.disconnect();
        monitor.analyser?.disconnect();
        await stopStream(monitor.stream);
        if (monitor.audioContext) {
            await monitor.audioContext.close().catch(() => {});
        }
        updateMicrophoneMonitorUi(rootElementId, 0);
    }

    async function startMicrophoneLevelMonitor(rootElementId, deviceId) {
        const root = document.getElementById(rootElementId);
        if (!root || !navigator.mediaDevices?.getUserMedia) {
            return;
        }

        await stopMicrophoneLevelMonitor(rootElementId);
        updateMicrophoneMonitorUi(rootElementId, 0);

        let stream = null;
        let audioContext = null;
        let analyser = null;
        let sourceNode = null;

        try {
            stream = await navigator.mediaDevices.getUserMedia({
                audio: deviceId ? { deviceId: { exact: deviceId } } : true,
                video: false
            });

            audioContext = new AudioContext();
            analyser = audioContext.createAnalyser();
            analyser.fftSize = 1024;
            analyser.smoothingTimeConstant = 0.82;

            sourceNode = audioContext.createMediaStreamSource(stream);
            sourceNode.connect(analyser);

            const samples = new Uint8Array(analyser.fftSize);
            const monitor = {
                stream,
                audioContext,
                analyser,
                sourceNode,
                frameHandle: 0
            };

            const step = () => {
                if (!audioMonitorMap.has(rootElementId)) {
                    return;
                }

                analyser.getByteTimeDomainData(samples);

                let sumSquares = 0;
                for (let index = 0; index < samples.length; index += 1) {
                    const normalizedSample = (samples[index] - 128) / 128;
                    sumSquares += normalizedSample * normalizedSample;
                }

                const rms = Math.sqrt(sumSquares / samples.length);
                updateMicrophoneMonitorUi(rootElementId, rms * microphoneMonitorLevelMultiplier);
                monitor.frameHandle = window.requestAnimationFrame(step);
            };

            await audioContext.resume().catch(() => {});
            audioMonitorMap.set(rootElementId, monitor);
            step();
        } catch (error) {
            sourceNode?.disconnect();
            analyser?.disconnect();
            await stopStream(stream);
            if (audioContext) {
                await audioContext.close().catch(() => {});
            }
            updateMicrophoneMonitorUi(rootElementId, 0);
            throw error;
        }
    }

    applyDocumentCulture(getStoredCulture() || getBrowserCultures()[0] || defaultCultureName);
    initializeAppShell();

    window.PrompterLive = {
        localization: {
            applyDocumentCulture(cultureName) {
                return applyDocumentCulture(cultureName);
            },
            getBrowserCultures() {
                return getBrowserCultures();
            },
            getStoredCulture() {
                return getStoredCulture();
            },
            setStoredCulture(cultureName) {
                const normalizedCulture = applyDocumentCulture(cultureName);
                window.localStorage.setItem(cultureSettingKey, normalizedCulture);
                return normalizedCulture;
            }
        },
        shell: {
            attachDiagnosticsLogger,
            hideBootstrapError,
            hideConnectivityStatus,
            showBootstrapError,
            showConnectivityOffline() {
                showConnectivityStatus(shellStateOffline);
            },
            showConnectivityOnline() {
                showConnectivityStatus(shellStateOnline);
            }
        },
        storage: {
            load(key) {
                return window.localStorage.getItem(key);
            },
            remove(key) {
                window.localStorage.removeItem(key);
            },
            save(key, value) {
                if (typeof value !== "string") {
                    window.localStorage.removeItem(key);
                    return;
                }

                window.localStorage.setItem(key, value);
            }
        },

        settings: {
            loadJson(key) {
                return window.localStorage.getItem(settingsPrefix + key);
            },
            load(key) {
                try {
                    const raw = window.localStorage.getItem(settingsPrefix + key);
                    return raw ? JSON.parse(raw) : null;
                } catch {
                    return null;
                }
            },
            saveJson(key, json) {
                if (typeof json !== "string") {
                    window.localStorage.removeItem(settingsPrefix + key);
                    return;
                }

                window.localStorage.setItem(settingsPrefix + key, json);
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
            },

            async startMicrophoneLevelMonitor(elementId, deviceId) {
                await startMicrophoneLevelMonitor(elementId, deviceId);
            },

            async stopMicrophoneLevelMonitor(elementId) {
                await stopMicrophoneLevelMonitor(elementId);
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
