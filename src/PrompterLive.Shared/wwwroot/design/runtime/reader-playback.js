// Teleprompter playback runtime. This file owns card progression and timing only.

const ACTIVE_CLASS_NAME = 'active';
const READER_TIME_TOTAL_DEFAULT_MS = 147000;
const READER_WORD_DURATION_DEFAULT_MS = 600;
const READER_WORD_PAUSE_DEFAULT_MS = 0;
const READER_PLAY_BUTTON_ID = 'tp-play-btn';
const READER_PROGRESS_FILL_ID = 'rd-progress-fill';
const READER_TIME_SELECTOR = '.rd-time';
const READER_ACTIVE_WORD_SELECTOR = '.rd-card-active .rd-w.rd-now';
const READER_CARDS_SELECTOR = '.rd-cluster-wrap .rd-card';
const READER_STAGE_SELECTOR = '.rd-stage';
const READER_CLUSTER_TEXT_SELECTOR = '.rd-cluster-text';
const READER_WORD_SELECTOR = '.rd-w';
const READER_GROUP_SELECTOR = '.rd-g';
const READER_TOGGLE_ICON_SELECTOR = '[data-toggle-icon]';
const READER_PREVIOUS_CARD_CLASS = 'rd-card-prev';
const READER_NEXT_CARD_CLASS = 'rd-card-next';
const READER_ACTIVE_CARD_CLASS = 'rd-card-active';
const READER_ACTIVE_WORD_CLASS = 'rd-now';
const READER_READ_WORD_CLASS = 'rd-read';
const READER_ACTIVE_GROUP_CLASS = 'rd-g-active';
const READER_COUNTDOWN_ID = 'rd-countdown';
const READER_CARD_TRANSITION_DELAY_MS = 850;
const READER_COUNTDOWN_PRE_DELAY_MS = 600;
const READER_COUNTDOWN_STEP_MS = 700;
const READER_FIRST_WORD_DELAY_MS = 700;
const READER_DEFAULT_WORD_DELAY_MS = 600;
const READER_MIN_TOTAL_SECONDS = 1;

let tpPlaying = false;
let readerWordIndex = -1;
let readerCardIndex = 0;
let countdownActive = false;
let readerTimer = null;

function getReaderWordDuration(word) {
    const duration = Number.parseInt(word?.dataset?.ms || String(READER_WORD_DURATION_DEFAULT_MS), 10);
    return Number.isFinite(duration) && duration > 0 ? duration : READER_WORD_DURATION_DEFAULT_MS;
}

function getReaderWordPause(word) {
    const pause = Number.parseInt(word?.dataset?.pauseMs || String(READER_WORD_PAUSE_DEFAULT_MS), 10);
    return Number.isFinite(pause) && pause > 0 ? pause : READER_WORD_PAUSE_DEFAULT_MS;
}

function getReaderTotalMilliseconds() {
    const timeElement = document.querySelector(READER_TIME_SELECTOR);
    const totalMilliseconds = Number.parseInt(timeElement?.dataset?.totalMs || String(READER_TIME_TOTAL_DEFAULT_MS), 10);
    return Number.isFinite(totalMilliseconds) && totalMilliseconds > 0 ? totalMilliseconds : READER_TIME_TOTAL_DEFAULT_MS;
}

function setReaderPlayButtonState(isPlaying) {
    const button = document.getElementById(READER_PLAY_BUTTON_ID);
    if (!button) {
        return;
    }

    button.querySelectorAll(READER_TOGGLE_ICON_SELECTOR).forEach(icon => {
        icon.hidden = icon.getAttribute('data-toggle-icon') !== (isPlaying ? 'pause' : 'play');
    });
}

function toggleTp() {
    if (countdownActive) {
        return;
    }

    if (tpPlaying) {
        tpPlaying = false;
        clearTimeout(readerTimer);
        setReaderPlayButtonState(false);
    } else if (readerWordIndex > 0) {
        startPlaying();
    } else {
        startCountdown();
    }
}

