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
    const KINETIC_DEFAULT = { ratio: 0.82, floor: 260, cap: 560 };
    const BEAM_DEFAULT = { ratio: 0.88, floor: 220, cap: 520, easing: "cubic-bezier(.28, .12, .22, 1)" };
    const LENS_CUE_DEFAULT = { ratio: 0.9, floor: 260, cap: 520, easing: "cubic-bezier(.22, 1, .36, 1)" };

    //  Keep cue-name ownership in one ordered registry instead of
    //  parallel raw-string maps and priority arrays. The first
    //  matching profile wins, so ordering here is intentional.
    //
    //  Cue → lens transition character. Easing captures the "feel"
    //  (snap vs glide vs linear flow); the duration is still derived
    //  from the target beat's wall-clock duration so the lens never
    //  promises a longer slide than the word / pause will last.
    const CUE_MOTION_PROFILES = [
        {
            cue: "staccato",
            kinetic: { ratio: 0.42, floor: 180, cap: 260 },
            beam: { ratio: 0.46, floor: 140, cap: 220, easing: "cubic-bezier(.5, 0, .24, 1)" },
            lens: { ratio: 0.35, floor: 120, cap: 220, easing: "cubic-bezier(.5, 0, .2, 1)" }
        },
        { cue: "stress", kinetic: { ratio: 0.5, floor: 220, cap: 340 } },
        {
            cue: "loud",
            kinetic: { ratio: 0.7, floor: 260, cap: 480 },
            beam: { ratio: 0.72, floor: 220, cap: 340, easing: "cubic-bezier(.36, 0, .22, 1)" }
        },
        {
            cue: "urgent",
            kinetic: { ratio: 0.72, floor: 260, cap: 460 },
            beam: { ratio: 0.64, floor: 180, cap: 300, easing: "cubic-bezier(.42, 0, .24, 1)" },
            lens: { ratio: 0.75, floor: 200, cap: 320, easing: "cubic-bezier(.3, 0, .3, 1)" }
        },
        { cue: "energetic", kinetic: { ratio: 0.78, floor: 300, cap: 520 } },
        { cue: "excited", kinetic: { ratio: 0.78, floor: 300, cap: 520 } },
        {
            cue: "building",
            kinetic: { ratio: 0.9, floor: 340, cap: 640 },
            beam: { ratio: 0.94, floor: 280, cap: 440, easing: "cubic-bezier(.24, .08, .18, 1)" }
        },
        {
            cue: "legato",
            kinetic: { ratio: 1.0, floor: 420, cap: 760 },
            beam: { ratio: 1.02, floor: 320, cap: 620, easing: "linear" },
            lens: { ratio: 1.1, floor: 420, cap: 720, easing: "linear" }
        },
        {
            cue: "calm",
            kinetic: { ratio: 0.95, floor: 380, cap: 700 },
            beam: { ratio: 0.98, floor: 300, cap: 620, easing: "cubic-bezier(.2, .12, .12, 1)" },
            lens: { ratio: 1.0, floor: 380, cap: 640, easing: "cubic-bezier(.22, 1, .36, 1)" }
        },
        {
            cue: "aside",
            kinetic: { ratio: 0.88, floor: 340, cap: 620 },
            beam: { ratio: 0.84, floor: 260, cap: 520, easing: "cubic-bezier(.26, .12, .18, 1)" },
            lens: { ratio: 0.9, floor: 300, cap: 540, easing: "cubic-bezier(.22, 1, .36, 1)" }
        },
        {
            cue: "soft",
            kinetic: { ratio: 0.92, floor: 360, cap: 680 },
            beam: { ratio: 0.98, floor: 300, cap: 600, easing: "cubic-bezier(.2, .12, .12, 1)" },
            lens: { ratio: 1.0, floor: 360, cap: 620, easing: "cubic-bezier(.22, 1, .36, 1)" }
        },
        {
            cue: "whisper",
            kinetic: { ratio: 0.92, floor: 360, cap: 680 },
            beam: { ratio: 0.98, floor: 300, cap: 600, easing: "cubic-bezier(.2, .12, .12, 1)" },
            lens: { ratio: 1.0, floor: 360, cap: 620, easing: "cubic-bezier(.22, 1, .36, 1)" }
        },
        {
            cue: "xslow",
            kinetic: { ratio: 1.0, floor: 400, cap: 760 },
            beam: { ratio: 1.0, floor: 320, cap: 620, easing: "cubic-bezier(.22, .12, .16, 1)" },
            lens: { ratio: 1.0, floor: 380, cap: 680, easing: "cubic-bezier(.22, 1, .36, 1)" }
        },
        {
            cue: "slow",
            kinetic: { ratio: 0.92, floor: 360, cap: 680 },
            beam: { ratio: 0.96, floor: 300, cap: 600, easing: "cubic-bezier(.22, .12, .16, 1)" },
            lens: { ratio: 1.0, floor: 360, cap: 620, easing: "cubic-bezier(.22, 1, .36, 1)" }
        },
        {
            cue: "sad",
            kinetic: { ratio: 0.95, floor: 380, cap: 700 },
            beam: { ratio: 0.98, floor: 300, cap: 620, easing: "cubic-bezier(.22, .12, .16, 1)" },
            lens: { ratio: 1.0, floor: 380, cap: 640, easing: "cubic-bezier(.22, 1, .36, 1)" }
        },
        {
            cue: "xfast",
            beam: { ratio: 0.6, floor: 160, cap: 240, easing: "cubic-bezier(.5, 0, .28, 1)" },
            lens: { ratio: 0.75, floor: 160, cap: 260, easing: "cubic-bezier(.3, 0, .3, 1)" }
        },
        {
            cue: "fast",
            beam: { ratio: 0.72, floor: 180, cap: 300, easing: "cubic-bezier(.42, 0, .26, 1)" },
            lens: { ratio: 0.85, floor: 200, cap: 340, easing: "cubic-bezier(.3, 0, .3, 1)" }
        }
    ];

    function resolveCueMotion(cueTags, key, fallback) {
        if (Array.isArray(cueTags) && cueTags.length > 0) {
            for (const profile of CUE_MOTION_PROFILES) {
                if (cueTags.includes(profile.cue) && profile[key]) {
                    return profile[key];
                }
            }
        }

        return fallback;
    }

    function deriveMotionDuration(durationMs, timing) {
        const safeDuration = Number(durationMs) > 0 ? Number(durationMs) : 400;
        const raw = Math.round(safeDuration * timing.ratio);
        return Math.max(timing.floor, Math.min(timing.cap, raw));
    }

    function resolveBeatDuration(durationMs, playbackRate) {
        const safeDuration = Number(durationMs) > 0 ? Number(durationMs) : 400;
        const safePlaybackRate = Number(playbackRate) > 0 ? Number(playbackRate) : 1;
        return safeDuration / safePlaybackRate;
    }

    function resolveKineticTiming(cueTags) {
        return resolveCueMotion(cueTags, "kinetic", KINETIC_DEFAULT);
    }

    function resolveBeamTiming(cueTags) {
        return resolveCueMotion(cueTags, "beam", BEAM_DEFAULT);
    }

    function resolveKineticDuration(cueTags, durationMs, playbackRate) {
        return deriveMotionDuration(resolveBeatDuration(durationMs, playbackRate), resolveKineticTiming(cueTags));
    }

    function resolveBeamDuration(cueTags, durationMs, playbackRate) {
        return deriveMotionDuration(resolveBeatDuration(durationMs, playbackRate), resolveBeamTiming(cueTags));
    }

    function clearWordEnvelopes() {
        for (const word of document.querySelectorAll(`.${ACTIVE_WORD_CLASS}`)) {
            if (!(word instanceof HTMLElement)) {
                continue;
            }

            word.classList.remove(ACTIVE_WORD_CLASS);
            word.style.removeProperty("--rd-kinetic-duration");
            word.style.removeProperty("--rd-beam-duration");
            word.style.removeProperty("--rd-beam-easing");
        }
    }

    function resolveLensCharacter(cueTags) {
        return resolveCueMotion(cueTags, "lens", LENS_CUE_DEFAULT);
    }

    function applyCueLensTiming(lens, cueTags, wordDurationMs) {
        const character = resolveLensCharacter(cueTags);
        const clamped = deriveMotionDuration(wordDurationMs, character);
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

        const paddingX = Math.max(8, targetRect.width * 0.09);
        const paddingY = Math.max(5, targetRect.height * 0.12);

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
            const beamTiming = resolveBeamTiming(cueTags);
            const beamDuration = resolveBeamDuration(cueTags, durationMs, playbackRate);

            clearWordEnvelopes();
            word.style.setProperty("--rd-kinetic-duration", `${kineticDuration}ms`);
            word.style.setProperty("--rd-beam-duration", `${beamDuration}ms`);
            word.style.setProperty("--rd-beam-easing", beamTiming.easing);
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
