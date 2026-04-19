// Kinetic reader — JS bridge for word-envelope timing and the focus lens.
//
// Word cue motion still lives in CSS `@keyframes`, but JS owns the
// exact wall-clock duration. On every new active word we:
//   1. resolve the actual spoken duration for that cue;
//   2. write CSS variables for the kinetic/beam timing;
//   3. toggle `.rd-kinetic-active` so the CSS animation restarts cleanly.
//
// This keeps the visual envelope aligned with the reader loop without
// pushing layout-affecting work into JS.
(function () {
    const kineticReaderNamespace = "KineticReaderInterop";
    const ACTIVE_WORD_SELECTOR = ".rd-stage.rd-reading-active .rd-w.rd-now";
    const ACTIVE_WORD_CLASS = "rd-kinetic-active";
    const KINETIC_TIMING = {
        staccato: { ratio: 0.42, floor: 180, cap: 260 },
        stress:   { ratio: 0.5,  floor: 220, cap: 340 },
        loud:     { ratio: 0.7,  floor: 260, cap: 480 },
        urgent:   { ratio: 0.72, floor: 260, cap: 460 },
        energetic:{ ratio: 0.78, floor: 300, cap: 520 },
        excited:  { ratio: 0.78, floor: 300, cap: 520 },
        building: { ratio: 0.9,  floor: 340, cap: 640 },
        calm:     { ratio: 0.95, floor: 380, cap: 700 },
        legato:   { ratio: 1.0,  floor: 420, cap: 760 },
        aside:    { ratio: 0.88, floor: 340, cap: 620 },
        soft:     { ratio: 0.92, floor: 360, cap: 680 },
        whisper:  { ratio: 0.92, floor: 360, cap: 680 },
        slow:     { ratio: 0.92, floor: 360, cap: 680 },
        xslow:    { ratio: 1.0,  floor: 400, cap: 760 },
        sad:      { ratio: 0.95, floor: 380, cap: 700 }
    };
    const KINETIC_DEFAULT = { ratio: 0.82, floor: 260, cap: 560 };
    const KINETIC_PRIORITY = [
        "staccato", "stress", "loud", "urgent", "energetic", "excited",
        "building", "legato", "calm", "aside", "soft", "whisper",
        "xslow", "slow", "sad"
    ];

    //  Cue → lens transition character. Easing captures the "feel"
    //  (snap vs glide vs linear flow); the DURATION is derived from
    //  the target beat's wall-clock duration so the lens never
    //  promises a longer slide than the word / pause will last. At
    //  fast WPM the glide tightens automatically instead of breaking.
    //    ratio — portion of the target duration the glide occupies
    //    cap   — hard ceiling so slow beats don't drag the glide
    //    floor — minimum so fast beats still read as "slide"
    const LENS_CUE_CHARACTER = {
        staccato: { ratio: 0.35, floor: 120, cap: 220, easing: "cubic-bezier(.5, 0, .2, 1)" },
        xfast:    { ratio: 0.75, floor: 160, cap: 260, easing: "cubic-bezier(.3, 0, .3, 1)" },
        fast:     { ratio: 0.85, floor: 200, cap: 340, easing: "cubic-bezier(.3, 0, .3, 1)" },
        urgent:   { ratio: 0.75, floor: 200, cap: 320, easing: "cubic-bezier(.3, 0, .3, 1)" },
        legato:   { ratio: 1.1,  floor: 420, cap: 720, easing: "linear" },
        calm:     { ratio: 1.0,  floor: 380, cap: 640, easing: "cubic-bezier(.22, 1, .36, 1)" },
        soft:     { ratio: 1.0,  floor: 360, cap: 620, easing: "cubic-bezier(.22, 1, .36, 1)" },
        whisper:  { ratio: 1.0,  floor: 360, cap: 620, easing: "cubic-bezier(.22, 1, .36, 1)" },
        slow:     { ratio: 1.0,  floor: 360, cap: 620, easing: "cubic-bezier(.22, 1, .36, 1)" },
        xslow:    { ratio: 1.0,  floor: 380, cap: 680, easing: "cubic-bezier(.22, 1, .36, 1)" },
        sad:      { ratio: 1.0,  floor: 380, cap: 640, easing: "cubic-bezier(.22, 1, .36, 1)" },
        aside:    { ratio: 0.9,  floor: 300, cap: 540, easing: "cubic-bezier(.22, 1, .36, 1)" }
    };
    const LENS_CUE_DEFAULT = { ratio: 0.9, floor: 260, cap: 520, easing: "cubic-bezier(.22, 1, .36, 1)" };
    const LENS_CUE_PRIORITY = [
        "staccato", "xfast", "urgent", "legato", "xslow", "slow",
        "whisper", "soft", "calm", "sad", "fast", "aside"
    ];

    function resolveKineticTiming(cueTags) {
        if (Array.isArray(cueTags) && cueTags.length > 0) {
            for (const candidate of KINETIC_PRIORITY) {
                if (cueTags.includes(candidate) && KINETIC_TIMING[candidate]) {
                    return KINETIC_TIMING[candidate];
                }
            }
        }
        return KINETIC_DEFAULT;
    }

    function resolveKineticDuration(cueTags, durationMs, playbackRate) {
        const timing = resolveKineticTiming(cueTags);
        const safeDuration = Number(durationMs) > 0 ? Number(durationMs) : 400;
        const safePlaybackRate = Number(playbackRate) > 0 ? Number(playbackRate) : 1;
        const adjustedDuration = safeDuration / safePlaybackRate;
        const raw = Math.round(adjustedDuration * timing.ratio);
        return Math.max(timing.floor, Math.min(timing.cap, raw));
    }

    function clearWordEnvelopes() {
        for (const word of document.querySelectorAll(`.${ACTIVE_WORD_CLASS}`)) {
            if (!(word instanceof HTMLElement)) {
                continue;
            }

            word.classList.remove(ACTIVE_WORD_CLASS);
            word.style.removeProperty("--rd-kinetic-duration");
            word.style.removeProperty("--rd-beam-duration");
        }
    }

    function resolveLensCharacter(cueTags) {
        if (Array.isArray(cueTags) && cueTags.length > 0) {
            for (const candidate of LENS_CUE_PRIORITY) {
                if (cueTags.includes(candidate) && LENS_CUE_CHARACTER[candidate]) {
                    return LENS_CUE_CHARACTER[candidate];
                }
            }
        }
        return LENS_CUE_DEFAULT;
    }

    function applyCueLensTiming(lens, cueTags, wordDurationMs) {
        const character = resolveLensCharacter(cueTags);
        const safeWordMs = Number(wordDurationMs) > 0 ? Number(wordDurationMs) : 400;
        const raw = Math.round(safeWordMs * character.ratio);
        const clamped = Math.max(character.floor, Math.min(character.cap, raw));
        lens.style.setProperty("--rd-lens-duration", `${clamped}ms`);
        lens.style.setProperty("--rd-lens-easing", character.easing);
    }

    //  Position the focus lens so it covers the given target element
    //  (a word or a pause marker). The lens glides to its new target
    //  via the CSS transitions on `.rd-focus-lens`. First show snaps
    //  the lens into place (no visible slide from (0, 0)); subsequent
    //  calls glide smoothly.
    function positionFocusLensForTarget(lens, target) {
        const targetRect = target.getBoundingClientRect();
        const parent = lens.parentElement;
        if (!(parent instanceof HTMLElement) || targetRect.width === 0) {
            return;
        }
        const parentRect = parent.getBoundingClientRect();

        const paddingX = Math.max(10, targetRect.width * 0.12);
        const paddingY = Math.max(6, targetRect.height * 0.15);

        const left = targetRect.left - parentRect.left - paddingX;
        const top = targetRect.top - parentRect.top - paddingY;
        const width = targetRect.width + paddingX * 2;
        const height = targetRect.height + paddingY * 2;

        const isFirstShow = !lens.classList.contains("rd-focus-lens-active");
        if (isFirstShow) {
            const previousTransition = lens.style.transition;
            lens.style.transition = "opacity .3s ease-out";
            lens.style.transform = `translate3d(${left}px, ${top}px, 0)`;
            lens.style.width = `${width}px`;
            lens.style.height = `${height}px`;
            lens.classList.add("rd-focus-lens-active");
            //  Force a reflow so the snapped transform commits before
            //  the transition is re-enabled for future glides.
            void lens.offsetWidth;
            lens.style.transition = previousTransition;
            return;
        }

        lens.style.transform = `translate3d(${left}px, ${top}px, 0)`;
        lens.style.width = `${width}px`;
        lens.style.height = `${height}px`;
    }

    window[kineticReaderNamespace] = {
        //  Restart the active word's CSS envelope with runtime-derived
        //  timing so cue motion tracks the reader loop instead of a
        //  fixed stylesheet duration.
        activateWord(durationMs, cueTags, playbackRate) {
            const word = document.querySelector(ACTIVE_WORD_SELECTOR);
            if (!(word instanceof HTMLElement)) {
                clearWordEnvelopes();
                return;
            }

            const kineticDuration = resolveKineticDuration(cueTags, durationMs, playbackRate);
            const beamDuration = Math.max(160, Math.round(kineticDuration * 0.92));

            clearWordEnvelopes();
            word.style.setProperty("--rd-kinetic-duration", `${kineticDuration}ms`);
            word.style.setProperty("--rd-beam-duration", `${beamDuration}ms`);
            //  Reflow between remove/add guarantees the CSS animation
            //  restarts even when the same DOM node becomes active again.
            void word.offsetWidth;
            word.classList.add(ACTIVE_WORD_CLASS);
        },

        //  Fade every focus lens out. Called on playback stop / reset.
        clearAll() {
            clearWordEnvelopes();
            for (const lens of document.querySelectorAll(".rd-focus-lens")) {
                lens.classList.remove("rd-focus-lens-active");
            }
        },

        //  Slide the focus lens to the word / pause element identified
        //  by `targetId` inside the lens owned by `lensId`. `cueTags`
        //  and `targetDurationMs` tune the glide character and duration.
        positionLens(lensId, targetId, cueTags, targetDurationMs) {
            const lens = document.getElementById(lensId);
            const target = document.getElementById(targetId);
            if (!(lens instanceof HTMLElement) || !(target instanceof HTMLElement)) {
                return;
            }
            applyCueLensTiming(lens, cueTags, targetDurationMs);
            requestAnimationFrame(() => {
                positionFocusLensForTarget(lens, target);
            });
        },

        //  Hide the lens on the given card without nuking its last
        //  transform, so the next position call slides from where it
        //  already was instead of snapping from the top-left corner.
        hideLens(lensId) {
            const lens = document.getElementById(lensId);
            if (lens instanceof HTMLElement) {
                lens.classList.remove("rd-focus-lens-active");
            }
        },

        //  Force the browser to paint the current DOM state and return
        //  once it has. Used by the card-transition prepare step so the
        //  "snap to starting position with transition:none" state is
        //  committed to pixels BEFORE the transition is re-enabled.
        //  Double rAF guarantees a style recompute + an actual paint
        //  frame before the next C# continuation touches classes.
        commitFrame() {
            return new Promise(resolve => {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => resolve());
                });
            });
        }
    };
})();
