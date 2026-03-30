// Teleprompter camera and segment metadata runtime.

const ACTIVE_CLASS_NAME = 'active';
const DEFAULT_SEGMENT_BACKGROUND = 'warm';
const DEFAULT_SEGMENT_EMOTION = 'warm';
const DEFAULT_SEGMENT_NAME = 'Intro';
const READER_CAMERA_SELECTOR = '.rd-camera[data-camera-role]';
const READER_CAMERA_TINT_ID = 'rd-camera-tint';
const READER_CAMERA_BUTTON_ID = 'rd-cam-btn';
const READER_CAMERA_AUTOSTART_VALUE = 'true';
const READER_GRADIENT_ID = 'rd-gradient';
const READER_GRADIENT_CLASS_NAME = 'rd-gradient';
const READER_EDGE_SEGMENT_SELECTOR = '.rd-edge-segs > div';
const READER_SEGMENT_INDEX_OPACITY_ACTIVE = '1';
const READER_SEGMENT_INDEX_OPACITY_INACTIVE = '0.4';
const READER_NEUTRAL_SEGMENT = 'neutral';

let readerCameraRequestedState = null;
let readerCameraOperationId = 0;
let currentSegmentIndex = 0;

function getReaderCameraElements() {
    return Array.from(document.querySelectorAll(READER_CAMERA_SELECTOR));
}

function applyReaderCameraVisualState(cameras, tint, button, nextActive) {
    cameras.forEach(camera => camera.classList.toggle(ACTIVE_CLASS_NAME, nextActive));
    tint.classList.toggle(ACTIVE_CLASS_NAME, nextActive);
    button.classList.toggle(ACTIVE_CLASS_NAME, nextActive);
}

async function setReaderCameraActive(nextActive) {
    const cameras = getReaderCameraElements();
    const tint = document.getElementById(READER_CAMERA_TINT_ID);
    const button = document.getElementById(READER_CAMERA_BUTTON_ID);
    if (!cameras.length || !tint || !button) {
        return;
    }

    readerCameraRequestedState = !!nextActive;
    const operationId = ++readerCameraOperationId;
    const attachableCameras = cameras.filter(camera => (camera.dataset.cameraDeviceId || '').length > 0);
    const targetCameras = attachableCameras.length > 0 ? attachableCameras : cameras.slice(0, 1);

    applyReaderCameraVisualState(cameras, tint, button, nextActive);

    if (nextActive) {
        await Promise.all(targetCameras.map(async camera => {
            try {
                await window.PrompterLive.media.attachCamera(camera.id, camera.dataset.cameraDeviceId || '', true);
            } catch {
                // Keep the visual state even when the browser blocks access.
            }
        }));
    } else {
        await Promise.all(cameras.map(async camera => {
            try {
                await window.PrompterLive.media.detachCamera(camera.id);
            } catch {
            }
        }));
    }

    if (operationId !== readerCameraOperationId) {
        return;
    }

    applyReaderCameraVisualState(cameras, tint, button, nextActive);
}

async function toggleReaderCamera() {
    const cameras = getReaderCameraElements();
    if (!cameras.length) {
        return;
    }

    const isActive = cameras.some(camera => camera.classList.contains(ACTIVE_CLASS_NAME));
    await setReaderCameraActive(!isActive);
}

async function initializeReaderCamera() {
    const cameras = getReaderCameraElements();
    if (!cameras.length) {
        return;
    }

    const shouldAutoStart = cameras.some(camera => camera.dataset.cameraAutostart === READER_CAMERA_AUTOSTART_VALUE);
    const requestedState = readerCameraRequestedState ?? shouldAutoStart;
    await setReaderCameraActive(requestedState);
}

function getReaderSegmentMeta(index) {
    const cards = getReaderCards();
    const card = cards[index];
    if (!card) {
        return {
            name: DEFAULT_SEGMENT_NAME,
            emotion: DEFAULT_SEGMENT_EMOTION,
            bg: DEFAULT_SEGMENT_BACKGROUND
        };
    }

    return {
        name: card.dataset.segmentName || `Block ${index + 1}`,
        emotion: card.dataset.segmentEmotion || READER_NEUTRAL_SEGMENT,
        bg: card.dataset.segmentBg || READER_NEUTRAL_SEGMENT
    };
}

function goToSegment(index) {
    const cards = getReaderCards();
    if (!cards.length) {
        return;
    }

    if (index < 0) {
        index = 0;
    }

    if (index >= cards.length) {
        index = cards.length - 1;
    }

    currentSegmentIndex = index;
    const segment = getReaderSegmentMeta(index);

    const gradient = document.getElementById(READER_GRADIENT_ID);
    if (gradient) {
        gradient.className = READER_GRADIENT_CLASS_NAME;
        if (segment.bg) {
            gradient.classList.add(segment.bg);
        }
    }

    const segments = document.querySelectorAll(READER_EDGE_SEGMENT_SELECTOR);
    segments.forEach((segmentElement, segmentIndex) => {
        segmentElement.style.opacity = segmentIndex === index
            ? READER_SEGMENT_INDEX_OPACITY_ACTIVE
            : READER_SEGMENT_INDEX_OPACITY_INACTIVE;
    });

    window.PrompterLiveDesign?.reportReaderSegmentChanged(index, segment.name, segment.emotion);
}

function nextSegment() {
    goToSegment(currentSegmentIndex + 1);
}

function prevSegment() {
    goToSegment(currentSegmentIndex - 1);
}
