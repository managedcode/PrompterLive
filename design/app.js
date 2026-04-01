// ============================================
// NAVIGATION
// ============================================

// Page map for multi-file navigation
const _PAGES = {
    library: 'index.html',
    editor: 'editor.html',
    teleprompter: 'teleprompter.html',
    golive: 'golive.html',
    settings: 'settings.html',
    rsvp: 'rsvp.html'
};

function navigateTo(screenId) {
    if (_PAGES[screenId]) {
        window.location.href = _PAGES[screenId];
    }
}

function updateAppHeader(screenId) {
    const center = document.getElementById('app-header-center');
    const right = document.getElementById('app-header-right');
    if (!center || !right) return;

    const backBtn = `<button class="btn-back" onclick="navigateTo('library')"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15,18 9,12 15,6"/></svg></button>`;

    const goLiveBtn = `<button class="btn-golive-header" onclick="navigateTo('golive')">
        <span class="btn-golive-dot"></span>
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="23,7 16,12 23,17"/><rect x="1" y="5" width="15" height="14" rx="2" ry="2"/></svg>
        GO LIVE
    </button>`;

    const settingsBtn = `<button class="btn-ghost btn-settings-gear" onclick="navigateTo('settings')">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
    </button>`;

    switch (screenId) {
        case 'library':
            center.innerHTML = `
                <div class="lib-breadcrumb">
                    <span class="bc-item">All Scripts</span>
                    <span class="bc-sep">/</span>
                    <span class="bc-item bc-current">Presentations</span>
                </div>`;
            right.innerHTML = `
                <div class="lib-search">
                    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/></svg>
                    <input type="text" placeholder="Search..." />
                </div>
                ${goLiveBtn}
                <button class="btn-create" onclick="navigateTo('editor')">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
                    New Script
                </button>`;
            break;
        case 'editor':
            center.innerHTML = `${backBtn}<span class="top-bar-title">Product Launch</span>`;
            right.innerHTML = `
                <button class="btn-ghost" onclick="navigateTo('rsvp')">
                    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>
                    Learn
                </button>
                <button class="btn-gold" onclick="navigateTo('teleprompter')">
                    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5,3 19,12 5,21"/></svg>
                    Read
                </button>
                ${goLiveBtn}`;
            break;
        case 'settings':
            center.innerHTML = `${backBtn}<span class="top-bar-title">Settings</span>`;
            right.innerHTML = `${goLiveBtn}`;
            break;
        case 'rsvp':
            center.innerHTML = `${backBtn}<span class="top-bar-title">Product Launch</span><span style="color:var(--text-4);font-size:12px">Intro / Opening Block</span>`;
            right.innerHTML = `<span class="top-bar-title" style="font-size:13px">300 WPM</span>${goLiveBtn}`;
            break;
        case 'teleprompter':
            center.innerHTML = `${backBtn}<span class="top-bar-title">Product Launch</span><span style="color:var(--text-4);font-size:12px" id="rd-header-segment">Intro · Opening Block</span>`;
            right.innerHTML = `${goLiveBtn}`;
            break;
        case 'golive':
            center.innerHTML = '';
            right.innerHTML = '';
            break;
        default:
            center.innerHTML = '';
            right.innerHTML = '';
    }
}

// ============================================
// RSVP (Learn mode — simple word-by-word)
// ============================================

let rsvpSpeed = 300;
let rsvpPlaying = true;

function changeRsvpSpeed(delta) {
    rsvpSpeed = Math.max(100, Math.min(600, rsvpSpeed + delta));
    const el = document.getElementById('rsvp-speed');
    if (el) el.textContent = rsvpSpeed;
    const badge = document.getElementById('rsvp-wpm-badge');
    if (badge) badge.textContent = rsvpSpeed + ' WPM';
}

function toggleRsvp() {
    rsvpPlaying = !rsvpPlaying;
    const btn = document.getElementById('rsvp-play-btn');
    if (rsvpPlaying) {
        btn.innerHTML = '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="6" y="4" width="4" height="16"/><rect x="14" y="4" width="4" height="16"/></svg>';
    } else {
        btn.innerHTML = '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5,3 19,12 5,21"/></svg>';
    }
}

const rsvpWords = [
    'Good', 'morning', 'everyone,', 'and', 'welcome', 'to', 'what', 'I', 'believe',
    'will', 'be', 'a', 'transformative', 'moment', 'for', 'our', 'company.',
    'Today,', "we're", 'not', 'just', 'launching', 'a', 'product', '—',
    "we're", 'introducing', 'a', 'solution', 'that', 'will', 'revolutionize',
    'how', 'our', 'customers', 'interact', 'with', 'technology.'
];

let rsvpIndex = 12;

function getORP(word) {
    const len = word.replace(/[^a-zA-Z]/g, '').length;
    if (len <= 1) return 0;
    if (len <= 5) return 1;
    if (len <= 9) return 2;
    return 3;
}

function renderRsvpWord() {
    const word = rsvpWords[rsvpIndex];
    const orp = getORP(word);
    const container = document.getElementById('rsvp-word');
    if (!container) return;

    // Render focus word with ORP
    let html = '';
    for (let i = 0; i < word.length; i++) {
        html += `<span${i === orp ? ' class="orp"' : ''}>${word[i]}</span>`;
    }
    container.innerHTML = html;

    // Position the word so the ORP letter aligns with the center vertical line
    const orpEl = container.querySelector('.orp');
    if (orpEl) {
        const containerRect = container.parentElement.getBoundingClientRect();
        const orpRect = orpEl.getBoundingClientRect();
        const containerCenter = containerRect.left + containerRect.width / 2;
        const orpCenter = orpRect.left + orpRect.width / 2;
        const shift = containerCenter - orpCenter;
        container.style.transform = `translateX(${shift}px)`;
    }

    // Left context (5 words)
    const leftEl = document.getElementById('rsvp-ctx-l');
    if (leftEl) {
        const leftWords = rsvpWords.slice(Math.max(0, rsvpIndex - 5), rsvpIndex);
        leftEl.innerHTML = leftWords.map(w => `<span>${w}</span>`).join('');
    }

    // Right context (5 words)
    const rightEl = document.getElementById('rsvp-ctx-r');
    if (rightEl) {
        const rightWords = rsvpWords.slice(rsvpIndex + 1, rsvpIndex + 6);
        rightEl.innerHTML = rightWords.map(w => `<span>${w}</span>`).join('');
    }

    const fill = document.querySelector('.rsvp-progress-fill');
    if (fill) fill.style.width = ((rsvpIndex + 1) / rsvpWords.length * 100) + '%';
}

// Step RSVP by N words (negative = back)
function stepRsvpWord(n) {
    rsvpIndex = (rsvpIndex + n + rsvpWords.length) % rsvpWords.length;
    renderRsvpWord();
}

setInterval(() => {
    if (rsvpPlaying && document.getElementById('screen-rsvp')?.classList.contains('active')) {
        rsvpIndex = (rsvpIndex + 1) % rsvpWords.length;
        renderRsvpWord();
    }
}, 800);

// ============================================
// READER — card-by-card with word highlighting
// ============================================

let tpSpeed = 140;
let tpPlaying = false; // Start paused — user presses play
let readerWordIndex = -1;
let readerCardIndex = 0;
let countdownActive = false;

function changeTpSpeed(delta) {
    tpSpeed = Math.max(80, Math.min(220, tpSpeed + delta));
    const el = document.getElementById('tp-speed');
    if (el) el.textContent = tpSpeed;
}

function toggleTp() {
    if (countdownActive) return; // Ignore during countdown

    if (tpPlaying) {
        // Pause — keep current word highlighted, just stop advancing
        tpPlaying = false;
        clearTimeout(readerTimer);
        const btn = document.getElementById('tp-play-btn');
        if (btn) btn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5,3 19,12 5,21"/></svg>';
    } else if (readerWordIndex > 0) {
        // Resume from pause — no countdown, just continue
        startPlaying();
    } else {
        // First start — full countdown 3-2-1
        startCountdown();
    }
}

function startCountdown() {
    countdownActive = true;
    const overlay = document.getElementById('rd-countdown');
    if (!overlay) return startPlaying();

    // Show overlay with dark backdrop first (pre-pause)
    overlay.textContent = '';
    overlay.classList.add('active');
    overlay.classList.remove('rd-pulse');

    // Flow: 3 → 2 → 1 → overlay gone → empty beat (no highlight) → first word lights up
    setTimeout(() => {
        let count = 3;
        overlay.textContent = count;

        const tick = setInterval(() => {
            count--;
            if (count > 0) {
                overlay.textContent = count;
            } else {
                // "1" shown → clear overlay instantly
                clearInterval(tick);
                overlay.textContent = '';
                overlay.classList.remove('active');
                countdownActive = false;
                // Empty beat — text visible but NO word highlighted
                setTimeout(() => {
                    // First word lights up → startPlaying schedules
                    // the next word after a full 600ms interval
                    highlightFirstWord();
                    startPlaying();
                }, 700);
            }
        }, 700);
    }, 600);
}

// Light up the first word — visual cue that reading begins.
// Don't re-center: preCenterCard already positioned text for word 0.
function highlightFirstWord() {
    const cards = getReaderCards();
    const card = cards[readerCardIndex];
    if (!card) return;
    const words = card.querySelectorAll('.rd-w');
    if (!words.length) return;
    readerWordIndex = 0;
    words[0].classList.add('rd-now');
    const g = words[0].closest('.rd-g');
    if (g) g.classList.add('rd-g-active');
}

function startPlaying() {
    tpPlaying = true;
    countdownActive = false;
    const btn = document.getElementById('tp-play-btn');
    if (btn) btn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="6" y="4" width="4" height="16"/><rect x="14" y="4" width="4" height="16"/></svg>';
    scheduleNextWord();
}

function getReaderCards() {
    return document.querySelectorAll('.rd-cluster-wrap .rd-card');
}

function showCard(index) {
    const cards = getReaderCards();
    if (!cards.length) return;

    cards.forEach((card, i) => {
        card.classList.remove('rd-card-active', 'rd-card-prev', 'rd-card-next');
        if (i === index) {
            card.classList.add('rd-card-active');
        } else if (i === index - 1) {
            card.classList.add('rd-card-prev');
        } else if (i === index + 1) {
            card.classList.add('rd-card-next');
        } else if (i < index) {
            card.classList.add('rd-card-prev');
        } else {
            card.classList.add('rd-card-next');
        }
    });

    // Update segment info and block indicator
    goToSegment(index);
    updateBlockIndicator();
}

