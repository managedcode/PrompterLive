# TPS Cue Screenshot Opus Review

## Scope

External review via `claude --model opus` inspected all 52 README TPS cue PNGs before the final polish pass.

## Findings Used For Remediation

- `07-speed-xslow`, `08-speed-slow`, and `48-phrase-speed-slow` used overly wide tracking that made the cue words read as separated letters.
- `04-pause-500ms` and `05-pause-1s` exposed slug-like words (`pause500`, `pause1s`) instead of reader-friendly text.
- `23-emotion-professional` was too close to baseline text.
- `24-emotion-focused` and `34-contour-energy` were too close in the green/teal family.
- `28-delivery-aside` and `29-delivery-rhetorical` needed clearer separation.
- `40-guide-pronunciation` and `41-guide-phonetic` used the same visual guide treatment.
- `42-guide-stress` had a stress mark that was too small at README scale.
- `45-edit-point-high` looked like a square/missing-glyph marker.

## Explicit Non-Fix

- `47-metadata-archetype` remains structurally meaningful rather than decorative, because root `AGENTS.md` says archetype cues must not create reader-facing decorative word styles.

## Final Follow-Up

- A post-fix Opus review reported `51/52 PASS` and called out `27-emotion-neutral.png`; that card was the only remaining blocker across the Opus pass.
- The neutral screenshot was adjusted after that review by removing the warm block context, raising contrast, and regenerating the full matrix.
