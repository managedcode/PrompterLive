(function () {
    const audioInputKind = "audioinput";
    const audioOutputKind = "audiooutput";
    const cameraCaptureMap = new Map();
    const cameraTrackMap = new Map();
    const defaultDeviceId = "default";
    const microphoneMonitorLevelMultiplier = 2800;
    const monitorMap = new Map();
    const pendingCameraCaptureMap = new Map();
    const readerRecordingTimesliceMs = 500;
    const recordingDefaultExtension = "webm";
    const readerRecordingMimeCandidates = Object.freeze({
        audio: [
            "audio/webm;codecs=opus",
            "audio/webm"
        ],
        video: [
            "video/webm;codecs=vp9,opus",
            "video/webm;codecs=vp8,opus",
            "video/webm"
        ]
    });
    let readerRecordingSession = null;
    const remoteCaptureMap = new Map();
    const remoteStreamMap = new Map();
    const appleVendorFragment = "Apple";
    const touchMacPlatform = "MacIntel";
    const videoInputKind = "videoinput";
    const runtimeGlobalName = "__prompterOneRuntime";
    const mediaContractProperty = "media";
    const defaultMediaRuntimeContract = Object.freeze({
        browserMediaInteropNamespace: "BrowserMediaInterop",
        captureCapabilitiesOverrideGlobalName: "__prompterOneMediaCapabilityOverride",
        liveKitClientGlobalName: "LivekitClient",
        mediaHarnessEnabledProperty: "mediaHarnessEnabled",
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

    function isMediaHarnessEnabled() {
        return window[runtimeGlobalName]?.[getMediaRuntimeString("mediaHarnessEnabledProperty")] === true;
    }

    function copySyntheticMetadata(source, target) {
        const metadata = source?.[getMediaRuntimeString("syntheticMetadataProperty")];
        if (!metadata || !target || typeof target !== "object") {
            return;
        }

        setSyntheticMetadata(target, metadata);
    }

    function setSyntheticMetadata(target, metadata) {
        if (!metadata || !target || typeof target !== "object") {
            return;
        }

        Object.defineProperty(target, getMediaRuntimeString("syntheticMetadataProperty"), {
            configurable: true,
            enumerable: false,
            value: metadata,
            writable: true
        });
    }

    function createSyntheticAudioAnalyserFallback(track, options) {
        const resolvedOptions = Object.assign({
            cloneTrack: false
        }, options);
        const sourceTrack = resolvedOptions.cloneTrack
            ? track?.mediaStreamTrack?.clone?.() ?? null
            : null;
        let sampleIndex = 0;

        return {
            calculateVolume() {
                sampleIndex += 1;
                return 0.02 + (Math.abs(Math.sin(sampleIndex / 3)) * 0.015);
            },
            async cleanup() {
                sourceTrack?.stop?.();
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
        const harnessGlobalName = getMediaRuntimeString("syntheticHarnessGlobalName");
        return isMediaHarnessEnabled()
            && typeof window[harnessGlobalName] === "object"
            && window[harnessGlobalName] !== null;
    }

    function normalizeCaptureCapabilities(rawCapabilities) {
        return {
            supportsConcurrentLocalCameraCaptures: rawCapabilities?.supportsConcurrentLocalCameraCaptures !== false
        };
    }

    function isAppleMobileWebKit() {
        const userAgent = typeof navigator.userAgent === "string" ? navigator.userAgent : "";
        const platform = typeof navigator.platform === "string" ? navigator.platform : "";
        const vendor = typeof navigator.vendor === "string" ? navigator.vendor : "";
        const maxTouchPoints = Number.isFinite(navigator.maxTouchPoints) ? navigator.maxTouchPoints : 0;
        const isAppleMobileUserAgent = /iPad|iPhone|iPod/.test(userAgent);
        const isTouchMac = platform === touchMacPlatform && maxTouchPoints > 1;

        return vendor.includes(appleVendorFragment) && (isAppleMobileUserAgent || isTouchMac);
    }

    function getCaptureCapabilities() {
        const overriddenCapabilities = isMediaHarnessEnabled()
            ? window[getMediaRuntimeString("captureCapabilitiesOverrideGlobalName")]
            : null;
        if (overriddenCapabilities && typeof overriddenCapabilities === "object") {
            return normalizeCaptureCapabilities(overriddenCapabilities);
        }

        if (hasSyntheticMediaHarness()) {
            const harnessCapabilities = window[getMediaRuntimeString("syntheticHarnessGlobalName")]?.getCaptureCapabilities?.();
            if (harnessCapabilities && typeof harnessCapabilities === "object") {
                return normalizeCaptureCapabilities(harnessCapabilities);
            }
        }

        return normalizeCaptureCapabilities({
            supportsConcurrentLocalCameraCaptures: !isAppleMobileWebKit()
        });
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

        return Object.assign({}, baseClient ?? {}, {
            Room: roomType,
            Track: {
                Source: {
                    Camera: "camera",
                    Microphone: "microphone"
                }
            },
            createAudioAnalyser: createSyntheticAudioAnalyserFallback,
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
        });
    }

    function getLiveKitClient() {
        const client = window[getMediaRuntimeString("liveKitClientGlobalName")];
        if (hasSyntheticMediaHarness()) {
            return buildSyntheticLiveKitClient(client);
        }

        if (hasLiveKitMediaClient(client)) {
            return client;
        }

        throw new Error("LiveKit client runtime is not available.");
    }

    function getTrackCaptureOptions(deviceId) {
        return deviceId ? { deviceId } : true;
    }

    function readRequestValue(request, camelName, pascalName, fallback = null) {
        if (request && Object.prototype.hasOwnProperty.call(request, camelName)) {
            return request[camelName];
        }

        if (request && Object.prototype.hasOwnProperty.call(request, pascalName)) {
            return request[pascalName];
        }

        return fallback;
    }

    function normalizeBooleanOption(value, fallback = false) {
        return typeof value === "boolean" ? value : fallback;
    }

    function normalizePositiveNumberOption(value) {
        const number = Number(value);
        return Number.isFinite(number) && number > 0 ? number : null;
    }

    function buildAudioCaptureOptions(request) {
        const options = {};
        const deviceId = readRequestValue(request, "microphoneDeviceId", "MicrophoneDeviceId", "");
        if (typeof deviceId === "string" && deviceId.length > 0) {
            options.deviceId = deviceId;
        }

        options.echoCancellation = normalizeBooleanOption(readRequestValue(request, "echoCancellation", "EchoCancellation"), true);
        options.noiseSuppression = normalizeBooleanOption(readRequestValue(request, "noiseSuppression", "NoiseSuppression"), true);
        options.autoGainControl = normalizeBooleanOption(readRequestValue(request, "autoGainControl", "AutoGainControl"), true);

        if (normalizeBooleanOption(readRequestValue(request, "voiceIsolation", "VoiceIsolation"), false)) {
            options.voiceIsolation = true;
        }

        const channelCount = normalizePositiveNumberOption(readRequestValue(request, "channelCount", "ChannelCount"));
        const sampleRate = normalizePositiveNumberOption(readRequestValue(request, "sampleRate", "SampleRate"));
        const sampleSize = normalizePositiveNumberOption(readRequestValue(request, "sampleSize", "SampleSize"));
        if (channelCount !== null) {
            options.channelCount = channelCount;
        }

        if (sampleRate !== null) {
            options.sampleRate = sampleRate;
        }

        if (sampleSize !== null) {
            options.sampleSize = sampleSize;
        }

        return options;
    }

    function resolveSupportedRecordingMimeType(includeVideo) {
        if (typeof MediaRecorder === "undefined") {
            throw new Error("MediaRecorder is not available in this browser.");
        }

        const candidates = includeVideo
            ? readerRecordingMimeCandidates.video
            : readerRecordingMimeCandidates.audio;
        for (const candidate of candidates) {
            if (MediaRecorder.isTypeSupported(candidate)) {
                return candidate;
            }
        }

        return includeVideo ? "video/webm" : "audio/webm";
    }

    function getRecordingFileExtension(mimeType) {
        const baseType = String(mimeType || "").split(";")[0].toLowerCase();
        if (baseType.includes("/ogg")) {
            return "ogg";
        }

        if (baseType.includes("/mp4")) {
            return "mp4";
        }

        return recordingDefaultExtension;
    }

    function sanitizeRecordingFileStem(fileStem) {
        const stem = String(fileStem || "").trim().toLowerCase();
        const safe = stem.replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
        return safe || "reader-rehearsal";
    }

    function buildReaderRecordingFileName(fileStem, mimeType) {
        const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
        return `${sanitizeRecordingFileStem(fileStem)}-${timestamp}.${getRecordingFileExtension(mimeType)}`;
    }

    async function saveReaderRecordingBlob(blob, fileName, mimeType) {
        if (typeof window.showSaveFilePicker === "function") {
            const handle = await window.showSaveFilePicker({
                suggestedName: fileName,
                types: [
                    {
                        accept: {
                            [String(mimeType || "video/webm").split(";")[0]]: [`.${getRecordingFileExtension(mimeType)}`]
                        },
                        description: "PrompterOne reader recording"
                    }
                ]
            });
            const writable = await handle.createWritable();
            await writable.write(blob);
            await writable.close();
            return;
        }

        const objectUrl = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = objectUrl;
        link.download = fileName;
        link.style.display = "none";
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    }

    function getCaptureKey(deviceId) {
        return deviceId || defaultDeviceId;
    }

    function getVideoElement(elementId) {
        const element = document.getElementById(elementId);
        return element instanceof HTMLVideoElement ? element : null;
    }

    function releaseLocalTrack(track) {
        if (!track) {
            return;
        }

        try {
            track.detach?.();
        } catch {
        }

        try {
            track.stop?.();
        } catch {
        }

        try {
            track.mediaStreamTrack?.stop?.();
        } catch {
        }
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
        if (hasSyntheticMediaHarness()) {
            const syntheticDevices = window[getMediaRuntimeString("syntheticHarnessGlobalName")]?.listDevices?.();
            if (Array.isArray(syntheticDevices)) {
                return syntheticDevices.filter(device => device?.kind === kind);
            }
        }

        if (navigator.mediaDevices?.enumerateDevices) {
            try {
                const devices = await navigator.mediaDevices.enumerateDevices();
                return devices.filter(device => device.kind === kind);
            } catch {
            }
        }

        try {
            const liveKitClient = getLiveKitClient();
            return await liveKitClient.Room.getLocalDevices(kind, false);
        } catch {
            return [];
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

    async function releaseAllCameraTracks() {
        for (const elementId of Array.from(cameraTrackMap.keys())) {
            await releaseCameraTrack(elementId);
        }
    }

    function createCameraCapture(deviceId) {
        const liveKitClient = getLiveKitClient();
        return liveKitClient.createLocalVideoTrack(getTrackCaptureOptions(deviceId)).then(track => ({
            refCount: 0,
            track
        }));
    }

    async function getOrCreateCameraCapture(captureKey, deviceId) {
        const existingCapture = cameraCaptureMap.get(captureKey);
        if (existingCapture) {
            return existingCapture;
        }

        let pendingCapture = pendingCameraCaptureMap.get(captureKey);
        if (!pendingCapture) {
            pendingCapture = createCameraCapture(deviceId);
            pendingCameraCaptureMap.set(captureKey, pendingCapture);
        }

        try {
            const resolvedCapture = await pendingCapture;
            if (!cameraCaptureMap.has(captureKey)) {
                cameraCaptureMap.set(captureKey, resolvedCapture);
            }

            return cameraCaptureMap.get(captureKey);
        } finally {
            if (pendingCameraCaptureMap.get(captureKey) === pendingCapture) {
                pendingCameraCaptureMap.delete(captureKey);
            }
        }
    }

    async function acquireCameraCapture(deviceId) {
        const captureKey = getCaptureKey(deviceId);
        const capture = await getOrCreateCameraCapture(captureKey, deviceId);

        capture.refCount += 1;
        const stream = new MediaStream([capture.track.mediaStreamTrack]);
        copySyntheticMetadata(capture.track.mediaStreamTrack, stream);

        return {
            captureKey,
            stream,
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
        releaseLocalTrack(capture.track);
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
        releaseLocalTrack(monitor.track);

        await notifyMonitorLevel(monitor, 0);
    }

    async function stopActiveReaderRecording() {
        const session = readerRecordingSession;
        if (!session) {
            return {
                active: false,
                fileName: "",
                mimeType: "",
                mode: "",
                sizeBytes: 0
            };
        }

        readerRecordingSession = null;
        const stopped = new Promise((resolve, reject) => {
            session.recorder.addEventListener("stop", resolve, { once: true });
            session.recorder.addEventListener("error", event => reject(event?.error ?? new Error("MediaRecorder failed.")), { once: true });
        });

        if (session.recorder.state !== "inactive") {
            session.recorder.stop();
        }

        await stopped;
        const blob = new Blob(session.chunks, { type: session.mimeType });
        await saveReaderRecordingBlob(blob, session.fileName, session.mimeType);

        session.releaseTracks();

        return {
            active: false,
            fileName: session.fileName,
            mimeType: session.mimeType,
            mode: session.mode,
            sizeBytes: blob.size
        };
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
            tracks.forEach(releaseLocalTrack);
        }
    }

    window[getMediaRuntimeString("browserMediaInteropNamespace")] = {
        getCaptureCapabilities() {
            return getCaptureCapabilities();
        },

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
            return await window[getMediaRuntimeString("browserMediaInteropNamespace")].queryPermissions();
        },

        async startReaderRecording(request) {
            if (readerRecordingSession) {
                await stopActiveReaderRecording();
            }

            const liveKitClient = getLiveKitClient();
            const mode = readRequestValue(request, "mode", "Mode", "video-audio");
            const includeVideo = mode !== "audio";
            const fileStem = readRequestValue(request, "fileStem", "FileStem", "reader-rehearsal");
            const cameraDeviceId = readRequestValue(request, "cameraDeviceId", "CameraDeviceId", "");
            const mimeType = resolveSupportedRecordingMimeType(includeVideo);
            const tracks = [];
            let videoCapture = null;
            let audioTrack = null;

            try {
                audioTrack = await liveKitClient.createLocalAudioTrack(buildAudioCaptureOptions(request));
                tracks.push(audioTrack.mediaStreamTrack);

                if (includeVideo) {
                    videoCapture = await acquireSharedVideoCapture(cameraDeviceId);
                    tracks.push(videoCapture.track.mediaStreamTrack);
                }

                const stream = new MediaStream(tracks);
                const chunks = [];
                const recorder = new MediaRecorder(stream, {
                    mimeType
                });
                recorder.addEventListener("dataavailable", event => {
                    if (event.data?.size > 0) {
                        chunks.push(event.data);
                    }
                });
                recorder.start(readerRecordingTimesliceMs);

                const fileName = buildReaderRecordingFileName(fileStem, mimeType);
                readerRecordingSession = {
                    chunks,
                    fileName,
                    mimeType,
                    mode,
                    recorder,
                    releaseTracks() {
                        releaseLocalTrack(audioTrack);
                        if (videoCapture) {
                            void releaseSharedVideoCapture(videoCapture.captureKey);
                        }
                    }
                };

                return {
                    active: true,
                    fileName,
                    mimeType,
                    mode,
                    sizeBytes: 0
                };
            } catch (error) {
                releaseLocalTrack(audioTrack);
                if (videoCapture) {
                    await releaseSharedVideoCapture(videoCapture.captureKey);
                }

                throw error;
            }
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
            copySyntheticMetadata(capture.track.mediaStreamTrack, element.srcObject);
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

        async detachAllCameras() {
            await releaseAllCameraTracks();
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
                    releaseLocalTrack(track);
                }

                await notifyMonitorLevel({ observer }, 0);
                throw error;
            }
        },

        async stopMicrophoneLevelMonitor(elementId) {
            await releaseMonitor(elementId);
        },

        async stopReaderRecording() {
            return await stopActiveReaderRecording();
        }
    };
})();
