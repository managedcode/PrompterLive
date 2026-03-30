// Teleprompter controls runtime. This file owns visual controls, camera wiring, and segment metadata.

const ACTIVE_CLASS_NAME = 'active';
const READER_FONT_SIZE_LABEL_ID = 'rd-font-label';
const READER_FONT_SIZE_PROPERTY = '--rd-font-size';
const READER_CLUSTER_WRAP_SELECTOR = '.rd-cluster-wrap';

const READER_STAGE_SELECTOR = '.rd-stage';
const READER_FOCAL_LABEL_ID = 'rd-focal-val';
const READER_FOCAL_GUIDE_ID = 'rd-guide-h';
const READER_GUIDE_ACTIVE_DURATION_MS = 800;

const READER_WIDTH_LABEL_ID = 'rd-width-val';
const READER_GUIDE_LEFT_ID = 'rd-guide-v-l';
const READER_GUIDE_RIGHT_ID = 'rd-guide-v-r';

const READER_BLOCK_INDICATOR_ID = 'rd-block-indicator';
const READER_TEXT_RESET_VALUE = '0';
const READER_PERCENT_SUFFIX = '%';
const READER_PIXEL_SUFFIX = 'px';

let rdFocalPct = 30;
let focalGuideTimer = null;
let widthGuideTimer = null;

function resetReaderCardVisualState(card) {
    card.querySelectorAll(READER_WORD_SELECTOR).forEach(word => word.classList.remove(READER_READ_WORD_CLASS, READER_ACTIVE_WORD_CLASS));
    card.querySelectorAll(READER_GROUP_SELECTOR).forEach(group => group.classList.remove(READER_ACTIVE_GROUP_CLASS));

    const text = card.querySelector(READER_CLUSTER_TEXT_SELECTOR);
    if (text) {
        text.style.transform = '';
        text.dataset.ty = READER_TEXT_RESET_VALUE;
    }
}

function setReaderFontSize(value) {
    const nextFontSize = Number.parseInt(value, 10);
    if (!Number.isFinite(nextFontSize)) {
        return;
    }

    const wrap = document.querySelector(READER_CLUSTER_WRAP_SELECTOR);
    if (wrap) {
        wrap.style.setProperty(READER_FONT_SIZE_PROPERTY, `${nextFontSize}${READER_PIXEL_SUFFIX}`);
    }

    const label = document.getElementById(READER_FONT_SIZE_LABEL_ID);
    if (label) {
        label.textContent = String(nextFontSize);
    }
}

function setReaderFocalPoint(value) {
    const nextFocalPoint = Number.parseInt(value, 10);
    if (!Number.isFinite(nextFocalPoint)) {
        return;
    }

    rdFocalPct = nextFocalPoint;

    const label = document.getElementById(READER_FOCAL_LABEL_ID);
    if (label) {
        label.textContent = rdFocalPct + READER_PERCENT_SUFFIX;
    }

    const guide = document.getElementById(READER_FOCAL_GUIDE_ID);
    if (guide) {
        guide.style.top = rdFocalPct + READER_PERCENT_SUFFIX;
        guide.classList.add(ACTIVE_CLASS_NAME);
        clearTimeout(focalGuideTimer);
        focalGuideTimer = setTimeout(() => guide.classList.remove(ACTIVE_CLASS_NAME), READER_GUIDE_ACTIVE_DURATION_MS);
    }

    centerActiveLine(true);
}

function setReaderTextWidth(value) {
    const nextTextWidth = parseInt(value, 10);
    if (!Number.isFinite(nextTextWidth)) {
        return;
    }

    const wrap = document.querySelector(READER_CLUSTER_WRAP_SELECTOR);
    if (wrap) {
        wrap.style.maxWidth = nextTextWidth + READER_PIXEL_SUFFIX;
    }

    const label = document.getElementById(READER_WIDTH_LABEL_ID);
    if (label) {
        label.textContent = String(nextTextWidth);
    }

    const stage = document.querySelector(READER_STAGE_SELECTOR);
    const guideLeft = document.getElementById(READER_GUIDE_LEFT_ID);
    const guideRight = document.getElementById(READER_GUIDE_RIGHT_ID);
    if (stage && wrap && guideLeft && guideRight) {
        const stageRect = stage.getBoundingClientRect();
        const wrapRect = wrap.getBoundingClientRect();
        guideLeft.style.left = (wrapRect.left - stageRect.left) + READER_PIXEL_SUFFIX;
        guideRight.style.left = (wrapRect.right - stageRect.left) + READER_PIXEL_SUFFIX;
        guideLeft.classList.add(ACTIVE_CLASS_NAME);
        guideRight.classList.add(ACTIVE_CLASS_NAME);
        clearTimeout(widthGuideTimer);
        widthGuideTimer = setTimeout(() => {
            guideLeft.classList.remove(ACTIVE_CLASS_NAME);
            guideRight.classList.remove(ACTIVE_CLASS_NAME);
        }, READER_GUIDE_ACTIVE_DURATION_MS);
    }

    centerActiveLine(true);
}

