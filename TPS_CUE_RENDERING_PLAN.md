# TPS Cue Rendering Specification

Status: implemented and verified

## Inputs

- Canonical TPS reference: https://tps.managed-code.com/ (v1.1.0, built April 5, 2026).
- Color-emotion reference: https://link.springer.com/article/10.1186/s40359-025-03034-y for hue, saturation, brightness, warm/cool, and common emotion associations.
- Music-notation references: https://en.wikipedia.org/wiki/List_of_musical_symbols and https://www.musictheoryacademy.com/how-to-read-sheet-music/dynamics/ for slur, staccato, crescendo hairpin, breath, and dynamic cue metaphors.
- Product rule: reader output must show clean spoken text only; raw TPS tags stay invisible.
- Product rule: every supported cue needs a visible editor and reader affordance, not just parser support.
- Product rule: teleprompter reader text must never overlap, merge words, show decorative grid/ruler textures, or move the active focus-word baseline when context wraps.
- Product rule: cue-matrix screenshots must use cue-named focus words when possible so each row is self-explanatory, for example `[sad]sad[/sad]` rather than a generic styled word.
- Product rule: AI graph and assistant model access must stay on the Microsoft Agent Framework path.

## Rendering Principles

1. Stable layout beats expressive styling. Cue effects may change color, gradient, shadow, underline style, opacity, or timing, but must not collapse readable word spacing or make lines reflow while the active word is being read.
2. Speed is timing plus a bounded word-shape cue. Slow cues widen tracking and visibly expand the cue word; fast cues use small negative tracking and a pace underline so the cue word becomes visibly tighter without touching neighboring words.
3. Volume and energy may use word size, weight, opacity, and glow, but must avoid transforms that move baselines or make neighboring words collide.
4. Delivery and articulation should use music-like notation marks, not decorative cards or raw tag labels: legato uses a slur, staccato uses dots, and building uses a crescendo hairpin.
5. Emotion is a surface/context tint plus active-word accent. Emotion colors must be meaningfully distinct: red for urgency, yellow/orange for happy or energetic delivery, green/teal for focus or calm, blue for sadness or professionalism, and violet for concern or motivation. Do not recolor entire passages so strongly that text becomes hard to read.
6. Pronunciation/stress help must be visible but calm: dotted underline, tooltip/overlay in editor, and a large readable reader guide above the word when a pronunciation or phonetic cue is present.
7. Motion should explain reading flow. Card/phrase transitions can slide on the vertical axis, but individual words should not drift, jump, or animate into place after appearing.

## Cue Mapping

| TPS cue | Reader visual treatment | Editor authoring treatment | Motion/timing rule | Tests |
| --- | --- | --- | --- | --- |
| Segment/block WPM | No raw header in reader; playback timing uses effective WPM. | Monaco token + hover/intellisense, section metadata. | Changes phrase duration only. | Timing probe confirms effective WPM. |
| `[xslow]`, `[slow]` | Wider positive letter spacing, broader word-shape treatment, dotted pacing underline, active word remains readable and visibly wider than normal. | Token color + completion + hover. | Slower phrase/word duration from TPS runtime. | Letter spacing and measured width are greater than normal, with no overlap. |
| `[fast]`, `[xfast]`, `[normal]` | Fast/xfast use bounded compact tracking and a pace underline; normal resets to base spacing. | Token color + completion + hover. | Faster phrase/word duration from TPS runtime. | Fast/xfast letter spacing and measured width are lower than normal, with no overlap. |
| `/`, `//`, `[pause:...]` | Short pause: small breath dot; medium/long pause: low-contrast phrase break, no visible grid line. | Inline marker token and hover with duration. | Pause duration comes from TPS runtime. | Pause timing probe and no decorative line/grid assertion. |
| `[breath]` | Tiny breath mark, no added timing. | Token + hover. | Does not add pause duration. | Timing test distinguishes breath from pause. |
| `[loud]` | Larger word size, stronger weight, warmer active accent, no baseline scale jump. | Token + hover. | No timing change. | CSS/geometry test verifies size, weight, and stable bounds. |
| `[soft]` | Smaller, lower-opacity, lighter cool accent. | Token + hover. | No timing change. | Contrast and visibility assertion. |
| `[whisper]` | Smaller, lighter, airy italic style with dotted texture and normal readable spacing. | Token + hover. | No timing change. | No overlap and readable color assertion. |
| `[emphasis]`, `*`, `**` | Distinct editorial treatments: `[emphasis]` uses strong underline/weight, markdown bold uses heavier word shape, markdown italic uses a clearly slanted italic word shape. | Monaco markdown/TPS decorations. | No timing change. | Emphasis attribute/style tests plus no overlap. |
| `[highlight]` | Subtle translucent background behind the word, not a color-only cue. | Token + hover. | No timing change. | Highlight remains visible on dark background. |
| Inline emotions (`warm`, `urgent`, `calm`, `focused`, `professional`, `concerned`, `motivational`, `excited`, `happy`, `sad`, `energetic`, `neutral`) | Distinct active-word gradients and shadows grounded in common color-emotion associations; surrounding words remain readable. | Completion + hover + semantic color token. | Segment/block emotion changes fade surface over about 3 seconds; urgent/excited/energetic may use restrained saturation animation. | Emotion menu/cue screenshot and contrast checks. |
| `[sarcasm]` | Subtle italic/rose accent; no gimmick label. | Token + hover. | No timing change. | Cue class and active color test. |
| `[aside]` | Slightly dimmer/lower-emphasis, parenthetical feel. | Token + hover. | Often pairs with fast timing if author tagged speed; aside itself no timing change. | Cue class and opacity test. |
| `[rhetorical]` | Clear violet accent and statement-like underline, not question-mark decoration. | Token + hover. | No timing change. | Cue class test. |
| `[building]` | Crescendo-style hairpin plus progressive `--tps-build-progress` and weight/intensity across the span; phrase spans use one group-level hairpin. Avoid transform/scale that shifts lines. | Token + hover. | No timing change unless nested speed. | Later words in span have higher cue progress/weight, and phrase spans suppress word-level hairpins. |
| `[legato]` | Music-like curved slur and slightly connected visual rhythm with bounded tighter tracking that cannot merge words; phrase spans use one group-level slur when the reader group is fully legato. | Token + hover. | No timing change. | Word or group slur pseudo-element, negative tracking, and no overlap. |
| `[staccato]` | Music-like dots and crisp higher weight; use natural word gaps, not injected separators. | Token + hover. | No timing change. | Dot pseudo-element and no overlap. |
| `[energy:N]` | Energy controls glow/weight within bounded values; no scale baseline shift. Normalize with `(N - 1) / 9` so 1 is no extra intensity and 10 is full intensity. | Token + range validation hover. | No timing change. | CSS variable clamped 1-10 and style visible. |
| `[melody:N]` | Wavy underline intensity; high melody gets stronger wave, low melody stays nearly flat. Normalize with `(N - 1) / 9`. | Token + range validation hover. | No timing change. | CSS variable clamped 1-10. |
| `[phonetic:/fəˈnɛtɪk/]`, `[pronunciation:prəˌnʌnsiˈeɪʃən]` | Subtle dotted underline plus a large readable real pronunciation/IPA guide above the word, never replacing the spoken word or changing phonetic casing. | Hover/tooltip displays the guide. | No timing change. | Visible pseudo-guide, metadata attribute, minimum 24px guide font-size, and screenshot example. |
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
- Reader Playwright screenshot for the cue matrix, including one-word cue examples and short phrase-span examples.
- Geometry test that adjacent word bounding boxes never overlap for fast/xfast, underline, stress, phrase-span, and punctuation-heavy cases.
- Speed visual test that proves xslow/slow/normal/fast/xfast/explicit fast WPM map to ordered letter spacing while the cue-named focus words remain non-overlapping.
- Pronunciation visual test that proves the large readable guide is present in the rendered reader UI.
- Geometry test that active focus-word center/baseline stays stable when context wraps to one, two, and three lines.
- Visual assertion that the reader surface has no visible grid/ruler background in the prompter view.
- Timing probe that verifies TPS speed and pause tags change playback timing but breath/stress/pronunciation do not.
- Real-model smoke for graph extraction and AI Spotlight through Microsoft Agent Framework configuration loaded from ignored local appsettings.