// Center the active word's line at screen center
// Center the active word at the focal point (30% from top)
// smooth=true uses CSS transition, smooth=false is instant (no flash)
function centerActiveLine(smooth) {
    const word = document.querySelector('.rd-card-active .rd-w.rd-now');
    if (!word) return;
    const text = word.closest('.rd-cluster-text');
    if (!text) return;
    const stage = document.querySelector('.rd-stage');
    if (!stage) return;

    // Disable transition to measure natural position without flash
    text.style.transition = 'none';
    const prevTy = text.style.transform;
    text.style.transform = 'none';
    void text.offsetHeight;

    const stageRect = stage.getBoundingClientRect();
    const wordRect = word.getBoundingClientRect();
    const focalPoint = stageRect.top + stageRect.height * (rdFocalPct / 100);
    const wordCenter = wordRect.top + wordRect.height / 2;
    const ty = focalPoint - wordCenter;

    if (smooth) {
        // Restore previous position instantly, then animate to new
        text.style.transform = prevTy || 'none';
        void text.offsetHeight;
        text.style.transition = '';
        text.style.transform = `translateY(${ty}px)`;
    } else {
        // Set instantly (for new card entry — no jump)
        text.style.transform = `translateY(${ty}px)`;
        void text.offsetHeight;
        requestAnimationFrame(() => { text.style.transition = ''; });
    }
}

// Pre-position text on a card BEFORE it becomes visible
// so it slides in already centered — no jump after transition
function preCenterCard(card) {
    if (!card) return;
    const firstWord = card.querySelector('.rd-w');
    if (!firstWord) return;
    const text = card.querySelector('.rd-cluster-text');
    if (!text) return;
    const stage = document.querySelector('.rd-stage');
    if (!stage) return;

    // Temporarily make card visible but transparent to measure
    const prevOpacity = card.style.opacity;
    const prevTransform = card.style.transform;
    card.style.opacity = '0';
    card.style.transition = 'none';
    card.style.transform = 'translateY(0)';
    text.style.transition = 'none';
    text.style.transform = 'none';
    firstWord.classList.add('rd-now');
    void text.offsetHeight;

    const stageRect = stage.getBoundingClientRect();
    const wordRect = firstWord.getBoundingClientRect();
    const focalPoint = stageRect.top + stageRect.height * (rdFocalPct / 100);
    const ty = focalPoint - (wordRect.top + wordRect.height / 2);

    // Set text position and restore card to off-screen
    text.style.transform = `translateY(${ty}px)`;
    card.style.transform = prevTransform;
    card.style.opacity = prevOpacity;
    void card.offsetHeight;
    card.style.transition = '';
    text.style.transition = '';
}

function advanceReaderWord() {
    const cards = getReaderCards();
    if (!cards.length) return;

    const activeCard = cards[readerCardIndex];
    if (!activeCard) return;

    const words = activeCard.querySelectorAll('.rd-w');
    if (!words.length) return;

    readerWordIndex++;

    // If we've gone past all words in this card, move to next card
    if (readerWordIndex >= words.length) {
        readerWordIndex = -1;
        readerCardIndex++;
        if (readerCardIndex >= cards.length) {
            readerCardIndex = 0;
            cards.forEach(c => {
                c.querySelectorAll('.rd-w').forEach(w => w.classList.remove('rd-read', 'rd-now'));
                c.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
                const t = c.querySelector('.rd-cluster-text');
                if (t) { t.style.transform = ''; t.dataset.ty = '0'; }
            });
        }
        // Pre-center next card BEFORE it slides in — no jump
        const nextCard = cards[readerCardIndex];
        if (nextCard) preCenterCard(nextCard);

        const wasPaused = !tpPlaying;
        tpPlaying = false;
        showCard(readerCardIndex);

        // Wait for slide transition, then resume
        setTimeout(() => {
            readerWordIndex = 0;
            if (!wasPaused) tpPlaying = true;
        }, 850);
        return;
    }

    words.forEach((w, i) => {
        w.classList.remove('rd-now');
        if (i < readerWordIndex) {
            w.classList.add('rd-read');
        } else if (i === readerWordIndex) {
            w.classList.add('rd-now');
            w.classList.remove('rd-read');
        } else {
            w.classList.remove('rd-read');
        }
    });

    // Phrase-group highlighting: elevate the entire thought-group
    // containing the current word so peripheral vision can preview
    // upcoming words within the same semantic unit
    activeCard.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
    const currentWord = words[readerWordIndex];
    if (currentWord) {
        const parentGroup = currentWord.closest('.rd-g');
        if (parentGroup) parentGroup.classList.add('rd-g-active');
    }

    // Center the active line on screen (smooth scroll)
    centerActiveLine(true);

    // Progress (global across all cards)
    let totalWords = 0, totalRead = 0;
    cards.forEach((c, ci) => {
        const cw = c.querySelectorAll('.rd-w').length;
        totalWords += cw;
        if (ci < readerCardIndex) totalRead += cw;
        else if (ci === readerCardIndex) totalRead += readerWordIndex;
    });
    const progress = totalWords ? (totalRead / totalWords * 100) : 0;
    const fill = document.getElementById('rd-progress-fill');
    if (fill) fill.style.width = progress + '%';
    const timeEl = document.querySelector('.rd-time');
    if (timeEl) {
        const sec = Math.round((progress / 100) * 147);
        timeEl.textContent = `${Math.floor(sec/60)}:${String(sec%60).padStart(2,'0')} / 2:27`;
    }
}

// Font size control
let rdFontSize = 36;
function changeFontSize(delta) {
    rdFontSize = Math.max(24, Math.min(56, rdFontSize + delta));
    const wrap = document.querySelector('.rd-cluster-wrap');
    if (wrap) wrap.style.setProperty('--rd-font-size', rdFontSize + 'px');
    const label = document.getElementById('rd-font-label');
    if (label) label.textContent = rdFontSize;
}

// Focal point control (vertical position of reading line)
let rdFocalPct = 30;
let focalGuideTimer = null;
function changeFocalPoint(val) {
    rdFocalPct = parseInt(val);
    const label = document.getElementById('rd-focal-val');
    if (label) label.textContent = rdFocalPct + '%';
    // Show horizontal guide line at the focal point
    const guide = document.getElementById('rd-guide-h');
    if (guide) {
        guide.style.top = rdFocalPct + '%';
        guide.classList.add('active');
        clearTimeout(focalGuideTimer);
        focalGuideTimer = setTimeout(() => guide.classList.remove('active'), 800);
    }
    // Re-center the active line at the new focal point
    centerActiveLine(true);
}

// Text width control
let rdTextWidth = 750;
let widthGuideTimer = null;
function changeTextWidth(val) {
    rdTextWidth = parseInt(val);
    const wrap = document.querySelector('.rd-cluster-wrap');
    if (wrap) wrap.style.maxWidth = rdTextWidth + 'px';
    const label = document.getElementById('rd-width-val');
    if (label) label.textContent = rdTextWidth;
    // Show vertical guide lines at text edges
    const stage = document.querySelector('.rd-stage');
    const guideL = document.getElementById('rd-guide-v-l');
    const guideR = document.getElementById('rd-guide-v-r');
    if (stage && wrap && guideL && guideR) {
        const stageRect = stage.getBoundingClientRect();
        const wrapRect = wrap.getBoundingClientRect();
        guideL.style.left = (wrapRect.left - stageRect.left) + 'px';
        guideR.style.left = (wrapRect.right - stageRect.left) + 'px';
        guideL.classList.add('active');
        guideR.classList.add('active');
        clearTimeout(widthGuideTimer);
        widthGuideTimer = setTimeout(() => {
            guideL.classList.remove('active');
            guideR.classList.remove('active');
        }, 800);
    }
    // Re-center after width change (line positions may shift)
    centerActiveLine(true);
}

// Step forward/back by one word (manual word navigation)
function stepWord(direction) {
    const cards = getReaderCards();
    if (!cards.length) return;
    const activeCard = cards[readerCardIndex];
    if (!activeCard) return;
    const words = activeCard.querySelectorAll('.rd-w');
    if (!words.length) return;

    if (direction > 0) {
        // Forward — same as advanceReaderWord but manual
        advanceReaderWord();
    } else {
        // Back — go to previous word
        if (readerWordIndex > 0) {
            // Un-read current word
            const cur = words[readerWordIndex];
            if (cur) cur.classList.remove('rd-now');
            readerWordIndex--;
            // Update states
            words.forEach((w, i) => {
                w.classList.remove('rd-now', 'rd-read');
                if (i < readerWordIndex) w.classList.add('rd-read');
                else if (i === readerWordIndex) w.classList.add('rd-now');
            });
            // Update phrase group
            activeCard.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
            const cw = words[readerWordIndex];
            if (cw) {
                const pg = cw.closest('.rd-g');
                if (pg) pg.classList.add('rd-g-active');
            }
            centerActiveLine(true);
        }
    }
}

// Jump to next/prev card
function jumpCard(direction) {
    const cards = getReaderCards();

    // ── Back button: first press → start of current block,
    //    second press (if already at start) → previous block.
    //    Like a music player: tap back = restart song, tap again = prev song.
    if (direction < 0) {
        if (readerWordIndex > 1) {
            // Not at start yet — rewind to beginning of current block
            const card = cards[readerCardIndex];
            if (card) {
                card.querySelectorAll('.rd-w').forEach(w => w.classList.remove('rd-read', 'rd-now'));
                card.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
                const t = card.querySelector('.rd-cluster-text');
                if (t) { t.style.transform = ''; t.dataset.ty = '0'; }
                preCenterCard(card);
                // Remove rd-now from preCenterCard measurement
                card.querySelectorAll('.rd-w').forEach(w => w.classList.remove('rd-now'));
                card.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
            }
            readerWordIndex = 0;
            highlightFirstWord();
            return;
        }
        // Already at start — fall through to jump to previous block
    }

    const newIndex = readerCardIndex + direction;
    if (newIndex < 0 || newIndex >= cards.length) return;

    // Reset words and text position in current card
    const currentCard = cards[readerCardIndex];
    if (currentCard) {
        currentCard.querySelectorAll('.rd-w').forEach(w => w.classList.remove('rd-read', 'rd-now'));
        currentCard.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
        const t = currentCard.querySelector('.rd-cluster-text');
        if (t) { t.style.transform = ''; t.dataset.ty = '0'; }
    }
    // Pre-center target card before it slides in
    const targetCard = cards[newIndex];
    if (targetCard) {
        targetCard.querySelectorAll('.rd-w').forEach(w => w.classList.remove('rd-read', 'rd-now'));
        targetCard.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
        const t = targetCard.querySelector('.rd-cluster-text');
        if (t) { t.style.transform = ''; t.dataset.ty = '0'; }
        preCenterCard(targetCard);
        // Remove rd-now from preCenterCard measurement
        targetCard.querySelectorAll('.rd-w').forEach(w => w.classList.remove('rd-now'));
    }

    const wasPaused = !tpPlaying;
    tpPlaying = false;
    readerCardIndex = newIndex;
    readerWordIndex = 0;
    showCard(readerCardIndex);

    // Wait for slide transition, then resume
    setTimeout(() => {
        if (!wasPaused) tpPlaying = true;
    }, 850);
}

