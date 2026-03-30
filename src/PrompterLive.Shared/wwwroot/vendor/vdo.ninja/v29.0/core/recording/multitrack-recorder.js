import { waitForLegacySession } from '../legacy/session-bridge.js';
import { monitorTrackLevel } from '../audio/meters.js';
import { TrackRecorder } from './track-recorder.js';
import { convertBlobToWav } from './wav-encoder.js';

const DEFAULT_OPTIONS = {
  includeLocal: true,
  includeRemotes: true,
  includeScreenshares: false,
  includeVideo: false,
  mimeType: null,
  timeslice: 0,
  bitsPerSecond: null,
  monitorLevels: true,
  audioContext: null,
  targetSampleRate: 48000,
  filenamePrefix: 'podcast',
};

function sanitizeSegment(value, fallback = 'track') {
  if (!value) {
    return fallback;
  }
  return String(value)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '') || fallback;
}

function extensionFromMime(mimeType) {
  if (!mimeType) {
    return 'bin';
  }
  if (mimeType.includes('wav')) {
    return 'wav';
  }
  if (mimeType.includes('webm')) {
    return 'webm';
  }
  if (mimeType.includes('ogg')) {
    return 'ogg';
  }
  if (mimeType.includes('mp4')) {
    return 'mp4';
  }
  if (mimeType.includes('m4a')) {
    return 'm4a';
  }
  return 'bin';
}

function buildTimestamp(epoch = Date.now()) {
  return new Date(epoch).toISOString().replace(/[:.]/g, '-');
}

function safeCloneTrack(track) {
  try {
    return track.clone();
  } catch (error) {
    console.warn('Unable to clone track, using original reference', error);
    return track;
  }
}

function gatherTracksFromStream(stream, { includeVideo }) {
  if (!stream) {
    return { audio: [], video: [] };
  }
  const audio = stream.getAudioTracks ? stream.getAudioTracks() : [];
  const video = includeVideo && stream.getVideoTracks ? stream.getVideoTracks() : [];
  return {
    audio: audio.filter(Boolean),
    video: video.filter(Boolean),
  };
}

export class MultiTrackRecorder extends EventTarget {
  constructor(options = {}) {
    super();
    this.options = { ...DEFAULT_OPTIONS, ...options };
    this.sessionPromise = null;
    this.session = null;
    this.recorders = new Map();
    this.files = new Map();
    this.trackMeters = new Map();
    this.trackStartTimes = new Map(); // Per-track start times relative to session start
    this.startedAt = null;
  }

  async ensureSession() {
    if (this.session) {
      return this.session;
    }
    if (!this.sessionPromise) {
      this.sessionPromise = waitForLegacySession();
    }
    this.session = await this.sessionPromise;
    return this.session;
  }

  async listRecordableParticipants() {
    const session = await this.ensureSession();
    const participants = [];

    if (this.options.includeLocal && session.streamSrc) {
      participants.push({
        uuid: 'local',
        kind: 'local',
        label: session.label || 'Host',
        stream: session.streamSrc,
        streamID: session.streamID,
      });
    }

    if (this.options.includeRemotes && session.rpcs) {
      Object.entries(session.rpcs).forEach(([uuid, peer]) => {
        if (!peer) {
          return;
        }
        const label = peer.label || peer.streamID || uuid;
        const stream = peer.streamSrc || peer.stream || peer.videoElement?.srcObject;
        if (!stream) {
          return;
        }
        participants.push({
          uuid,
          kind: 'remote',
          label,
          stream,
          streamID: peer.streamID,
        });

        if (this.options.includeScreenshares && peer.screenShareStream) {
          participants.push({
            uuid: `${uuid}:screen`,
            kind: 'screenshare',
            label: `${label} (Screen)`,
            stream: peer.screenShareStream,
            streamID: peer.streamID ? `${peer.streamID}:screen` : `${uuid}:screen`,
          });
        }
      });
    }

    return participants;
  }

