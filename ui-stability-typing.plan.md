# UI Stability And Typing Plan

Chosen brainstorm: `ui-stability-typing.brainstorm.md`

## Goal

Restore real editor typing, remove non-design editor chrome, repair learn and teleprompter layout parity, and finalize logging/error handling without introducing backend behavior.

## Scope

In scope:
- editor input/focus/caret stability
- editor left sidebar parity with `new-design`
- learn and teleprompter layout fixes
- `ILogger` and routed UI diagnostics
- short targeted tests for each repaired flow

Out of scope:
- backend work
- hybrid/native host work
- unrelated feature expansion

## Constraints And Risks

- runtime stays standalone WASM only
- `new-design/` remains the visual source of truth
- tests must stay short and targeted during iteration
- if selection sync is wrong, typing, floating toolbar, and undo/redo can all regress together

## Baseline

- [x] Read `docs/Architecture.md`
- [x] Read nearest local `AGENTS.md` files
- [x] Run focused reproduction checks for editor, learn, and teleprompter
- [x] Record any failing tests before fixes

Known failing or broken flows to address:
- [x] Manual editor typing in the real browser is broken
  - symptom: user cannot type into the editor control
  - suspected cause: selection/focus sync and stage layering
  - fix status: fixed with direct DOM-selection refresh and debounced autosave
- [x] Left-bottom `ACTIVE SEGMENT / ACTIVE BLOCK` editor block does not match `new-design`
  - symptom: extra sidebar block exists in the rendered editor
  - suspected cause: `EditorStructureInspector` still mounted in the left rail
  - fix status: fixed by removing the legacy inspector mount from the sidebar
- [x] Learn control/layout is visibly broken
  - symptom: learn screen looks wrong against the reference
  - suspected cause: rendering drift or broken DOM/CSS contract
  - fix status: verified live after runtime review; no additional code change required in this pass
- [x] Teleprompter text layout is broken
  - symptom: text spacing/line flow collapses in teleprompter
  - suspected cause: reader chunk DOM contract differs from `new-design`
  - fix status: fixed by rendering explicit spaces between word/group chunks
- [x] Diagnostics/logging integration is unfinished
  - symptom: changes exist but are not fully validated and committed
  - suspected cause: partial implementation
  - fix status: validated through focused diagnostics tests and live browser logging

## Ordered Implementation Steps

- [x] Reproduce editor typing, learn, and teleprompter issues in a live browser
  - verification:
    - run the standalone app
    - inspect live DOM/CSS/interaction behavior

- [x] Fix editor input/focus and click-to-type behavior
  - verification:
    - add/update a targeted UI test that clicks and types manually
    - ensure caret remains usable after edits and toolbar actions

- [x] Remove the non-design left-bottom editor inspector block and keep editing via supported controls
  - verification:
    - add/update a component/UI test that asserts the extra block is absent
    - verify metadata editing still works from the right rail

- [x] Repair learn layout and controls to match `new-design`
  - verification:
    - add/update a focused UI regression on learn rendering and control actions

- [x] Repair teleprompter text spacing, grouping, and visible layout
  - verification:
    - add/update a focused UI regression for text spacing and visible reader controls

- [x] Finalize diagnostics/logging integration
  - verification:
    - keep bUnit diagnostics tests green
    - keep UI diagnostics banner test green

- [x] Run final validation
  - verification:
    - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - focused component tests
    - focused UI tests
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`

## Testing Methodology

- broken user flows are tested through real browser interactions first
- component tests cover structural parity and diagnostics rendering
- UI tests must be short and explicit:
  - editor click + type
  - absence of removed sidebar block
  - learn controls render and respond
  - teleprompter text spacing/control bar render correctly
  - diagnostics banner appears for recoverable failures

Quality bar:
- every broken flow must have a targeted automated regression
- all relevant focused suites must be green before the broader solution test pass
