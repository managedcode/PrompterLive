// Kinetic reader — Web Animations API driven word envelopes. The reader
// timeline is authoritative: every `activateWord` call receives the exact
// wall-clock duration the word will occupy before the next advance, plus
// the TPS cue tags on that word, and plays a smooth GPU-promoted envelope
// that lasts precisely that long. No fixed CSS keyframe durations, no
// layout-changing properties, no vertical baseline motion.
//
// Composition rule: every envelope targets a disjoint property set
// (opacity, filter, text-shadow, background-size, or a specific custom
// property) so multiple envelopes can run on the same element without
// fighting the same CSS property.
(function () {
    const kineticReaderNamespace = "KineticReaderInterop";
    const reducedMotionQuery = typeof window.matchMedia === "function"
        ? window.matchMedia("(prefers-reduced-motion: reduce)")
        : null;

    const activeAnimationsByElement = new WeakMap();

    function isReducedMotion() {
        return reducedMotionQuery !== null && reducedMotionQuery.matches;
    }

    function cancelAnimationsFor(element) {
        const handles = activeAnimationsByElement.get(element);
        if (!Array.isArray(handles)) {
            return;
        }

        for (const handle of handles) {
            try {
                handle.cancel();
            } catch {
                /* detached animations throw — safe to ignore */
            }
        }
        activeAnimationsByElement.delete(element);
    }

    function pushAnimation(element, handle) {
        if (!(handle instanceof Animation)) {
            return;
        }
        let handles = activeAnimationsByElement.get(element);
        if (!Array.isArray(handles)) {
            handles = [];
            activeAnimationsByElement.set(element, handles);
        }
        handles.push(handle);
    }

    // -----------------------------------------------------------------
    //  Envelope builders — each returns a KeyframeEffect-like shape and
    //  targets a DIFFERENT property, so envelopes compose on the element.
    // -----------------------------------------------------------------

    //  Default envelope — intentionally FLAT. `--tps-kin-now` holds at
    //  1 the entire time the word is active. A ramp or bell curve on
    //  this variable creates a visible per-word glow pulsation that
    //  reads as flicker/blinking when strung together across a phrase
    //  ("блимкає на словах"). Cue-specific envelopes (loud, urgent,
    //  whisper, stress, etc.) still shape their own variables to give
    //  those words character — the base flow stays glass-smooth so
    //  plain reading is a clean travelling light, not a pulse train.
    function flowSwellKeyframes() {
        return [
            { offset: 0, "--tps-kin-now": "1" },
            { offset: 1, "--tps-kin-now": "1" }
        ];
    }

    //  Loud — warm intensity pulse that LANDS hard on word onset then
    //  sustains above baseline. Big brightness + saturate contribution
    //  makes the word feel projected. No horizontal translate — volume
    //  is communicated by weight + warm halo on rd-now, not by motion.
    function loudShoutKeyframes() {
        return [
            { offset: 0,    "--tps-kin-loud": "0" },
            { offset: 0.15, "--tps-kin-loud": "1" },
            { offset: 0.5,  "--tps-kin-loud": "0.8" },
            { offset: 1,    "--tps-kin-loud": "0.55" }
        ];
    }

    //  Soft breathe — gentle opacity + subtle glow over the word time.
    function softBreatheKeyframes() {
        return [
            { offset: 0, "--tps-kin-soft": "0.2" },
            { offset: 0.5, "--tps-kin-soft": "1" },
            { offset: 1, "--tps-kin-soft": "0.4" }
        ];
    }

    //  Whisper mist — mask-image sweep + brightness dim + a quiet
    //  leftward drift that reads like the word is being hushed away,
    //  without ever oscillating back. Drift magnitude is tiny (1.5px
    //  in CSS) so it never collides with neighbouring words.
    function whisperMistKeyframes() {
        return [
            { offset: 0,   "--tps-kin-whisper": "0",   "--tps-kin-whisper-x": "0" },
            { offset: 0.4, "--tps-kin-whisper": "1",   "--tps-kin-whisper-x": "-0.55" },
            { offset: 1,   "--tps-kin-whisper": "0.3", "--tps-kin-whisper-x": "-1" }
        ];
    }

    //  Urgent — Apple "wrong-password" style rapid shake that decays.
    //  The `--tps-kin-urgent-x` oscillates between -1..+1 and is mapped
    //  in CSS to a small `translateX` so the word flicks side to side
    //  without changing its size or pushing adjacent words. Pure
    //  horizontal motion keeps the line height and baseline stable.
    function urgentFlashKeyframes() {
        return [
            { offset: 0,    "--tps-kin-urgent": "0",    "--tps-kin-urgent-x": "0" },
            { offset: 0.07, "--tps-kin-urgent": "1",    "--tps-kin-urgent-x": "-1" },
            { offset: 0.18, "--tps-kin-urgent": "0.92", "--tps-kin-urgent-x": "1" },
            { offset: 0.3,  "--tps-kin-urgent": "0.82", "--tps-kin-urgent-x": "-0.75" },
            { offset: 0.44, "--tps-kin-urgent": "0.68", "--tps-kin-urgent-x": "0.55" },
            { offset: 0.6,  "--tps-kin-urgent": "0.5",  "--tps-kin-urgent-x": "-0.3" },
            { offset: 0.78, "--tps-kin-urgent": "0.32", "--tps-kin-urgent-x": "0.15" },
            { offset: 1,    "--tps-kin-urgent": "0.2",  "--tps-kin-urgent-x": "0" }
        ];
    }

    //  Staccato — a very short bright pulse that punctuates the word.
    //  No translateX (would read as urgency/shake). The short spike on
    //  `--tps-kin-staccato` drives a brief brightness/shadow peak in
    //  the CSS rule for `.rd-w.rd-now.tps-staccato`, giving each note
    //  a crisp articulated "tick" that matches the musical staccato
    //  convention (detached, punchy).
    function staccatoKeyframes() {
        return [
            { offset: 0,    "--tps-kin-staccato": "0" },
            { offset: 0.12, "--tps-kin-staccato": "1" },
            { offset: 0.3,  "--tps-kin-staccato": "0.5" },
            { offset: 1,    "--tps-kin-staccato": "0.1" }
        ];
    }

    //  Stress — sforzando single flick. One sharp left→right → settle.
    //  Signed `-x` variable drives translateX for punchy accent without
    //  scaling the glyph.
    function stressAccentKeyframes() {
        return [
            { offset: 0,    "--tps-kin-stress": "0",   "--tps-kin-stress-x": "0" },
            { offset: 0.08, "--tps-kin-stress": "1",   "--tps-kin-stress-x": "1" },
            { offset: 0.22, "--tps-kin-stress": "0.85","--tps-kin-stress-x": "-0.5" },
            { offset: 0.4,  "--tps-kin-stress": "0.6", "--tps-kin-stress-x": "0.2" },
            { offset: 1,    "--tps-kin-stress": "0.3", "--tps-kin-stress-x": "0" }
        ];
    }

    //  Calm float — ultra-gentle sine-like envelope for calm/aside/legato.
    //  No horizontal motion — calm/legato must read as smooth flow.
    function calmFloatKeyframes() {
        return [
            { offset: 0, "--tps-kin-calm": "0" },
            { offset: 0.5, "--tps-kin-calm": "1" },
            { offset: 1, "--tps-kin-calm": "0" }
        ];
    }

    //  Energetic — bouncy wobble: gentle left↔right oscillation through
    //  the word's duration. Slower than urgent, with fewer peaks.
    function energeticKeyframes() {
        return [
            { offset: 0,    "--tps-kin-energetic": "0",   "--tps-kin-energetic-x": "0" },
            { offset: 0.22, "--tps-kin-energetic": "0.8", "--tps-kin-energetic-x": "0.7" },
            { offset: 0.5,  "--tps-kin-energetic": "1",   "--tps-kin-energetic-x": "-0.7" },
            { offset: 0.78, "--tps-kin-energetic": "0.7", "--tps-kin-energetic-x": "0.4" },
            { offset: 1,    "--tps-kin-energetic": "0.3", "--tps-kin-energetic-x": "0" }
        ];
    }

    //  Building — monotonic 0 → 1 ramp so the accumulated glow rises as
    //  the word is said.
    function buildingKeyframes() {
        return [
            { offset: 0, "--tps-kin-building": "0" },
            { offset: 1, "--tps-kin-building": "1" }
        ];
    }

    // -----------------------------------------------------------------
    //  Envelope selection — map from cue tag → envelope builder. Every
    //  active word gets flowSwell as its baseline; extra envelopes stack
    //  via their own custom properties without conflict.
    // -----------------------------------------------------------------
    const cueEnvelopes = {
        loud: loudShoutKeyframes,
        soft: softBreatheKeyframes,
        whisper: whisperMistKeyframes,
        urgent: urgentFlashKeyframes,
        stress: stressAccentKeyframes,
        staccato: staccatoKeyframes,
        calm: calmFloatKeyframes,
        aside: calmFloatKeyframes,
        legato: calmFloatKeyframes,
        energetic: energeticKeyframes,
        excited: energeticKeyframes,
        building: buildingKeyframes
    };

    function registerCustomProperty(name) {
        if (typeof CSS === "undefined" || typeof CSS.registerProperty !== "function") {
            return;
        }
        try {
            CSS.registerProperty({
                name,
                syntax: "<number>",
                inherits: false,
                initialValue: "0"
            });
        } catch {
            /* already registered, or unsupported — safe to ignore */
        }
    }
    for (const name of [
        "--tps-kin-now",
        "--tps-kin-loud",
        "--tps-kin-soft",
        "--tps-kin-whisper",
        "--tps-kin-whisper-x",
        "--tps-kin-urgent",
        "--tps-kin-urgent-x",
        "--tps-kin-stress",
        "--tps-kin-stress-x",
        "--tps-kin-staccato",
        "--tps-kin-calm",
        "--tps-kin-energetic",
        "--tps-kin-energetic-x",
        "--tps-kin-building"
    ]) {
        registerCustomProperty(name);
    }

    function pickEnvelopes(cueTags) {
        const selected = [];
        if (!Array.isArray(cueTags)) {
            return selected;
        }
        for (const tag of cueTags) {
            if (typeof tag !== "string") {
                continue;
            }
            const builder = cueEnvelopes[tag];
            if (typeof builder === "function" && !selected.includes(builder)) {
                selected.push(builder);
            }
        }
        return selected;
    }

    function runEnvelope(element, keyframes, durationMs, extraOptions) {
        if (!Array.isArray(keyframes) || keyframes.length === 0) {
            return null;
        }
        const options = Object.assign(
            {
                //  Stretch the envelope a bit longer than the raw word
                //  duration so the tail of one word overlaps with the
                //  rise of the next. The extra window is what makes the
                //  focus glow read as a single flowing wave instead of
                //  per-word on/off blinks.
                duration: Math.max(260, Math.round(durationMs * 1.35)),
                //  Symmetric ease-in-ease-out (sine-like) so rise and
                //  decay feel organic. No hard corners.
                easing: "cubic-bezier(.45, .05, .55, .95)",
                fill: "forwards",
                iterations: 1
            },
            extraOptions || {}
        );
        try {
            const handle = element.animate(keyframes, options);
            pushAnimation(element, handle);
            return handle;
        } catch {
            return null;
        }
    }

    //  Cue → lens transition character. Easing captures the "feel"
    //  (snap vs glide vs linear flow), while the DURATION is derived
    //  from the target word's actual wall-clock duration so the lens
    //  never promises a longer slide than the word will last — at fast
    //  WPM the glide tightens automatically instead of breaking.
    //
    //  Ratio = how much of the word's duration the lens glide occupies.
    //  Cap = hard ceiling so slow words don't produce a sleepy 1 s glide.
    //  Floor = minimum so very fast words still read as "slide", not
    //  a hard cut.
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
    //  Cue priority — staccato beats default even if the word also
    //  carries a mild emotion colour.
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
        //  Fall back to the default floor when wordDurationMs isn't
        //  passed — keeps old callers working without breaking timing.
        const safeWordMs = Number(wordDurationMs) > 0 ? Number(wordDurationMs) : 400;
        const raw = Math.round(safeWordMs * character.ratio);
        const clamped = Math.max(character.floor, Math.min(character.cap, raw));
        lens.style.setProperty("--rd-lens-duration", `${clamped}ms`);
        lens.style.setProperty("--rd-lens-easing", character.easing);
    }

    //  Position the focus lens so it covers the given target element
    //  (a word or a pause marker). The lens slides smoothly to its new
    //  position/size via the CSS transitions on `.rd-focus-lens` — this
    //  is the single "slide flow" visual in the reader. No layout-
    //  affecting properties are ever set here; only transform + width +
    //  height (transform is compositor-only, width/height on an
    //  absolutely-positioned element don't affect siblings).
    //
    //  If the lens is currently hidden (first time it's being shown on
    //  this card, or after a clearAll), we snap it to the target
    //  without the CSS transition, THEN fade opacity in. Otherwise the
    //  lens would visibly slide from its last-known position (often
    //  0,0 after a card rebuild) to the new word — the user calls this
    //  the "залета-вилети промахується" overshoot.
    function positionFocusLensForTarget(lens, target) {
        const targetRect = target.getBoundingClientRect();
        const parent = lens.parentElement;
        if (!(parent instanceof HTMLElement) || targetRect.width === 0) {
            return;
        }
        const parentRect = parent.getBoundingClientRect();

        //  Extra padding around the target so the lens reads as a soft
        //  aura, not a tight marker. Bigger horizontal than vertical
        //  so short words still get a visible glow on both sides.
        const paddingX = Math.max(10, targetRect.width * 0.12);
        const paddingY = Math.max(6, targetRect.height * 0.15);

        const left = targetRect.left - parentRect.left - paddingX;
        const top = targetRect.top - parentRect.top - paddingY;
        const width = targetRect.width + paddingX * 2;
        const height = targetRect.height + paddingY * 2;

        const isFirstShow = !lens.classList.contains("rd-focus-lens-active");
        if (isFirstShow) {
            //  Suspend the transition so the lens appears AT the target,
            //  not sliding from (0, 0). Opacity still fades in via its
            //  own transition to give a gentle reveal.
            const previousTransition = lens.style.transition;
            lens.style.transition = "opacity .3s ease-out";
            lens.style.transform = `translate3d(${left}px, ${top}px, 0)`;
            lens.style.width = `${width}px`;
            lens.style.height = `${height}px`;
            lens.classList.add("rd-focus-lens-active");
            //  Force a reflow so the browser commits the snapped
            //  transform/size before the transition is re-enabled for
            //  future slides.
            void lens.offsetWidth;
            lens.style.transition = previousTransition;
            return;
        }

        lens.style.transform = `translate3d(${left}px, ${top}px, 0)`;
        lens.style.width = `${width}px`;
        lens.style.height = `${height}px`;
    }

    window[kineticReaderNamespace] = {
        //  Fire the kinetic envelope set for the current active word.
        //   durationMs   — scaled wall-clock duration the word will
        //                  occupy before the next word becomes active.
        //   cueTags      — string array of cue keys (loud, soft, …).
        //   playbackRate - float multiplier for in-flight rate changes.
        //  The active word is resolved via the `.rd-w.rd-now` class the
        //  Blazor renderer has just applied; we defer one animation
        //  frame so the class is observed after StateHasChanged paints.
        activateWord(durationMs, cueTags, playbackRate) {
            if (isReducedMotion()) {
                return;
            }
            const safeDuration = Number(durationMs) > 0 ? Number(durationMs) : 320;
            const safeRate = Number(playbackRate) > 0 ? Number(playbackRate) : 1;
            const envelopes = pickEnvelopes(cueTags);

            requestAnimationFrame(() => {
                const element = document.querySelector(".rd-w.rd-now");
                if (!(element instanceof HTMLElement)) {
                    return;
                }

                cancelAnimationsFor(element);

                //  Base envelope: every active word swells and settles
                //  over its real reading duration. Keyframe offsets are
                //  proportional (0..1) so WAAPI scales them to the word's
                //  actual duration — the builder takes no argument.
                runEnvelope(element, flowSwellKeyframes(), safeDuration, { playbackRate: safeRate });

                //  Cue-specific envelopes layer on top via independent CSS
                //  custom properties, so they compose without overwriting.
                for (const builder of envelopes) {
                    runEnvelope(element, builder(), safeDuration, { playbackRate: safeRate });
                }
            });
        },

        //  Cancel every active-word animation. Called on stop / reset.
        clearAll() {
            // WeakMap has no iteration — we rely on the fact that any DOM
            // element that is detached will have its entry GC'd. For an
            // explicit stop we sweep the visible reader tree.
            const live = document.querySelectorAll(".rd-w.rd-now");
            for (const element of live) {
                cancelAnimationsFor(element);
            }
            //  Fade out every lens so the warm glow disappears with the
            //  reader playback stop. CSS opacity transition handles the
            //  fade — transform/width stay at their last slide position
            //  so resuming picks up without a visual jump.
            for (const lens of document.querySelectorAll(".rd-focus-lens")) {
                lens.classList.remove("rd-focus-lens-active");
            }
        },

        //  Slide the focus lens to the word / pause element identified by
        //  `targetId` inside the lens owned by `lensId`. `cueTags` is the
        //  cue class list for the incoming target so the lens transition
        //  timing matches cue psychology (staccato snaps, legato glides).
        //  `targetDurationMs` is the target beat's wall-clock duration
        //  (scaled for the current playback speed) so the lens glide
        //  ratio-sizes itself against real reading pace instead of a
        //  fixed duration — prevents overshoot/undershoot at fast or
        //  slow WPM.
        //  Called from ActivateReaderWordAsync and from the pause-beat
        //  transition, so the lens always sits on the current "beat".
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

        //  Hide the lens on the given card without nuking its last known
        //  transform, so the NEXT position call slides from where it
        //  already was instead of snapping in from the top-left corner.
        hideLens(lensId) {
            const lens = document.getElementById(lensId);
            if (lens instanceof HTMLElement) {
                lens.classList.remove("rd-focus-lens-active");
            }
        },

        //  Force the browser to paint the current DOM state and return
        //  once it has. Used by the card-transition prepare step so the
        //  "snap to starting position with transition:none" state is
        //  actually committed to pixels BEFORE the transition is
        //  re-enabled and the card's class swap triggers the animation.
        //  Without this, wrap-around playback (last card → card 0) saw
        //  the incoming card interpolate from its PREVIOUS position
        //  (above, as rd-card-prev) all the way down to 0 — i.e. the
        //  card descended from the top instead of rising from below.
        //  Double rAF guarantees both (a) the style recompute from the
        //  snap class change commits, and (b) one frame actually paints
        //  before the next C# continuation modifies classes again.
        commitFrame() {
            return new Promise(resolve => {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => resolve());
                });
            });
        }

        //  Runtime speed change — retarget every in-flight animation so
        //  it finishes at the new wall-clock deadline without restarting.
        setPlaybackRate(rate) {
            const safeRate = Number(rate) > 0 ? Number(rate) : 1;
            const live = document.querySelectorAll(".rd-w.rd-now");
            for (const element of live) {
                const handles = activeAnimationsByElement.get(element);
                if (!Array.isArray(handles)) {
                    continue;
                }
                for (const handle of handles) {
                    try {
                        handle.updatePlaybackRate(safeRate);
                    } catch {
                        /* cancelled animations throw — safe to ignore */
                    }
                }
            }
        }
    };
})();
