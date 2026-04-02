(function () {
    const browserMediaNamespace = "BrowserMediaInterop";
    const interopNamespace = "PrompterOneGoLiveRemoteSources";
    const reconnectDelayMs = 5000;
    const sourceRoleFlag = 1;
    const streamingPlatformLiveKit = 0;
    const streamingPlatformVdoNinja = 1;
    const supportNamespace = "PrompterOneGoLiveOutputSupport";
    const syntheticHarnessGlobal = "__prompterOneMediaHarness";
    const vdoSdkGlobal = "VDONinjaSDK";
    const streamMetadataVersion = 1;
    const sessions = new Map();

    function getBrowserMedia() {
        const browserMedia = window[browserMediaNamespace];
        if (!browserMedia?.registerRemoteStream || !browserMedia?.unregisterRemoteStream) {
            throw new Error("Browser media remote-stream registry is not available.");
        }

        return browserMedia;
    }

    function getSupport() {
        const support = window[supportNamespace];
        if (!support?.normalizeRequest) {
            throw new Error("Go Live output support runtime is not available.");
        }

        return support;
    }

    function ensureSession(sessionId) {
        if (!sessions.has(sessionId)) {
            sessions.set(sessionId, { connections: new Map() });
        }

        return sessions.get(sessionId);
    }

    function getSyntheticHarness() {
        const harness = window[syntheticHarnessGlobal];
        return harness && typeof harness.getRemoteSources === "function"
            ? harness
            : null;
    }

    function buildConfigKey(connection) {
        return [
            connection.connectionId,
            connection.platformKind,
            connection.serverUrl,
            connection.baseUrl,
            connection.roomName,
            connection.token,
            connection.publishUrl,
            connection.viewUrl
        ].join("|");
    }

    function buildRemoteSourceId(connection, externalId) {
        return `${connection.connectionId}:${externalId}`;
    }

    function buildSourceLabel(connection, label) {
        return label || connection.name || connection.connectionId;
    }

    function createSourceMetadata(connection, sourceId, label) {
        return {
            audioDeviceId: null,
            audioLabel: label,
            connectionId: connection.connectionId,
            isSynthetic: false,
            platformKind: connection.platformKind,
            remoteSourceId: sourceId,
            streamMetadataVersion,
            videoDeviceId: sourceId,
            videoLabel: label
        };
    }

    function upsertSource(connectionState, sourceId, label, stream) {
        if (!(stream instanceof MediaStream)) {
            return;
        }

        const resolvedLabel = buildSourceLabel(connectionState.connection, label);
        getBrowserMedia().registerRemoteStream(
            sourceId,
            stream,
            createSourceMetadata(connectionState.connection, sourceId, resolvedLabel));

        connectionState.sources.set(sourceId, {
            connectionId: connectionState.connection.connectionId,
            deviceId: sourceId,
            isConnected: true,
            label: resolvedLabel,
            platformKind: connectionState.connection.platformKind,
            sourceId
        });
        connectionState.registeredStreams.set(sourceId, stream);
    }

    function removeSource(connectionState, sourceId) {
        getBrowserMedia().unregisterRemoteStream(sourceId);
        connectionState.sources.delete(sourceId);
        connectionState.registeredStreams.delete(sourceId);
    }

    function clearSources(connectionState) {
        for (const sourceId of connectionState.sources.keys()) {
            getBrowserMedia().unregisterRemoteStream(sourceId);
        }

        connectionState.sources.clear();
        connectionState.registeredStreams.clear();
    }

    function canReuseConnectionState(connectionState) {
        if (connectionState.connected) {
            return true;
        }

        return (Date.now() - connectionState.lastAttemptAt) < reconnectDelayMs;
    }

    function buildSnapshot(session) {
        const connections = Array.from(session.connections.values()).map(connectionState => ({
            connectionId: connectionState.connection.connectionId,
            connected: connectionState.connected === true,
            platformKind: connectionState.connection.platformKind,
            roomName: connectionState.connection.roomName || "",
            serverUrl: connectionState.connection.serverUrl || "",
            sources: Array.from(connectionState.sources.values())
        }));
        const sources = connections.flatMap(connection => connection.sources);
        return { connections, sources };
    }

    async function disconnectConnection(connectionState) {
        clearSources(connectionState);
        connectionState.connected = false;

        if (connectionState.platform === streamingPlatformLiveKit) {
            connectionState.room?.disconnect?.();
            connectionState.room = null;
            return;
        }

        if (connectionState.controller?.stop) {
            connectionState.controller.stop();
        }

        if (connectionState.sdk) {
            if (connectionState.viewStreamId) {
                await connectionState.sdk.stopViewing(connectionState.viewStreamId).catch(() => {});
            }

            await connectionState.sdk.leaveRoom?.().catch(() => {});
            connectionState.sdk.disconnect?.();
            connectionState.sdk = null;
        }
    }

    function createConnectionState(connection) {
        return {
            configKey: buildConfigKey(connection),
            connected: false,
            connection,
            controller: null,
            participantStreams: new Map(),
            platform: connection.platformKind,
            lastAttemptAt: 0,
            registeredStreams: new Map(),
            room: null,
            sdk: null,
            sources: new Map(),
            viewStreamId: ""
        };
    }

    function recordConnectionFailure(session, connection, error) {
        const connectionState = session.connections.get(connection.connectionId) ?? createConnectionState(connection);
        connectionState.connection = connection;
        connectionState.configKey = buildConfigKey(connection);
        connectionState.connected = false;
        connectionState.lastAttemptAt = Date.now();
        clearSources(connectionState);
        session.connections.set(connection.connectionId, connectionState);
        console.warn("Go Live remote-source connection failed.", {
            connectionId: connection.connectionId,
            message: error instanceof Error ? error.message : String(error || "")
        });
    }

    async function syncSyntheticConnection(session, connection) {
        const harness = getSyntheticHarness();
        if (!harness) {
            return false;
        }

        const syntheticSources = await Promise.resolve(harness.getRemoteSources(connection.connectionId) || []);
        if (!Array.isArray(syntheticSources) || syntheticSources.length === 0) {
            return false;
        }

        const existing = session.connections.get(connection.connectionId);
        if (existing && existing.configKey !== buildConfigKey(connection)) {
            await disconnectConnection(existing);
            session.connections.delete(connection.connectionId);
        }

        const connectionState = session.connections.get(connection.connectionId) ?? createConnectionState(connection);
        connectionState.connection = connection;
        connectionState.configKey = buildConfigKey(connection);
        connectionState.connected = true;
        const nextSourceIds = new Set();

        syntheticSources.forEach(source => {
            const sourceId = buildRemoteSourceId(connection, source.sourceId || source.id || source.label || connection.connectionId);
            nextSourceIds.add(sourceId);

            if (connectionState.registeredStreams.get(sourceId) !== source.stream) {
                removeSource(connectionState, sourceId);
            }

            upsertSource(connectionState, sourceId, source.label, source.stream);
        });

        Array.from(connectionState.sources.keys())
            .filter(sourceId => !nextSourceIds.has(sourceId))
            .forEach(sourceId => removeSource(connectionState, sourceId));

        session.connections.set(connection.connectionId, connectionState);
        return true;
    }

    function getLiveKitClient() {
        const client = window.LivekitClient;
        if (!client?.Room) {
            throw new Error("LiveKit client runtime is not available.");
        }

        return client;
    }

    function getParticipantId(participant) {
        return participant?.identity || participant?.sid || "participant";
    }

    function syncLiveKitParticipant(connectionState, participant) {
        const participantId = getParticipantId(participant);
        const sourceId = buildRemoteSourceId(connectionState.connection, participantId);
        const existingStream = connectionState.participantStreams.get(participantId) || new MediaStream();
        const publications = Array.from(participant?.trackPublications?.values?.() || []);

        publications.forEach(publication => {
            const mediaTrack = publication?.track?.mediaStreamTrack;
            if (!mediaTrack) {
                return;
            }

            existingStream.getTracks()
                .filter(track => track.kind === mediaTrack.kind)
                .forEach(track => existingStream.removeTrack(track));
            existingStream.addTrack(mediaTrack);
        });

        if (existingStream.getVideoTracks().length === 0) {
            connectionState.participantStreams.delete(participantId);
            removeSource(connectionState, sourceId);
            return;
        }

        connectionState.participantStreams.set(participantId, existingStream);
        upsertSource(connectionState, sourceId, participant?.name || participantId, existingStream);
    }

    function removeLiveKitParticipant(connectionState, participant) {
        const participantId = getParticipantId(participant);
        connectionState.participantStreams.delete(participantId);
        removeSource(connectionState, buildRemoteSourceId(connectionState.connection, participantId));
    }

    function attachLiveKitListeners(connectionState) {
        const room = connectionState.room;
        room
            .on("participantConnected", participant => syncLiveKitParticipant(connectionState, participant))
            .on("participantDisconnected", participant => removeLiveKitParticipant(connectionState, participant))
            .on("trackSubscribed", (_track, _publication, participant) => syncLiveKitParticipant(connectionState, participant))
            .on("trackUnsubscribed", (_track, _publication, participant) => syncLiveKitParticipant(connectionState, participant))
            .on("disconnected", () => {
                connectionState.connected = false;
                clearSources(connectionState);
            });
    }

    async function ensureLiveKitConnection(session, connection) {
        const existing = session.connections.get(connection.connectionId);
        if (existing && existing.configKey === buildConfigKey(connection) && canReuseConnectionState(existing)) {
            return existing;
        }

        if (existing) {
            await disconnectConnection(existing);
        }

        const client = getLiveKitClient();
        const connectionState = createConnectionState(connection);
        connectionState.lastAttemptAt = Date.now();
        connectionState.room = new client.Room();
        attachLiveKitListeners(connectionState);
        await connectionState.room.connect(connection.serverUrl, connection.token);
        connectionState.connected = true;

        Array.from(connectionState.room.remoteParticipants?.values?.() || [])
            .forEach(participant => syncLiveKitParticipant(connectionState, participant));

        session.connections.set(connection.connectionId, connectionState);
        return connectionState;
    }

    function parseVdoViewStreamId(connection) {
        if (!connection.viewUrl) {
            return "";
        }

        try {
            return new URL(connection.viewUrl).searchParams.get("view") || "";
        }
        catch {
            return "";
        }
    }

    function buildVdoOptions(connection) {
        const options = {};
        if (connection.baseUrl) {
            try {
                const url = new URL(connection.baseUrl);
                options.host = `${url.protocol === "https:" ? "wss" : "ws"}://${url.host}`;
                options.salt = url.host;
            }
            catch {
            }
        }

        return options;
    }

    function attachVdoListeners(connectionState) {
        connectionState.sdk.addEventListener("track", event => {
            const detail = event.detail || {};
            const externalId = detail.streamID || detail.uuid || connectionState.connection.connectionId;
            const sourceId = buildRemoteSourceId(connectionState.connection, externalId);
            const stream = connectionState.participantStreams.get(externalId) || new MediaStream();
            const incomingTracks = detail.streams?.[0]?.getTracks?.() || (detail.track ? [detail.track] : []);

            incomingTracks.forEach(track => {
                stream.getTracks()
                    .filter(existingTrack => existingTrack.kind === track.kind)
                    .forEach(existingTrack => stream.removeTrack(existingTrack));
                stream.addTrack(track);
            });

            connectionState.participantStreams.set(externalId, stream);
            if (stream.getVideoTracks().length === 0) {
                return;
            }

            upsertSource(connectionState, sourceId, detail.info?.label || externalId, stream);
        });
        connectionState.sdk.addEventListener("disconnected", () => {
            connectionState.connected = false;
            clearSources(connectionState);
        });
    }

    async function ensureVdoConnection(session, connection) {
        const existing = session.connections.get(connection.connectionId);
        if (existing && existing.configKey === buildConfigKey(connection) && canReuseConnectionState(existing)) {
            return existing;
        }

        if (existing) {
            await disconnectConnection(existing);
        }

        const VdoCtor = window[vdoSdkGlobal] || window.VDONinja;
        if (typeof VdoCtor !== "function") {
            throw new Error("VDO.Ninja SDK runtime is not available.");
        }

        const connectionState = createConnectionState(connection);
        connectionState.lastAttemptAt = Date.now();
        connectionState.sdk = new VdoCtor(buildVdoOptions(connection));
        attachVdoListeners(connectionState);
        await connectionState.sdk.connect();
        connectionState.connected = true;

        if (connection.roomName) {
            connectionState.controller = await connectionState.sdk.autoConnect({
                mode: "full",
                room: connection.roomName
            }).catch(() => null);
        }
        else {
            const viewStreamId = parseVdoViewStreamId(connection);
            connectionState.viewStreamId = viewStreamId;
            if (viewStreamId) {
                await connectionState.sdk.view(viewStreamId, {});
            }
        }

        session.connections.set(connection.connectionId, connectionState);
        return connectionState;
    }

    window[interopNamespace] = {
        getSessionState(sessionId) {
            const session = sessions.get(sessionId);
            return session ? buildSnapshot(session) : null;
        },

        async stopSession(sessionId) {
            const session = sessions.get(sessionId);
            if (!session) {
                return;
            }

            for (const connectionState of session.connections.values()) {
                await disconnectConnection(connectionState);
            }

            sessions.delete(sessionId);
        },

        async syncConnections(sessionId, rawRequest) {
            const session = ensureSession(sessionId);
            const request = getSupport().normalizeRequest(rawRequest);
            const desiredConnections = request.transportConnections.filter(connection =>
                connection.isEnabled && (connection.roles & sourceRoleFlag) === sourceRoleFlag);
            const desiredIds = new Set(desiredConnections.map(connection => connection.connectionId));

            for (const [connectionId, connectionState] of session.connections.entries()) {
                if (desiredIds.has(connectionId)) {
                    continue;
                }

                await disconnectConnection(connectionState);
                session.connections.delete(connectionId);
            }

            for (const connection of desiredConnections) {
                try {
                    if (await syncSyntheticConnection(session, connection)) {
                        continue;
                    }

                    if (connection.platformKind === streamingPlatformLiveKit) {
                        await ensureLiveKitConnection(session, connection);
                        continue;
                    }

                    if (connection.platformKind === streamingPlatformVdoNinja) {
                        await ensureVdoConnection(session, connection);
                    }
                }
                catch (error) {
                    recordConnectionFailure(session, connection, error);
                }
            }
        }
    };
})();
