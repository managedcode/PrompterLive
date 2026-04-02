(function () {
    const audioInputKind = "audioinput";
    const audioOutputKind = "audiooutput";
    const cameraCaptureMap = new Map();
    const cameraTrackMap = new Map();
    const defaultDeviceId = "default";
    const interopNamespace = "BrowserMediaInterop";
    const liveKitClientGlobal = "LivekitClient";
    const microphoneMonitorLevelMultiplier = 2800;
    const monitorMap = new Map();
    const remoteCaptureMap = new Map();
    const remoteStreamMap = new Map();
    const syntheticHarnessGlobal = "__prompterOneMediaHarness";
    const syntheticMetadataProperty = "__prompterOneSyntheticMedia";
    const videoInputKind = "videoinput";

    function copySyntheticMetadata(source, target) {
        const metadata = source?.[syntheticMetadataProperty];
        if (!metadata || !target || typeof target !== "object") {
            return;
        }

        setSyntheticMetadata(target, metadata);
    }

    function setSyntheticMetadata(target, metadata) {
        if (!metadata || !target || typeof target !== "object") {
            return;
        }

        Object.defineProperty(target, syntheticMetadataProperty, {
            configurable: true,
            enumerable: false,
            value: metadata,
            writable: true
        });
    }

    function createAudioAnalyserFallback(track, options) {
        const resolvedOptions = Object.assign({
            cloneTrack: false,
            fftSize: 2048,
            smoothingTimeConstant: 0.8,
            minDecibels: -100,
            maxDecibels: -80
        }, options);
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (!AudioContext) {
            throw new Error("Audio Context not supported on this browser");
        }

        const audioContext = new AudioContext({ latencyHint: "interactive" });
        const sourceTrack = resolvedOptions.cloneTrack ? track.mediaStreamTrack.clone() : track.mediaStreamTrack;
        const sourceNode = audioContext.createMediaStreamSource(new MediaStream([sourceTrack]));
        const analyser = audioContext.createAnalyser();
        analyser.minDecibels = resolvedOptions.minDecibels;
        analyser.maxDecibels = resolvedOptions.maxDecibels;
        analyser.fftSize = resolvedOptions.fftSize;
        analyser.smoothingTimeConstant = resolvedOptions.smoothingTimeConstant;
        sourceNode.connect(analyser);

        const dataArray = new Uint8Array(analyser.frequencyBinCount);

        return {
            analyser,
            calculateVolume() {
                analyser.getByteFrequencyData(dataArray);

                let sum = 0;
                for (const amplitude of dataArray) {
                    sum += Math.pow(amplitude / 255, 2);
                }

                return Math.sqrt(sum / dataArray.length);
            },
            async cleanup() {
                sourceNode.disconnect();
                analyser.disconnect();
                await audioContext.close().catch(() => {});

                if (resolvedOptions.cloneTrack) {
                    sourceTrack.stop();
                }
            }
        };
    }

    function createWrappedLocalTrack(mediaStreamTrack) {
        const mediaStream = new MediaStream([mediaStreamTrack]);
        copySyntheticMetadata(mediaStreamTrack, mediaStream);
        const attachedElements = new Set();

        return {
            kind: mediaStreamTrack.kind,
            mediaStreamTrack,
            attach(element) {
                const target = element ?? document.createElement(mediaStreamTrack.kind === audioInputKind ? "audio" : "video");
                target.srcObject = mediaStream;
                attachedElements.add(target);
                copySyntheticMetadata(mediaStreamTrack, target.srcObject);
                return target;
            },
            detach(element) {
                const targets = element ? [element] : Array.from(attachedElements);
                targets.forEach(target => {
                    if (target?.srcObject === mediaStream) {
                        target.srcObject = null;
                    }

                    attachedElements.delete(target);
                });

                return element ?? targets;
            },
            stop() {
                mediaStreamTrack.stop();
                attachedElements.forEach(target => {
                    if (target?.srcObject === mediaStream) {
                        target.srcObject = null;
                    }
                });
                attachedElements.clear();
            }
        };
    }

    function createWrappedRemoteVideoTrack(sourceId, stream) {
        const mediaStream = stream instanceof MediaStream ? stream : new MediaStream();
        const mediaStreamTrack = mediaStream.getVideoTracks()[0];
        if (!mediaStreamTrack) {
            throw new Error(`Remote stream '${sourceId}' does not contain a video track.`);
        }

        const attachedElements = new Set();

        return {
            kind: mediaStreamTrack.kind,
            mediaStream,
            mediaStreamTrack,
            attach(element) {
                const target = element ?? document.createElement("video");
                target.srcObject = mediaStream;
                attachedElements.add(target);
                copySyntheticMetadata(mediaStream, target.srcObject);
                return target;
            },
            detach(element) {
                const targets = element ? [element] : Array.from(attachedElements);
                targets.forEach(target => {
                    if (target?.srcObject === mediaStream) {
                        target.srcObject = null;
                    }

                    attachedElements.delete(target);
                });

                return element ?? targets;
            },
            stop() {
            }
        };
    }

    function hasLiveKitMediaClient(client) {
        return Boolean(
            client?.Room
            && typeof client.createAudioAnalyser === "function"
            && typeof client.createLocalAudioTrack === "function"
            && typeof client.createLocalTracks === "function"
            && typeof client.createLocalVideoTrack === "function");
    }

    function hasSyntheticMediaHarness() {
        return typeof window[syntheticHarnessGlobal] === "object" && window[syntheticHarnessGlobal] !== null;
    }

    async function getLocalDevicesFallback(kind, requestPermissions) {
        if (requestPermissions && navigator.mediaDevices?.getUserMedia && (kind === audioInputKind || kind === videoInputKind)) {
            const permissionStream = await navigator.mediaDevices.getUserMedia({
                audio: kind === audioInputKind,
                video: kind === videoInputKind
            });
            permissionStream.getTracks().forEach(track => track.stop());
        }

        if (!navigator.mediaDevices?.enumerateDevices) {
            return [];
        }

        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.filter(device => device.kind === kind);
    }

    async function createLocalTracksFallback(options) {
        if (!navigator.mediaDevices?.getUserMedia) {
            throw new Error("Browser media devices API is not available.");
        }

        const constraints = {
            audio: options?.audio ?? false,
            video: options?.video ?? false
        };

        if (!constraints.audio && !constraints.video) {
            throw new TypeError("Synthetic local track capture requires audio or video.");
        }

        const stream = await navigator.mediaDevices.getUserMedia(constraints);
        return stream.getTracks().map(createWrappedLocalTrack);
    }

    function buildSyntheticLiveKitClient(baseClient) {
        const roomType = baseClient?.Room ?? class Room {};
        roomType.getLocalDevices ??= getLocalDevicesFallback;

        return Object.assign({
            Room: roomType,
            Track: {
                Source: {
                    Camera: "camera",
                    Microphone: "microphone"
                }
            },
            createAudioAnalyser: createAudioAnalyserFallback,
            createLocalAudioTrack(options) {
                return createLocalTracksFallback({
                    audio: options ?? true,
                    video: false
                }).then(tracks => tracks[0]);
            },
            createLocalTracks: createLocalTracksFallback,
            createLocalVideoTrack(options) {
                return createLocalTracksFallback({
                    audio: false,
                    video: options ?? true
                }).then(tracks => tracks[0]);
            }
        }, baseClient ?? {});
    }

    function getLiveKitClient() {
        const client = window[liveKitClientGlobal];
        if (hasLiveKitMediaClient(client)) {
            return client;
        }

        if (hasSyntheticMediaHarness()) {
            return buildSyntheticLiveKitClient(client);
        }

        throw new Error("LiveKit client runtime is not available.");
    }

    function getTrackCaptureOptions(deviceId) {
        return deviceId ? { deviceId } : true;
    }

    function getCaptureKey(deviceId) {
        return deviceId || defaultDeviceId;
    }

    function getVideoElement(elementId) {
        const element = document.getElementById(elementId);
        return element instanceof HTMLVideoElement ? element : null;
    }

    function normalizeDevice(device, kind) {
        const deviceId = typeof device?.deviceId === "string"
            ? device.deviceId
            : "";
        const label = typeof device?.label === "string"
            ? device.label.trim()
            : "";

        return {
            deviceId,
            isDefault: device?.isDefault === true || deviceId === defaultDeviceId,
            kind,
            label
        };
    }

    async function notifyMonitorLevel(monitor, levelPercent) {
        if (!monitor?.observer) {
            return;
        }

        const normalizedLevel = Number.isFinite(levelPercent)
            ? Math.max(0, Math.min(100, Math.round(levelPercent)))
            : 0;

        await monitor.observer.invokeMethodAsync("UpdateLevel", normalizedLevel).catch(() => {});
    }

    async function loadDevicesForKind(kind) {
        const liveKitClient = getLiveKitClient();

        try {
            return await liveKitClient.Room.getLocalDevices(kind, false);
        } catch {
            if (!navigator.mediaDevices?.enumerateDevices) {
                return [];
            }

            const devices = await navigator.mediaDevices.enumerateDevices();
            return devices.filter(device => device.kind === kind);
        }
    }

    async function releaseCameraTrack(elementId) {
        const preview = cameraTrackMap.get(elementId);
        if (preview) {
            cameraTrackMap.delete(elementId);

            try {
                preview.track.detach(preview.element);
            } catch {
            }

            await releaseSharedVideoCapture(preview.captureKey);
        }

        const element = getVideoElement(elementId);
        if (element) {
            element.pause?.();
            element.srcObject = null;
        }
    }

    async function acquireCameraCapture(deviceId) {
        const captureKey = getCaptureKey(deviceId);
        let capture = cameraCaptureMap.get(captureKey);
        if (!capture) {
            const liveKitClient = getLiveKitClient();
            capture = {
                refCount: 0,
                track: await liveKitClient.createLocalVideoTrack(getTrackCaptureOptions(deviceId))
            };
            cameraCaptureMap.set(captureKey, capture);
        }

        capture.refCount += 1;

        return {
            captureKey,
            stream: new MediaStream([capture.track.mediaStreamTrack]),
            track: capture.track
        };
    }

    async function releaseCameraCapture(captureKey) {
        const capture = cameraCaptureMap.get(captureKey);
        if (!capture) {
            return;
        }

        capture.refCount = Math.max(0, capture.refCount - 1);
        if (capture.refCount > 0) {
            return;
        }

        cameraCaptureMap.delete(captureKey);

        try {
            capture.track.stop();
        } catch {
        }
    }

    function removeAttachedRemoteElements(captureKey) {
        for (const [elementId, preview] of cameraTrackMap.entries()) {
            if (preview.captureKey !== captureKey) {
                continue;
            }

            preview.track.detach(preview.element);
            cameraTrackMap.delete(elementId);
            const element = getVideoElement(elementId);
            element?.pause?.();
            if (element) {
                element.srcObject = null;
            }
        }
    }

    async function acquireRemoteCameraCapture(sourceId) {
        const captureKey = getCaptureKey(sourceId);
        let capture = remoteCaptureMap.get(captureKey);
        if (!capture) {
            const stream = remoteStreamMap.get(captureKey);
            if (!(stream instanceof MediaStream)) {
                throw new Error(`Remote stream '${sourceId}' is not registered.`);
            }

            capture = {
                refCount: 0,
                stream,
                track: createWrappedRemoteVideoTrack(captureKey, stream)
            };
            remoteCaptureMap.set(captureKey, capture);
        }

        capture.refCount += 1;

        return {
            captureKey,
            stream: capture.stream,
            track: capture.track
        };
    }

    async function releaseRemoteCameraCapture(captureKey) {
        const capture = remoteCaptureMap.get(captureKey);
        if (!capture) {
            return;
        }

        capture.refCount = Math.max(0, capture.refCount - 1);
        if (capture.refCount > 0) {
            return;
        }

        capture.track.detach();
        remoteCaptureMap.delete(captureKey);
    }

    async function acquireSharedVideoCapture(sourceIdOrDeviceId) {
        return remoteStreamMap.has(getCaptureKey(sourceIdOrDeviceId))
            ? await acquireRemoteCameraCapture(sourceIdOrDeviceId)
            : await acquireCameraCapture(sourceIdOrDeviceId);
    }

    async function releaseSharedVideoCapture(captureKey) {
        if (remoteCaptureMap.has(captureKey)) {
            await releaseRemoteCameraCapture(captureKey);
            return;
        }

        await releaseCameraCapture(captureKey);
    }

    function registerRemoteStream(sourceId, stream, metadata) {
        if (!(stream instanceof MediaStream)) {
            throw new Error(`Remote source '${sourceId}' must provide a MediaStream.`);
        }

        if (metadata && typeof metadata === "object") {
            setSyntheticMetadata(stream, metadata);
            stream.getTracks().forEach(track => setSyntheticMetadata(track, metadata));
        }

        remoteStreamMap.set(getCaptureKey(sourceId), stream);
    }

    function unregisterRemoteStream(sourceId) {
        const captureKey = getCaptureKey(sourceId);
        remoteStreamMap.delete(captureKey);
        removeAttachedRemoteElements(captureKey);
        void releaseRemoteCameraCapture(captureKey);
    }

    async function releaseMonitor(elementId) {
        const monitor = monitorMap.get(elementId);
        if (!monitor) {
            return;
        }

        monitorMap.delete(elementId);

        if (monitor.frameHandle) {
            window.cancelAnimationFrame(monitor.frameHandle);
        }

        await monitor.analyserHandle.cleanup().catch(() => {});

        try {
            monitor.track.stop();
        } catch {
        }

        await notifyMonitorLevel(monitor, 0);
    }

    async function requestMediaPermissions() {
        const liveKitClient = getLiveKitClient();
        let tracks = [];

        try {
            tracks = await liveKitClient.createLocalTracks({
                audio: true,
                video: true
            });
        } catch {
        } finally {
            tracks.forEach(track => {
                try {
                    track.stop();
                } catch {
                }
            });
        }
    }

    window[interopNamespace] = {
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
            await requestMediaPermissions();
            return await window[interopNamespace].queryPermissions();
        },

        async listDevices() {
            const orderedKinds = [videoInputKind, audioInputKind, audioOutputKind];
            const groups = await Promise.all(orderedKinds.map(loadDevicesForKind));

            return groups.flatMap((devices, groupIndex) =>
                devices.map(device => normalizeDevice(device, orderedKinds[groupIndex])));
        },

        async attachCamera(elementId, deviceId, muted) {
            const element = getVideoElement(elementId);
            if (!element) {
                return;
            }

            await releaseCameraTrack(elementId);

            const capture = await acquireSharedVideoCapture(deviceId);
            element.autoplay = true;
            element.muted = muted !== false;
            element.playsInline = true;
            capture.track.attach(element);
            copySyntheticMetadata(capture.stream, element.srcObject);
            await element.play().catch(() => {});

            cameraTrackMap.set(elementId, { captureKey: capture.captureKey, element, track: capture.track });
        },

        async createLocalAudioTrack(deviceId) {
            const liveKitClient = getLiveKitClient();
            return await liveKitClient.createLocalAudioTrack(getTrackCaptureOptions(deviceId));
        },

        async createSharedCameraTrack(deviceId) {
            return await acquireSharedVideoCapture(deviceId);
        },

        async detachCamera(elementId) {
            await releaseCameraTrack(elementId);
        },

        async releaseSharedCameraTrack(captureKey) {
            await releaseSharedVideoCapture(captureKey);
        },

        hasRegisteredRemoteStream(sourceId) {
            return remoteStreamMap.has(getCaptureKey(sourceId));
        },

        registerRemoteStream(sourceId, stream, metadata) {
            registerRemoteStream(sourceId, stream, metadata);
        },

        unregisterRemoteStream(sourceId) {
            unregisterRemoteStream(sourceId);
        },

        async startMicrophoneLevelMonitor(elementId, deviceId, observer) {
            const liveKitClient = getLiveKitClient();

            await releaseMonitor(elementId);

            let analyserHandle = null;
            let track = null;

            try {
                track = await liveKitClient.createLocalAudioTrack(getTrackCaptureOptions(deviceId));
                analyserHandle = liveKitClient.createAudioAnalyser(track, {
                    cloneTrack: false,
                    fftSize: 1024,
                    smoothingTimeConstant: 0.82
                });

                const monitor = {
                    analyserHandle,
                    frameHandle: 0,
                    observer,
                    track
                };

                const step = () => {
                    if (!monitorMap.has(elementId)) {
                        return;
                    }

                    const volume = analyserHandle.calculateVolume();
                    void notifyMonitorLevel(monitor, volume * microphoneMonitorLevelMultiplier);
                    monitor.frameHandle = window.requestAnimationFrame(step);
                };

                monitorMap.set(elementId, monitor);
                await notifyMonitorLevel(monitor, 0);
                step();
            } catch (error) {
                if (analyserHandle) {
                    await analyserHandle.cleanup().catch(() => {});
                }

                if (track) {
                    try {
                        track.stop();
                    } catch {
                    }
                }

                await notifyMonitorLevel({ observer }, 0);
                throw error;
            }
        },

        async stopMicrophoneLevelMonitor(elementId) {
            await releaseMonitor(elementId);
        }
    };
})();
