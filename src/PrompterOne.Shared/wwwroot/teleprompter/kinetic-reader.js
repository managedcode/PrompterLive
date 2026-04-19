// Kinetic reader — thin JS bridge used by Blazor playback.
//
// All cue kinetic animations (loud, soft, whisper, urgent, stress,
// staccato, energetic, excited, building, calm, legato, aside) are
// pure CSS `@keyframes` defined on `.rd-stage.rd-reading-active
// .rd-w.rd-now.tps-*` in 10-reading-states.css. CSS fires them
// automatically on class match, every engine repaints. No WAAPI,
// no `@property` custom-property interpolation, no hacks.
//
// This module is now only responsible for the focus lens (the soft
// warm aura that glides BETWEEN words) and the `commitFrame()` helper
// used by card transitions to force a paint between snap and animate.
(function () {
    const kineticReaderNamespace = "KineticReaderInterop";

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
        //  Kept as an API surface for `ActivateReaderWordAsync` in C#.
        //  Cue kinetics are CSS-only now, so this is a no-op beyond
        //  giving the C# side a single awaitable round-trip for the
        //  word-activation lifecycle.
        activateWord() {
            /* CSS @keyframes handles it */
        },

        //  Fade every focus lens out. Called on playback stop / reset.
        clearAll() {
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
