(function () {
    const streamMap = new Map();
    const audioMonitorMap = new Map();
    const interopNamespace = "BrowserMediaInterop";
    const microphoneMonitorLevelMultiplier = 2800;

    async function stopStream(stream) {
        if (!stream) {
            return;
        }

        stream.getTracks().forEach(track => track.stop());
    }

    async function notifyMonitorLevel(monitor, levelPercent) {
        if (!monitor?.observer) {
            return null;
        }

        const normalizedLevel = Number.isFinite(levelPercent)
            ? Math.max(0, Math.min(100, Math.round(levelPercent)))
            : 0;

        await monitor.observer.invokeMethodAsync("UpdateLevel", normalizedLevel).catch(() => {});
    }

    async function stopMicrophoneLevelMonitor(rootElementId) {
        const monitor = audioMonitorMap.get(rootElementId);
        if (!monitor) {
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

        await notifyMonitorLevel(monitor, 0);
    }

    async function startMicrophoneLevelMonitor(rootElementId, deviceId, observer) {
        if (!navigator.mediaDevices?.getUserMedia) {
            return;
        }

        await stopMicrophoneLevelMonitor(rootElementId);

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
                observer,
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
                void notifyMonitorLevel(monitor, rms * microphoneMonitorLevelMultiplier);
                monitor.frameHandle = window.requestAnimationFrame(step);
            };

            await audioContext.resume().catch(() => {});
            audioMonitorMap.set(rootElementId, monitor);
            await notifyMonitorLevel(monitor, 0);
            step();
        } catch (error) {
            sourceNode?.disconnect();
            analyser?.disconnect();
            await stopStream(stream);
            if (audioContext) {
                await audioContext.close().catch(() => {});
            }

            await notifyMonitorLevel({ observer }, 0);
            throw error;
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
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
                await stopStream(stream);
            } catch {
            }

            return await window[interopNamespace].queryPermissions();
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

        async startMicrophoneLevelMonitor(elementId, deviceId, observer) {
            await startMicrophoneLevelMonitor(elementId, deviceId, observer);
        },

        async stopMicrophoneLevelMonitor(elementId) {
            await stopMicrophoneLevelMonitor(elementId);
        }
    };
})();