// Update block indicator
function updateBlockIndicator() {
    const el = document.getElementById('rd-block-indicator');
    const cards = getReaderCards();
    if (el) el.textContent = (readerCardIndex + 1) + ' / ' + cards.length;
}

function resetReader() {
    readerWordIndex = -1;
    readerCardIndex = 0;
    tpPlaying = false; // Start paused
    countdownActive = false;
    const cards = getReaderCards();
    cards.forEach(c => {
        c.querySelectorAll('.rd-w').forEach(w => w.classList.remove('rd-read', 'rd-now'));
        c.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
        const t = c.querySelector('.rd-cluster-text');
        if (t) { t.style.transform = ''; t.dataset.ty = '0'; }
    });
    // Pre-center first card, then show it — no jump
    const firstCard = cards[0];
    if (firstCard) preCenterCard(firstCard);
    // Remove rd-now that preCenterCard added for measurement —
    // no word should be bright until countdown finishes
    cards.forEach(c => {
        c.querySelectorAll('.rd-w').forEach(w => w.classList.remove('rd-now'));
        c.querySelectorAll('.rd-g').forEach(g => g.classList.remove('rd-g-active'));
    });
    showCard(0);

    // Show play button (paused state)
    const btn = document.getElementById('tp-play-btn');
    if (btn) btn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5,3 19,12 5,21"/></svg>';
    // Reset progress
    const fill = document.getElementById('rd-progress-fill');
    if (fill) fill.style.width = '0%';
    const timeEl = document.querySelector('.rd-time');
    if (timeEl) timeEl.textContent = '0:00 / 2:27';
}

// Auto-advance reader — timeout chain guarantees each word gets full duration.
// setInterval would fire on a fixed clock, cutting words short on resume.
let readerTimer = null;
function scheduleNextWord() {
    clearTimeout(readerTimer);
    readerTimer = setTimeout(() => {
        if (tpPlaying && document.getElementById('screen-teleprompter')?.classList.contains('active')) {
            advanceReaderWord();
        }
        if (tpPlaying) scheduleNextWord();
    }, 600);
}

// Camera toggle
function toggleReaderCamera() {
    const cam = document.getElementById('rd-camera');
    const tint = document.getElementById('rd-camera-tint');
    const btn = document.getElementById('rd-cam-btn');
    cam.classList.toggle('active');
    tint.classList.toggle('active');
    btn.classList.toggle('active');
}

// Focus mode toggle
function toggleFocusMode() {
    const btn = document.getElementById('rd-focus-btn');
    btn.classList.toggle('active');
    // Could hide header/footer for immersive mode
}

// ============================================
// KEYBOARD SHORTCUTS
// ============================================

// ── Segment navigation ──
// Demo segments for prototype — in production these come from the TPS parser
const demoSegments = [
    { name: 'Intro', emotion: 'warm', wpm: 140, color: '#F59E0B', bg: 'warm' },
    { name: 'Problem', emotion: 'concerned', wpm: 150, color: '#EF4444', bg: 'concerned' },
    { name: 'Solution', emotion: 'focused', wpm: 160, color: '#10B981', bg: 'focused' },
];
let currentSegmentIndex = 0;

function goToSegment(index) {
    if (index < 0) index = 0;
    if (index >= demoSegments.length) index = demoSegments.length - 1;
    currentSegmentIndex = index;

    const seg = demoSegments[index];

    // Update gradient background with smooth transition
    const gradient = document.getElementById('rd-gradient');
    if (gradient) {
        gradient.className = 'rd-gradient';
        if (seg.bg) gradient.classList.add(seg.bg);
    }

    // Update edge section label
    const sectionEl = document.querySelector('.rd-edge-section');
    if (sectionEl) sectionEl.textContent = seg.name;

    // Update header subtitle
    const headerSeg = document.getElementById('rd-header-segment');
    if (headerSeg) headerSeg.textContent = seg.name + ' · ' + seg.emotion.charAt(0).toUpperCase() + seg.emotion.slice(1);

    // Update segment indicator colors
    const segs = document.querySelectorAll('.rd-edge-segs > div');
    segs.forEach((s, i) => {
        s.style.opacity = i === index ? '1' : '0.4';
    });
}

function nextSegment() { goToSegment(currentSegmentIndex + 1); }
function prevSegment() { goToSegment(currentSegmentIndex - 1); }

// ── Keyboard shortcuts ──
document.addEventListener('keydown', (e) => {
    const isTp = document.getElementById('screen-teleprompter')?.classList.contains('active');
    const isRsvp = document.getElementById('screen-rsvp')?.classList.contains('active');

    if (e.key === 'Escape') {
        const active = document.querySelector('.screen.active');
        if (active && active.id !== 'screen-library') {
            if (isTp || isRsvp) {
                navigateTo('editor');
            } else {
                navigateTo('library');
            }
        }
        return;
    }

    // Only process shortcuts when in teleprompter or RSVP mode
    if (!isTp && !isRsvp) return;
    if (e.target.matches('input, [contenteditable]')) return;

    switch (e.key) {
        // ── Play / Pause ──
        case ' ':
            e.preventDefault();
            if (isRsvp) toggleRsvp();
            if (isTp) toggleTp();
            break;

        // ── Segment navigation ──
        case 'ArrowRight':
        case 'PageDown':
            e.preventDefault();
            if (isTp) nextSegment();
            break;
        case 'ArrowLeft':
        case 'PageUp':
            e.preventDefault();
            if (isTp) prevSegment();
            break;

        // ── Speed ──
        case 'ArrowUp':
            e.preventDefault();
            if (isTp) changeTpSpeed(10);
            if (isRsvp) changeRsvpSpeed(10);
            break;
        case 'ArrowDown':
            e.preventDefault();
            if (isTp) changeTpSpeed(-10);
            if (isRsvp) changeRsvpSpeed(-10);
            break;

        // ── Camera toggle ──
        case 'c':
        case 'C':
            if (isTp) toggleReaderCamera();
            break;

        // ── Focus mode ──
        case 'f':
        case 'F':
            if (isTp) toggleFocusMode();
            break;
    }
});

// ============================================
// SETTINGS — section switching
// ============================================

function showSetSection(id) {
    document.querySelectorAll('.set-panel').forEach(p => p.style.display = 'none');
    const target = document.getElementById('set-' + id);
    if (target) target.style.display = '';

    document.querySelectorAll('.set-nav-item').forEach(n => n.classList.remove('active'));
    event.currentTarget.classList.add('active');
}

// ============================================
// SETTINGS — Save / Discard banner
// ============================================

let _settingsDirty = false;

function markSettingsDirty() {
    if (_settingsDirty) return;
    _settingsDirty = true;
    const banner = document.getElementById('set-save-banner');
    if (banner) banner.classList.add('visible');
}

function saveSettings() {
    _settingsDirty = false;
    const banner = document.getElementById('set-save-banner');
    if (banner) banner.classList.remove('visible');
    // Flash "Saved" feedback on banner before hiding
    const textEl = banner && banner.querySelector('.set-save-banner-text');
    if (textEl) {
        textEl.textContent = 'Changes saved';
        setTimeout(() => { textEl.textContent = 'Unsaved changes'; }, 1500);
    }
}

function discardSettings() {
    _settingsDirty = false;
    const banner = document.getElementById('set-save-banner');
    if (banner) banner.classList.remove('visible');
}

// ============================================
// EDITOR — Structure navigation
// ============================================