  createTrackRecorders(participant, options, startOffsetSeconds = 0) {
    const { includeVideo, mimeType, bitsPerSecond, timeslice, monitorLevels, audioContext } = options;
    const { audio, video } = gatherTracksFromStream(participant.stream, { includeVideo });
    const recorders = [];
    const trackStartOffset = Number.isFinite(startOffsetSeconds) ? Math.max(0, startOffsetSeconds) : 0;

    audio.forEach((track, index) => {
      const cloned = safeCloneTrack(track);
      const recorder = new TrackRecorder({
        track: cloned,
        uuid: participant.uuid,
        label: `${participant.label || participant.uuid}#${index + 1}`,
        kind: 'audio',
        mimeType,
      });
      recorder.start({ timeslice, bitsPerSecond });
      recorders.push({ recorder, trackType: 'audio', channelIndex: index, startOffsetSeconds: trackStartOffset });
      if (monitorLevels && audioContext) {
        monitorTrackLevel(audioContext, cloned, {
          uuid: participant.uuid,
          trackType: 'audio',
          metadata: { channelIndex: index, label: participant.label },
        })
          .then((meter) => {
            const meterKey = `${participant.uuid}:audio:${index}`;
            this.trackMeters.set(meterKey, meter);
            this.dispatchEvent(
              new CustomEvent('meter-ready', {
                detail: {
                  participant,
                  trackType: 'audio',
                  channelIndex: index,
                  meter,
                },
              }),
            );
          })
          .catch((error) => {
            console.warn('Failed to attach meter to track', error);
          });
      }
    });

    video.forEach((track, index) => {
      const cloned = safeCloneTrack(track);
      const recorder = new TrackRecorder({
        track: cloned,
        uuid: participant.uuid,
        label: `${participant.label || participant.uuid}-video#${index + 1}`,
        kind: 'video',
        mimeType: null,
      });
      recorder.start({ timeslice, bitsPerSecond });
      recorders.push({ recorder, trackType: 'video', channelIndex: index, startOffsetSeconds: trackStartOffset });
    });

    return recorders;
  }

  attachRecorderHandlers(participant, recorders) {
    recorders.forEach(({ recorder, trackType, channelIndex, startOffsetSeconds }) => {
      const key = `${participant.uuid}:${trackType}:${channelIndex}`;
      this.recorders.set(key, recorder);
      this.trackStartTimes.set(key, startOffsetSeconds);
      recorder.addEventListener('data', (event) => {
        this.dispatchEvent(
          new CustomEvent('chunk', {
            detail: {
              participant,
              trackType,
              channelIndex,
              data: event.detail,
            },
          }),
        );
      });
      recorder.addEventListener('error', (event) => {
        this.dispatchEvent(
          new CustomEvent('error', {
            detail: {
              participant,
              trackType,
              channelIndex,
              error: event.detail,
            },
          }),
        );
      });
      recorder.addEventListener('stop', () => {
        const blob = recorder.toBlob();
        if (blob) {
          const fileKey = `${participant.uuid}:${trackType}:${channelIndex}`;
          this.files.set(fileKey, {
            blob,
            originalBlob: blob,
            participant,
            trackType,
            channelIndex,
            mimeType: recorder.mimeType,
            originalMimeType: recorder.mimeType,
            durationSeconds: typeof recorder.getDurationSeconds === 'function' ? recorder.getDurationSeconds() : null,
            recorderLabel: recorder.label,
            startOffsetSeconds,
          });
        }
        this.dispatchEvent(
          new CustomEvent('track-stopped', {
            detail: {
              participant,
              trackType,
              channelIndex,
            },
          }),
        );
      });
    });
  }

  addParticipant(participant) {
    if (!this.startedAt) {
      throw new Error('Cannot add participant: recording not started.');
    }
    if (!participant || !participant.stream) {
      throw new Error('Cannot add participant: missing stream.');
    }
    const startOffsetSeconds = (Date.now() - this.startedAt) / 1000;
    const recorders = this.createTrackRecorders(participant, this.options, startOffsetSeconds);
    if (!recorders.length) {
      return { added: false, tracks: 0, startOffsetSeconds };
    }
    this.attachRecorderHandlers(participant, recorders);
    this.dispatchEvent(
      new CustomEvent('participant-added', {
        detail: {
          participant,
          trackCount: recorders.length,
          startOffsetSeconds,
        },
      }),
    );
    return { added: true, tracks: recorders.length, startOffsetSeconds };
  }

  isRecording() {
    return this.startedAt !== null && this.recorders.size > 0;
  }

  async start(customOptions = {}) {
    const options = { ...this.options, ...customOptions };
    this.options = options;
    this.files.clear();
    this.startedAt = Date.now();

    if (this.recorders.size) {
      throw new Error('MultiTrackRecorder already running.');
    }

    const participants = await this.listRecordableParticipants();
    const extras = Array.isArray(options.extraParticipants)
      ? options.extraParticipants
          .map((participant, index) => {
            if (!participant || !participant.stream) {
              return null;
            }
            const uuid = participant.uuid || `external-${index}`;
            return {
              uuid,
              kind: participant.kind || 'external',
              label: participant.label || uuid,
              stream: participant.stream,
              streamID: participant.streamID || uuid,
              external: true,
            };
          })
          .filter(Boolean)
      : [];
    const participantLookup = new Map();
    participants.forEach((participant) => {
      if (participant && participant.uuid) {
        participantLookup.set(participant.uuid, participant);
      }
    });
    extras.forEach((participant) => {
      if (!participantLookup.has(participant.uuid)) {
        participants.push(participant);
        participantLookup.set(participant.uuid, participant);
      }
    });

    if (!participants.length) {
      throw new Error('No recordable participants found.');
    }

    participants.forEach((participant) => {
      const recorders = this.createTrackRecorders(participant, options, 0);
      this.attachRecorderHandlers(participant, recorders);
    });

    this.dispatchEvent(new CustomEvent('start', { detail: { participants, startedAt: this.startedAt } }));
  }

