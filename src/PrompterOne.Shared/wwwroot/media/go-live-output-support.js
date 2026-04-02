(function () {
    const supportNamespace = "PrompterOneGoLiveOutputSupport";
    const abortErrorName = "AbortError";
    const audioCodecAac = "AAC";
    const audioCodecMp3 = "MP3";
    const audioCodecOpus = "Opus";
    const audioRouteBoth = 2;
    const audioRouteStream = 1;
    const codecSeparator = ";";
    const fileNameTimestampPattern = /[:.]/g;
    const fileNameUnsafePattern = /[^a-z0-9]+/gi;
    const fileSystemSaveMode = "file-system";
    const mimeTypeMp4 = "video/mp4";
    const mimeTypeWebm = "video/webm";
    const recordingDefaultExtension = "webm";
    const recordingFileStemFallback = "go-live-recording";
    const recordingMimeFallbackCandidates = [
        "video/webm;codecs=vp9,opus",
        "video/webm;codecs=vp8,opus",
        "video/mp4;codecs=avc1.42E01E,mp4a.40.2",
        "video/webm"
    ];
    const recordingTimesliceMs = 1000;
    const savePickerDescription = "PrompterOne recording";
    const videoCodecAv1 = "AV1";
    const videoCodecH264 = "H.264 (AVC)";
    const videoCodecH265 = "H.265 (HEVC)";
    const videoCodecVp9 = "VP9";

    function readValue(source, camelCaseName, pascalCaseName, fallbackValue) {
        if (!source || typeof source !== "object") {
            return fallbackValue;
        }

        const value = source[camelCaseName] ?? source[pascalCaseName];
        return value ?? fallbackValue;
    }

    function normalizeBoolean(value, fallbackValue) {
        return typeof value === "boolean" ? value : fallbackValue;
    }

    function normalizeNumber(value, fallbackValue) {
        return typeof value === "number" && Number.isFinite(value) ? value : fallbackValue;
    }

    function normalizeString(value, fallbackValue) {
        return typeof value === "string" ? value : fallbackValue;
    }

    function normalizeTransform(rawTransform) {
        return {
            height: normalizeNumber(readValue(rawTransform, "height", "Height", 0.25), 0.25),
            includeInOutput: normalizeBoolean(readValue(rawTransform, "includeInOutput", "IncludeInOutput", true), true),
            mirrorHorizontal: normalizeBoolean(readValue(rawTransform, "mirrorHorizontal", "MirrorHorizontal", false), false),
            mirrorVertical: normalizeBoolean(readValue(rawTransform, "mirrorVertical", "MirrorVertical", false), false),
            opacity: normalizeNumber(readValue(rawTransform, "opacity", "Opacity", 1), 1),
            rotation: normalizeNumber(readValue(rawTransform, "rotation", "Rotation", 0), 0),
            visible: normalizeBoolean(readValue(rawTransform, "visible", "Visible", true), true),
            width: normalizeNumber(readValue(rawTransform, "width", "Width", 0.25), 0.25),
            x: normalizeNumber(readValue(rawTransform, "x", "X", 0.5), 0.5),
            y: normalizeNumber(readValue(rawTransform, "y", "Y", 0.5), 0.5),
            zIndex: normalizeNumber(readValue(rawTransform, "zIndex", "ZIndex", 0), 0)
        };
    }

    function normalizeVideoSource(rawSource) {
        const transform = normalizeTransform(readValue(rawSource, "transform", "Transform", {}));
        const isPrimary = normalizeBoolean(readValue(rawSource, "isPrimary", "IsPrimary", false), false);

        return {
            deviceId: normalizeString(readValue(rawSource, "deviceId", "DeviceId", ""), ""),
            isPrimary,
            isRenderable: isPrimary || (transform.visible && transform.includeInOutput),
            label: normalizeString(readValue(rawSource, "label", "Label", ""), ""),
            sourceId: normalizeString(readValue(rawSource, "sourceId", "SourceId", ""), ""),
            transform
        };
    }

    function normalizeAudioInput(rawInput) {
        const isMuted = normalizeBoolean(readValue(rawInput, "isMuted", "IsMuted", false), false);
        const routeTarget = normalizeNumber(readValue(rawInput, "routeTarget", "RouteTarget", audioRouteBoth), audioRouteBoth);

        return {
            delayMs: normalizeNumber(readValue(rawInput, "delayMs", "DelayMs", 0), 0),
            deviceId: normalizeString(readValue(rawInput, "deviceId", "DeviceId", ""), ""),
            gain: normalizeNumber(readValue(rawInput, "gain", "Gain", 1), 1),
            isMuted,
            isPrimary: normalizeBoolean(readValue(rawInput, "isPrimary", "IsPrimary", false), false),
            isRoutedToProgram: !isMuted && (routeTarget === audioRouteStream || routeTarget === audioRouteBoth),
            label: normalizeString(readValue(rawInput, "label", "Label", ""), ""),
            routeTarget
        };
    }

    function normalizeProgramVideo(rawProgramVideo) {
        return {
            frameRate: normalizeNumber(readValue(rawProgramVideo, "frameRate", "FrameRate", 30), 30),
            frameRateLabel: normalizeString(readValue(rawProgramVideo, "frameRateLabel", "FrameRateLabel", ""), ""),
            height: normalizeNumber(readValue(rawProgramVideo, "height", "Height", 1080), 1080),
            resolutionLabel: normalizeString(readValue(rawProgramVideo, "resolutionLabel", "ResolutionLabel", ""), ""),
            width: normalizeNumber(readValue(rawProgramVideo, "width", "Width", 1920), 1920)
        };
    }

    function normalizeRecording(rawRecording) {
        return {
            audioBitrateKbps: normalizeNumber(readValue(rawRecording, "audioBitrateKbps", "AudioBitrateKbps", 320), 320),
            audioChannelCount: normalizeNumber(readValue(rawRecording, "audioChannelCount", "AudioChannelCount", 2), 2),
            audioCodecLabel: normalizeString(readValue(rawRecording, "audioCodecLabel", "AudioCodecLabel", audioCodecAac), audioCodecAac),
            audioSampleRate: normalizeNumber(readValue(rawRecording, "audioSampleRate", "AudioSampleRate", 48000), 48000),
            containerLabel: normalizeString(readValue(rawRecording, "containerLabel", "ContainerLabel", mimeTypeWebm), mimeTypeWebm),
            fileStem: normalizeString(readValue(rawRecording, "fileStem", "FileStem", recordingFileStemFallback), recordingFileStemFallback),
            preferFilePicker: normalizeBoolean(readValue(rawRecording, "preferFilePicker", "PreferFilePicker", true), true),
            videoBitrateKbps: normalizeNumber(readValue(rawRecording, "videoBitrateKbps", "VideoBitrateKbps", 8000), 8000),
            videoCodecLabel: normalizeString(readValue(rawRecording, "videoCodecLabel", "VideoCodecLabel", videoCodecVp9), videoCodecVp9)
        };
    }

    function normalizeRequest(rawRequest) {
        const normalized = {
            audioInputs: (readValue(rawRequest, "audioInputs", "AudioInputs", []) ?? []).map(normalizeAudioInput),
            liveKitEnabled: normalizeBoolean(readValue(rawRequest, "liveKitEnabled", "LiveKitEnabled", false), false),
            liveKitRoomName: normalizeString(readValue(rawRequest, "liveKitRoomName", "LiveKitRoomName", ""), ""),
            liveKitServerUrl: normalizeString(readValue(rawRequest, "liveKitServerUrl", "LiveKitServerUrl", ""), ""),
            liveKitToken: normalizeString(readValue(rawRequest, "liveKitToken", "LiveKitToken", ""), ""),
            obsEnabled: normalizeBoolean(readValue(rawRequest, "obsEnabled", "ObsEnabled", false), false),
            primarySourceId: normalizeString(readValue(rawRequest, "primarySourceId", "PrimarySourceId", ""), ""),
            programVideo: normalizeProgramVideo(readValue(rawRequest, "programVideo", "ProgramVideo", {})),
            recording: normalizeRecording(readValue(rawRequest, "recording", "Recording", {})),
            recordingEnabled: normalizeBoolean(readValue(rawRequest, "recordingEnabled", "RecordingEnabled", false), false),
            vdoNinjaEnabled: normalizeBoolean(readValue(rawRequest, "vdoNinjaEnabled", "VdoNinjaEnabled", false), false),
            vdoNinjaPublishUrl: normalizeString(readValue(rawRequest, "vdoNinjaPublishUrl", "VdoNinjaPublishUrl", ""), ""),
            vdoNinjaRoomName: normalizeString(readValue(rawRequest, "vdoNinjaRoomName", "VdoNinjaRoomName", ""), ""),
            videoSources: (readValue(rawRequest, "videoSources", "VideoSources", []) ?? []).map(normalizeVideoSource)
        };

        normalized.primaryCameraDeviceId = normalized.videoSources.find(source => source.isPrimary)?.deviceId
            ?? normalized.videoSources.find(source => source.transform.visible && source.transform.includeInOutput)?.deviceId
            ?? "";
        normalized.primaryMicrophoneDeviceId = normalized.audioInputs.find(input => input.isPrimary)?.deviceId ?? "";
        return normalized;
    }

    function getBaseMimeType(mimeType) {
        return String(mimeType || "").split(codecSeparator, 1)[0] || mimeTypeWebm;
    }

    function getRecordingFileExtension(mimeType) {
        return getBaseMimeType(mimeType).includes("mp4")
            ? "mp4"
            : recordingDefaultExtension;
    }

    function sanitizeRecordingFileStem(fileStem) {
        const normalizedStem = String(fileStem || "")
            .trim()
            .toLowerCase()
            .replace(fileNameUnsafePattern, "-")
            .replace(/-{2,}/g, "-")
            .replace(/^-|-$/g, "");

        return normalizedStem || recordingFileStemFallback;
    }

    function buildRecordingFileName(fileStem, mimeType) {
        const safeStem = sanitizeRecordingFileStem(fileStem);
        const timestamp = new Date().toISOString().replace(fileNameTimestampPattern, "-");
        return `${safeStem}-${timestamp}.${getRecordingFileExtension(mimeType)}`;
    }

    function triggerRecordingDownload(blob, fileName) {
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        link.rel = "noopener";
        link.hidden = true;
        document.body.appendChild(link);
        link.click();
        window.setTimeout(() => {
            link.remove();
            URL.revokeObjectURL(url);
        }, 0);
    }

    function dedupeCandidates(candidates) {
        return [...new Set(candidates.filter(Boolean))];
    }

    function buildRequestedMimeCandidates(recording) {
        const preferredCandidates = [];
        const containerLabel = String(recording.containerLabel || "").toUpperCase();
        const videoCodecLabel = recording.videoCodecLabel;
        const audioCodecLabel = recording.audioCodecLabel;

        if (containerLabel === "MP4" || containerLabel === "MOV") {
            if (videoCodecLabel === videoCodecH264 && audioCodecLabel === audioCodecAac) {
                preferredCandidates.push("video/mp4;codecs=avc1.42E01E,mp4a.40.2");
            }

            if (videoCodecLabel === videoCodecH265 && audioCodecLabel === audioCodecAac) {
                preferredCandidates.push("video/mp4;codecs=hvc1.1.6.L93.B0,mp4a.40.2");
            }

            if (videoCodecLabel === videoCodecAv1 && audioCodecLabel === audioCodecAac) {
                preferredCandidates.push("video/mp4;codecs=av01.0.08M.08,mp4a.40.2");
            }

            preferredCandidates.push(mimeTypeMp4);
        }

        if (containerLabel === "WEBM") {
            if (videoCodecLabel === videoCodecVp9 && audioCodecLabel === audioCodecOpus) {
                preferredCandidates.push("video/webm;codecs=vp9,opus");
            }

            if (videoCodecLabel === videoCodecAv1 && audioCodecLabel === audioCodecOpus) {
                preferredCandidates.push("video/webm;codecs=av1,opus");
            }

            if (videoCodecLabel === videoCodecH264 && audioCodecLabel === audioCodecOpus) {
                preferredCandidates.push("video/webm;codecs=h264,opus");
            }

            preferredCandidates.push(mimeTypeWebm);
        }

        if (audioCodecLabel === audioCodecMp3) {
            preferredCandidates.push("video/mp4;codecs=avc1.42E01E,mp4a.40.2");
        }

        return dedupeCandidates([...preferredCandidates, ...recordingMimeFallbackCandidates]);
    }

    async function resolveSupportedRecordingMimeType(request) {
        if (typeof MediaRecorder === "undefined") {
            throw new Error("MediaRecorder is not available in this browser.");
        }

        const candidates = buildRequestedMimeCandidates(request.recording);
        let supportedFallback = "";

        for (const candidate of candidates) {
            if (!MediaRecorder.isTypeSupported(candidate)) {
                continue;
            }

            if (!supportedFallback) {
                supportedFallback = candidate;
            }

            if (!navigator.mediaCapabilities?.encodingInfo) {
                return candidate;
            }

            try {
                const capability = await navigator.mediaCapabilities.encodingInfo({
                    audio: {
                        bitrate: request.recording.audioBitrateKbps * 1000,
                        channels: request.recording.audioChannelCount,
                        contentType: getBaseMimeType(candidate).includes("mp4")
                            ? "audio/mp4;codecs=mp4a.40.2"
                            : "audio/webm;codecs=opus",
                        samplerate: request.recording.audioSampleRate
                    },
                    type: "record",
                    video: {
                        bitrate: request.recording.videoBitrateKbps * 1000,
                        contentType: candidate,
                        framerate: request.programVideo.frameRate,
                        height: request.programVideo.height,
                        width: request.programVideo.width
                    }
                });

                if (capability?.supported) {
                    return candidate;
                }
            }
            catch {
            }
        }

        if (supportedFallback) {
            return supportedFallback;
        }

        throw new Error("This browser does not support the requested recording export profile.");
    }

    async function prepareRecordingSink(session, request, mimeType) {
        session.recordingFileName = buildRecordingFileName(request.recording.fileStem, mimeType);
        session.recordingBytes = 0;
        session.recordingSaveMode = "download";
        session.recordingWritable = null;
        session.recordingFileHandle = null;
        session.recordingChunks = [];
        session.recordingWritePromise = Promise.resolve();

        if (!request.recording.preferFilePicker || typeof window.showSaveFilePicker !== "function") {
            return;
        }

        try {
            const handle = await window.showSaveFilePicker({
                suggestedName: session.recordingFileName,
                types: [
                    {
                        accept: {
                            [getBaseMimeType(mimeType)]: [`.${getRecordingFileExtension(mimeType)}`]
                        },
                        description: savePickerDescription
                    }
                ]
            });
            const writable = await handle.createWritable();

            session.recordingFileHandle = handle;
            session.recordingWritable = writable;
            session.recordingSaveMode = fileSystemSaveMode;
        }
        catch (error) {
            if (error?.name === abortErrorName) {
                session.recordingSaveMode = "download";
                return;
            }
        }
    }

    function queueRecordingWrite(session, data) {
        session.recordingBytes += data.size;
        session.recordingWritePromise = session.recordingWritePromise.then(() => session.recordingWritable.write(data));
        return session.recordingWritePromise;
    }

    async function startRecordingSegment(session) {
        if (!session.mediaStream) {
            throw new Error("Recording requires an active program stream.");
        }

        const recorder = new MediaRecorder(session.mediaStream, {
            audioBitsPerSecond: session.requestSnapshot.recording.audioBitrateKbps * 1000,
            mimeType: session.recordingMimeType,
            videoBitsPerSecond: session.requestSnapshot.recording.videoBitrateKbps * 1000
        });

        recorder.addEventListener("dataavailable", event => {
            if (!event.data || event.data.size <= 0) {
                return;
            }

            if (session.recordingWritable) {
                void queueRecordingWrite(session, event.data);
                return;
            }

            session.recordingChunks.push(event.data);
            session.recordingBytes += event.data.size;
        });

        session.mediaRecorder = recorder;
        recorder.start(recordingTimesliceMs);
    }

    async function stopRecordingSegment(session) {
        const recorder = session.mediaRecorder;
        if (!recorder) {
            return;
        }

        if (recorder.state === "inactive") {
            session.mediaRecorder = null;
            return;
        }

        await new Promise((resolve, reject) => {
            const handleStop = () => {
                recorder.removeEventListener("error", handleError);
                resolve();
            };
            const handleError = event => {
                recorder.removeEventListener("stop", handleStop);
                reject(event?.error ?? new Error("MediaRecorder failed."));
            };

            recorder.addEventListener("stop", handleStop, { once: true });
            recorder.addEventListener("error", handleError, { once: true });
            recorder.stop();
        });

        session.mediaRecorder = null;
    }

    async function finalizeRecording(session) {
        if (session.recordingWritable) {
            await session.recordingWritePromise.catch(() => {});
            await session.recordingWritable.close().catch(() => {});
            return;
        }

        if (session.recordingChunks.length === 0) {
            return;
        }

        const blob = new Blob(session.recordingChunks, { type: session.recordingMimeType || mimeTypeWebm });
        triggerRecordingDownload(blob, session.recordingFileName || buildRecordingFileName(recordingFileStemFallback, blob.type));
    }

    window[supportNamespace] = {
        finalizeRecording,
        normalizeRequest,
        prepareRecordingSink,
        resolveSupportedRecordingMimeType,
        startRecordingSegment,
        stopRecordingSegment
    };
})();
