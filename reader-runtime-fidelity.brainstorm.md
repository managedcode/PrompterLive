# Reader Runtime Fidelity Brainstorm

## Problem

`learn` and `teleprompter` diverged from `/Users/ksemenenko/Developer/PrompterLive/new-design/index.html`.

Observed runtime regressions:

- RSVP focus word is not centered on the ORP line.
- Teleprompter renders overlay camera boxes that do not exist in the reference design.
- Teleprompter groups too many words into single phrase clusters, causing unreadable long lines.

## In Scope

- Restore `learn` RSVP layout behavior to centered ORP playback.
- Restore `teleprompter` camera behavior to a single background camera under text.
- Restore teleprompter phrase grouping closer to the reference design.
- Add browser regressions for RSVP centering and teleprompter camera/background behavior.

## Out Of Scope

- Broader editor fixes.
- New media backends.
- Native/hybrid hosts.

## Options

### Option A: Patch CSS only

Pros:

- Smallest diff.

Cons:

- Does not fix the incorrect RSVP runtime transform logic.
- Does not remove extra teleprompter camera DOM/runtime behavior.

### Option B: Restore runtime contracts to match `new-design`

Pros:

- Fixes actual behavior, not just visuals.
- Allows tests to verify the reference layout directly.

Cons:

- Touches both Razor and JS.

## Recommended Direction

Option B.

1. Remove teleprompter overlay camera rendering and keep only one primary background camera.
2. Change teleprompter phrase grouping to break on pauses, punctuation, and short phrase limits.
3. Fix RSVP centering by aligning ORP inside the focus-word container instead of shifting against the whole row.
4. Add browser assertions for ORP centering and no overlay camera boxes.
