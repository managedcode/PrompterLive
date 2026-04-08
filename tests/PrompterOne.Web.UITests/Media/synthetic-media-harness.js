(() => {
    const mediaDevicesProperty = "mediaDevices";
    const enumerateDevicesMethod = "enumerateDevices";
    const getUserMediaMethod = "getUserMedia";
    const videoKind = "videoinput";
    const audioKind = "audioinput";
    const audioContextCtor = window.AudioContext || window.webkitAudioContext;
    const defaultVideoFrameRate = 24;
    const canvasWidth = 640;
    const canvasHeight = 360;
    const frameIntervalMs = Math.round(1000 / defaultVideoFrameRate);
    const defaultAudioGain = 0.08;
    const debugAudioLevelMultiplier = 2800;
    const debugAudioMeterFftSize = 1024;
    const primaryCameraId = "browser-cam-primary";
    const secondaryCameraId = "browser-cam-secondary";
    const primaryCameraLabel = "Browser Camera A";
    const secondaryCameraLabel = "Browser Camera B";
    const primaryMicrophoneId = "browser-mic-primary";
    const primaryMicrophoneLabel = "Browser Microphone";
    const primaryCameraColor = "#4fe6cf";
    const secondaryCameraColor = "#f2b866";
    const defaultDeviceId = "default";
    const emptyDeviceLabel = "";
    const exactConstraint = "exact";
    const idealConstraint = "ideal";
    const defaultGroupId = "prompterone-browser-group";
    const primaryCameraGroupId = "prompterone-camera-a";
    const secondaryCameraGroupId = "prompterone-camera-b";
    const primaryMicrophoneGroupId = "prompterone-mic-a";
    const streamMetadataVersion = 1;
    const remoteSourceColors = ["#5fd2ff", "#f98f6f", "#8fd56e", "#f1c75c"];
    const remoteSourceToneBase = 310;
    const runtimeGlobalName = "__prompterOneRuntime";
    const mediaContractProperty = "media";
    const defaultMediaRuntimeContract = Object.freeze({
        concealDeviceIdentitySessionFlag: "__prompterOneConcealDeviceIdentityUntilMediaRequest",
        remoteSourceSeedGlobalName: "__prompterOneRemoteSourceSeed",
        syntheticHarnessGlobalName: "__prompterOneMediaHarness",
        syntheticMetadataProperty: "__prompterOneSyntheticMedia"
    });

    function getMediaRuntimeContract() {
        return window[runtimeGlobalName]?.[mediaContractProperty] ?? defaultMediaRuntimeContract;
    }

    function getMediaRuntimeString(propertyName) {
        const value = getMediaRuntimeContract()?.[propertyName];
        return typeof value === "string" && value.length > 0
            ? value
            : defaultMediaRuntimeContract[propertyName];
    }

    const harnessGlobalName = getMediaRuntimeString("syntheticHarnessGlobalName");
    const syntheticProperty = getMediaRuntimeString("syntheticMetadataProperty");
    const concealIdentitySessionFlag = getMediaRuntimeString("concealDeviceIdentitySessionFlag");
    const remoteSourceSeedGlobal = getMediaRuntimeString("remoteSourceSeedGlobalName");

    if (typeof window[harnessGlobalName] === "object" && window[harnessGlobalName] !== null) {
        return;
    }

    const devices = Object.freeze([
        Object.freeze({
            deviceId: primaryCameraId,
            kind: videoKind,
            label: primaryCameraLabel,
            groupId: primaryCameraGroupId,
            isDefault: true,
            color: primaryCameraColor
        }),
        Object.freeze({
            deviceId: secondaryCameraId,
            kind: videoKind,
            label: secondaryCameraLabel,
            groupId: secondaryCameraGroupId,
            isDefault: false,
            color: secondaryCameraColor
        }),
        Object.freeze({
            deviceId: primaryMicrophoneId,
            kind: audioKind,
            label: primaryMicrophoneLabel,
            groupId: primaryMicrophoneGroupId,
            isDefault: true,
            tone: 220
        })
    ]);
    const deviceLabelOverrides = new Map();
    const requestLog = [];
    const remoteSourcesByConnection = new Map();
    let requestId = 0;
    let captureCapabilities = {
        supportsConcurrentLocalCameraCaptures: true
    };
    let lastAudioError = emptyDeviceLabel;
    let lastAudioLevelPercent = 0;
    let lastAudioMode = emptyDeviceLabel;
    let concealDeviceIdentityUntilMediaRequest =
        window.sessionStorage?.getItem(concealIdentitySessionFlag) === "true";
    let hasResolvedMediaRequest = false;

    const mediaDevices = navigator[mediaDevicesProperty] ?? {};
    if (!(mediaDevicesProperty in navigator)) {
        Object.defineProperty(navigator, mediaDevicesProperty, {
            configurable: true,
            enumerable: true,
            value: mediaDevices
        });
    }

    function cloneJson(value) {
        return JSON.parse(JSON.stringify(value));
    }

    function toDeviceDescriptor(device) {
        const label = resolveDeviceLabel(device);
        return {
            deviceId: resolveDeviceId(device),
            kind: device.kind,
            label,
            groupId: device.groupId || defaultGroupId,
            toJSON() {
                return {
                    deviceId: resolveDeviceId(device),
                    kind: device.kind,
                    label,
                    groupId: device.groupId || defaultGroupId
                };
            }
        };
    }

    function resolveDeviceId(device) {
        if (!device || typeof device !== "object") {
            return emptyDeviceLabel;
        }

        return shouldExposeDeviceIdentity()
            ? (typeof device.deviceId === "string" ? device.deviceId : emptyDeviceLabel)
            : emptyDeviceLabel;
    }

    function resolveDeviceLabel(device) {
        if (!device || typeof device !== "object") {
            return emptyDeviceLabel;
        }

        if (!shouldExposeDeviceIdentity()) {
            return emptyDeviceLabel;
        }

        if (deviceLabelOverrides.has(device.deviceId)) {
            return deviceLabelOverrides.get(device.deviceId) ?? emptyDeviceLabel;
        }

        return typeof device.label === "string"
            ? device.label
            : emptyDeviceLabel;
    }

    function shouldExposeDeviceIdentity() {
        return !concealDeviceIdentityUntilMediaRequest || hasResolvedMediaRequest;
    }

    function readRequestedDeviceId(kindConstraint) {
        if (!kindConstraint || typeof kindConstraint !== "object") {
            return null;
        }

        const deviceIdConstraint = kindConstraint.deviceId;
        if (typeof deviceIdConstraint === "string") {
            return deviceIdConstraint;
        }

        if (Array.isArray(deviceIdConstraint?.[exactConstraint])) {
            return deviceIdConstraint[exactConstraint][0] ?? null;
        }

        if (typeof deviceIdConstraint?.[exactConstraint] === "string") {
            return deviceIdConstraint[exactConstraint];
        }

        if (Array.isArray(deviceIdConstraint?.[idealConstraint])) {
            return deviceIdConstraint[idealConstraint][0] ?? null;
        }

        if (typeof deviceIdConstraint?.[idealConstraint] === "string") {
            return deviceIdConstraint[idealConstraint];
        }

        return null;
    }

    function resolveDevice(kind, requestedDeviceId) {
        const matchingDevices = devices.filter(device => device.kind === kind);
        if (!matchingDevices.length) {
            throw new DOMException(`No ${kind} device is available.`, "NotFoundError");
        }

        if (!requestedDeviceId || requestedDeviceId === defaultDeviceId) {
            return matchingDevices.find(device => device.isDefault) ?? matchingDevices[0];
        }

        return matchingDevices.find(device => device.deviceId === requestedDeviceId)
            ?? matchingDevices.find(device => device.isDefault)
            ?? matchingDevices[0];
    }

    function attachMetadata(target, metadata) {
        if (!target || typeof target !== "object") {
            return;
        }

        Object.defineProperty(target, syntheticProperty, {
            configurable: true,
            enumerable: false,
            value: metadata,
            writable: true
        });
    }

    function createVideoStream(device) {
        const label = resolveDeviceLabel(device);
        const canvas = document.createElement("canvas");
        canvas.width = canvasWidth;
        canvas.height = canvasHeight;
        const context = canvas.getContext("2d");
        let tick = 0;
        let intervalId = 0;
        let cleanedUp = false;

        function renderFrame() {
            if (!context) {
                return;
            }

            tick += 1;
            context.clearRect(0, 0, canvasWidth, canvasHeight);
            context.fillStyle = device.color;
            context.fillRect(0, 0, canvasWidth, canvasHeight);
            context.fillStyle = "rgba(8, 11, 20, 0.32)";
            context.fillRect(24, 24, canvasWidth - 48, canvasHeight - 48);
            context.fillStyle = "#f3e6ca";
            context.font = "bold 32px monospace";
            context.fillText(label, 40, 72);
            context.font = "20px monospace";
            context.fillText(`Frame ${tick}`, 40, 112);
        }

        function cleanup() {
            if (cleanedUp) {
                return;
            }

            cleanedUp = true;
            window.clearInterval(intervalId);
        }

        renderFrame();
        intervalId = window.setInterval(renderFrame, frameIntervalMs);

        const stream = canvas.captureStream(defaultVideoFrameRate);
        const trackMetadata = {
            isSynthetic: true,
            streamMetadataVersion,
            videoDeviceId: device.deviceId,
            audioDeviceId: null,
            videoLabel: label,
            audioLabel: null
        };

        attachMetadata(stream, trackMetadata);
        stream.getTracks().forEach(track => {
            const originalStop = track.stop.bind(track);
            let stopped = false;
            attachMetadata(track, trackMetadata);
            track.stop = () => {
                if (stopped) {
                    return;
                }

                stopped = true;
                cleanup();
                originalStop();
            };
        });

        return stream;
    }

    function createAudioStream(device) {
        const audioContext = audioContextCtor
            ? new audioContextCtor({ latencyHint: "interactive" })
            : null;
        if (!audioContext) {
            throw new DOMException("AudioContext is not available.", "NotSupportedError");
        }
        lastAudioMode = "audio-context";

        const label = resolveDeviceLabel(device);
        const oscillator = audioContext.createOscillator();
        const analyserNode = audioContext.createAnalyser();
        const gainNode = audioContext.createGain();
        const destination = audioContext.createMediaStreamDestination();
        const sinkNode = audioContext.createGain();
        let cleanedUp = false;
        let probeFrameHandle = 0;

        oscillator.type = "sine";
        oscillator.frequency.value = device.tone;
        gainNode.gain.value = defaultAudioGain;
        sinkNode.gain.value = 0;
        oscillator.connect(gainNode);
        gainNode.connect(analyserNode);
        analyserNode.connect(destination);
        analyserNode.connect(sinkNode);
        sinkNode.connect(audioContext.destination);
        analyserNode.fftSize = debugAudioMeterFftSize;
        oscillator.start();
        audioContext.resume().catch(() => {});

        const probeSamples = new Uint8Array(debugAudioMeterFftSize);
        const updateLevel = () => {
            if (cleanedUp) {
                return;
            }

            analyserNode.getByteTimeDomainData(probeSamples);
            let sum = 0;
            for (const sample of probeSamples) {
                const normalizedSample = (sample - 128) / 128;
                sum += normalizedSample * normalizedSample;
            }

            lastAudioLevelPercent = Math.max(0, Math.min(100, Math.round(Math.sqrt(sum / probeSamples.length) * debugAudioLevelMultiplier)));
            probeFrameHandle = window.requestAnimationFrame(updateLevel);
        };
        updateLevel();

        function cleanup() {
            if (cleanedUp) {
                return;
            }

            cleanedUp = true;
            if (probeFrameHandle) {
                window.cancelAnimationFrame(probeFrameHandle);
            }

            oscillator.stop();
            oscillator.disconnect();
            analyserNode.disconnect();
            gainNode.disconnect();
            destination.disconnect();
            sinkNode.disconnect();
            audioContext.close().catch(() => {});
        }

        const stream = destination.stream;
        const trackMetadata = {
            isSynthetic: true,
            streamMetadataVersion,
            videoDeviceId: null,
            audioDeviceId: device.deviceId,
            videoLabel: null,
            audioLabel: label
        };

        attachMetadata(stream, trackMetadata);
        stream.getTracks().forEach(track => {
            const originalStop = track.stop.bind(track);
            let stopped = false;
            attachMetadata(track, trackMetadata);
            track.stop = () => {
                if (stopped) {
                    return;
                }

                stopped = true;
                cleanup();
                originalStop();
            };
        });

        return stream;
    }

    function createMediaStream(constraints) {
        const hasVideo = constraints?.video !== undefined && constraints.video !== false;
        const hasAudio = constraints?.audio !== undefined && constraints.audio !== false;

        if (!hasVideo && !hasAudio) {
            throw new TypeError("Synthetic getUserMedia requires audio or video.");
        }

        const requestedVideoDeviceId = hasVideo ? readRequestedDeviceId(constraints.video) : null;
        const requestedAudioDeviceId = hasAudio ? readRequestedDeviceId(constraints.audio) : null;
        const videoDevice = hasVideo ? resolveDevice(videoKind, requestedVideoDeviceId) : null;
        const audioDevice = hasAudio ? resolveDevice(audioKind, requestedAudioDeviceId) : null;
        const videoStream = videoDevice ? createVideoStream(videoDevice) : null;
        const audioStream = audioDevice ? createAudioStream(audioDevice) : null;
        const stream = new MediaStream([
            ...(videoStream ? videoStream.getVideoTracks() : []),
            ...(audioStream ? audioStream.getAudioTracks() : [])
        ]);
        const metadata = {
            isSynthetic: true,
            streamMetadataVersion,
            videoDeviceId: videoDevice?.deviceId ?? null,
            audioDeviceId: audioDevice?.deviceId ?? null,
            videoLabel: videoDevice ? resolveDeviceLabel(videoDevice) : null,
            audioLabel: audioDevice ? resolveDeviceLabel(audioDevice) : null
        };

        attachMetadata(stream, metadata);
        requestLog.push({
            requestId: ++requestId,
            hasVideo,
            hasAudio,
            requestedVideoDeviceId,
            requestedAudioDeviceId,
            resolvedVideoDeviceId: videoDevice?.deviceId ?? null,
            resolvedAudioDeviceId: audioDevice?.deviceId ?? null
        });
        hasResolvedMediaRequest = true;

        return stream;
    }

    function disposeRemoteSources(items) {
        items.forEach(item => {
            item.stream?.getTracks?.().forEach(track => track.stop());
        });
    }

    function buildRemoteSourceStream(source, index) {
        const sourceId = typeof source?.sourceId === "string" && source.sourceId
            ? source.sourceId
            : `remote-source-${index + 1}`;
        const label = typeof source?.label === "string" && source.label
            ? source.label
            : `Remote Source ${index + 1}`;
        const color = typeof source?.color === "string" && source.color
            ? source.color
            : remoteSourceColors[index % remoteSourceColors.length];
        const tone = Number.isFinite(source?.tone)
            ? source.tone
            : remoteSourceToneBase + (index * 40);
        const videoStream = createVideoStream({
            color,
            deviceId: sourceId,
            kind: videoKind,
            label
        });
        const audioStream = createAudioStream({
            deviceId: sourceId,
            kind: audioKind,
            label,
            tone
        });
        const stream = new MediaStream([
            ...videoStream.getVideoTracks(),
            ...audioStream.getAudioTracks()
        ]);
        const metadata = {
            audioDeviceId: sourceId,
            audioLabel: label,
            isSynthetic: true,
            remoteSourceId: sourceId,
            streamMetadataVersion,
            videoDeviceId: sourceId,
            videoLabel: label
        };

        attachMetadata(stream, metadata);
        stream.getTracks().forEach(track => attachMetadata(track, metadata));

        return {
            label,
            sourceId,
            stream
        };
    }

    Object.defineProperty(mediaDevices, enumerateDevicesMethod, {
        configurable: true,
        enumerable: true,
        writable: true,
        value: async () => devices.map(toDeviceDescriptor)
    });

    Object.defineProperty(mediaDevices, getUserMediaMethod, {
        configurable: true,
        enumerable: true,
        writable: true,
        value: async constraints => createMediaStream(constraints || {})
    });

    window[harnessGlobalName] = Object.freeze({
        listDevices() {
            return devices.map(device => ({
                deviceId: resolveDeviceId(device),
                label: resolveDeviceLabel(device),
                kind: device.kind,
                isDefault: device.isDefault
            }));
        },
        clearDeviceLabels() {
            deviceLabelOverrides.clear();
            devices.forEach(device => {
                deviceLabelOverrides.set(device.deviceId, emptyDeviceLabel);
            });
        },
        restoreDeviceLabels() {
            deviceLabelOverrides.clear();
        },
        clearRequestLog() {
            requestLog.length = 0;
        },
        concealDeviceIdentityUntilRequest() {
            concealDeviceIdentityUntilMediaRequest = true;
            hasResolvedMediaRequest = false;
            window.sessionStorage?.setItem(concealIdentitySessionFlag, "true");
        },
        getRequestLog() {
            return cloneJson(requestLog);
        },
        getRemoteSources(connectionId) {
            if (!remoteSourcesByConnection.has(connectionId)) {
                const seededSources = window[remoteSourceSeedGlobal]?.[connectionId];
                if (Array.isArray(seededSources) && seededSources.length > 0) {
                    this.setRemoteSources(connectionId, seededSources);
                }
            }

            return remoteSourcesByConnection.get(connectionId) ?? [];
        },
        getElementState(elementId) {
            const element = document.getElementById(elementId);
            const stream = element?.srcObject;
            const metadata = stream?.[syntheticProperty] ?? null;

            return {
                hasElement: Boolean(element),
                hasStream: stream instanceof MediaStream,
                paused: Boolean(element?.paused),
                readyState: Number.isFinite(element?.readyState) ? element.readyState : -1,
                videoTrackCount: stream instanceof MediaStream ? stream.getVideoTracks().length : 0,
                audioTrackCount: stream instanceof MediaStream ? stream.getAudioTracks().length : 0,
                metadata
            };
        },
        getCaptureCapabilities() {
            return cloneJson(captureCapabilities);
        },
        setRemoteSources(connectionId, sources) {
            const existing = remoteSourcesByConnection.get(connectionId) ?? [];
            disposeRemoteSources(existing);
            remoteSourcesByConnection.set(
                connectionId,
                (Array.isArray(sources) ? sources : []).map(buildRemoteSourceStream));
        },
        setCaptureCapabilities(nextCapabilities) {
            captureCapabilities = {
                supportsConcurrentLocalCameraCaptures:
                    nextCapabilities?.supportsConcurrentLocalCameraCaptures !== false
            };
        },
        clearRemoteSources(connectionId) {
            if (typeof connectionId === "string" && connectionId) {
                const existing = remoteSourcesByConnection.get(connectionId) ?? [];
                disposeRemoteSources(existing);
                remoteSourcesByConnection.delete(connectionId);
                return;
            }

            Array.from(remoteSourcesByConnection.values()).forEach(disposeRemoteSources);
            remoteSourcesByConnection.clear();
        },
        restoreDeviceIdentity() {
            concealDeviceIdentityUntilMediaRequest = false;
            hasResolvedMediaRequest = false;
            window.sessionStorage?.removeItem(concealIdentitySessionFlag);
        }
    });
})();
