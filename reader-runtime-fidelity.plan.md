# Reader Runtime Fidelity Plan

Reference: `reader-runtime-fidelity.brainstorm.md`

## Goal

Bring `learn` and `teleprompter` runtime behavior back in line with `/Users/ksemenko/Developer/PrompterLive/new-design/index.html`.

## Scope

In scope:

- RSVP ORP centering
- Teleprompter single background camera layer
- Teleprompter readable phrase groups
- Updated automated tests

Out of scope:

- Editor runtime
- New features

## Constraints And Risks

- Keep standalone WASM workflow unchanged.
- Do not reintroduce full-page reload navigation.
- Keep runtime camera controls functional with one reader camera layer.

## Baseline

- [x] Review `docs/Architecture.md`
- [x] Review nearest local `AGENTS.md`
- [ ] Run focused baseline tests for `learn` and `teleprompter`

## Ordered Steps

- [ ] Fix test baselines so they assert reference behavior instead of the current broken behavior
  - Verification:
    - focused bUnit tests
    - focused Playwright tests

- [ ] Restore teleprompter camera scene to one background layer
  - Verification:
    - bUnit camera markup assertions
    - Playwright assertion that no overlay camera element exists

- [ ] Restore teleprompter phrase grouping to short readable groups
  - Verification:
    - bUnit phrase-group assertions
    - Playwright overflow/group-size assertions

- [ ] Restore RSVP ORP centering
  - Verification:
    - Playwright position assertions comparing ORP and center line

- [ ] Run broader validation
  - Verification:
    - `dotnet build PrompterLive.slnx`
    - relevant component tests
    - relevant UI tests
    - `dotnet format PrompterLive.slnx`

## Failing Tests Tracking

- [ ] `TeleprompterPage_UsesReferenceSizedReaderGroupsForSecurityIncident`
  - Symptom: invalid bUnit query-parameter setup
  - Fix path: navigate with `NavigationManager` before render

- [ ] `Teleprompter_UsesReferenceSizedReaderGroupsForSecurityIncident`
  - Symptom: missing Playwright assertion import
  - Fix path: add `using static Microsoft.Playwright.Assertions;`

## Done Criteria

- [ ] `learn` focus word aligns to center ORP line in browser
- [ ] `teleprompter` camera is only background under the text
- [ ] `teleprompter` no longer renders floating overlay camera box
- [ ] `teleprompter` active reader phrases are short and readable
- [ ] focused tests are green
- [ ] broader validation commands are green
