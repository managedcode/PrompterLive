# Editor Visual Authoring Brainstorm

## Problem

The editor still leaks front matter into the visible authoring surface and still behaves like a source editor with decorations rather than a true visual TPS authoring surface. The user requirement for this pass is stricter:

- metadata must live only in the metadata rail, not in the editable source surface
- authoring must stay inline and visual
- toolbar and dropdown buttons must be explicitly interactive
- UI tests must cover the full menu/button surface

## In Scope

- hide front matter from the visible editor surface
- keep metadata persisted in the file, but only through the metadata panel
- make toolbar dropdowns click-open and testable
- make all actionable toolbar buttons produce visible editor behavior or an explicit local UI response
- expand UI test coverage for toolbar and menu actions

## Out Of Scope

- remote AI provider integration
- collaborative editing
- backend storage
- replacing TPS persistence with a new document format

## Constraints

- stay standalone WASM
- stay aligned with `new-design`
- keep TPS as the stored source format
- do not mutate `new-design/`

## Options

### Option A: Move to a full contenteditable DOM editor

Pros:

- closest to classic WYSIWYG editing

Cons:

- high complexity
- selection and TPS serialization risks
- much larger JS surface

### Option B: Keep the current overlay/textarea architecture, but make it visual-body-only and metadata-separated

Pros:

- preserves TPS as canonical source
- simpler persistence and testing
- fixes the visible front matter problem directly
- lets toolbar actions stay deterministic

Cons:

- still source-backed rather than a pure DOM document model

## Recommended Direction

Choose Option B.

Treat the editor surface as a visual TPS body editor:

- load only the TPS body into the visible editor
- assemble the persisted file from `metadata rail + body editor`
- make dropdowns explicitly openable by click
- give every actionable toolbar item a stable test id and deterministic mutation
- let AI buttons open a local command panel since no backend AI provider exists in runtime

## Risks

- body-only selection indices can drift if any code path still assumes raw-document offsets
- toolbar coverage can become brittle if selectors are not explicit
- the visible body editor can desync from persisted metadata if save composition is not centralized

## Mitigations

- centralize file assembly in the editor page
- keep the visible editor source body-only everywhere
- update tests first for hidden metadata and explicit menu interactions