function edNavTo(el) {
    const nav = el.dataset.nav;
    if (!nav) return;
    const parts = nav.split('-');
    const segs = document.querySelectorAll('.ed-content .ed-seg');
    let target = null;

    if (parts[0] === 'seg') {
        const segIdx = parseInt(parts[1]);
        target = segs[segIdx];
    } else if (parts[0] === 'blk') {
        const segIdx = parseInt(parts[1]);
        const blkIdx = parseInt(parts[2]);
        if (segs[segIdx]) {
            const blocks = segs[segIdx].querySelectorAll('.ed-blk');
            target = blocks[blkIdx];
        }
    }

    if (target) {
        const container = document.querySelector('.ed-content');
        if (container) {
            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
        // Flash highlight
        target.classList.add('ed-nav-flash');
        setTimeout(() => target.classList.remove('ed-nav-flash'), 1200);
    }

    // Update active state in tree
    document.querySelectorAll('.ed-tree-seg').forEach(s => s.classList.remove('active'));
    document.querySelectorAll('.ed-tree-block').forEach(b => b.classList.remove('active'));
    el.classList.add('active');
    // If clicked on a block, also activate its parent segment
    if (parts[0] === 'blk') {
        const parentSeg = document.querySelector(`[data-nav="seg-${parts[1]}"]`);
        if (parentSeg) parentSeg.classList.add('active');
    }
}

// ============================================
// GO-LIVE (Director Studio)
// ============================================

let glRecording = false;
let glStreaming = false;
let glStudioMode = 'director'; // 'director' or 'studio'

// ── Studio Mode (Director / Studio) ──
function setStudioMode(mode) {
    glStudioMode = mode;

    // Update buttons
    document.querySelectorAll('.gl-mode-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    const activeBtn = document.querySelector(`.gl-mode-${mode}`);
    if (activeBtn) activeBtn.classList.add('active');

    // Show/hide director controls (only in director mode - room management)
    const roomSection = document.querySelector('.gl-room-section');
    if (roomSection) {
        roomSection.style.display = mode === 'director' ? '' : 'none';
    }

    // In studio mode, simplify camera list
    const camCards = document.querySelectorAll('.gl-cam-card:not(.gl-src-screen)');
    camCards.forEach((card, i) => {
        if (mode === 'studio') {
            // In studio mode, only show first camera (your camera) + screen share always visible
            card.style.display = i === 0 ? '' : 'none';
        } else {
            card.style.display = '';
        }
    });

    // Update sources header
    const sourcesHeader = document.querySelector('.gl-sources-header .section-label');
    if (sourcesHeader) {
        sourcesHeader.textContent = mode === 'director' ? 'CAMERAS' : 'SOURCES';
    }
}

// ── Room Code Management ──
const roomJoinPathPrefix = '/join/';

function buildRoomInviteUrl(code) {
    return new URL(`${roomJoinPathPrefix}${code}`, window.location.origin).toString();
}

function generateRoomCode() {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    let code = '';
    for (let i = 0; i < 3; i++) code += chars[Math.floor(Math.random() * chars.length)];
    code += '-';
    for (let i = 0; i < 3; i++) code += chars[Math.floor(Math.random() * chars.length)];
    return code;
}

function copyRoomCode() {
    const code = document.getElementById('gl-room-code')?.textContent;
    if (code) {
        navigator.clipboard.writeText(buildRoomInviteUrl(code)).then(() => {
            const btn = document.querySelector('.gl-room-copy-btn');
            if (btn) {
                btn.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>';
                setTimeout(() => {
                    btn.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></svg>';
                }, 2000);
            }
        });
    }
}

function createRoom() {
    // Generate room code
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    let code = '';
    for (let i = 0; i < 3; i++) code += chars[Math.floor(Math.random() * chars.length)];
    code += '-';
    for (let i = 0; i < 3; i++) code += chars[Math.floor(Math.random() * chars.length)];

    const codeEl = document.getElementById('gl-room-code');
    if (codeEl) codeEl.textContent = code;

    // Toggle states
    document.getElementById('gl-room-empty').style.display = 'none';
    document.getElementById('gl-room-active').style.display = 'flex';
}

function endRoom() {
    document.getElementById('gl-room-active').style.display = 'none';
    document.getElementById('gl-room-empty').style.display = 'flex';
}

function copyRoomInvite() {
    const code = document.getElementById('gl-room-code')?.textContent;
    if (code) {
        navigator.clipboard.writeText(buildRoomInviteUrl(code)).catch(() => {});
        const btn = document.querySelector('.gl-room-invite-btn');
        if (btn) {
            const orig = btn.innerHTML;
            btn.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg> Link Copied!';
            setTimeout(() => { btn.innerHTML = orig; }, 2000);
        }
    }
}

function togglePartMute(row) {
    const btn = row.querySelector ? row.querySelector('.gl-part-mute') : row;
    if (btn) btn.classList.toggle('muted');
}

function togglePartMenu(btn) {
    document.querySelectorAll('.gl-part-menu.open').forEach(m => m.classList.remove('open'));
    const menu = btn.nextElementSibling;
    if (menu) menu.classList.toggle('open');
}

// ============================================
// WEBRTC ROOM MANAGEMENT (LiveKit / VDO.Ninja)
// ============================================

// Room state
let roomConnected = false;
let roomParticipants = [];
let localStream = null;

// Initialize WebRTC room (placeholder for LiveKit/VDO.Ninja integration)
// LiveKit: https://docs.livekit.io/reference/client-sdk-js/
// VDO.Ninja: https://sdk.vdo.ninja/
async function initRoom(mode = 'director') {
    console.log('[Room] Initializing in', mode, 'mode');

    // In production, this would:
    // 1. Connect to LiveKit Cloud or self-hosted server
    // 2. Or use VDO.Ninja SDK for P2P connections

    // LiveKit example (requires livekit-client package):
    // import { Room, RoomEvent, Track } from 'livekit-client';
    // const room = new Room();
    // await room.connect(wsUrl, token);
    // room.on(RoomEvent.ParticipantConnected, (participant) => {
    //     addParticipant(participant);
    // });

    // VDO.Ninja example (via iframe or SDK):
    // const vdon = new VDONinjaSDK();
    // vdon.joinRoom(roomId, { video: true, audio: true });
    // vdon.on('participant', (p) => addParticipant(p));

    roomConnected = true;
    updateParticipantCount();
}

// Add remote participant
function addParticipant(participant) {
    roomParticipants.push(participant);
    updateParticipantCount();
    // In production: render their video feed to a camera card
}

// Remove participant
function removeParticipant(participantId) {
    roomParticipants = roomParticipants.filter(p => p.id !== participantId);
    updateParticipantCount();
}

// Update participant count display
function updateParticipantCount() {
    const countEl = document.getElementById('gl-participant-count');
    if (countEl) {
        countEl.textContent = roomParticipants.length + 1; // +1 for self
    }
}

// Leave room
async function leaveRoom() {
    console.log('[Room] Leaving room');
    // In production: room.disconnect();
    roomConnected = false;
    roomParticipants = [];
    updateParticipantCount();
}

// Invite participant (generate invite link)
function inviteParticipant() {
    const code = document.getElementById('gl-room-code')?.textContent;
    const inviteUrl = buildRoomInviteUrl(code);

    // Show invite modal or copy to clipboard
    navigator.clipboard.writeText(inviteUrl).then(() => {
        alert(`Invite link copied!\n\n${inviteUrl}`);
    });
}

// ============================================
// DIRECTOR CONTROLS
// ============================================

let allMuted = false;
let talkbackActive = false;

// Mute all participants
function toggleMuteAll() {
    allMuted = !allMuted;
    const btn = document.getElementById('gl-mute-all-btn');
    if (btn) {
        btn.classList.toggle('active', allMuted);
        if (allMuted) {
            btn.innerHTML = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="1" y1="1" x2="23" y2="23"/><path d="M9 9v3a3 3 0 0 0 5.12 2.12M15 9.34V4a3 3 0 0 0-5.94-.6"/><path d="M17 16.95A7 7 0 0 1 5 12v-2m14 0v2a7 7 0 0 1-.11 1.23"/><line x1="12" y1="19" x2="12" y2="23"/></svg>';
        } else {
            btn.innerHTML = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 1a3 3 0 0 0-3 3v8a3 3 0 0 0 6 0V4a3 3 0 0 0-3-3z"/><path d="M19 10v2a7 7 0 0 1-14 0v-2"/><line x1="12" y1="19" x2="12" y2="23"/></svg>';
        }
    }
    console.log('[Director] Mute all:', allMuted);
    // In production: send mute command to all participants via LiveKit/VDO.Ninja
}

// Send CUE to talent (visual/audio signal to start)
function sendCue() {
    console.log('[Director] Sending CUE to talent');
    // Flash the CUE button
    const btn = document.querySelector('.gl-dir-btn-cue');
    if (btn) {
        btn.style.background = 'rgba(16,185,129,.5)';
        btn.style.boxShadow = '0 0 20px rgba(16,185,129,.4)';
        setTimeout(() => {
            btn.style.background = '';
            btn.style.boxShadow = '';
        }, 300);
    }
    // In production: send cue signal to all participants
    // This would trigger a visual flash or beep on their prompter
}

// Talkback - director speaks to talent (not on stream)
function toggleTalkback() {
    talkbackActive = !talkbackActive;
    const btn = document.getElementById('gl-talkback-btn');
    if (btn) {
        btn.classList.toggle('active', talkbackActive);
    }
    console.log('[Director] Talkback:', talkbackActive ? 'ON' : 'OFF');
    // In production: open private audio channel to talent
}

// Scene definitions — each scene = layout + sources array
const GL_SCENES = {
    scene1: { sources: ['cam1'],            layout: 'full',  name: 'Camera 1' },
    scene2: { sources: ['cam1', 'cam2'],    layout: 'split', name: 'Interview' },
    scene3: { sources: ['slides'],          layout: 'full',  name: 'Slides' },
    scene4: { sources: ['slides', 'cam1'],  layout: 'pip',   name: 'PiP Slides' },
};

// Layout icons for scene cards
const _LAYOUT_ICONS = {
    full:  '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/></svg>',
    split: '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><line x1="12" y1="3" x2="12" y2="21"/></svg>',
    pip:   '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><rect x="13" y="13" width="7" height="5" rx="1"/></svg>',
};

let _activeSceneId = 'scene1';

// Select a scene — loads its layout + sources onto the canvas
function selectScene(card, sceneId) {
    document.querySelectorAll('.gl-scene-chip').forEach(c => c.classList.remove('active'));
    card.classList.add('active');

    const scene = GL_SCENES[sceneId];
    if (!scene) return;
    _activeSceneId = sceneId;

    // Set the primary source for canvas
    _glPreview = scene.sources[0] || null;

    // Apply layout to canvas
    const feed = document.getElementById('gl-program-feed');
    if (feed) {
        feed.classList.remove('gl-layout-full', 'gl-layout-split', 'gl-layout-pip');
        feed.classList.add('gl-layout-' + scene.layout);

        // Render sources in canvas based on layout
        _renderCanvasLayout(feed, scene);
    }

    _updateProgramMonitor();
    _updateSwitchButton();

    // Update layout buttons to match scene
    document.querySelectorAll('.gl-scene-btn').forEach(b => b.classList.remove('active'));
    const matchBtn = document.querySelector(`.gl-scene-btn[data-layout="${scene.layout}"]`);
    if (matchBtn) matchBtn.classList.add('active');

    // Highlight sources used in this scene
    document.querySelectorAll('.gl-cam-card').forEach(c => {
        c.classList.remove('gl-cam-inpreview');
        if (scene.sources.includes(c.dataset.src)) c.classList.add('gl-cam-inpreview');
    });
    _updateSourceBadges();
}

// Render multi-source layout in canvas
function _renderCanvasLayout(feed, scene) {
    // Clear existing layout overlays
    feed.querySelectorAll('.gl-canvas-slot').forEach(el => el.remove());

    if (scene.layout === 'split' && scene.sources.length >= 2) {
        // Two side-by-side feeds
        scene.sources.slice(0, 2).forEach((srcId, i) => {
            const slot = document.createElement('div');
            slot.className = 'gl-canvas-slot gl-canvas-slot-' + (i === 0 ? 'left' : 'right');
            const srcName = GL_SOURCES[srcId]?.name || srcId;
            slot.innerHTML = `<div class="gl-slot-feed gl-feed-${srcId}"></div><span class="gl-slot-label">${srcName}</span>`;
            feed.appendChild(slot);
        });
    } else if (scene.layout === 'pip' && scene.sources.length >= 2) {
        // Main feed + small overlay
        const pip = document.createElement('div');
        pip.className = 'gl-canvas-slot gl-canvas-slot-pip';
        const srcName = GL_SOURCES[scene.sources[1]]?.name || scene.sources[1];
        pip.innerHTML = `<div class="gl-slot-feed gl-feed-${scene.sources[1]}"></div><span class="gl-slot-label">${srcName}</span>`;
        feed.appendChild(pip);
    }
}

// Double-click to rename a scene
function renameScene(nameEl) {
    const old = nameEl.textContent;
    nameEl.contentEditable = 'true';
    nameEl.focus();
    const range = document.createRange();
    range.selectNodeContents(nameEl);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);
    nameEl.onblur = () => {
        nameEl.contentEditable = 'false';
        const sceneCard = nameEl.closest('.gl-scene-card');
        const sid = sceneCard && sceneCard.dataset.scene;
        if (sid && GL_SCENES[sid]) GL_SCENES[sid].name = nameEl.textContent || old;
    };
    nameEl.onkeydown = (e) => { if (e.key === 'Enter') { e.preventDefault(); nameEl.blur(); } };
}

// Delete a scene
function deleteScene(btn, sceneId) {
    const chip = btn.closest('.gl-scene-chip');
    const wasActive = chip.classList.contains('active');
    delete GL_SCENES[sceneId];
    chip.remove();
    if (wasActive) {
        const first = document.querySelector('.gl-scene-chip');
        if (first) first.click();
    }
}

// Add a new scene
function addScene() {
    const list = document.getElementById('gl-scenes-list');
    if (!list) return;
    const n = list.querySelectorAll('.gl-scene-card').length + 1;
    const sid = 'scene' + Date.now();
    GL_SCENES[sid] = { sources: [], layout: 'full', name: 'Scene ' + n };
    const card = document.createElement('div');
    card.className = 'gl-scene-card';
    card.dataset.scene = sid;
    card.setAttribute('onclick', `selectScene(this,'${sid}')`);
    card.className = 'gl-scene-chip';
    card.innerHTML = `
        <svg class="gl-scene-chip-icon" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/></svg>
        <span class="gl-scene-chip-name" ondblclick="renameScene(this)">Scene ${n}</span>
        <button class="gl-scene-chip-del" onclick="event.stopPropagation();deleteScene(this,'${sid}')" title="Delete">&times;</button>`;
    list.appendChild(card);
    card.click();
}

// Change layout of active scene
function setSceneLayout(btn, layout) {
    btn.closest('.gl-scene-tools').querySelectorAll('.gl-scene-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');

    if (_activeSceneId && GL_SCENES[_activeSceneId]) {
        GL_SCENES[_activeSceneId].layout = layout;
        // Update scene card icon
        const card = document.querySelector(`.gl-scene-chip[data-scene="${_activeSceneId}"]`);
        if (card) {
            const icon = card.querySelector('.gl-scene-chip-icon');
            if (icon) icon.outerHTML = _LAYOUT_ICONS[layout] || _LAYOUT_ICONS.full;
        }
        // Re-render canvas
        const feed = document.getElementById('gl-program-feed');
        if (feed) {
            feed.classList.remove('gl-layout-full', 'gl-layout-split', 'gl-layout-pip');
            feed.classList.add('gl-layout-' + layout);
            _renderCanvasLayout(feed, GL_SCENES[_activeSceneId]);
        }
    }
}

// Audio mixer helpers
function toggleMixMute(btn) {
    btn.classList.toggle('muted');
    const ch = btn.closest('.gl-mix-ch');
    if (ch) ch.classList.toggle('gl-mix-muted');
}

// ============================================
// INPUT CARD MENU & DRAG-DROP
// ============================================

function toggleInputMenu(btn) {
    // Close all other menus
    document.querySelectorAll('.gl-cam-menu.open').forEach(m => m.classList.remove('open'));
    const menu = btn.nextElementSibling;
    if (menu) menu.classList.toggle('open');
}
// Close menus on click outside
document.addEventListener('click', () => {
    document.querySelectorAll('.gl-cam-menu.open').forEach(m => m.classList.remove('open'));
});

function connectInput(btn) {
    const card = btn.closest('.gl-cam-card');
    if (card) selectSource(card);
    btn.closest('.gl-cam-menu').classList.remove('open');
}
function muteInput(btn) {
    btn.closest('.gl-cam-menu').classList.remove('open');
}
function renameInput(btn) {
    const card = btn.closest('.gl-cam-card');
    const nameEl = card?.querySelector('.gl-cam-name');
    btn.closest('.gl-cam-menu').classList.remove('open');
    if (nameEl) {
        nameEl.contentEditable = 'true';
        nameEl.focus();
        document.execCommand('selectAll');
        nameEl.onblur = () => { nameEl.contentEditable = 'false'; };
        nameEl.onkeydown = (e) => { if (e.key === 'Enter') { e.preventDefault(); nameEl.blur(); } };
    }
}
function removeInput(btn) {
    const card = btn.closest('.gl-cam-card');
    if (card) card.remove();
}

// Drag from input cards
function dragSource(e) {
    const card = e.target.closest('.gl-cam-card');
    const srcId = card?.dataset.src;
    if (srcId) {
        e.dataTransfer.setData('text/plain', srcId);
        e.dataTransfer.effectAllowed = 'copy';
    }
}

// Canvas drop zone
function canvasDragOver(e) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'copy';
    document.getElementById('gl-canvas-drop-hint')?.classList.add('active');
}
function canvasDragLeave(e) {
    document.getElementById('gl-canvas-drop-hint')?.classList.remove('active');
}
function canvasDrop(e) {
    e.preventDefault();
    document.getElementById('gl-canvas-drop-hint')?.classList.remove('active');
    const srcId = e.dataTransfer.getData('text/plain');
    if (!srcId || !GL_SOURCES[srcId]) return;

    const container = document.getElementById('gl-canvas-sources');
    const feed = document.getElementById('gl-program-feed');
    if (!container || !feed) return;

    // Hide empty state
    const empty = document.getElementById('gl-canvas-empty');
    if (empty) empty.style.display = 'none';

    // Calculate drop position as percentage
    const rect = feed.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width * 100).toFixed(1);
    const y = ((e.clientY - rect.top) / rect.height * 100).toFixed(1);

    addCanvasSource(srcId, parseFloat(x), parseFloat(y));
}

