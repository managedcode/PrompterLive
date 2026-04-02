(() => {
    const audioContextCtor = window.AudioContext || window.webkitAudioContext;
    const harnessGlobalName = "__prompterOneRecordingFileHarness";
    const audioSampleWaitMs = 100;
    const blobMimeFallback = "video/webm";
    const minimumAudibleFrequencyValue = 8;
    const minimumVisibleChannelValue = 12;
    const minimumVisiblePixelCount = 16;
    const visibleVideoProbeTimeoutMs = 1500;
    const visibleVideoPollDelayMs = 100;
    const readyStateHaveCurrentData = 2;

    if (typeof window[harnessGlobalName] === "object" && window[harnessGlobalName] !== null) {
        return;
    }

    let pickerCallCount = 0;
    let savedBlob = null;
    let savedFileName = "";

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
            sizeBytes: savedBlob?.size ?? 0
        };
    }

    async function detectAudibleAudio(captureStream) {
        if (!captureStream?.getAudioTracks?.().length || !audioContextCtor) {
            return false;
        }

        const audioContext = new audioContextCtor({ latencyHint: "interactive" });
        const sourceNode = audioContext.createMediaStreamSource(captureStream);
        const analyser = audioContext.createAnalyser();
        analyser.fftSize = 1024;
        analyser.smoothingTimeConstant = 0.2;
        sourceNode.connect(analyser);

        try {
            await audioContext.resume().catch(() => {});
            await new Promise(resolve => window.setTimeout(resolve, audioSampleWaitMs));

            const samples = new Uint8Array(analyser.frequencyBinCount);
            analyser.getByteFrequencyData(samples);
            return samples.some(value => value >= minimumAudibleFrequencyValue);
        }
        finally {
            sourceNode.disconnect();
            analyser.disconnect();
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

        return {
            hasVisibleVideo: highestVisiblePixelCount >= minimumVisiblePixelCount,
            nonBlackPixelCount: highestVisiblePixelCount
        };
    }

    async function analyzeSavedRecording() {
        if (!(savedBlob instanceof Blob)) {
            return null;
        }

        const objectUrl = URL.createObjectURL(savedBlob);
        const videoElement = document.createElement("video");
        videoElement.muted = true;
        videoElement.playsInline = true;
        videoElement.src = objectUrl;

        try {
            if (videoElement.readyState < readyStateHaveCurrentData) {
                await waitForMediaEvent(videoElement, "loadeddata", "error");
            }

            await videoElement.play().catch(() => {});
            await new Promise(resolve => window.setTimeout(resolve, audioSampleWaitMs));

            const captureStream = typeof videoElement.captureStream === "function"
                ? videoElement.captureStream()
                : null;
            const hasAudioTrack = Boolean(captureStream?.getAudioTracks?.().length);
            const hasAudibleAudio = await detectAudibleAudio(captureStream);
            const visibleVideo = await detectVisibleVideoAcrossFrames(videoElement);

            captureStream?.getTracks?.().forEach(track => track.stop());

            return {
                fileName: savedFileName,
                hasAudibleAudio,
                hasAudioTrack,
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
                    }
                };
            }
        };
    };

    window[harnessGlobalName] = Object.freeze({
        analyzeSavedRecording,
        getSavedRecordingState,
        reset() {
            pickerCallCount = 0;
            savedBlob = null;
            savedFileName = "";
        }
    });
})();
