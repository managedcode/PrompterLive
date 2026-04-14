# TPS Cue Rendering Plan

Status: draft before implementation

## Inputs

- Canonical TPS reference: https://tps.managed-code.com/ (v1.1.0, built April 5, 2026).
- Product rule: reader output must show clean spoken text only; raw TPS tags stay invisible.
- Product rule: every supported cue needs a visible editor and reader affordance, not just parser support.
- Product rule: teleprompter reader text must never overlap, merge words, show decorative grid/ruler textures, or move the active focus-word baseline when context wraps.
- Product rule: AI graph and assistant model access must stay on the Microsoft Agent Framework path.

## Rendering Principles

1. Stable layout beats expressive styling. Cue effects may change color, weight, underline style, opacity, or timing, but must not collapse readable word spacing or make lines reflow while the active word is being read.
2. Speed is primarily timing. Slow cues may widen spacing because that improves scanability; fast cues must not use negative spacing in the prompter because it can visually merge adjacent words.
3. Volume and energy may use weight and opacity. Avoid large transforms that move baselines or change neighboring line geometry.
4. Delivery and articulation should use underlines, separator rhythm, and subtle motion, not decorative cards or raw tag labels.
5. Emotion is a surface/context tint plus active-word accent. Do not recolor entire passages so strongly that text becomes hard to read.
6. Pronunciation/stress help must be available but calm: dotted underline, tooltip/overlay in editor, subtle in-reader hint only when useful.
7. Motion should explain reading flow. Card/phrase transitions can slide on the vertical axis, but individual words should not drift, jump, or animate into place after appearing.

## Cue Mapping

| TPS cue | Reader visual treatment | Editor authoring treatment | Motion/timing rule | Tests |
| --- | --- | --- | --- | --- |
| Segment/block WPM | No raw header in reader; playback timing uses effective WPM. | Monaco token + hover/intellisense, section metadata. | Changes phrase duration only. | Timing probe confirms effective WPM. |
| `[xslow]`, `[slow]` | Wider positive letter spacing, dotted pacing underline, active word remains readable. | Token color + completion + hover. | Slower phrase/word duration from TPS runtime. | Letter spacing is positive and no overlap. |
| `[fast]`, `[xfast]`, `[normal]` | Fast/xfast keep normal non-negative spacing; xfast may dim slightly to signal skimming. | Token color + completion + hover. | Faster phrase/word duration from TPS runtime. | Letter spacing >= 0 and no overlap. |
| `/`, `//`, `[pause:...]` | Short pause: small breath dot; medium/long pause: low-contrast phrase break, no visible grid line. | Inline marker token and hover with duration. | Pause duration comes from TPS runtime. | Pause timing probe and no decorative line/grid assertion. |
| `[breath]` | Tiny breath mark, no added timing. | Token + hover. | Does not add pause duration. | Timing test distinguishes breath from pause. |
| `[loud]` | Stronger weight, warmer active accent, no baseline scale jump. | Token + hover. | No timing change. | CSS/geometry test verifies stable bounds. |
| `[soft]` | Lower opacity, lighter cool accent. | Token + hover. | No timing change. | Contrast and visibility assertion. |
| `[whisper]` | Lighter italic/dim style with normal readable spacing. | Token + hover. | No timing change. | No overlap and readable color assertion. |
| `[emphasis]`, `*`, `**` | Underline/bold/strong active treatment using existing emphasis hierarchy. | Monaco markdown/TPS decorations. | No timing change. | Existing cue rendering plus no overlap. |
| `[highlight]` | Subtle translucent background behind the word, not a color-only cue. | Token + hover. | No timing change. | Highlight remains visible on dark background. |
| Inline emotions (`warm`, `urgent`, `calm`, `focused`, `professional`, `concerned`, `motivational`, `excited`, `happy`, `sad`, `energetic`, `neutral`) | Active-word accent and reader surface tint; surrounding words remain readable. | Completion + hover + semantic color token. | Segment/block emotion changes fade surface over about 3 seconds; inline emotion changes are immediate but calm. | Emotion menu/cue screenshot and contrast checks. |
| `[sarcasm]` | Subtle italic/rose accent; no gimmick label. | Token + hover. | No timing change. | Cue class and active color test. |
| `[aside]` | Slightly dimmer/lower-emphasis, parenthetical feel. | Token + hover. | Often pairs with fast timing if author tagged speed; aside itself no timing change. | Cue class and opacity test. |
| `[rhetorical]` | Clear violet accent and statement-like underline, not question-mark decoration. | Token + hover. | No timing change. | Cue class test. |
| `[building]` | Crescendo by progressive `--tps-build-progress` and weight/intensity across the span; avoid transform/scale that shifts lines. | Token + hover. | No timing change unless nested speed. | Later words in span have higher cue progress/weight. |
| `[legato]` | Smooth/wavy underline and slightly connected visual rhythm without negative spacing. | Token + hover. | No timing change. | Underline style and no overlap. |
| `[staccato]` | Dotted underline and crisp higher weight; use natural word gaps, not injected separators. | Token + hover. | No timing change. | Dotted underline and no overlap. |
| `[energy:N]` | Energy controls glow/weight within bounded values; no scale baseline shift. Normalize with `(N - 1) / 9` so 1 is no extra intensity and 10 is full intensity. | Token + range validation hover. | No timing change. | CSS variable clamped 1-10 and style visible. |
| `[melody:N]` | Wavy underline intensity; high melody gets stronger wave, low melody stays nearly flat. Normalize with `(N - 1) / 9`. | Token + range validation hover. | No timing change. | CSS variable clamped 1-10. |
| `[phonetic:IPA]`, `[pronunciation:guide]` | Subtle dotted underline; optional tooltip/assistive overlay, never replacing spoken word. | Hover/tooltip displays the guide. | No timing change. | Tooltip/attribute and screenshot example. |
| `[stress]`, `[stress:guide]` | Stressed syllable/word gets clear underline/weight; guide stays tooltip-like. | Hover/tooltip shows guide. | No timing change. | Stress style and guide metadata test. |
| `[edit_point]`, `[edit_point:medium/high]` | Not spoken; reader can show only a non-disruptive operator marker or omit from live text. | Editor marker with priority. | No timing change. | Edit marker not rendered as spoken word. |
| `Archetype:*`, `Speaker:*` | Reader metadata only; can influence validation and optional chrome, not per-word raw nodes. | Section metadata + diagnostics. | Archetype recommended WPM only when no explicit WPM. | Graph/readable metadata test. |