let _canvasSrcCounter = 0;
function addCanvasSource(srcId, xPct, yPct) {
    const container = document.getElementById('gl-canvas-sources');
    if (!container) return;
    const src = GL_SOURCES[srcId];
    if (!src) return;

    const id = 'csrc-' + (++_canvasSrcCounter);
    const el = document.createElement('div');
    el.className = 'gl-canvas-src';
    el.id = id;
    el.dataset.srcId = srcId;
    // Default 40% width, 16:9 aspect
    el.style.cssText = `left:${Math.max(0, xPct - 20)}%;top:${Math.max(0, yPct - 15)}%;width:40%;height:45%;`;
    el.innerHTML = `
        <div class="gl-canvas-src-feed ${src.feed}"></div>
        <span class="gl-canvas-src-label">${src.label}</span>
        <button class="gl-canvas-src-del" onclick="removeCanvasSource('${id}')">&times;</button>
        <div class="gl-canvas-handle gl-canvas-handle-tl"></div>
        <div class="gl-canvas-handle gl-canvas-handle-tr"></div>
        <div class="gl-canvas-handle gl-canvas-handle-bl"></div>
        <div class="gl-canvas-handle gl-canvas-handle-br"></div>
    `;
    el.addEventListener('mousedown', (e) => startCanvasDrag(e, el));
    el.addEventListener('click', (e) => {
        e.stopPropagation();
        selectCanvasSource(el);
    });
    // Resize handles
    el.querySelectorAll('.gl-canvas-handle').forEach(h => {
        h.addEventListener('mousedown', (e) => { e.stopPropagation(); startCanvasResize(e, el, h); });
    });
    container.appendChild(el);
    selectCanvasSource(el);

    // Set as preview source
    _glPreview = srcId;
    _updateProgramMonitor();
}

function selectCanvasSource(el) {
    document.querySelectorAll('.gl-canvas-src').forEach(s => s.classList.remove('selected'));
    el.classList.add('selected');
}

function removeCanvasSource(id) {
    const el = document.getElementById(id);
    if (el) el.remove();
    // Show empty if no sources left
    const container = document.getElementById('gl-canvas-sources');
    if (container && container.children.length === 0) {
        const empty = document.getElementById('gl-canvas-empty');
        if (empty) empty.style.display = '';
    }
}

// Deselect on canvas click
document.addEventListener('click', (e) => {
    if (e.target.closest('.gl-monitor-feed') && !e.target.closest('.gl-canvas-src')) {
        document.querySelectorAll('.gl-canvas-src').forEach(s => s.classList.remove('selected'));
    }
});

// ── Canvas drag (move) ──
function startCanvasDrag(e, el) {
    if (e.target.closest('.gl-canvas-handle') || e.target.closest('.gl-canvas-src-del')) return;
    e.preventDefault();
    const container = document.getElementById('gl-canvas-sources');
    if (!container) return;
    const cRect = container.getBoundingClientRect();
    const startX = e.clientX;
    const startY = e.clientY;
    const startLeft = el.offsetLeft;
    const startTop = el.offsetTop;

    function onMove(ev) {
        const dx = ev.clientX - startX;
        const dy = ev.clientY - startY;
        el.style.left = ((startLeft + dx) / cRect.width * 100) + '%';
        el.style.top = ((startTop + dy) / cRect.height * 100) + '%';
    }
    function onUp() {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
    }
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
}

// ── Canvas resize ──
function startCanvasResize(e, el, handle) {
    e.preventDefault();
    const container = document.getElementById('gl-canvas-sources');
    if (!container) return;
    const cRect = container.getBoundingClientRect();
    const startX = e.clientX;
    const startY = e.clientY;
    const startW = el.offsetWidth;
    const startH = el.offsetHeight;
    const startL = el.offsetLeft;
    const startT = el.offsetTop;
    const isBR = handle.classList.contains('gl-canvas-handle-br');
    const isTL = handle.classList.contains('gl-canvas-handle-tl');
    const isTR = handle.classList.contains('gl-canvas-handle-tr');
    const isBL = handle.classList.contains('gl-canvas-handle-bl');

    function onMove(ev) {
        const dx = ev.clientX - startX;
        const dy = ev.clientY - startY;
        if (isBR) {
            el.style.width = ((startW + dx) / cRect.width * 100) + '%';
            el.style.height = ((startH + dy) / cRect.height * 100) + '%';
        } else if (isTL) {
            el.style.left = ((startL + dx) / cRect.width * 100) + '%';
            el.style.top = ((startT + dy) / cRect.height * 100) + '%';
            el.style.width = ((startW - dx) / cRect.width * 100) + '%';
            el.style.height = ((startH - dy) / cRect.height * 100) + '%';
        } else if (isTR) {
            el.style.top = ((startT + dy) / cRect.height * 100) + '%';
            el.style.width = ((startW + dx) / cRect.width * 100) + '%';
            el.style.height = ((startH - dy) / cRect.height * 100) + '%';
        } else if (isBL) {
            el.style.left = ((startL + dx) / cRect.width * 100) + '%';
            el.style.width = ((startW - dx) / cRect.width * 100) + '%';
            el.style.height = ((startH + dy) / cRect.height * 100) + '%';
        }
    }
    function onUp() {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
    }
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
}

function updateMixFader(slider) {
    const ch = slider.closest('.gl-mix-ch');
    if (!ch) return;
    const val = parseInt(slider.value);
    // Convert 0-100 to dB scale
    let db;
    if (val === 0) { db = '-∞'; }
    else {
        const dbVal = (val / 100) * 60 - 60; // -60 to 0
        db = dbVal.toFixed(1) + ' dB';
    }
    const dbEl = ch.querySelector('.gl-mix-db-val');
    if (dbEl) dbEl.textContent = db;
    // Level bar follows fader position
    const bar = ch.querySelector('.gl-mix-level');
    if (bar) bar.style.width = val + '%';
}

// ── Animated VU meters ──
let _vuAnimFrame = null;
const _vuChannels = {};

function _initVuMeters() {
    const channels = document.querySelectorAll('.gl-mix-ch');
    channels.forEach((ch, i) => {
        const bar = ch.querySelector('.gl-mix-level');
        const slider = ch.querySelector('.gl-mix-fader');
        if (!bar || !slider) return;
        const name = ch.dataset.ch || 'ch' + i;
        _vuChannels[name] = {
            bar,
            slider,
            ch,
            level: parseFloat(bar.style.width) || 50,
            target: parseFloat(bar.style.width) || 50,
            peak: 0,
            peakDecay: 0
        };
    });
    _animateVu();
}

