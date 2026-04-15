# TPS Cue Screenshot Claude Review

## Scope

Second-pass review via `claude --model sonnet` inspected all 52 README TPS cue PNGs before the final polish pass.

## Findings Used For Remediation

- Speed cues were the highest-impact issue: `xslow`, `slow`, and phrase slow looked stretched and distracting.
- `xfast` needed a stronger visible speed cue at README scale.
- `27-emotion-neutral` needed recapture consistency with the rest of the full-card screenshot set.
- `28-delivery-aside` versus `29-delivery-rhetorical`, `34-contour-energy` versus `35-contour-melody`, and `40-guide-pronunciation` versus `41-guide-phonetic` needed stronger visual separation.
- `45-edit-point-high` needed to avoid a square marker that could read as a broken glyph.
- `42-guide-stress` needed a larger stress accent and clearer dedicated stress stroke.

## Remediation Direction

The final pass tightens slow tracking, strengthens xfast, recaptures all screenshots, separates guide and contour families, and replaces the high edit marker with a multi-bar CSS marker.

## Final Follow-Up

- A post-fix Sonnet review reported `51/52 PASS` and one remaining blocker on `27-emotion-neutral.png`.
- The neutral cue was then raised to a higher-contrast silver treatment, moved out of the warm block context, and the full 52-card matrix was regenerated again.
- Local visual inspection confirmed the final neutral screenshot is full-card, full-resolution, and readable.