## Verification Evidence

- README screenshots:
  - `docs/screenshots/readme/tps-editor-cues.png` (1920 x 1080)
  - `docs/screenshots/readme/tps-teleprompter-cues.png` (1366 x 768)
  - `docs/screenshots/readme/editor.png` (1440 x 1100, real-model graph canvas)
- Source browser artifacts:
  - `output/playwright/teleprompter-tps-cue-rendering/01-teleprompter-cue-rendering.png`
  - `output/playwright/teleprompter-tps-cue-rendering/02-teleprompter-cue-text.png`
  - `output/playwright/manual-real-ai-check/editor-graph-real-model-maf-canvas.png`
- Local real-model smoke completed against Azure OpenAI through the Microsoft Agent Framework path. The graph status reached `Model`, the browser sent the model request through the configured Azure OpenAI deployment, and no production graph extraction call bypassed the `ChatClientAgent` composition path.

### Visual Screenshot Audit

The latest cue-matrix audit compares related screenshots instead of treating screenshot generation as the finish line:

- `warm`, `urgent`, and `excited` now land in different hue families in the generated screenshots: amber/orange, crimson/red, and magenta/violet. A crop-based color probe measured average active-word hues around 32°, 359°, and 275°, with pairwise RGB distances of about 106, 164, and 160, so these cues are no longer near-identical tints.
- `soft` and `whisper` separate by both color and form: soft keeps a readable cool light treatment, while whisper is dimmer, italic, and dotted.
- `legato` and `staccato` separate by both color and articulation texture: legato uses teal with a curved music-like slur and tighter connected tracking, with one slur across grouped legato phrases, while staccato uses a pinker tone, dot texture, higher weight, and wider clipped tracking.
- `building` now follows a music crescendo metaphor: single-word building gets a word-level hairpin, while a two-word tagged phrase gets one wrapper-level hairpin and suppresses per-word hairpins.
- Editorial cues no longer collapse into the same underline: `[emphasis]`, markdown bold, markdown italic, and `[highlight]` carry separate metadata and reader treatments.
- `pronunciation` and `phonetic` screenshots now show large rehearsal guides above the active word, with a test floor of 24px so the guide reads as pronunciation help rather than a tiny tooltip pill.
- Speed screenshots preserve the cue-named focus words while tests compare computed tracking order and non-overlap instead of relying on generic same-word width screenshots.

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
- Later review tightened the music-notation contract: phrase-level legato and building cues must render one wrapper pseudo-element across the entire tagged phrase, while single-word cues may render word-level pseudo-elements. The tests now assert that phrase wrappers own the slur/hairpin and word pseudo-content is disabled.