function _animateVu() {
    for (const key in _vuChannels) {
        const c = _vuChannels[key];
        if (c.ch.classList.contains('gl-mix-muted')) {
            c.target = 0;
        } else {
            const faderVal = parseInt(c.slider.value) / 100;
            // Simulate audio signal with randomness
            const base = faderVal * 75;
            const noise = (Math.random() - 0.5) * 25 * faderVal;
            const burst = Math.random() < 0.03 ? 15 * faderVal : 0; // occasional transients
            c.target = Math.max(0, Math.min(95, base + noise + burst));
        }
        // Smooth rise, slower fall (like real VU)
        if (c.target > c.level) {
            c.level += (c.target - c.level) * 0.35;
        } else {
            c.level += (c.target - c.level) * 0.12;
        }
        c.bar.style.width = c.level.toFixed(1) + '%';
    }
    _vuAnimFrame = requestAnimationFrame(_animateVu);
}

function _stopVuMeters() {
    if (_vuAnimFrame) cancelAnimationFrame(_vuAnimFrame);
    _vuAnimFrame = null;
}

// Update AIR indicator dot (red when streaming or recording)
function updateAirDot() {
    const dot = document.getElementById('gl-air-dot');
    const monitor = document.getElementById('gl-preview-monitor');
    const live = glStreaming || glRecording;
    if (dot) dot.classList.toggle('gl-air-dot-live', live);
    if (monitor) monitor.classList.toggle('gl-monitor-air-live', live);
}

// Share director's screen
function shareScreen() {
    console.log('[Director] Starting screen share');
    // In production: navigator.mediaDevices.getDisplayMedia()
    // Then send stream to room
    if (navigator.mediaDevices?.getDisplayMedia) {
        navigator.mediaDevices.getDisplayMedia({ video: true })
            .then(stream => {
                console.log('[Director] Screen share started');
                // Add as new source
            })
            .catch(err => {
                console.log('[Director] Screen share cancelled');
            });
    }
}

// End session for all participants
function endSession() {
    if (confirm('End session for all participants?')) {
        console.log('[Director] Ending session');
        // In production: disconnect all participants
        // room.disconnect();
        leaveRoom();
        navigateTo('editor');
    }
}
let glTimerInterval = null;
let glTimerSeconds = 0;

function toggleGoLiveRec() {
    glRecording = !glRecording;
    const btn = document.getElementById('gl-rec-btn');
    const badge = document.getElementById('gl-status-badge');
    btn.classList.toggle('active', glRecording);

    if (glRecording) {
        badge.textContent = 'REC';
        badge.className = 'gl-badge gl-badge-rec';
        startGlTimer();
    } else if (glStreaming) {
        badge.textContent = 'LIVE';
        badge.className = 'gl-badge gl-badge-live';
    } else {
        badge.textContent = 'IDLE';
        badge.className = 'gl-badge gl-badge-idle';
        stopGlTimer();
    }
}

function toggleGoLiveStream() {
    glStreaming = !glStreaming;
    const btn = document.getElementById('gl-stream-btn');
    const badge = document.getElementById('gl-status-badge');

    if (glStreaming) {
        btn.innerHTML = '<span class="gl-stream-icon"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="6" y="4" width="4" height="16"/><rect x="14" y="4" width="4" height="16"/></svg></span> STOP STREAM';
        btn.classList.add('active');
        badge.textContent = 'LIVE';
        badge.className = 'gl-badge gl-badge-live';
        startGlTimer();
        updateAirDot();
        const statStatus = document.getElementById('gl-stat-status');
        if (statStatus) { statStatus.textContent = 'Live'; statStatus.className = 'gl-stat-value gl-stat-live'; }
    } else {
        btn.innerHTML = '<span class="gl-stream-icon"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="3"/><path d="M16.24 7.76a6 6 0 0 1 0 8.49"/><path d="M19.07 4.93a10 10 0 0 1 0 14.14"/><path d="M7.76 16.24a6 6 0 0 1 0-8.49"/><path d="M4.93 19.07a10 10 0 0 1 0-14.14"/></svg></span> START STREAM';
        btn.classList.remove('active');
        updateAirDot();
        const statStatus2 = document.getElementById('gl-stat-status');
        if (statStatus2) { statStatus2.textContent = 'Offline'; statStatus2.className = 'gl-stat-value gl-stat-offline'; }
        const bitrate2 = document.getElementById('gl-stat-bitrate');
        if (bitrate2) bitrate2.textContent = '— kbps';
        if (glRecording) {
            badge.textContent = 'REC';
            badge.className = 'gl-badge gl-badge-rec';
        } else {
            badge.textContent = 'IDLE';
            badge.className = 'gl-badge gl-badge-idle';
            stopGlTimer();
        }
    }
}

function updatePipFeed() {
    const programFeed = document.querySelector('#gl-program-monitor .gl-tp-camera .gl-feed-active');
    const pipFeed = document.getElementById('gl-pip-feed');
    if (programFeed && pipFeed) {
        pipFeed.className = programFeed.className;
    }
    // Update PiP badge text
    const pipBadge = document.querySelector('.gl-pip-badge');
    if (pipBadge) {
        if (glStreaming) {
            pipBadge.innerHTML = '<span class="gl-pip-dot"></span> LIVE';
        } else if (glRecording) {
            pipBadge.innerHTML = '<span class="gl-pip-dot"></span> REC';
        }
    }
}

function hidePip() {
    const pip = document.getElementById('gl-pip');
    if (pip) pip.classList.remove('active');
}

function startGlTimer() {
    if (glTimerInterval) return;
    const timerEl = document.getElementById('gl-timer');
    timerEl.classList.add('active');
    glTimerInterval = setInterval(() => {
        glTimerSeconds++;
        const h = String(Math.floor(glTimerSeconds / 3600)).padStart(2, '0');
        const m = String(Math.floor((glTimerSeconds % 3600) / 60)).padStart(2, '0');
        const s = String(glTimerSeconds % 60).padStart(2, '0');
        const timeStr = `${h}:${m}:${s}`;
        timerEl.textContent = timeStr;
        // Sync PiP timer
        const pipTimer = document.getElementById('gl-pip-timer');
        if (pipTimer) pipTimer.textContent = timeStr;
    }, 1000);
}

function stopGlTimer() {
    if (glTimerInterval) {
        clearInterval(glTimerInterval);
        glTimerInterval = null;
    }
    glTimerSeconds = 0;
    const timerEl = document.getElementById('gl-timer');
    timerEl.textContent = '00:00:00';
    timerEl.classList.remove('active');
    // Hide PiP when stream/rec fully stops
    if (!glStreaming && !glRecording) {
        hidePip();
    }
}

function toggleGlTp() {
    const btn = document.getElementById('gl-play-btn');
    const isPlaying = btn.classList.toggle('active');
    if (isPlaying) {
        btn.innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="6" y="4" width="4" height="16"/><rect x="14" y="4" width="4" height="16"/></svg>';
    } else {
        btn.innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5,3 19,12 5,21"/></svg>';
    }
}

// Animate audio meters for visual effect (right panel)
function animateGlMeters() {
    const ids = ['gl-meter-r1', 'gl-meter-r2', 'gl-meter-r3'];
    const ranges = [[40, 80], [15, 40], [35, 70]];
    ids.forEach((id, i) => {
        const el = document.getElementById(id);
        if (el) el.style.width = (ranges[i][0] + Math.random() * (ranges[i][1] - ranges[i][0])) + '%';
    });
    requestAnimationFrame(() => setTimeout(animateGlMeters, 100 + Math.random() * 150));
}
animateGlMeters();

// Update stats when streaming
function updateGlStats() {
    if (!glStreaming) return;
    const ping = document.getElementById('gl-stat-ping');
    const bitrate = document.getElementById('gl-stat-bitrate');
    const jitter = document.getElementById('gl-stat-jitter');
    const upload = document.getElementById('gl-stat-upload');
    if (ping) {
        const p = 18 + Math.floor(Math.random() * 15);
        ping.textContent = p + ' ms';
        ping.className = 'gl-stat-value ' + (p < 50 ? 'gl-stat-good' : p < 100 ? 'gl-stat-warn' : 'gl-stat-bad');
    }
    if (bitrate) bitrate.textContent = (4500 + Math.floor(Math.random() * 1500)) + ' kbps';
    if (jitter) jitter.textContent = (1 + Math.floor(Math.random() * 4)) + ' ms';
    if (upload) upload.textContent = (7.5 + Math.random() * 2).toFixed(1) + ' Mbps';
}
setInterval(updateGlStats, 2000);

// ============================================
// SETTINGS — DEVICE CARDS
// ============================================

function toggleDeviceCard(toggleEl) {
    const card = toggleEl.closest('.set-device-card');
    const isOn = toggleEl.classList.toggle('on');
    card.classList.toggle('active', isOn);
}

// Camera preview selection
const camData = {
    cam1: { name: 'Logitech C920 HD Pro', meta: 'USB \u00b7 1920\u00d71080 \u00b7 30fps \u00b7 Primary',
            bg: "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='400' height='225'%3E%3Crect fill='%23182030' width='400' height='225'/%3E%3Ccircle cx='200' cy='85' r='35' fill='%23354060'/%3E%3Cellipse cx='200' cy='165' rx='50' ry='40' fill='%23354060'/%3E%3C/svg%3E\")" },
    cam2: { name: 'FaceTime HD Camera', meta: 'Built-in \u00b7 1280\u00d7720 \u00b7 30fps',
            bg: "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='400' height='225'%3E%3Crect fill='%23152828' width='400' height='225'/%3E%3Ccircle cx='200' cy='85' r='35' fill='%23305050'/%3E%3Cellipse cx='200' cy='165' rx='50' ry='40' fill='%23305050'/%3E%3C/svg%3E\")" },
    cam3: { name: 'OBS Virtual Camera', meta: 'Virtual \u00b7 1920\u00d71080',
            bg: 'linear-gradient(135deg, #0c0c14 0%, #0a0a12 100%)' }
};

function selectCamPreview(card) {
    document.querySelectorAll('.set-device-card[data-cam]').forEach(c => c.classList.remove('set-cam-selected'));
    card.classList.add('set-cam-selected');
    const id = card.dataset.cam;
    const data = camData[id];
    if (!data) return;
    const feed = document.getElementById('set-cam-preview-feed');
    const name = document.getElementById('set-cam-preview-name');
    const meta = document.getElementById('set-cam-preview-meta');
    if (feed) feed.style.backgroundImage = data.bg;
    if (name) name.textContent = data.name;
    if (meta) meta.textContent = data.meta;
}