function startCountdown() {
    countdownActive = true;

    const overlay = document.getElementById(READER_COUNTDOWN_ID);
    if (!overlay) {
        startPlaying();
        return;
    }

    overlay.textContent = '';
    overlay.classList.add(ACTIVE_CLASS_NAME);
    overlay.classList.remove('rd-pulse');

    setTimeout(() => {
        let count = 3;
        overlay.textContent = count;

        const tick = setInterval(() => {
            count--;
            if (count > 0) {
                overlay.textContent = count;
                return;
            }

            clearInterval(tick);
            overlay.textContent = '';
            overlay.classList.remove(ACTIVE_CLASS_NAME);
            countdownActive = false;

            setTimeout(() => {
                highlightFirstWord();
                startPlaying();
            }, READER_FIRST_WORD_DELAY_MS);
        }, READER_COUNTDOWN_STEP_MS);
    }, READER_COUNTDOWN_PRE_DELAY_MS);
}

function highlightFirstWord() {
    const cards = getReaderCards();
    const activeCard = cards[readerCardIndex];
    if (!activeCard) {
        return;
    }

    readerWordIndex = 0;
    setActiveReaderWord(activeCard, readerWordIndex);
    updateReaderProgress();
}

function startPlaying() {
    tpPlaying = true;
    countdownActive = false;
    setReaderPlayButtonState(true);
    scheduleNextWord();
}

function getReaderCards() {
    return document.querySelectorAll(READER_CARDS_SELECTOR);
}

function showCard(index) {
    const cards = getReaderCards();
    if (!cards.length) {
        return;
    }

    cards.forEach((card, cardIndex) => {
        card.classList.remove(READER_ACTIVE_CARD_CLASS, READER_PREVIOUS_CARD_CLASS, READER_NEXT_CARD_CLASS);
        if (cardIndex === index) {
            card.classList.add(READER_ACTIVE_CARD_CLASS);
        } else if (cardIndex === index - 1 || cardIndex < index) {
            card.classList.add(READER_PREVIOUS_CARD_CLASS);
        } else {
            card.classList.add(READER_NEXT_CARD_CLASS);
        }
    });

    goToSegment(index);
    updateBlockIndicator();
}

function formatSeconds(totalSeconds) {
    const safeSeconds = Math.max(0, totalSeconds | 0);
    const minutes = Math.floor(safeSeconds / 60);
    const seconds = String(safeSeconds % 60).padStart(2, '0');
    return `${minutes}:${seconds}`;
}

function getReaderTotalSeconds() {
    return Math.max(READER_MIN_TOTAL_SECONDS, Math.ceil(getReaderTotalMilliseconds() / 1000));
}

function getReaderElapsedMilliseconds() {
    const cards = getReaderCards();
    let elapsed = 0;

    cards.forEach((card, cardIndex) => {
        const words = Array.from(card.querySelectorAll(READER_WORD_SELECTOR));
        if (cardIndex < readerCardIndex) {
            words.forEach(word => {
                elapsed += getReaderWordDuration(word) + getReaderWordPause(word);
            });
            return;
        }

        if (cardIndex !== readerCardIndex || readerWordIndex <= 0) {
            return;
        }

        words.slice(0, readerWordIndex).forEach(word => {
            elapsed += getReaderWordDuration(word) + getReaderWordPause(word);
        });
    });

    return elapsed;
}

function updateReaderProgress() {
    const totalMilliseconds = getReaderTotalMilliseconds();
    const elapsedMilliseconds = Math.min(totalMilliseconds, getReaderElapsedMilliseconds());
    const progress = totalMilliseconds > 0 ? (elapsedMilliseconds / totalMilliseconds) * 100 : 0;

    const progressFill = document.getElementById(READER_PROGRESS_FILL_ID);
    if (progressFill) {
        progressFill.style.width = progress + '%';
    }

    const timeElement = document.querySelector(READER_TIME_SELECTOR);
    if (timeElement) {
        timeElement.textContent = `${formatSeconds(Math.floor(elapsedMilliseconds / 1000))} / ${formatSeconds(Math.ceil(totalMilliseconds / 1000))}`;
    }
}

