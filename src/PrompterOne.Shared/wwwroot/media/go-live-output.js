(function () {
    const AudioContextCtor = window.AudioContext || window.webkitAudioContext;
    const audioLevelMultiplier = 2800;
    const audioMeterFftSize = 1024;
    const audioMeterSmoothingTime = 0.82;
    const composerNamespace = "PrompterOneGoLiveMediaComposer";
    const interopNamespace = "PrompterOneGoLiveOutput";
    const supportNamespace = "PrompterOneGoLiveOutputSupport";
    const vdoNinjaNamespace = "PrompterOneGoLiveOutputVdoNinja";
    const outputSessions = new Map();
    const browserEnvironment = "browser";
    const liveKitAudioSource = "microphone";
    const liveKitAudioTrackName = "prompterone-program-audio";
    const liveKitVideoSource = "camera";
    const liveKitVideoTrackName = "prompterone-program-video";
    const obsStudioEnvironment = "obsstudio";
    const primaryAudioElementPrefix = "prompterone-go-live-obs-audio-";

    function getComposer() {
        const composer = window[composerNamespace];
        if (!composer?.ensureProgramSession) {
            throw new Error("Go Live media compositor runtime is not available.");
        }

        return composer;
    }

    function getSupport() {
        const support = window[supportNamespace];
        if (!support?.normalizeRequest) {
            throw new Error("Go Live output support runtime is not available.");
        }

        return support;
    }

    function getVdoNinjaRuntime() {
        const runtime = window[vdoNinjaNamespace];
        if (!runtime?.startSession || !runtime?.stopSession || !runtime?.buildSnapshot) {
            throw new Error("Go Live VDO.Ninja runtime is not available.");
        }

        return runtime;
    }

    function getLiveKitClient() {
        const client = window.LivekitClient;
        if (!client?.Room) {
            throw new Error("LiveKit client runtime is not available.");
        }

        return client;
    }

    function getObsEnvironment() {
        return typeof window.obsstudio === "object" && window.obsstudio !== null
            ? obsStudioEnvironment
            : browserEnvironment;
    }

    function clampLevelPercent(value) {
        return Math.max(0, Math.min(100, Math.round(value)));
    }

    function ensureSession(sessionId) {
        if (!outputSessions.has(sessionId)) {
            outputSessions.set(sessionId, {
                audioDeviceId: "",
                liveKitActive: false,
                liveKitConnected: false,
                liveKitPublishedAudioTrack: null,
                liveKitPublishedVideoTrack: null,
                liveKitRoom: null,
                liveKitRoomName: "",
                liveKitServerUrl: "",
                mediaRecorder: null,
                mediaStream: null,
                obsActive: false,
                obsAudioElementId: "",
                obsEnvironment: browserEnvironment,
                programAudioMeter: null,
                programLevelPercent: 0,
                recordingActive: false,
                recordingBytes: 0,
                recordingChunks: [],
                recordingFileHandle: null,
                recordingFileName: "",
                recordingMimeType: "",
                recordingRequestedAudioCodec: "",
                recordingRequestedContainer: "",
                recordingRequestedVideoCodec: "",
                recordingSaveMode: "",
                recordingWritable: null,
                recordingWritePromise: Promise.resolve(),
                requestSnapshot: null,
                videoDeviceId: "",
                vdoNinjaActive: false,
                vdoNinjaConnected: false,
                vdoNinjaJoinedRoom: false,
                vdoNinjaLastPeerLatencyMs: 0,
                vdoNinjaPeerCount: 0,
                vdoNinjaPublishUrl: "",
                vdoNinjaPublisher: null,
                vdoNinjaRoomName: "",
                vdoNinjaStreamId: ""
            });
        }

        return outputSessions.get(sessionId);
    }

    async function releaseProgramAudioMeter(session) {
        const meter = session.programAudioMeter;
        session.programAudioMeter = null;
        session.programLevelPercent = 0;

        if (!meter) {
            return;
        }

        if (meter.frameHandle) {
            window.cancelAnimationFrame(meter.frameHandle);
        }

        meter.sourceNode.disconnect();
        meter.analyser.disconnect();
        meter.track.stop();
        await meter.audioContext.close().catch(() => {});
    }

    async function syncProgramAudioMeter(session) {
        const audioTrack = session.mediaStream?.getAudioTracks()?.[0] ?? null;
        if (!audioTrack || !AudioContextCtor) {
            await releaseProgramAudioMeter(session);
            return;
        }

        if (session.programAudioMeter?.sourceId === audioTrack.id) {
            return;
        }

        await releaseProgramAudioMeter(session);

        const audioContext = new AudioContextCtor({ latencyHint: "interactive" });
        const track = audioTrack.clone();
        const sourceNode = audioContext.createMediaStreamSource(new MediaStream([track]));
        const analyser = audioContext.createAnalyser();
        analyser.fftSize = audioMeterFftSize;
        analyser.smoothingTimeConstant = audioMeterSmoothingTime;
        sourceNode.connect(analyser);

        const data = new Uint8Array(analyser.frequencyBinCount);
        const meter = { analyser, audioContext, frameHandle: 0, sourceId: audioTrack.id, sourceNode, track };
        const step = () => {
            if (session.programAudioMeter !== meter) {
                return;
            }

            analyser.getByteFrequencyData(data);
            let sum = 0;
            for (const amplitude of data) {
                sum += Math.pow(amplitude / 255, 2);
            }

            session.programLevelPercent = clampLevelPercent(Math.sqrt(sum / data.length) * audioLevelMultiplier);
            meter.frameHandle = window.requestAnimationFrame(step);
        };

        session.programAudioMeter = meter;
        await audioContext.resume().catch(() => {});
        step();
    }

    async function republishLiveKitTracks(session, liveKitClient) {
        const room = session.liveKitRoom;
        if (!room?.localParticipant) {
            throw new Error("LiveKit room is not connected.");
        }

        if (session.liveKitPublishedVideoTrack) {
            await room.localParticipant.unpublishTrack(session.liveKitPublishedVideoTrack, false).catch(() => {});
            session.liveKitPublishedVideoTrack = null;
        }

        if (session.liveKitPublishedAudioTrack) {
            await room.localParticipant.unpublishTrack(session.liveKitPublishedAudioTrack, false).catch(() => {});
            session.liveKitPublishedAudioTrack = null;
        }

        const videoTrack = session.mediaStream?.getVideoTracks()?.[0] ?? null;
        const audioTrack = session.mediaStream?.getAudioTracks()?.[0] ?? null;

        if (videoTrack) {
            await room.localParticipant.publishTrack(videoTrack, {
                name: liveKitVideoTrackName,
                source: liveKitClient.Track?.Source?.Camera ?? liveKitVideoSource
            });
            session.liveKitPublishedVideoTrack = videoTrack;
        }

        if (audioTrack) {
            await room.localParticipant.publishTrack(audioTrack, {
                name: liveKitAudioTrackName,
                source: liveKitClient.Track?.Source?.Microphone ?? liveKitAudioSource
            });
            session.liveKitPublishedAudioTrack = audioTrack;
        }
    }

    async function detachObsAudio(session) {
        if (!session.obsAudioElementId) {
            return;
        }

        const element = document.getElementById(session.obsAudioElementId);
        if (element) {
            element.pause?.();
            element.srcObject = null;
            element.remove();
        }

        session.obsAudioElementId = "";
    }

    async function attachObsAudio(sessionId, session) {
        await detachObsAudio(session);
        session.obsEnvironment = getObsEnvironment();

        if (!session.mediaStream?.getAudioTracks()?.length || session.obsEnvironment !== obsStudioEnvironment) {
            return;
        }

        const elementId = `${primaryAudioElementPrefix}${sessionId}`;
        const audioElement = document.createElement("audio");
        audioElement.id = elementId;
        audioElement.autoplay = true;
        audioElement.hidden = true;
        audioElement.muted = false;
        audioElement.playsInline = true;
        audioElement.srcObject = session.mediaStream;
        document.body.appendChild(audioElement);
        await audioElement.play().catch(() => {});
        session.obsAudioElementId = elementId;
    }

    async function cleanupSessionIfIdle(sessionId, session) {
        if (session.liveKitActive || session.obsActive || session.recordingActive || session.vdoNinjaActive) {
            return;
        }

        await detachObsAudio(session);
        await releaseProgramAudioMeter(session);
        await getComposer().cleanupProgramSession(session);
        outputSessions.delete(sessionId);
    }

    async function ensureProgramSession(session, rawRequest) {
        const request = getSupport().normalizeRequest(rawRequest);
        await getComposer().ensureProgramSession(session, request);
        session.requestSnapshot = request;
        session.videoDeviceId = request.primaryCameraDeviceId;
        session.audioDeviceId = request.primaryMicrophoneDeviceId;
        await syncProgramAudioMeter(session);
        return request;
    }

    function buildRuntimeState(session) {
        const programState = getComposer().getProgramState(session);
        return {
            audioDeviceId: session.audioDeviceId,
            audio: {
                programLevelPercent: session.programLevelPercent,
                recordingLevelPercent: session.recordingActive ? session.programLevelPercent : 0
            },
            hasMediaStream: Boolean(session.mediaStream),
            liveKit: {
                active: session.liveKitActive,
                connected: session.liveKitConnected,
                publishedAudio: Boolean(session.liveKitPublishedAudioTrack),
                publishedVideo: Boolean(session.liveKitPublishedVideoTrack),
                roomName: session.liveKitRoomName,
                serverUrl: session.liveKitServerUrl
            },
            obs: {
                active: session.obsActive,
                audioAttached: Boolean(session.obsAudioElementId),
                audioElementId: session.obsAudioElementId,
                environment: session.obsEnvironment
            },
            program: {
                audioInputCount: programState.audioInputCount,
                frameRate: programState.frameRate,
                height: session.requestSnapshot?.programVideo?.height ?? 0,
                primarySourceId: programState.primarySourceId,
                videoSourceCount: programState.videoSourceCount,
                width: session.requestSnapshot?.programVideo?.width ?? 0
            },
            recording: {
                active: session.recordingActive,
                audioBitrateKbps: session.requestSnapshot?.recording?.audioBitrateKbps ?? 0,
                fileName: session.recordingFileName,
                mimeType: session.recordingMimeType,
                requestedAudioCodec: session.recordingRequestedAudioCodec,
                requestedContainer: session.recordingRequestedContainer,
                requestedVideoCodec: session.recordingRequestedVideoCodec,
                saveMode: session.recordingSaveMode,
                sizeBytes: session.recordingBytes,
                videoBitrateKbps: session.requestSnapshot?.recording?.videoBitrateKbps ?? 0
            },
            vdoNinja: getVdoNinjaRuntime().buildSnapshot(session),
            videoDeviceId: session.videoDeviceId
        };
    }

    window[interopNamespace] = {
        async startLiveKitSession(sessionId, rawRequest) {
            const session = ensureSession(sessionId);
            const request = await ensureProgramSession(session, rawRequest);
            const liveKitClient = getLiveKitClient();

            if (!session.liveKitRoom
                || !session.liveKitConnected
                || session.liveKitServerUrl !== request.liveKitServerUrl
                || session.liveKitRoomName !== request.liveKitRoomName) {
                session.liveKitRoom?.disconnect?.();
                session.liveKitRoom = new liveKitClient.Room();
                await session.liveKitRoom.connect(request.liveKitServerUrl, request.liveKitToken);
                session.liveKitConnected = true;
                session.liveKitServerUrl = request.liveKitServerUrl;
                session.liveKitRoomName = request.liveKitRoomName;
            }

            await republishLiveKitTracks(session, liveKitClient);
            session.liveKitActive = true;
        },

        async startLocalRecording(sessionId, rawRequest) {
            const session = ensureSession(sessionId);
            const support = getSupport();
            const request = await ensureProgramSession(session, rawRequest);

            if (session.recordingActive) {
                return;
            }

            session.recordingMimeType = await support.resolveSupportedRecordingMimeType(request);
            session.recordingRequestedContainer = request.recording.containerLabel;
            session.recordingRequestedVideoCodec = request.recording.videoCodecLabel;
            session.recordingRequestedAudioCodec = request.recording.audioCodecLabel;
            await support.prepareRecordingSink(session, request, session.recordingMimeType);
            await support.startRecordingSegment(session);
            session.recordingActive = true;
        },

        async startVdoNinjaSession(sessionId, rawRequest) {
            const session = ensureSession(sessionId);
            const request = await ensureProgramSession(session, rawRequest);
            await getVdoNinjaRuntime().startSession(session, sessionId, request);
        },

        async stopLiveKitSession(sessionId) {
            const session = outputSessions.get(sessionId);
            if (!session) {
                return;
            }

            if (session.liveKitPublishedVideoTrack && session.liveKitRoom?.localParticipant) {
                await session.liveKitRoom.localParticipant.unpublishTrack(session.liveKitPublishedVideoTrack, false).catch(() => {});
            }

            if (session.liveKitPublishedAudioTrack && session.liveKitRoom?.localParticipant) {
                await session.liveKitRoom.localParticipant.unpublishTrack(session.liveKitPublishedAudioTrack, false).catch(() => {});
            }

            session.liveKitPublishedVideoTrack = null;
            session.liveKitPublishedAudioTrack = null;
            session.liveKitActive = false;
            session.liveKitConnected = false;
            session.liveKitServerUrl = "";
            session.liveKitRoomName = "";
            session.liveKitRoom?.disconnect?.();
            session.liveKitRoom = null;

            await cleanupSessionIfIdle(sessionId, session);
        },

        async stopVdoNinjaSession(sessionId) {
            const session = outputSessions.get(sessionId);
            if (!session) {
                return;
            }

            await getVdoNinjaRuntime().stopSession(session);
            await cleanupSessionIfIdle(sessionId, session);
        },

        async stopLocalRecording(sessionId) {
            const session = outputSessions.get(sessionId);
            if (!session || !session.recordingActive) {
                return;
            }

            await getSupport().stopRecordingSegment(session);
            session.recordingActive = false;
            await getSupport().finalizeRecording(session);

            session.recordingChunks = [];
            session.recordingFileHandle = null;
            session.recordingFileName = "";
            session.recordingMimeType = "";
            session.recordingRequestedAudioCodec = "";
            session.recordingRequestedContainer = "";
            session.recordingRequestedVideoCodec = "";
            session.recordingSaveMode = "";
            session.recordingBytes = 0;
            session.recordingWritable = null;
            session.recordingWritePromise = Promise.resolve();
            await cleanupSessionIfIdle(sessionId, session);
        },

        async startObsBrowserOutput(sessionId, rawRequest) {
            const session = ensureSession(sessionId);
            await ensureProgramSession(session, rawRequest);
            session.obsActive = true;
            await attachObsAudio(sessionId, session);
        },

        async stopObsBrowserOutput(sessionId) {
            const session = outputSessions.get(sessionId);
            if (!session) {
                return;
            }

            session.obsActive = false;
            await detachObsAudio(session);
            session.obsEnvironment = browserEnvironment;
            await cleanupSessionIfIdle(sessionId, session);
        },

        async updateSessionDevices(sessionId, rawRequest) {
            const session = outputSessions.get(sessionId);
            if (!session) {
                return;
            }

            await ensureProgramSession(session, rawRequest);

            if (session.liveKitActive) {
                await republishLiveKitTracks(session, getLiveKitClient());
            }

            if (session.vdoNinjaActive) {
                await getVdoNinjaRuntime().startSession(session, sessionId, session.requestSnapshot);
            }

            if (session.obsActive) {
                await attachObsAudio(sessionId, session);
            }
        },

        getSessionState(sessionId) {
            const session = outputSessions.get(sessionId);
            return session ? buildRuntimeState(session) : null;
        }
    };
})();