function setPrimaryCam(btn) {
    // Remove active from all primary buttons
    document.querySelectorAll('.set-btn-primary-cam').forEach(b => {
        b.classList.remove('active');
        b.disabled = false;
        b.classList.remove('set-btn-golden');
        b.classList.add('set-btn-outline');
        b.innerHTML = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg> Set as Primary';
        b.setAttribute('onclick', 'setPrimaryCam(this)');
    });
    // Remove old primary badge
    document.querySelectorAll('.set-badge-primary').forEach(b => b.remove());

    // Activate clicked button
    btn.classList.add('active');
    btn.classList.remove('set-btn-outline');
    btn.classList.add('set-btn-golden');
    btn.disabled = true;
    btn.innerHTML = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg> Primary Camera';
    btn.removeAttribute('onclick');

    // Add primary badge to the camera preview
    const card = btn.closest('.set-device-card');
    const preview = card.querySelector('.set-cam-preview');
    if (preview) {
        const badge = document.createElement('span');
        badge.className = 'set-device-badge set-badge-primary';
        badge.innerHTML = '<svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg> Primary';
        preview.appendChild(badge);
    }
}

// Streaming destination card toggle
function toggleDestCard(card) {
    card.classList.toggle('open');
}

// Update destination card title when Display Name changes
function updateDestName(input) {
    const card = input.closest('.set-dest-card');
    if (!card) return;
    const nameEl = card.querySelector('.set-dest-name');
    if (nameEl) nameEl.textContent = input.value || 'Untitled';
}

// ============================================
// GO LIVE DIRECTOR — PREVIEW / PROGRAM SWITCHER
// ============================================

// Track which source is on PROGRAM and which is in PREVIEW
let _glProgram = 'cam1';   // currently on-air
let _glPreview = null;     // queued next (null = nothing selected)

// Camera metadata: feed class + display label
const GL_SOURCES = {
    cam1:     { feed: 'gl-feed-cam1', label: 'Camera 1', reader: 'Alex' },
    cam2:     { feed: 'gl-feed-cam2', label: 'Camera 2', reader: 'Sarah' },
    prompter: { feed: 'gl-feed-cam3', label: 'Prompter Display', reader: null },
    slides:   { feed: 'gl-feed-cam4', label: 'Slides', reader: null },
};

// Called when clicking a camera card — loads it into large PREVIEW monitor
function selectSource(card) {
    const srcId = card.dataset.src;
    if (!srcId) return;

    // If clicking the same as already in preview, clear it
    if (srcId === _glPreview) {
        _glPreview = null;
    } else {
        _glPreview = srcId;
    }

    // Update card highlight states
    document.querySelectorAll('.gl-cam-card').forEach(c => {
        const id = c.dataset.src;
        c.classList.remove('gl-cam-onair', 'gl-cam-inpreview');
        if (id === _glProgram) c.classList.add('gl-cam-onair');
        if (id === _glPreview) c.classList.add('gl-cam-inpreview');
    });

    // Update badge on the selected card
    _updateSourceBadges();

    // Update large PREVIEW monitor (center) with selected camera
    _updateProgramMonitor();

    // Show/hide SWITCH button
    _updateSwitchButton();
}

// Called by SWITCH button — sends selected camera to ON AIR
function cutToPreview() {
    if (!_glPreview || _glPreview === _glProgram) return;

    // Move selected camera to ON AIR
    _glProgram = _glPreview;
    _glPreview = null;

    // Update card states
    document.querySelectorAll('.gl-cam-card').forEach(c => {
        c.classList.remove('gl-cam-onair', 'gl-cam-inpreview');
        if (c.dataset.src === _glProgram) c.classList.add('gl-cam-onair');
    });
    _updateSourceBadges();

    // Update ON AIR monitor (right) with new live source
    _updatePreviewMonitor();

    // Clear large PREVIEW monitor (center)
    _updateProgramMonitor();

    // Hide SWITCH button
    _updateSwitchButton();

    // Flash the ON AIR monitor border red briefly
    const onair = document.getElementById('gl-preview-monitor');
    if (onair) {
        onair.style.boxShadow = '0 0 0 2px rgba(239,68,68,.8), 0 0 40px rgba(239,68,68,.4)';
        setTimeout(() => { onair.style.boxShadow = ''; }, 600);
    }
}

// Show/hide SWITCH button based on preview state
function _updateSwitchButton() {
    // New big SWITCH button in preview monitor
    const switchBar = document.getElementById('gl-switch-bar');
    if (switchBar) {
        switchBar.style.display = _glPreview ? '' : 'none';
    }

    // Show director overlay when camera selected
    const dirOverlay = document.getElementById('gl-dir-overlay');
    if (dirOverlay) {
        dirOverlay.style.display = _glPreview ? '' : 'none';
    }

    // Old small button (keep hidden)
    const switchBtn = document.querySelector('.gl-cut-btn');
    if (switchBtn) {
        switchBtn.style.display = 'none';
    }
}

// Clear preview monitor
function _clearPreview() {
    const previewLabel = document.querySelector('#gl-preview-monitor .gl-monitor-res');
    if (previewLabel) previewLabel.textContent = 'Select source';

    const previewFeed = document.querySelector('#gl-preview-monitor .gl-monitor-feed');
    if (previewFeed) {
        previewFeed.innerHTML = `<div class="gl-preview-empty">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity=".2">
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
                <line x1="8" y1="21" x2="16" y2="21"/>
                <line x1="12" y1="17" x2="12" y2="21"/>
            </svg>
            <span>Click a camera to preview</span>
        </div>`;
    }
}

function _updateSourceBadges() {
    document.querySelectorAll('.gl-cam-card').forEach(c => {
        const badge = c.querySelector('.gl-cam-status-badge');
        if (!badge) return;
        const id = c.dataset.src;
        if (id === _glProgram) {
            badge.className = 'gl-cam-status-badge gl-badge-onair';
            badge.textContent = 'ON AIR';
        } else if (id === _glPreview) {
            badge.className = 'gl-cam-status-badge gl-badge-preview';
            badge.textContent = 'NEXT';
        } else {
            badge.className = 'gl-cam-status-badge gl-badge-idle';
            badge.textContent = id === 'prompter' ? 'PROMPTER' : 'READY';
            if (id === 'prompter') badge.className = 'gl-cam-status-badge gl-badge-prompter';
        }
    });
}

// CANVAS monitor (center, big) — shows source selected from left panel
function _updateProgramMonitor() {
    const src = GL_SOURCES[_glPreview];
    const feed = document.getElementById('gl-program-feed');  // center = CANVAS
    const label = document.getElementById('gl-program-src');
    if (!feed) return;

    if (!src) {
        feed.innerHTML = `<div class="gl-preview-empty">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" opacity=".25">
                <rect x="2" y="3" width="20" height="14" rx="2"/>
                <line x1="8" y1="21" x2="16" y2="21"/>
                <line x1="12" y1="17" x2="12" y2="21"/>
            </svg>
            <span>SELECT A SOURCE</span>
        </div>`;
        if (label) label.textContent = '—';
        return;
    }
    feed.innerHTML = `<div class="gl-feed-active ${src.feed}"></div>`;
    if (label) label.textContent = src.label;
}

// AIR monitor (right, small) — shows what's actually LIVE/streaming
function _updatePreviewMonitor() {
    const src = GL_SOURCES[_glProgram];
    const feed = document.getElementById('gl-preview-feed');  // right = AIR
    const label = document.getElementById('gl-preview-src');
    if (!feed || !src) return;

    const pip = document.getElementById('gl-pip-overlay');
    const pipHtml = pip ? pip.outerHTML : '';
    feed.innerHTML = `<div class="gl-feed-active ${src.feed}"></div>${pipHtml}`;
    if (label) label.textContent = src.label;
}

// ============================================
// GO LIVE DIRECTOR CONTROLS
// ============================================

function setLayout(btn, type) {
    btn.closest('.gl-layout-btns').querySelectorAll('.gl-layout-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');

    const feed = document.getElementById('gl-layout-feed');
    const pip = document.getElementById('gl-pip-slot');
    if (!feed) return;

    feed.classList.remove('gl-feed-split', 'gl-feed-7030', 'gl-feed-grid');

    if (type === 'full') {
        if (pip) pip.style.display = 'none';
    } else if (type === 'pip') {
        if (pip) pip.style.display = 'block';
    } else if (type === 'split') {
        if (pip) pip.style.display = 'none';
        feed.classList.add('gl-feed-split');
    } else if (type === 'split7030') {
        if (pip) pip.style.display = 'none';
        feed.classList.add('gl-feed-7030');
    } else if (type === 'grid') {
        if (pip) pip.style.display = 'none';
        feed.classList.add('gl-feed-grid');
    }
}

