(function () {
    const browserMediaNamespace = "BrowserMediaInterop";
    const composerNamespace = "PrompterOneGoLiveMediaComposer";
    const audioContextCtor = window.AudioContext || window.webkitAudioContext;
    const canvasContextType = "2d";
    const hiddenMediaStyle = "position:fixed;left:-10000px;top:-10000px;width:1px;height:1px;opacity:0;pointer-events:none;";
    const maxAudioDelaySeconds = 5;
    const overlayMinimumOpacity = 0;
    const overlayMaximumOpacity = 1;
    const primaryScale = 1;
    const videoReadyStateThreshold = 2;

    function ensureSessionInfrastructure(session) {
        session.videoBindings ??= new Map();
        session.audioBindings ??= new Map();
        session.mediaStream ??= null;
        session.requestSnapshot ??= null;
        session.renderHandle ??= 0;
        session.programCanvas ??= null;
        session.programContext ??= null;
        session.canvasStream ??= null;
        session.captureFrameRate ??= 0;
        session.audioContext ??= null;
        session.audioDestination ??= null;
        session.audioMasterGain ??= null;
        session.primarySourceAudioBinding ??= null;
    }

    function getBrowserMedia() {
        const browserMedia = window[browserMediaNamespace];
        if (!browserMedia?.createSharedCameraTrack || !browserMedia?.createLocalAudioTrack || !browserMedia?.releaseSharedCameraTrack) {
            throw new Error("Browser media runtime is not available.");
        }

        return browserMedia;
    }

    function clamp(value, minimum, maximum) {
        return Math.max(minimum, Math.min(maximum, value));
    }

    function stopTrackSet(stream) {
        if (!stream) {
            return;
        }

        stream.getTracks().forEach(track => track.stop());
    }

    function removeElement(element) {
        if (!element) {
            return;
        }

        element.pause?.();
        element.srcObject = null;
        element.remove();
    }

    function ensureProgramCanvas(session, request) {
        const needsCanvas = !session.programCanvas
            || !session.programContext
            || session.programCanvas.width !== request.programVideo.width
            || session.programCanvas.height !== request.programVideo.height
            || session.captureFrameRate !== request.programVideo.frameRate;

        if (!needsCanvas) {
            return;
        }

        stopTrackSet(session.canvasStream);

        const canvas = document.createElement("canvas");
        canvas.width = request.programVideo.width;
        canvas.height = request.programVideo.height;
        canvas.style.cssText = hiddenMediaStyle;
        document.body.appendChild(canvas);

        session.programCanvas?.remove();
        session.programCanvas = canvas;
        session.programContext = canvas.getContext(canvasContextType, { alpha: false });
        session.canvasStream = canvas.captureStream(request.programVideo.frameRate);
        session.captureFrameRate = request.programVideo.frameRate;
        session.mediaStream = null;
    }

    async function ensureAudioInfrastructure(session) {
        if (!audioContextCtor) {
            throw new Error("AudioContext is not available in this browser.");
        }

        if (!session.audioContext) {
            const context = new audioContextCtor();
            const destination = context.createMediaStreamDestination();
            const masterGain = context.createGain();

            masterGain.gain.value = primaryScale;
            masterGain.connect(destination);

            session.audioContext = context;
            session.audioDestination = destination;
            session.audioMasterGain = masterGain;
        }

        await session.audioContext.resume().catch(() => {});
    }

    function createVideoBinding(capture) {
        const element = document.createElement("video");
        element.autoplay = true;
        element.muted = true;
        element.playsInline = true;
        element.style.cssText = hiddenMediaStyle;
        document.body.appendChild(element);
        capture.track.attach(element);
        void element.play().catch(() => {});

        return {
            captureKey: capture.captureKey,
            element,
            stream: capture.stream ?? element.srcObject,
            track: capture.track
        };
    }

    async function ensureVideoBinding(session, deviceId) {
        if (session.videoBindings.has(deviceId)) {
            return;
        }

        const capture = await getBrowserMedia().createSharedCameraTrack(deviceId);
        session.videoBindings.set(deviceId, createVideoBinding(capture));
    }

    async function cleanupVideoBindings(session, request) {
        const requestedDeviceIds = new Set(
            request.videoSources
                .filter(source => source.isRenderable)
                .map(source => source.deviceId));

        for (const [deviceId, binding] of session.videoBindings.entries()) {
            if (requestedDeviceIds.has(deviceId)) {
                continue;
            }

            try {
                binding.track.detach(binding.element);
            }
            catch {
            }

            await getBrowserMedia().releaseSharedCameraTrack(binding.captureKey);
            removeElement(binding.element);
            session.videoBindings.delete(deviceId);
        }
    }

    async function ensureVideoBindings(session, request) {
        await cleanupVideoBindings(session, request);

        const requestedDeviceIds = [...new Set(
            request.videoSources
                .filter(source => source.isRenderable)
                .map(source => source.deviceId)
                .filter(Boolean))];

        for (const deviceId of requestedDeviceIds) {
            await ensureVideoBinding(session, deviceId);
        }
    }

    async function ensureAudioBinding(session, input) {
        if (session.audioBindings.has(input.deviceId)) {
            return;
        }

        const track = await getBrowserMedia().createLocalAudioTrack(input.deviceId);
        const sourceNode = session.audioContext.createMediaStreamSource(new MediaStream([track.mediaStreamTrack]));
        const delayNode = session.audioContext.createDelay(maxAudioDelaySeconds);
        const gainNode = session.audioContext.createGain();

        sourceNode.connect(delayNode);
        delayNode.connect(gainNode);

        session.audioBindings.set(input.deviceId, {
            delayNode,
            gainNode,
            outputConnected: false,
            sourceNode,
            track
        });
    }

    function disconnectAudioOutput(binding) {
        if (!binding.outputConnected) {
            return;
        }

        binding.gainNode.disconnect();
        binding.outputConnected = false;
    }

    function syncAudioBinding(session, input) {
        const binding = session.audioBindings.get(input.deviceId);
        if (!binding) {
            return;
        }

        binding.delayNode.delayTime.value = clamp(input.delayMs / 1000, 0, maxAudioDelaySeconds);
        binding.gainNode.gain.value = input.isRoutedToProgram ? Math.max(0, input.gain) : 0;

        disconnectAudioOutput(binding);
        if (!input.isRoutedToProgram) {
            return;
        }

        binding.gainNode.connect(session.audioMasterGain);
        binding.outputConnected = true;
    }

    async function cleanupAudioBindings(session, request) {
        const requestedDeviceIds = new Set(
            request.audioInputs
                .filter(input => input.isRoutedToProgram)
                .map(input => input.deviceId));

        for (const [deviceId, binding] of session.audioBindings.entries()) {
            if (requestedDeviceIds.has(deviceId)) {
                continue;
            }

            disconnectAudioOutput(binding);
            binding.sourceNode.disconnect();
            binding.delayNode.disconnect();
            binding.track.stop();
            session.audioBindings.delete(deviceId);
        }
    }

    async function ensureAudioBindings(session, request) {
        await cleanupAudioBindings(session, request);

        const routedInputs = request.audioInputs.filter(input => input.isRoutedToProgram && Boolean(input.deviceId));
        for (const input of routedInputs) {
            await ensureAudioBinding(session, input);
            syncAudioBinding(session, input);
        }
    }

    function cleanupPrimarySourceAudioBinding(session) {
        const binding = session.primarySourceAudioBinding;
        if (!binding) {
            return;
        }

        binding.gainNode.disconnect();
        binding.sourceNode.disconnect();
        binding.track.stop?.();
        session.primarySourceAudioBinding = null;
    }

    function syncPrimarySourceAudioBinding(session, request) {
        const primarySource = request.videoSources.find(source => source.isPrimary)
            ?? request.videoSources.find(source => source.isRenderable)
            ?? null;
        const binding = primarySource
            ? session.videoBindings.get(primarySource.deviceId)
            : null;
        const audioTrack = binding?.stream?.getAudioTracks?.()?.[0] ?? null;

        if (!primarySource || !audioTrack) {
            cleanupPrimarySourceAudioBinding(session);
            return;
        }

        if (session.primarySourceAudioBinding?.sourceId === primarySource.sourceId
            && session.primarySourceAudioBinding?.trackId === audioTrack.id) {
            return;
        }

        cleanupPrimarySourceAudioBinding(session);

        const track = audioTrack.clone();
        const sourceNode = session.audioContext.createMediaStreamSource(new MediaStream([track]));
        const gainNode = session.audioContext.createGain();

        gainNode.gain.value = primaryScale;
        sourceNode.connect(gainNode);
        gainNode.connect(session.audioMasterGain);

        session.primarySourceAudioBinding = {
            gainNode,
            sourceId: primarySource.sourceId,
            sourceNode,
            track,
            trackId: audioTrack.id
        };
    }

    function ensureProgramMediaStream(session) {
        if (session.mediaStream) {
            return;
        }

        const canvasTrack = session.canvasStream?.getVideoTracks()?.[0] ?? null;
        const audioTrack = session.audioDestination?.stream?.getAudioTracks()?.[0] ?? null;
        const tracks = [canvasTrack, audioTrack].filter(Boolean);
        session.mediaStream = new MediaStream(tracks);
    }

    function clearProgramFrame(session) {
        if (!session.programContext || !session.programCanvas) {
            return;
        }

        session.programContext.clearRect(0, 0, session.programCanvas.width, session.programCanvas.height);
    }

    function hasPlayableVideo(binding) {
        return binding?.element?.readyState >= videoReadyStateThreshold
            && binding.element.videoWidth > 0
            && binding.element.videoHeight > 0;
    }

    function drawCoverImage(context, element, targetX, targetY, targetWidth, targetHeight) {
        const sourceWidth = element.videoWidth;
        const sourceHeight = element.videoHeight;
        const sourceRatio = sourceWidth / sourceHeight;
        const targetRatio = targetWidth / targetHeight;

        let cropWidth = sourceWidth;
        let cropHeight = sourceHeight;
        let cropX = 0;
        let cropY = 0;

        if (sourceRatio > targetRatio) {
            cropWidth = sourceHeight * targetRatio;
            cropX = (sourceWidth - cropWidth) / 2;
        }
        else {
            cropHeight = sourceWidth / targetRatio;
            cropY = (sourceHeight - cropHeight) / 2;
        }

        context.drawImage(
            element,
            cropX,
            cropY,
            cropWidth,
            cropHeight,
            targetX,
            targetY,
            targetWidth,
            targetHeight);
    }

    function drawOverlaySource(session, source) {
        const binding = session.videoBindings.get(source.deviceId);
        if (!hasPlayableVideo(binding)) {
            return;
        }

        const context = session.programContext;
        const canvas = session.programCanvas;
        const width = canvas.width * source.transform.width;
        const height = canvas.height * source.transform.height;
        const centerX = canvas.width * source.transform.x;
        const centerY = canvas.height * source.transform.y;
        const rotation = source.transform.rotation * (Math.PI / 180);
        const mirrorX = source.transform.mirrorHorizontal ? -1 : 1;
        const mirrorY = source.transform.mirrorVertical ? -1 : 1;

        context.save();
        context.globalAlpha = clamp(source.transform.opacity, overlayMinimumOpacity, overlayMaximumOpacity);
        context.translate(centerX, centerY);
        context.rotate(rotation);
        context.scale(mirrorX, mirrorY);
        drawCoverImage(context, binding.element, -width / 2, -height / 2, width, height);
        context.restore();
    }

    function drawPrimarySource(session, source) {
        const binding = session.videoBindings.get(source.deviceId);
        if (!hasPlayableVideo(binding)) {
            clearProgramFrame(session);
            return;
        }

        drawCoverImage(
            session.programContext,
            binding.element,
            0,
            0,
            session.programCanvas.width,
            session.programCanvas.height);
    }

    function renderProgramFrame(session) {
        const request = session.requestSnapshot;
        if (!request || !session.programCanvas || !session.programContext) {
            return;
        }

        session.programContext.clearRect(0, 0, session.programCanvas.width, session.programCanvas.height);

        const renderableSources = request.videoSources.filter(source => source.isRenderable);
        if (renderableSources.length === 0) {
            return;
        }

        const primarySource = renderableSources.find(source => source.isPrimary) ?? renderableSources[0];
        drawPrimarySource(session, primarySource);

        renderableSources
            .filter(source => !source.isPrimary && source.transform.visible && source.transform.includeInOutput)
            .sort((left, right) => left.transform.zIndex - right.transform.zIndex)
            .forEach(source => drawOverlaySource(session, source));
    }

    function startRenderLoop(session) {
        if (session.renderHandle) {
            return;
        }

        const step = () => {
            if (!session.requestSnapshot) {
                session.renderHandle = 0;
                return;
            }

            renderProgramFrame(session);
            session.renderHandle = window.requestAnimationFrame(step);
        };

        session.renderHandle = window.requestAnimationFrame(step);
    }

    async function cleanupProgramSession(session) {
        if (session.renderHandle) {
            window.cancelAnimationFrame(session.renderHandle);
            session.renderHandle = 0;
        }

        stopTrackSet(session.mediaStream);
        stopTrackSet(session.canvasStream);
        session.mediaStream = null;
        session.canvasStream = null;

        for (const binding of session.videoBindings?.values() ?? []) {
            stopTrackSet(binding.stream);
            removeElement(binding.element);
        }

        for (const binding of session.audioBindings?.values() ?? []) {
            disconnectAudioOutput(binding);
            binding.sourceNode.disconnect();
            binding.delayNode.disconnect();
            stopTrackSet(binding.stream);
        }

        session.videoBindings?.clear?.();
        session.audioBindings?.clear?.();
        session.programCanvas?.remove?.();
        session.programCanvas = null;
        session.programContext = null;
        cleanupPrimarySourceAudioBinding(session);

        if (session.audioMasterGain) {
            session.audioMasterGain.disconnect();
            session.audioMasterGain = null;
        }

        if (session.audioContext) {
            await session.audioContext.close().catch(() => {});
            session.audioContext = null;
        }

        session.audioDestination = null;
        session.requestSnapshot = null;
    }

    async function ensureProgramSession(session, request) {
        ensureSessionInfrastructure(session);
        session.requestSnapshot = request;

        ensureProgramCanvas(session, request);
        await ensureAudioInfrastructure(session);
        await ensureVideoBindings(session, request);
        await ensureAudioBindings(session, request);
        syncPrimarySourceAudioBinding(session, request);
        ensureProgramMediaStream(session);
        renderProgramFrame(session);
        startRenderLoop(session);
    }

    function getProgramState(session) {
        const request = session.requestSnapshot;
        if (!request) {
            return {
                audioInputCount: 0,
                frameRate: 0,
                primarySourceId: "",
                videoSourceCount: 0
            };
        }

        return {
            audioInputCount: request.audioInputs.filter(input => input.isRoutedToProgram).length,
            frameRate: request.programVideo.frameRate,
            primarySourceId: request.primarySourceId,
            videoSourceCount: request.videoSources.filter(source => source.isRenderable).length
        };
    }

    window[composerNamespace] = {
        cleanupProgramSession,
        ensureProgramSession,
        getProgramState
    };
})();
