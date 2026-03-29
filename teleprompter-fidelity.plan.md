# Teleprompter Fidelity Plan

Reference brainstorm: `teleprompter-fidelity.brainstorm.md`

## Goal

Bring the teleprompter reading screen back to the original `new-design` HTML behavior and structure, especially phrase grouping, line wrapping, card layout, and playback interactions.

## Scope

In scope:

- phrase-aware teleprompter card generation
- reader group and pause rendering parity with `new-design`
- teleprompter-specific CSS/markup fixes required for parity
- automated regressions for the broken reading flow

Out of scope:

- editor changes unrelated to teleprompter rendering
- settings redesign
- backend or streaming changes

## Constraints And Risks

- preserve standalone WASM runtime
- preserve camera-layer behavior
- keep route and selector compatibility for Playwright
- JS reader logic depends on exact `.rd-card`, `.rd-g`, `.rd-w`, and pause structure
- fallback content without phrase metadata still needs readable grouping

## Testing Methodology

Flows covered:

- initial teleprompter render for a real sample script
- phrase-group DOM structure for active card content
- visible readable wrapping and non-concatenated text in browser
- play/pause and next/prev controls after the markup refactor

How they are tested:

- bUnit for rendered card/group/pause structure
- Playwright for the browser teleprompter route, card transitions, and visible text flow

Quality bar:

- no concatenated-word regressions
- no giant single-group sentences when phrase data exists
- teleprompter controls still work in real browser flow

## Baseline

- [ ] Run focused baseline tests before changes.
- [ ] If any existing relevant tests fail, record the symptom and fix path here.

Known failing or user-reported regressions to address:

- [ ] Teleprompter reading layout does not match `new-design` and produces broken long lines.
  - Symptom: active card text appears as oversized unwrapped runs in browser screenshots.
  - Suspected cause: teleprompter rebuilds cards from flattened block words instead of phrase-aware groups.
  - Fix status: pending

## Ordered Plan

- [ ] Inspect current teleprompter DOM and compare it with `new-design` reader markup.
  - Verification: capture the current behavior through focused tests or browser inspection.
- [ ] Add failing regression tests for phrase-group structure and browser-visible reading flow.
  - Verification: focused bUnit and Playwright runs fail before the fix.
- [ ] Refactor reader card generation to preserve phrase-aware grouping and explicit pauses.
  - Verification: new tests pass and active card markup matches the expected structural landmarks.
- [ ] Adjust teleprompter CSS or markup only where needed to restore `new-design` layout fidelity.
  - Verification: browser assertions for readable text and card state pass.
- [ ] Update durable docs if the teleprompter rendering contract changes materially.
  - Verification: `docs/Architecture.md` and feature docs reflect the runtime contract.
- [ ] Run final validation.
  - Verification:
    - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
    - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