## Animation Contract

- Card transitions use a vertical slide with easing that starts fast and settles gently.
- Active reader words do not translate, scale, or animate their baseline; only card shells and pre-aligned cluster text may slide.
- Building/energy cues are represented through bounded weight, color, underline, and text-shadow, not layout-affecting transforms.
- Previous/next cards may fade and slide, but active card content must be pinned to a stable reading line.
- Backward block navigation uses the same axis in reverse so returning content comes from the previous reading direction, not a random crossfade.
- The active focus word y-position must stay within a small tolerance across one-line, two-line, and three-line context layouts.

## Verification Plan

- Component tests for TPS cue class/style mapping and speed-derived spacing.
- Reader Playwright screenshot for the cue matrix.
- Geometry test that adjacent word bounding boxes never overlap for fast/xfast, underline, stress, and punctuation-heavy cases.
- Geometry test that active focus-word center/baseline stays stable when context wraps to one, two, and three lines.
- Visual assertion that the reader surface has no visible grid/ruler background in the prompter view.
- Timing probe that verifies TPS speed and pause tags change playback timing but breath/stress/pronunciation do not.
- Real-model smoke for graph extraction and AI Spotlight through Microsoft Agent Framework configuration loaded from ignored local appsettings.

## Claude Review

Captured before implementation and folded into the mapping above:

- Add explicit slow/normal cue handling so `[slow]` does not rely only on `[xslow]` styling, while `[normal]` resets spacing to normal when a source cue produces a normal effective WPM.
- Use a numeric building progress variable instead of transform scale; progressive weight and glow make `[building]` visible without moving baselines.
- Normalize `[energy:N]` and `[melody:N]` with `(N - 1) / 9`, not `N / 10`, so level 1 means no extra intensity and level 10 means full intensity.
- Keep highlight at a visible opacity floor around `0.18` so it remains legible on dark reader backgrounds.
- Render `[whisper]` as italic and subdued, `[loud]` as stronger weight, and avoid geometric scaling for both.
- Treat `[breath]` as a zero-duration breath glyph, not a hidden pause.
- Keep `[edit_point:*]` as an editor/operator marker and never as spoken reader text.
- Preserve speaker/archetype as section metadata or optional chrome, not raw inline reader tokens.