function stepWord(direction) {
    const cards = getReaderCards();
    if (!cards.length) {
        return;
    }

    const activeCard = cards[readerCardIndex];
    if (!activeCard) {
        return;
    }

    const words = activeCard.querySelectorAll(READER_WORD_SELECTOR);
    if (!words.length) {
        return;
    }

    if (direction > 0) {
        advanceReaderWord();
        return;
    }

    if (readerWordIndex <= 0) {
        return;
    }

    const currentWord = words[readerWordIndex];
    if (currentWord) {
        currentWord.classList.remove(READER_ACTIVE_WORD_CLASS);
    }

    readerWordIndex--;
    words.forEach((word, index) => {
        word.classList.remove(READER_ACTIVE_WORD_CLASS, READER_READ_WORD_CLASS);
        if (index < readerWordIndex) {
            word.classList.add(READER_READ_WORD_CLASS);
        } else if (index === readerWordIndex) {
            word.classList.add(READER_ACTIVE_WORD_CLASS);
        }
    });

    activeCard.querySelectorAll(READER_GROUP_SELECTOR).forEach(group => group.classList.remove(READER_ACTIVE_GROUP_CLASS));
    const focusedWord = words[readerWordIndex];
    if (focusedWord) {
        const focusedGroup = focusedWord.closest(READER_GROUP_SELECTOR);
        if (focusedGroup) {
            focusedGroup.classList.add(READER_ACTIVE_GROUP_CLASS);
        }
    }

    centerActiveLine(true);
    updateReaderProgress();
}

function jumpCard(direction) {
    const cards = getReaderCards();

    if (direction < 0 && readerWordIndex > 1) {
        const currentCard = cards[readerCardIndex];
        if (currentCard) {
            resetReaderCardVisualState(currentCard);
            preCenterCard(currentCard);
            currentCard.querySelectorAll(READER_WORD_SELECTOR).forEach(word => word.classList.remove(READER_ACTIVE_WORD_CLASS));
            currentCard.querySelectorAll(READER_GROUP_SELECTOR).forEach(group => group.classList.remove(READER_ACTIVE_GROUP_CLASS));
        }

        readerWordIndex = 0;
        highlightFirstWord();
        return;
    }

    const newIndex = readerCardIndex + direction;
    if (newIndex < 0 || newIndex >= cards.length) {
        return;
    }

    const currentCard = cards[readerCardIndex];
    if (currentCard) {
        resetReaderCardVisualState(currentCard);
    }

    const targetCard = cards[newIndex];
    if (targetCard) {
        resetReaderCardVisualState(targetCard);
        preCenterCard(targetCard);
        targetCard.querySelectorAll(READER_WORD_SELECTOR).forEach(word => word.classList.remove(READER_ACTIVE_WORD_CLASS));
    }

    const shouldResume = pauseReaderPlaybackForTransition();
    readerCardIndex = newIndex;
    readerWordIndex = 0;
    showCard(readerCardIndex);
    updateReaderProgress();

    setTimeout(() => {
        setActiveReaderWord(cards[readerCardIndex], readerWordIndex);
        updateReaderProgress();
        resumeReaderPlaybackAfterTransition(shouldResume);
    }, READER_CARD_TRANSITION_DELAY_MS);
}

function updateBlockIndicator() {
    const indicator = document.getElementById(READER_BLOCK_INDICATOR_ID);
    const cards = getReaderCards();
    if (indicator) {
        indicator.textContent = (readerCardIndex + 1) + ' / ' + cards.length;
    }
}

function resetReader() {
    readerWordIndex = -1;
    readerCardIndex = 0;
    tpPlaying = false;
    countdownActive = false;
    clearTimeout(readerTimer);

    const cards = getReaderCards();
    cards.forEach(card => resetReaderCardVisualState(card));

    const firstCard = cards[0];
    if (firstCard) {
        preCenterCard(firstCard);
    }

    cards.forEach(card => {
        card.querySelectorAll(READER_WORD_SELECTOR).forEach(word => word.classList.remove(READER_ACTIVE_WORD_CLASS));
        card.querySelectorAll(READER_GROUP_SELECTOR).forEach(group => group.classList.remove(READER_ACTIVE_GROUP_CLASS));
    });

    showCard(0);
    setReaderPlayButtonState(false);

    const progressFill = document.getElementById(READER_PROGRESS_FILL_ID);
    if (progressFill) {
        progressFill.style.width = '0%';
    }

    const timeElement = document.querySelector(READER_TIME_SELECTOR);
    if (timeElement) {
        timeElement.textContent = `0:00 / ${formatSeconds(getReaderTotalSeconds())}`;
    }
}

function scheduleNextWord() {
    clearTimeout(readerTimer);
    if (!tpPlaying) {
        return;
    }

    const currentWord = document.querySelector(READER_ACTIVE_WORD_SELECTOR);
    const delay = currentWord
        ? getReaderWordDuration(currentWord) + getReaderWordPause(currentWord)
        : READER_DEFAULT_WORD_DELAY_MS;

    readerTimer = setTimeout(() => {
        if (tpPlaying && isTeleprompterRuntimeActive()) {
            advanceReaderWord();
        }

        if (tpPlaying) {
            scheduleNextWord();
        }
    }, Math.max(READER_MIN_WORD_DELAY_MS, delay));
}
