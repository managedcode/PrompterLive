(() => {
    const harnessGlobalName = "__prompterOneMediaHarness";
    const mediaDevicesProperty = "mediaDevices";
    const enumerateDevicesMethod = "enumerateDevices";
    const getUserMediaMethod = "getUserMedia";
    const syntheticProperty = "__prompterOneSyntheticMedia";
    const videoKind = "videoinput";
    const audioKind = "audioinput";
    const defaultVideoFrameRate = 24;
    const canvasWidth = 640;
    const canvasHeight = 360;
    const frameIntervalMs = Math.round(1000 / defaultVideoFrameRate);
    const defaultAudioGain = 0.015;
    const primaryCameraId = "browser-cam-primary";
    const secondaryCameraId = "browser-cam-secondary";
    const primaryCameraLabel = "Browser Camera A";
    const secondaryCameraLabel = "Browser Camera B";
    const primaryMicrophoneId = "browser-mic-primary";
    const primaryMicrophoneLabel = "Browser Microphone";
    const primaryCameraColor = "#4fe6cf";
    const secondaryCameraColor = "#f2b866";
    const fallbackDeviceId = "default";
    const exactConstraint = "exact";
    const idealConstraint = "ideal";
    const defaultGroupId = "prompterone-browser-group";
    const primaryCameraGroupId = "prompterone-camera-a";
    const secondaryCameraGroupId = "prompterone-camera-b";
    const primaryMicrophoneGroupId = "prompterone-mic-a";
    const streamMetadataVersion = 1;

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
    const requestLog = [];
    let requestId = 0;

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
        return {
            deviceId: device.deviceId,
            kind: device.kind,
            label: device.label,
            groupId: device.groupId || defaultGroupId,
            toJSON() {
                return {
                    deviceId: device.deviceId,
                    kind: device.kind,
                    label: device.label,
                    groupId: device.groupId || defaultGroupId
                };
            }
        };
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

        if (!requestedDeviceId || requestedDeviceId === fallbackDeviceId) {
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
            context.fillText(device.label, 40, 72);
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
            videoLabel: device.label,
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
        const audioContext = new AudioContext();
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();
        const destination = audioContext.createMediaStreamDestination();
        let cleanedUp = false;

        oscillator.type = "sine";
        oscillator.frequency.value = device.tone;
        gainNode.gain.value = defaultAudioGain;
        oscillator.connect(gainNode);
        gainNode.connect(destination);
        oscillator.start();
        audioContext.resume().catch(() => {});

        function cleanup() {
            if (cleanedUp) {
                return;
            }

            cleanedUp = true;
            oscillator.stop();
            oscillator.disconnect();
            gainNode.disconnect();
            destination.disconnect();
            audioContext.close().catch(() => {});
        }

        const stream = destination.stream;
        const trackMetadata = {
            isSynthetic: true,
            streamMetadataVersion,
            videoDeviceId: null,
            audioDeviceId: device.deviceId,
            videoLabel: null,
            audioLabel: device.label
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
            videoLabel: videoDevice?.label ?? null,
            audioLabel: audioDevice?.label ?? null
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

        return stream;
    }

    Object.defineProperty(mediaDevices, enumerateDevicesMethod, {
        configurable: true,
        enumerable: true,
        value: async () => devices.map(toDeviceDescriptor)
    });

    Object.defineProperty(mediaDevices, getUserMediaMethod, {
        configurable: true,
        enumerable: true,
        value: async constraints => createMediaStream(constraints || {})
    });

    window[harnessGlobalName] = Object.freeze({
        listDevices() {
            return devices.map(device => ({
                deviceId: device.deviceId,
                label: device.label,
                kind: device.kind,
                isDefault: device.isDefault
            }));
        },
        clearRequestLog() {
            requestLog.length = 0;
        },
        getRequestLog() {
            return cloneJson(requestLog);
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
        }
    });
})();
