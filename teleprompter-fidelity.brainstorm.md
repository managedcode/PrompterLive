# Teleprompter Fidelity Brainstorm

## Problem

The current teleprompter reading screen drifts from `new-design/index.html` in behavior and layout.

Observed issues:

- phrase groups are too large, so lines do not wrap like the reference markup
- runtime reader cards are built from flattened block content instead of phrase-aware data
- card transitions and centered-reading layout do not match the original immersive reader
- existing tests only check for coarse readable spacing, not the actual reference structure

## Constraints

- keep the runtime standalone WASM only
- preserve current camera/background support
- keep `data-testid` selectors stable
- do not introduce backend code or extra dependencies
- use the existing `new-design` HTML, CSS, and JS as the source of truth

## Options

### Option 1: Patch CSS only

Adjust `rd-g` and `rd-cluster-text` styles to force wrapping.

Why not enough:

- the main issue is data shape, not only CSS
- flattened phrase groups still break active-word centering and card rhythm

### Option 2: Keep current word pipeline, add heuristic line breaks in Razor

Manually split compiled words into smaller groups inside `TeleprompterPage`.

Pros:

- localized change

Cons:

- duplicates phrase logic that already exists in `Core`
- easy to drift again from RSVP and parser behavior

### Option 3: Make teleprompter phrase-aware using existing parsed and RSVP grouping logic

Use structured phrase/block data when building reader cards, preserve explicit pauses, and only fall back to heuristic grouping when phrase data is missing.

Pros:

- aligned with `new-design` intent
- reuses existing domain logic
- supports realistic `rd-g` clusters for wrapping and highlighting
- easier to regression-test

Cons:

- requires a deeper refactor in `TeleprompterPage`
- needs stronger UI tests

## Recommended Direction

Use Option 3.

Implementation direction:

- rebuild reader-card generation around block phrases instead of flattened block content
- keep compiled formatting per phrase/word so TPS classes still render correctly
- preserve explicit pauses as standalone chunks between phrase groups
- tighten bUnit tests to assert card/group structure
- add browser tests that verify line wrapping, card state classes, and active-reader flow on a real teleprompter route

## Risks

- phrase-to-word timing can drift if compiled durations are not preserved
- card transitions can regress if JS expects specific DOM structure
- fallback scripts without phrases still need a safe grouping path
