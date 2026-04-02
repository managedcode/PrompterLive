(function () {
    const namespace = "PrompterOneGoLiveOutputVdoNinja";
    const connectedEvent = "connected";
    const defaultStreamLabel = "PrompterOne Program";
    const detailKey = "detail";
    const disconnectedEvent = "disconnected";
    const peerConnectedEvent = "peerConnected";
    const peerDisconnectedEvent = "peerDisconnected";
    const peerLatencyEvent = "peerLatency";
    const peerListingEvent = "peerListing";
    const publishingEvent = "publishing";
    const publishingStoppedEvent = "publishingStopped";
    const publishParam = "push";
    const roomJoinedEvent = "roomJoined";
    const roomLeftEvent = "roomLeft";
    const roomParam = "room";
    const shortRoomParam = "r";
    const streamIdSeparator = "-";
    const streamIdUnsafePattern = /[^a-z0-9]+/gi;
    const viewParam = "view";

    function ensureSessionDefaults(session) {
        session.vdoNinjaActive = Boolean(session.vdoNinjaActive);
        session.vdoNinjaConnected = Boolean(session.vdoNinjaConnected);
        session.vdoNinjaConnectionId = String(session.vdoNinjaConnectionId || "");
        session.vdoNinjaJoinedRoom = Boolean(session.vdoNinjaJoinedRoom);
        session.vdoNinjaLastPeerLatencyMs = Number.isFinite(session.vdoNinjaLastPeerLatencyMs) ? session.vdoNinjaLastPeerLatencyMs : 0;
        session.vdoNinjaPeerCount = Number.isFinite(session.vdoNinjaPeerCount) ? session.vdoNinjaPeerCount : 0;
        session.vdoNinjaPublishUrl = String(session.vdoNinjaPublishUrl || "");
        session.vdoNinjaPublisher = session.vdoNinjaPublisher || null;
        session.vdoNinjaRoomName = String(session.vdoNinjaRoomName || "");
        session.vdoNinjaStreamId = String(session.vdoNinjaStreamId || "");
        session.vdoNinjaViewUrl = String(session.vdoNinjaViewUrl || "");
    }

    function resetSessionState(session) {
        ensureSessionDefaults(session);
        session.vdoNinjaActive = false;
        session.vdoNinjaConnected = false;
        session.vdoNinjaConnectionId = "";
        session.vdoNinjaJoinedRoom = false;
        session.vdoNinjaLastPeerLatencyMs = 0;
        session.vdoNinjaPeerCount = 0;
        session.vdoNinjaPublishUrl = "";
        session.vdoNinjaRoomName = "";
        session.vdoNinjaStreamId = "";
        session.vdoNinjaViewUrl = "";
    }

    function getSdkConstructor() {
        const ctor = window.VDONinjaSDK || window.VDONinja;
        if (typeof ctor !== "function") {
            throw new Error("VDO.Ninja SDK runtime is not available.");
        }

        return ctor;
    }

    function sanitizeStreamSegment(value) {
        const normalized = String(value || "")
            .trim()
            .toLowerCase()
            .replace(streamIdUnsafePattern, streamIdSeparator)
            .replace(/-{2,}/g, streamIdSeparator)
            .replace(/^-|-$/g, "");

        return normalized || "program";
    }

    function parsePublishUrl(publishUrl) {
        if (!publishUrl) {
            return { publishUrl: "", roomName: "", streamId: "" };
        }

        try {
            const url = new URL(publishUrl, window.location.origin);
            return {
                publishUrl,
                roomName: url.searchParams.get(roomParam) || url.searchParams.get(shortRoomParam) || "",
                streamId: url.searchParams.get(publishParam) || url.searchParams.get(viewParam) || ""
            };
        } catch {
            return {
                publishUrl,
                roomName: "",
                streamId: ""
            };
        }
    }

    function resolveConfig(connection) {
        const parsed = parsePublishUrl(connection.publishUrl);
        const baseUrl = connection.baseUrl || "https://vdo.ninja/";
        const roomName = connection.roomName || parsed.roomName;
        const streamId = parsed.streamId
            || [connection.connectionId, roomName].filter(Boolean).map(sanitizeStreamSegment).join(streamIdSeparator);
        let publishUrl = parsed.publishUrl;
        let viewUrl = connection.viewUrl;

        if (!publishUrl && roomName) {
            const publishEndpoint = new URL(baseUrl, window.location.origin);
            publishEndpoint.searchParams.set(roomParam, roomName);
            publishEndpoint.searchParams.set(publishParam, streamId);
            publishUrl = publishEndpoint.toString();
        }

        if (!viewUrl && streamId) {
            const viewEndpoint = new URL(baseUrl, window.location.origin);
            viewEndpoint.searchParams.set(viewParam, streamId);
            viewUrl = viewEndpoint.toString();
        }

        return {
            connectionId: connection.connectionId,
            label: defaultStreamLabel,
            publishUrl,
            roomName,
            streamId: streamId || sanitizeStreamSegment(connection.connectionId),
            viewUrl
        };
    }

    function updatePeerCount(session, event) {
        const detail = event?.[detailKey];
        if (Array.isArray(detail)) {
            session.vdoNinjaPeerCount = detail.length;
            return;
        }

        if (Array.isArray(detail?.peers)) {
            session.vdoNinjaPeerCount = detail.peers.length;
            return;
        }

        if (Number.isFinite(detail?.count)) {
            session.vdoNinjaPeerCount = detail.count;
        }
    }

    function attachEvents(session, publisher) {
        publisher.addEventListener(connectedEvent, () => {
            session.vdoNinjaConnected = true;
        });
        publisher.addEventListener(disconnectedEvent, () => {
            session.vdoNinjaConnected = false;
            session.vdoNinjaActive = false;
        });
        publisher.addEventListener(roomJoinedEvent, () => {
            session.vdoNinjaJoinedRoom = true;
        });
        publisher.addEventListener(roomLeftEvent, () => {
            session.vdoNinjaJoinedRoom = false;
            session.vdoNinjaPeerCount = 0;
        });
        publisher.addEventListener(peerListingEvent, event => {
            updatePeerCount(session, event);
        });
        publisher.addEventListener(peerConnectedEvent, () => {
            session.vdoNinjaPeerCount += 1;
        });
        publisher.addEventListener(peerDisconnectedEvent, () => {
            session.vdoNinjaPeerCount = Math.max(0, session.vdoNinjaPeerCount - 1);
        });
        publisher.addEventListener(peerLatencyEvent, event => {
            const latency = event?.[detailKey]?.latency ?? event?.[detailKey]?.value;
            session.vdoNinjaLastPeerLatencyMs = Number.isFinite(latency) ? Math.round(latency) : session.vdoNinjaLastPeerLatencyMs;
        });
        publisher.addEventListener(publishingEvent, event => {
            session.vdoNinjaActive = true;
            session.vdoNinjaStreamId = event?.[detailKey]?.streamID || session.vdoNinjaStreamId;
        });
        publisher.addEventListener(publishingStoppedEvent, () => {
            session.vdoNinjaActive = false;
        });
    }

    async function createPublisher(session, config) {
        const Sdk = getSdkConstructor();
        const publisher = new Sdk({
            debug: false,
            label: config.label,
            room: config.roomName || undefined
        });

        attachEvents(session, publisher);
        await publisher.connect();
        if (config.roomName) {
            await publisher.joinRoom({ room: config.roomName });
            session.vdoNinjaJoinedRoom = true;
        }

        session.vdoNinjaPublisher = publisher;
        session.vdoNinjaConnected = true;
        return publisher;
    }

    async function stopSession(session) {
        ensureSessionDefaults(session);

        const publisher = session.vdoNinjaPublisher;
        if (!publisher) {
            resetSessionState(session);
            return;
        }

        if (session.vdoNinjaActive && typeof publisher.stopPublishing === "function") {
            await publisher.stopPublishing().catch(() => {});
        }

        if (session.vdoNinjaJoinedRoom && typeof publisher.leaveRoom === "function") {
            await publisher.leaveRoom().catch(() => {});
        }

        publisher.disconnect?.();
        session.vdoNinjaPublisher = null;
        resetSessionState(session);
    }

    async function startSession(session, connection) {
        ensureSessionDefaults(session);

        const config = resolveConfig(connection);
        const needsNewPublisher = !session.vdoNinjaPublisher || session.vdoNinjaRoomName !== config.roomName;

        if (needsNewPublisher) {
            await stopSession(session);
            await createPublisher(session, config);
        } else if (session.vdoNinjaActive) {
            await session.vdoNinjaPublisher.stopPublishing().catch(() => {});
        }

        await session.vdoNinjaPublisher.publish(session.mediaStream, {
            label: config.label,
            room: config.roomName || undefined,
            streamID: config.streamId
        });

        session.vdoNinjaActive = true;
        session.vdoNinjaConnectionId = config.connectionId;
        session.vdoNinjaPublishUrl = config.publishUrl;
        session.vdoNinjaRoomName = config.roomName;
        session.vdoNinjaStreamId = config.streamId;
        session.vdoNinjaViewUrl = config.viewUrl;
    }

    function buildSnapshot(session) {
        ensureSessionDefaults(session);

        return {
            active: session.vdoNinjaActive,
            connected: session.vdoNinjaConnected,
            lastPeerLatencyMs: session.vdoNinjaLastPeerLatencyMs,
            peerCount: session.vdoNinjaPeerCount,
            publishUrl: session.vdoNinjaPublishUrl,
            roomName: session.vdoNinjaRoomName,
            streamId: session.vdoNinjaStreamId,
            viewUrl: session.vdoNinjaViewUrl
        };
    }

    window[namespace] = {
        buildSnapshot,
        startSession,
        stopSession
    };
})();