  async stop({ markers = null } = {}) {
    const stops = [];
    this.recorders.forEach((recorder) => {
      stops.push(recorder.stop());
    });
    this.recorders.clear();
    const meterStops = [];
    this.trackMeters.forEach((meter) => {
      if (meter && typeof meter.disconnect === 'function') {
        meterStops.push(Promise.resolve().then(() => meter.disconnect()));
      }
    });
    this.trackMeters.clear();
    this.trackStartTimes.clear();
    await Promise.allSettled(stops);
    await Promise.allSettled(meterStops);
    await this.packageAudioFiles({ markers });
    const packaged = this.files;
    this.startedAt = null;
    this.dispatchEvent(new CustomEvent('stop', { detail: { files: packaged } }));
    return packaged;
  }

  getFiles() {
    return this.files;
  }

  getTrackMeter(uuid, trackType = 'audio', channelIndex = 0) {
    if (!uuid) {
      return null;
    }
    const key = `${uuid}:${trackType}:${channelIndex}`;
    return this.trackMeters.get(key) || null;
  }

  async packageAudioFiles({ markers = null } = {}) {
    if (!this.files.size) {
      return this.files;
    }
    const conversions = [];
    this.files.forEach((meta, key) => {
      const fileMeta = meta;
      if (!fileMeta.filename) {
        fileMeta.filename = this.generateFilename(fileMeta);
      }
      if (fileMeta.trackType !== 'audio' || !fileMeta.blob) {
        // For non-audio tracks we still normalise mime type/filename
        fileMeta.mimeType = fileMeta.mimeType || fileMeta.originalMimeType || fileMeta.blob?.type || 'application/octet-stream';
        fileMeta.size = fileMeta.blob?.size || fileMeta.originalBlob?.size || 0;
        return;
      }
      conversions.push(
        (async () => {
          try {
            const trackStartOffsetSeconds = fileMeta.startOffsetSeconds || 0;
            const wavBlob = await convertBlobToWav(fileMeta.blob, {
              sampleRate: this.options.targetSampleRate,
              markers,
              trackStartOffsetSeconds,
              audioContext: this.options.audioContext,
            });
            fileMeta.originalBlob = fileMeta.originalBlob || fileMeta.blob;
            fileMeta.originalMimeType = fileMeta.originalMimeType || fileMeta.mimeType;
            fileMeta.blob = wavBlob;
            fileMeta.mimeType = 'audio/wav';
            fileMeta.filename = this.generateFilename(fileMeta, { extension: 'wav' });
            fileMeta.size = wavBlob.size;
          } catch (error) {
            console.warn('Failed to package track as WAV, falling back to original blob', error);
            const extension = extensionFromMime(fileMeta.mimeType || fileMeta.originalMimeType || fileMeta.blob?.type);
            fileMeta.filename = this.generateFilename(fileMeta, { extension });
            fileMeta.packagingError = error;
            fileMeta.size = fileMeta.blob?.size || fileMeta.originalBlob?.size || 0;
          }
        })(),
      );
    });
    await Promise.allSettled(conversions);
    return this.files;
  }

  generateFilename(meta, { extension } = {}) {
    const prefix = sanitizeSegment(this.options.filenamePrefix, 'podcast');
    const room = sanitizeSegment(this.session?.roomid || this.session?.roomID || 'room');
    const participantLabel = sanitizeSegment(meta.participant?.streamID || meta.participant?.label || meta.participant?.uuid, meta.participant?.uuid || 'participant');
    const trackKind = sanitizeSegment(meta.trackType || 'track');
    const channel = typeof meta.channelIndex === 'number' ? `c${meta.channelIndex + 1}` : 'c1';
    const timestamp = buildTimestamp(this.startedAt);
    const ext = extension || extensionFromMime(meta.mimeType || meta.originalMimeType || 'audio/wav');
    return `${prefix}-${room}-${participantLabel}-${trackKind}-${channel}-${timestamp}.${ext}`;
  }
}