function setTransition(btn, type) {
    btn.closest('.gl-transition-btns').querySelectorAll('.gl-trans-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    window._glTransition = type;
}

function setDur(btn) {
    btn.closest('.gl-trans-dur').querySelectorAll('.gl-dur-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    const sec = parseFloat(btn.textContent.trim());
    if (!isNaN(sec)) window._glTransDur = sec * 1000;
}

function setCrop(btn, type) {
    btn.closest('.gl-crop-btns').querySelectorAll('.gl-crop-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    // If this camera is ON AIR, update the crop indicator on the program monitor
    const card = btn.closest('.gl-cam-card');
    if (card && card.classList.contains('gl-cam-onair')) {
        const indicator = document.getElementById('gl-crop-indicator');
        if (indicator) {
            if (type === 'full') {
                indicator.style.display = 'none';
            } else {
                indicator.style.display = 'flex';
                indicator.querySelector('span') ? (indicator.querySelector('span').textContent = type === 'hs' ? 'H&S' : 'FACE') : null;
            }
        }
    }
}

// ============================================
// TRANSITION CONTROLS
// ============================================

let _transitionType = 'cut';
let _transitionDur = 500;
let _sceneLayout = 'full';

// ── Scene Layout (Full, Split, PiP) ──
function setSceneLayout(btn, layout) {
    document.querySelectorAll('.gl-scene-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    _sceneLayout = layout;

    const programFeed = document.getElementById('gl-program-feed');
    const pipOverlay = document.getElementById('gl-pip-overlay');
    if (!programFeed) return;

    // Remove old layout classes
    programFeed.classList.remove('gl-layout-full', 'gl-layout-split', 'gl-layout-pip');
    programFeed.classList.add(`gl-layout-${layout}`);

    // Show/hide PiP overlay
    if (pipOverlay) {
        pipOverlay.style.display = layout === 'pip' ? 'block' : 'none';
    }
}

// ── PiP Position ──
let _pipPosition = 'bottom-right';
function setPipPosition(position) {
    _pipPosition = position;
    document.querySelectorAll('.gl-pip-pos-btn').forEach(b => b.classList.remove('active'));
    event.target.closest('.gl-pip-pos-btn').classList.add('active');

    const pip = document.getElementById('gl-pip-overlay');
    if (pip) {
        pip.classList.remove('top-left', 'top-right', 'bottom-left', 'bottom-right');
        pip.classList.add(position);
    }
}

function swapSources() {
    // Swap preview and program sources
    const temp = _glPreview;
    _glPreview = _glProgram;
    _glProgram = temp;
    updateMonitorFeeds();
}

// ── Overlays & Effects ──
let _overlays = { 1: false, 2: false, 3: false, 4: false };
let _effects = { blur: false, color: false, chroma: false };

function toggleOverlay(num) {
    _overlays[num] = !_overlays[num];
    const btns = document.querySelectorAll('.gl-fx-btn');
    if (btns[num - 1]) {
        btns[num - 1].classList.toggle('active', _overlays[num]);
    }
    console.log(`Overlay ${num}: ${_overlays[num] ? 'ON' : 'OFF'}`);
}

function toggleFx(fx) {
    _effects[fx] = !_effects[fx];
    // Find the button by title and toggle active state
    const btn = document.querySelector(`.gl-fx-btn[onclick*="${fx}"]`);
    if (btn) btn.classList.toggle('active', _effects[fx]);
    console.log(`Effect ${fx}: ${_effects[fx] ? 'ON' : 'OFF'}`);
}

function toggleChat() {
    toggleChatFloat();
}

function toggleChatFloat() {
    const chatFloat = document.getElementById('gl-chat-float');
    if (chatFloat) {
        chatFloat.classList.toggle('open');
    }
}

function toggleMultiview() {
    console.log('Multiview toggle');
    // Toggle multiview mode
}

// ── Layout Presets ──
function setLayoutPreset(preset) {
    document.querySelectorAll('.gl-preset-btn').forEach(b => b.classList.remove('active'));
    event.target.closest('.gl-preset-btn').classList.add('active');

    const content = document.querySelector('.gl-content');
    const program = document.querySelector('.gl-program');
    const chatPanel = document.getElementById('gl-chat-panel');

    // Remove all preset classes
    content.classList.remove('gl-layout-chat', 'gl-layout-multiview', 'gl-layout-fullpgm');

    switch(preset) {
        case 'chat':
            content.classList.add('gl-layout-chat');
            if (chatPanel) chatPanel.classList.add('open');
            break;
        case 'multiview':
            content.classList.add('gl-layout-multiview');
            break;
        case 'fullpgm':
            content.classList.add('gl-layout-fullpgm');
            break;
        default:
            // default layout
            break;
    }
}

// ── Tab Switching in right sidebar ──
function switchGlTab(btn, tabId) {
    // Update active tab button
    document.querySelectorAll('.gl-tab').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    // Show corresponding content
    document.querySelectorAll('.gl-tab-content').forEach(c => c.classList.remove('active'));
    const panel = document.getElementById('gl-tab-' + tabId);
    if (panel) panel.classList.add('active');
}

// ── Panel Toggle (show/hide left & right sidebars) ──
function toggleGlPanel(side) {
    const content = document.querySelector('.gl-content');
    const btn = document.getElementById(`gl-toggle-${side}`);
    const cls = side === 'left' ? 'gl-hide-left' : 'gl-hide-right';

    content.classList.toggle(cls);
    btn.classList.toggle('active');

    // Sync full program button state
    const fullBtn = document.getElementById('gl-toggle-fullpgm');
    const bothHidden = content.classList.contains('gl-hide-left') && content.classList.contains('gl-hide-right');
    if (fullBtn) fullBtn.classList.toggle('active', bothHidden);
}

function toggleGlFullProgram() {
    const content = document.querySelector('.gl-content');
    const leftBtn = document.getElementById('gl-toggle-left');
    const rightBtn = document.getElementById('gl-toggle-right');
    const fullBtn = document.getElementById('gl-toggle-fullpgm');

    const bothHidden = content.classList.contains('gl-hide-left') && content.classList.contains('gl-hide-right');

    if (bothHidden) {
        // Restore both panels
        content.classList.remove('gl-hide-left', 'gl-hide-right');
        leftBtn.classList.add('active');
        rightBtn.classList.add('active');
        fullBtn.classList.remove('active');
    } else {
        // Hide both panels
        content.classList.add('gl-hide-left', 'gl-hide-right');
        leftBtn.classList.remove('active');
        rightBtn.classList.remove('active');
        fullBtn.classList.add('active');
    }
}

// Set transition type (CUT, FADE, WIPE)
function setTransitionType(btn, type) {
    document.querySelectorAll('.gl-trans-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    _transitionType = type;
    updateTransitionStatus();
}

// Set transition duration
function setTransDuration(btn) {
    document.querySelectorAll('.gl-dur-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    const dur = parseFloat(btn.dataset.dur);
    if (!isNaN(dur)) _transitionDur = dur * 1000;
    updateTransitionStatus();
}

// Update status bar with current transition settings
function updateTransitionStatus() {
    const status = document.getElementById('gl-status-trans');
    if (status) {
        const durSec = _transitionDur / 1000;
        status.textContent = `${_transitionType.toUpperCase()} · ${durSec}s`;
    }
}

// Execute TAKE transition (Preview → Program)
function executeTransition() {
    if (!_glPreview) {
        console.log('No source in preview');
        return;
    }

    const takeBtn = document.getElementById('gl-take-btn');
    if (takeBtn) {
        takeBtn.classList.add('transitioning');
        setTimeout(() => takeBtn.classList.remove('transitioning'), 300);
    }

    if (_transitionType === 'cut') {
        // Instant cut
        performCut();
    } else {
        // Animated transition
        performAnimatedTransition(_transitionType, _transitionDur);
    }
}

// Execute AUTO transition (same as TAKE but uses selected transition)
function executeAutoTransition() {
    executeTransition();
}

// Perform instant cut
function performCut() {
    const prevSource = _glPreview;
    _glProgram = prevSource;
    // Keep canvas as-is (don't clear _glPreview)

    // Update AIR monitor with new live source
    _updatePreviewMonitor();

    // Update canvas monitor (keeps current source)
    _updateProgramMonitor();

    // Update camera cards - move ON AIR badge
    document.querySelectorAll('.gl-cam-card').forEach(card => {
        card.classList.remove('gl-cam-onair', 'gl-cam-inpreview');
        const badge = card.querySelector('.gl-cam-status-badge');
        if (badge) {
            badge.classList.remove('gl-badge-onair', 'gl-badge-preview');
            badge.classList.add('gl-badge-idle');
            badge.textContent = 'READY';
        }
    });

    const newOnAir = document.querySelector(`.gl-cam-card[data-src="${prevSource}"]`);
    if (newOnAir) {
        newOnAir.classList.add('gl-cam-onair');
        const badge = newOnAir.querySelector('.gl-cam-status-badge');
        if (badge) {
            badge.classList.remove('gl-badge-idle');
            badge.classList.add('gl-badge-onair');
            badge.textContent = 'ON AIR';
        }
    }

    _updateSwitchButton();
    console.log(`CUT to ${prevSource}`);
}

// Perform animated transition (fade, wipe, etc.)
function performAnimatedTransition(type, duration) {
    // For now, just perform a cut with a delay
    // In a real implementation, this would animate between sources
    console.log(`${type.toUpperCase()} transition over ${duration}ms`);

    setTimeout(() => {
        performCut();
    }, duration / 2);
}

// Handle T-bar manual transition control
function handleTbar(value) {
    // Manual transition control
    // 0 = full preview, 100 = full program (transitioned)
    if (value >= 95) {
        performCut();
        document.getElementById('gl-tbar').value = 0;
    }
}

// Toggle collapsible panels
function toggleCollapse(panelId) {
    const panel = document.getElementById(panelId);
    if (panel) {
        panel.classList.toggle('open');
    }
}

// ============================================
// INIT
// ============================================

renderRsvpWord();


// ============================================
// THEME SYSTEM
// ============================================

let currentTheme = localStorage.getItem('theme') || 'dark';
let currentAccent = localStorage.getItem('accent') || '#C4A060';

// Initialize theme on load
(function initTheme() {
    applyTheme(currentTheme);
    applyAccent(currentAccent);
})();

function setTheme(theme) {
    currentTheme = theme;
    localStorage.setItem('theme', theme);
    applyTheme(theme);

    // Update UI
    document.querySelectorAll('.set-theme-opt').forEach((opt, i) => {
        const themes = ['dark', 'light', 'system'];
        opt.classList.toggle('active', themes[i] === theme);
        opt.querySelector('input').checked = themes[i] === theme;
    });

    markSettingsDirty();
}

function applyTheme(theme) {
    const root = document.documentElement;

    // Handle system preference
    let effectiveTheme = theme;
    if (theme === 'system') {
        effectiveTheme = window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    }

    root.setAttribute('data-theme', effectiveTheme);
    document.body.classList.toggle('theme-light', effectiveTheme === 'light');
    document.body.classList.toggle('theme-dark', effectiveTheme === 'dark');
}

function setAccent(color) {
    currentAccent = color;
    localStorage.setItem('accent', color);
    applyAccent(color);

    // Update UI
    document.querySelectorAll('.set-accent-swatch').forEach(btn => {
        btn.classList.toggle('active', btn.style.background === color || btn.style.backgroundColor === color);
    });

    markSettingsDirty();
}

function applyAccent(color) {
    const root = document.documentElement;
    root.style.setProperty('--accent-color', color);

    // Generate accent variations
    const rgb = hexToRgb(color);
    if (rgb) {
        root.style.setProperty('--accent-rgb', `${rgb.r}, ${rgb.g}, ${rgb.b}`);
    }
}

function hexToRgb(hex) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
        r: parseInt(result[1], 16),
        g: parseInt(result[2], 16),
        b: parseInt(result[3], 16)
    } : null;
}

// Listen for system theme changes
window.matchMedia('(prefers-color-scheme: light)').addEventListener('change', (e) => {
    if (currentTheme === 'system') {
        applyTheme('system');
    }
});

// ============================================
// PAGE INIT — runs LAST so all variables are declared
// ============================================
(function initPage() {
    const path = window.location.pathname;
    const page = path.split('/').pop().replace('.html','') || 'index';
    const screenId = page === 'index' ? 'library' : page;

    // Update header for current page
    updateAppHeader(screenId);

    // Golive-specific init
    if (screenId === 'golive') {
        const appHeader = document.querySelector('.app-header');
        if (appHeader) appHeader.style.display = 'none';
        _updatePreviewMonitor();
        updateAirDot();
        _initVuMeters();
    }

    // Teleprompter-specific init
    if (screenId === 'teleprompter') {
        const appHeader = document.querySelector('.app-header');
        if (appHeader) appHeader.style.display = 'none';
        resetReader();

        // Prevent vertical sliders from eating scroll events
        document.querySelectorAll('.rd-vslider').forEach(slider => {
            slider.addEventListener('wheel', (e) => {
                e.preventDefault();
                // Pass scroll to the page instead
                const stage = document.querySelector('.rd-stage');
                if (stage) stage.scrollBy(0, e.deltaY);
            }, { passive: false });
        });
    }
})();