function centerActiveLine(smooth) {
    const word = document.querySelector(READER_ACTIVE_WORD_SELECTOR);
    if (!word) {
        return;
    }

    const text = word.closest(READER_CLUSTER_TEXT_SELECTOR);
    const stage = document.querySelector(READER_STAGE_SELECTOR);
    if (!text || !stage) {
        return;
    }

    text.style.transition = 'none';
    const previousTransform = text.style.transform;
    text.style.transform = 'none';
    void text.offsetHeight;

    const stageRect = stage.getBoundingClientRect();
    const wordRect = word.getBoundingClientRect();
    const focalPoint = stageRect.top + stageRect.height * (rdFocalPct / 100);
    const wordCenter = wordRect.top + wordRect.height / 2;
    const translateY = focalPoint - wordCenter;

    if (smooth) {
        text.style.transform = previousTransform || 'none';
        void text.offsetHeight;
        text.style.transition = '';
        text.style.transform = `translateY(${translateY}px)`;
        return;
    }

    text.style.transform = `translateY(${translateY}px)`;
    void text.offsetHeight;
    requestAnimationFrame(() => {
        text.style.transition = '';
    });
}

function preCenterCard(card) {
    if (!card) {
        return;
    }

    const firstWord = card.querySelector(READER_WORD_SELECTOR);
    const text = card.querySelector(READER_CLUSTER_TEXT_SELECTOR);
    const stage = document.querySelector(READER_STAGE_SELECTOR);
    if (!firstWord || !text || !stage) {
        return;
    }

    const previousOpacity = card.style.opacity;
    const previousTransform = card.style.transform;
    card.style.opacity = '0';
    card.style.transition = 'none';
    card.style.transform = 'translateY(0)';
    text.style.transition = 'none';
    text.style.transform = 'none';
    firstWord.classList.add(READER_ACTIVE_WORD_CLASS);
    void text.offsetHeight;

    const stageRect = stage.getBoundingClientRect();
    const wordRect = firstWord.getBoundingClientRect();
    const focalPoint = stageRect.top + stageRect.height * (rdFocalPct / 100);
    const translateY = focalPoint - (wordRect.top + wordRect.height / 2);

    text.style.transform = `translateY(${translateY}px)`;
    card.style.transform = previousTransform;
    card.style.opacity = previousOpacity;
    void card.offsetHeight;
    card.style.transition = '';
    text.style.transition = '';
}

function setActiveReaderWord(card, activeIndex) {
    if (!card) {
        return null;
    }

    const words = card.querySelectorAll(READER_WORD_SELECTOR);
    if (!words.length || activeIndex < 0 || activeIndex >= words.length) {
        return null;
    }

    words.forEach((word, index) => {
        word.classList.remove(READER_ACTIVE_WORD_CLASS);
        if (index < activeIndex) {
            word.classList.add(READER_READ_WORD_CLASS);
            return;
        }

        word.classList.remove(READER_READ_WORD_CLASS);
        if (index === activeIndex) {
            word.classList.add(READER_ACTIVE_WORD_CLASS);
        }
    });

    card.querySelectorAll(READER_GROUP_SELECTOR).forEach(group => group.classList.remove(READER_ACTIVE_GROUP_CLASS));
    const currentWord = words[activeIndex];
    const currentGroup = currentWord.closest(READER_GROUP_SELECTOR);
    if (currentGroup) {
        currentGroup.classList.add(READER_ACTIVE_GROUP_CLASS);
    }

    return currentWord;
}

function pauseReaderPlaybackForTransition() {
    const shouldResume = tpPlaying;
    tpPlaying = false;
    clearTimeout(readerTimer);
    return shouldResume;
}

function resumeReaderPlaybackAfterTransition(shouldResume) {
    if (!shouldResume) {
        return;
    }

    tpPlaying = true;
    scheduleNextWord();
}

function advanceReaderWord() {
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

    readerWordIndex++;

    if (readerWordIndex >= words.length) {
        readerWordIndex = -1;
        readerCardIndex++;

        if (readerCardIndex >= cards.length) {
            readerCardIndex = 0;
            cards.forEach(card => resetReaderCardVisualState(card));
        }

        const nextCard = cards[readerCardIndex];
        if (nextCard) {
            preCenterCard(nextCard);
        }

        const shouldResume = pauseReaderPlaybackForTransition();
        showCard(readerCardIndex);

        setTimeout(() => {
            readerWordIndex = 0;
            setActiveReaderWord(cards[readerCardIndex], readerWordIndex);
            updateReaderProgress();
            resumeReaderPlaybackAfterTransition(shouldResume);
        }, READER_CARD_TRANSITION_DELAY_MS);
        return;
    }

    setActiveReaderWord(activeCard, readerWordIndex);
    centerActiveLine(true);
    updateReaderProgress();
}
