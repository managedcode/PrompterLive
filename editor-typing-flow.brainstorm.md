# editor-typing-flow.brainstorm

## Problem Framing

The current editor acceptance suite proves command clicks, source-sync, and selection flows, but it still misses one explicit user-path requirement: literally typing TPS text into the editor.

The user asked for tests where the browser types script text directly in the editor. That needs to prove:

- the textarea accepts real keyboard typing
- typed TPS headers update the structure inspector
- typed body content persists through autosave and reload

## Options

### Option 1: Expand an existing editor UI test

Pros:
- fewer files
- quick to wire

Cons:
- makes `EditorInteractionTests.cs` denser
- mixes command-surface coverage with text-entry coverage

### Option 2: Add a dedicated typing acceptance test file

Pros:
- keeps the user flow isolated
- easier to maintain and extend with more typing regressions
- clearer intent: this file exists to prove live authoring by typing

Cons:
- one more test file

## Recommended Direction

Choose Option 2.

Add a focused Playwright test that clears the editor through keyboard shortcuts, types a compact TPS script with segment and block headers, then verifies structure inspector sync and persistence after reload.

## Risks

- typing can be slower and more timing-sensitive than direct value injection
- full-script typing can make tests too slow if the payload is large

## Mitigations

- keep the typed TPS fixture compact but structurally meaningful
- type the text with Playwright keyboard APIs and wait on observable UI state, not arbitrary long sleeps
- keep assertions on editor-visible outcomes: source value, structure inspector, and persisted reload state
