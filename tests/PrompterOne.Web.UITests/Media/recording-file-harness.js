(() => {
    const audioContextCtor = window.AudioContext || window.webkitAudioContext;
    const audioSampleWaitMs = 100;
    const blobMimeFallback = "video/webm";
    const minimumAudiblePcmSample = 0.0025;
    const mutedGainValue = 0;
    const minimumVisibleChannelValue = 12;
    const minimumVisiblePixelCount = 16;
    const visibleVideoProbeTimeoutMs = 1500;
    const visibleVideoPollDelayMs = 100;
    const visibleVideoSeekSamples = Object.freeze([0, 0.2, 0.45, 0.7, 0.92]);
    const readyStateHaveCurrentData = 2;
    const runtimeGlobalName = "__prompterOneRuntime";
    const mediaContractProperty = "media";
    const defaultMediaRuntimeContract = Object.freeze({
        recordingFileHarnessGlobalName: "__prompterOneRecordingFileHarness"
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

    const harnessGlobalName = getMediaRuntimeString("recordingFileHarnessGlobalName");

    if (typeof window[harnessGlobalName] === "object" && window[harnessGlobalName] !== null) {
        return;
    }

    let pickerCallCount = 0;
    let savedBlob = null;
    let savedFileName = "";
    const savedRecordings = [];
    const nativeMediaRecorder = window.MediaRecorder;

    function normalizePart(part) {
        if (part instanceof Blob) {
            return part;
        }

        if (part instanceof ArrayBuffer || ArrayBuffer.isView(part)) {
            return part;
        }

        return new Blob([part]);
    }

    function waitForMediaEvent(target, readyEventName, errorEventName) {
        return new Promise((resolve, reject) => {
            const handleReady = () => {
                target.removeEventListener(errorEventName, handleError);
                resolve();
            };

            const handleError = () => {
                target.removeEventListener(readyEventName, handleReady);
                reject(new Error("Unable to decode saved recording."));
            };

            target.addEventListener(readyEventName, handleReady, { once: true });
            target.addEventListener(errorEventName, handleError, { once: true });
        });
    }

    function getSavedRecordingState() {
        return {
            fileName: savedFileName,
            hasBlob: savedBlob instanceof Blob,
            mimeType: savedBlob?.type ?? "",
            pickerCallCount,
            savedRecordingCount: savedRecordings.length,
            sizeBytes: savedBlob?.size ?? 0
        };
    }

    function getSavedRecordingsState() {
        return savedRecordings.map(recording => ({
            fileName: recording.fileName,
            hasBlob: recording.blob instanceof Blob,
            mimeType: recording.blob?.type ?? "",
            sizeBytes: recording.blob?.size ?? 0
        }));
    }

    function hasDecodedAudio(videoElement) {
        const audioTracks = videoElement.audioTracks;
        if (audioTracks && typeof audioTracks.length === "number" && audioTracks.length > 0) {
            return true;
        }

        if (typeof videoElement.mozHasAudio === "boolean") {
            return videoElement.mozHasAudio;
        }

        return Number(videoElement.webkitAudioDecodedByteCount ?? 0) > 0;
    }

    function hasAudiblePcmSamples(audioBuffer) {
        for (let channelIndex = 0; channelIndex < audioBuffer.numberOfChannels; channelIndex++) {
            const samples = audioBuffer.getChannelData(channelIndex);
            for (const sample of samples) {
                if (Math.abs(sample) >= minimumAudiblePcmSample) {
                    return true;
                }
            }
        }

        return false;
    }

    async function analyzeDecodedAudio(savedBlob, videoElement) {
        if (!audioContextCtor) {
            return {
                hasAudibleAudio: false,
                hasAudioTrack: hasDecodedAudio(videoElement)
            };
        }

        const audioContext = new audioContextCtor({ latencyHint: "interactive" });

        try {
            const encodedBytes = await savedBlob.arrayBuffer();
            const decodedBuffer = await audioContext.decodeAudioData(encodedBytes.slice(0));

            return {
                hasAudibleAudio: hasAudiblePcmSamples(decodedBuffer),
                hasAudioTrack: decodedBuffer.numberOfChannels > 0 && decodedBuffer.length > 0
            };
        }
        catch {
            return {
                hasAudibleAudio: false,
                hasAudioTrack: hasDecodedAudio(videoElement)
            };
        }
        finally {
            await audioContext.close().catch(() => {});
        }
    }

    function detectVisibleVideo(videoElement) {
        const canvas = document.createElement("canvas");
        const width = Math.max(1, videoElement.videoWidth);
        const height = Math.max(1, videoElement.videoHeight);
        canvas.width = width;
        canvas.height = height;

        const context = canvas.getContext("2d");
        if (!context) {
            return {
                hasVisibleVideo: false,
                nonBlackPixelCount: 0
            };
        }

        context.drawImage(videoElement, 0, 0, width, height);
        const pixels = context.getImageData(0, 0, width, height).data;
        let nonBlackPixelCount = 0;

        for (let index = 0; index < pixels.length; index += 4) {
            if (pixels[index] >= minimumVisibleChannelValue
                || pixels[index + 1] >= minimumVisibleChannelValue
                || pixels[index + 2] >= minimumVisibleChannelValue) {
                nonBlackPixelCount += 1;
            }
        }

        return {
            hasVisibleVideo: nonBlackPixelCount >= minimumVisiblePixelCount,
            nonBlackPixelCount
        };
    }

    async function waitForNextVideoFrame(videoElement) {
        await new Promise(resolve => {
            let completed = false;

            const finish = () => {
                if (completed) {
                    return;
                }

                completed = true;
                resolve();
            };

            const timeoutId = window.setTimeout(finish, visibleVideoPollDelayMs);
            const canAwaitVideoFrame = typeof videoElement.requestVideoFrameCallback === "function"
                && !videoElement.paused
                && !videoElement.ended;

            if (!canAwaitVideoFrame) {
                return;
            }

            videoElement.requestVideoFrameCallback(() => {
                window.clearTimeout(timeoutId);
                finish();
            });
        });
    }

    async function seekToVideoSample(videoElement, timeSeconds) {
        await new Promise(resolve => {
            let completed = false;

            const finish = () => {
                if (completed) {
                    return;
                }

                completed = true;
                videoElement.removeEventListener("seeked", finish);
                resolve();
            };

            window.setTimeout(finish, visibleVideoPollDelayMs * 4);
            videoElement.addEventListener("seeked", finish, { once: true });

            try {
                videoElement.currentTime = timeSeconds;
            }
            catch {
                finish();
            }
        });
    }

    async function detectVisibleVideoAcrossSeekSamples(videoElement, highestVisiblePixelCount) {
        const duration = Number.isFinite(videoElement.duration)
            ? videoElement.duration
            : 0;
        if (duration <= 0) {
            return {
                hasVisibleVideo: highestVisiblePixelCount >= minimumVisiblePixelCount,
                nonBlackPixelCount: highestVisiblePixelCount
            };
        }

        const maximumSeekTime = Math.max(0, duration - 0.01);
        for (const sample of visibleVideoSeekSamples) {
            await seekToVideoSample(videoElement, Math.min(maximumSeekTime, duration * sample));

            const frame = detectVisibleVideo(videoElement);
            highestVisiblePixelCount = Math.max(highestVisiblePixelCount, frame.nonBlackPixelCount);
            if (frame.hasVisibleVideo) {
                return frame;
            }
        }

        return {
            hasVisibleVideo: highestVisiblePixelCount >= minimumVisiblePixelCount,
            nonBlackPixelCount: highestVisiblePixelCount
        };
    }

    async function detectVisibleVideoAcrossFrames(videoElement) {
        const deadline = Date.now() + visibleVideoProbeTimeoutMs;
        let highestVisiblePixelCount = 0;

        while (Date.now() <= deadline) {
            const sample = detectVisibleVideo(videoElement);
            highestVisiblePixelCount = Math.max(highestVisiblePixelCount, sample.nonBlackPixelCount);

            if (sample.hasVisibleVideo) {
                return sample;
            }

            await waitForNextVideoFrame(videoElement);
        }

        return await detectVisibleVideoAcrossSeekSamples(videoElement, highestVisiblePixelCount);
    }

    async function analyzeSavedRecording() {
        if (!(savedBlob instanceof Blob)) {
            return null;
        }

        const objectUrl = URL.createObjectURL(savedBlob);
        const videoElement = document.createElement("video");
        videoElement.muted = true;
        videoElement.playsInline = true;
        videoElement.loop = true;
        videoElement.src = objectUrl;

        try {
            if (videoElement.readyState < readyStateHaveCurrentData) {
                await waitForMediaEvent(videoElement, "loadeddata", "error");
            }

            await videoElement.play().catch(() => {});
            await new Promise(resolve => window.setTimeout(resolve, audioSampleWaitMs));

            const audioAnalysis = await analyzeDecodedAudio(savedBlob, videoElement);
            const visibleVideo = await detectVisibleVideoAcrossFrames(videoElement);

            return {
                fileName: savedFileName,
                hasAudibleAudio: audioAnalysis.hasAudibleAudio,
                hasAudioTrack: audioAnalysis.hasAudioTrack,
                hasVisibleVideo: visibleVideo.hasVisibleVideo,
                height: videoElement.videoHeight,
                mimeType: savedBlob.type,
                nonBlackPixelCount: visibleVideo.nonBlackPixelCount,
                pickerCallCount,
                sizeBytes: savedBlob.size,
                width: videoElement.videoWidth
            };
        }
        finally {
            videoElement.pause();
            videoElement.removeAttribute("src");
            videoElement.load();
            URL.revokeObjectURL(objectUrl);
        }
    }

    window.showSaveFilePicker = async options => {
        pickerCallCount += 1;
        savedBlob = null;
        savedFileName = options?.suggestedName ?? "";

        const parts = [];
        let outputMimeType = blobMimeFallback;

        return {
            async createWritable() {
                return {
                    async write(data) {
                        parts.push(normalizePart(data));
                        if (data instanceof Blob && data.type) {
                            outputMimeType = data.type;
                        }
                    },
                    async close() {
                        savedBlob = new Blob(parts, { type: outputMimeType });
                        savedRecordings.push({
                            blob: savedBlob,
                            fileName: savedFileName
                        });
                    }
                };
            }
        };
    };

    class SyntheticMediaRecorder extends EventTarget {
        static isTypeSupported(type) {
            if (nativeMediaRecorder && typeof nativeMediaRecorder.isTypeSupported === "function") {
                return nativeMediaRecorder.isTypeSupported(type);
            }

            return typeof type === "string" && type.length > 0;
        }

        constructor(stream, options) {
            super();
            this.stream = stream;
            this.mimeType = options?.mimeType || blobMimeFallback;
            this.state = "inactive";
            this._intervalId = 0;
        }

        start(timeslice) {
            if (this.state !== "inactive") {
                throw new DOMException("Synthetic MediaRecorder is already active.", "InvalidStateError");
            }

            this.state = "recording";
            const intervalMs = Number.isFinite(timeslice) && timeslice > 0 ? timeslice : 100;
            this._intervalId = window.setInterval(() => this._emitChunk(), intervalMs);
            window.setTimeout(() => this._emitChunk(), 0);
        }

        requestData() {
            if (this.state === "inactive") {
                return;
            }

            this._emitChunk();
        }

        stop() {
            if (this.state === "inactive") {
                return;
            }

            this.state = "inactive";
            if (this._intervalId) {
                window.clearInterval(this._intervalId);
                this._intervalId = 0;
            }

            window.setTimeout(() => {
                this._emitChunk();
                this.dispatchEvent(new Event("stop"));
                if (typeof this.onstop === "function") {
                    this.onstop(new Event("stop"));
                }
            }, 0);
        }

        _emitChunk() {
            const blob = new Blob([new Uint8Array([80, 49, 82, 69, 67])], { type: this.mimeType || blobMimeFallback });
            const event = new BlobEvent("dataavailable", { data: blob });
            this.dispatchEvent(event);
            if (typeof this.ondataavailable === "function") {
                this.ondataavailable(event);
            }
        }
    }

    window[harnessGlobalName] = Object.freeze({
        analyzeSavedRecording,
        disableSyntheticMediaRecorder() {
            if (nativeMediaRecorder) {
                window.MediaRecorder = nativeMediaRecorder;
            }
        },
        enableSyntheticMediaRecorder() {
            window.MediaRecorder = SyntheticMediaRecorder;
        },
        getSavedRecordingState,
        getSavedRecordingsState,
        reset() {
            pickerCallCount = 0;
            savedBlob = null;
            savedFileName = "";
            savedRecordings.length = 0;
        }
    });
})();
