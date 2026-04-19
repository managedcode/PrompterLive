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

    //  Default envelope — a breathing swell that drives both the text
    //  glow and the composite scale transform on the active word. Starts
    //  already a third lit (0.35) so the word does not "flash" on from
    //  zero, peaks at 1 just past mid-word, and settles at 0.45 so the
    //  tail still reads as "warm" while the next word rises. The CSS
    //  rule for `.rd-w.rd-now` reads --tps-kin-now to drive scale,
    //  filter, and text-shadow in sync so the operator sees the word
    //  actually breathe, not just shimmer.
    function flowSwellKeyframes() {
        return [
            { offset: 0,    "--tps-kin-now": "0.35" },
            { offset: 0.55, "--tps-kin-now": "1" },
            { offset: 1,    "--tps-kin-now": "0.45" }
        ];
    }

    //  Loud / sforzando — a front-loaded brightness+saturate spike that
    //  reads like a shout landing, then decays over the rest of the word.
    //  filter values use a separate per-word CSS variable so they stack
    //  with flow-swell without overwriting it.
    function loudShoutKeyframes() {
        return [
            { offset: 0, "--tps-kin-loud": "0" },
            { offset: 0.18, "--tps-kin-loud": "1" },
            { offset: 1, "--tps-kin-loud": "0.2" }
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

    //  Whisper mist — mask-image sweep + brightness dim.
    function whisperMistKeyframes() {
        return [
            { offset: 0, "--tps-kin-whisper": "0" },
            { offset: 0.4, "--tps-kin-whisper": "1" },
            { offset: 1, "--tps-kin-whisper": "0.3" }
        ];
    }

    //  Urgent — fast front-loaded flash driven by its own variable.
    function urgentFlashKeyframes() {
        return [
            { offset: 0, "--tps-kin-urgent": "0" },
            { offset: 0.15, "--tps-kin-urgent": "1" },
            { offset: 1, "--tps-kin-urgent": "0.2" }
        ];
    }

    //  Stress accent — a strong downbeat feel concentrated in the first
    //  third of the word, mapped to its own variable for composition.
    function stressAccentKeyframes() {
        return [
            { offset: 0, "--tps-kin-stress": "0" },
            { offset: 0.25, "--tps-kin-stress": "1" },
            { offset: 1, "--tps-kin-stress": "0.3" }
        ];
    }

    //  Calm float — ultra-gentle sine-like envelope for calm/aside/legato.
    function calmFloatKeyframes() {
        return [
            { offset: 0, "--tps-kin-calm": "0" },
            { offset: 0.5, "--tps-kin-calm": "1" },
            { offset: 1, "--tps-kin-calm": "0" }
        ];
    }

    //  Energetic — mid-loaded swell, driven by its own variable.
    function energeticKeyframes() {
        return [
            { offset: 0, "--tps-kin-energetic": "0" },
            { offset: 0.5, "--tps-kin-energetic": "1" },
            { offset: 1, "--tps-kin-energetic": "0.3" }
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
        "--tps-kin-urgent",
        "--tps-kin-stress",
        "--tps-kin-calm",
        "--tps-kin-energetic",
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
                //  over its real reading duration. This is the water-flow
                //  feel the operator reads as "alive".
                runEnvelope(element, flowSwellKeyframes(safeDuration), safeDuration, { playbackRate: safeRate });

                //  Cue-specific envelopes layer on top via independent CSS
                //  custom properties, so they compose without overwriting.
                for (const builder of envelopes) {
                    runEnvelope(element, builder(safeDuration), safeDuration, { playbackRate: safeRate });
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
        },

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
