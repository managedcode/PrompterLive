# UI Stability And Typing Brainstorm

## Chosen Scope

In scope:
- fix real manual typing in the editor source control
- remove non-design editor sidebar inspector blocks that do not exist in `new-design`
- repair learn and teleprompter rendering so layout and text flow match `new-design`
- finish `ILogger`-based diagnostics and visible error handling for routed screens
- add short targeted automated tests for the broken flows

Out of scope:
- new backend work
- new native host work
- new media providers or streaming capabilities
- broad feature expansion outside the broken screens

## Current State

- the editor uses a transparent textarea over a highlighted overlay
- typing works in an existing Playwright test, but the user cannot type manually in the real app
- the editor sidebar still renders `ACTIVE SEGMENT` / `ACTIVE BLOCK`, which are not present in `new-design`
- learn and teleprompter screens are visually broken against the reference screenshots
- diagnostics work is partially implemented but not finalized or committed

## Likely Causes

### Option A: selection sync is stealing focus/caret

- `OnAfterRenderAsync` calls `SetSelectionAsync` whenever the Blazor selection state differs from the DOM selection
- source edits and selection refreshes may cause focus churn or caret jumps during manual typing
- this matches the symptom where automation can type but a human cannot maintain focus

Pros:
- addresses the most likely editor-input regression
- small, targeted change

Risks:
- undo/redo and floating-toolbar positioning must still stay in sync

### Option B: overlay and textarea layering is wrong for pointer input

- the textarea is absolutely positioned and transparent
- if sizing or pointer routing is off in the real app, clicks may land in the wrong place

Pros:
- directly addresses click/focus problems

Risks:
- could hide the real issue if the deeper problem is focus churn

### Option C: editor shell drifted away from `new-design`

- `EditorStructureInspector` introduces a block not present in the reference
- the design likely expects only the structure tree on the left and metadata on the right

Pros:
- obvious parity fix

Risks:
- some existing tests will need to move to other editable controls

### Option D: learn and teleprompter rendering is over-normalizing text

- teleprompter currently composes words/groups dynamically
- the reported output shows collapsed spacing and poor line flow
- learn/reader may need to align more closely with the reference DOM/CSS contract instead of invented grouping

Pros:
- directly addresses visible user-facing breakage

Risks:
- the rendering pipeline may need selective simplification

## Recommended Direction

Use a combined approach:

1. reproduce the editor problem in the live browser
2. stop programmatic caret sync from stealing focus except when navigation or toolbar commands explicitly request it
3. make the editor stage click-focus the textarea reliably
4. remove the non-design left-bottom inspector block entirely
5. fix teleprompter and learn text-layout issues by aligning the rendered DOM/CSS contract to `new-design`
6. finalize diagnostics and add targeted short tests for:
   - manual typing
   - missing left-bottom inspector
   - learn screen controls and rendering
   - teleprompter text spacing/layout
   - diagnostics banner/error boundary

## Risks

- editor typing and selection behavior can become flaky if the DOM sync contract is not tightened carefully
- removing the sidebar inspector changes existing tests and authoring paths, so metadata editing must remain fully available in the right rail
- teleprompter layout fixes can accidentally break reader playback JS expectations

## Test Methodology

- use short Playwright browser regressions on the real standalone app
- prefer direct visible assertions over long full-suite runs during iteration
- keep bUnit tests for structural parity and diagnostics banner behavior
