# Library Folders Brainstorm

## Problem

The current library sidebar and folder behavior are hardcoded inside `LibraryPage.razor`.

Symptoms:

- folder names and hierarchy are hardcoded in the page
- folder counts are computed from hardcoded folder keys
- script-to-folder assignment is inferred from TPS metadata or id-based fallback switches
- "New Folder" is decorative only
- "Move to..." is decorative only

This does not satisfy the product need for real folder creation, real file organization, and realistic tests.

## In Scope

- persistent folder model for the standalone browser runtime
- real folder creation from the library UI
- moving scripts into folders
- dynamic folder tree and folder counts
- updated tests for creation, moving, and persistence

## Out Of Scope

- backend sync
- drag and drop
- favorites/recent implementation
- destructive folder deletion flow unless required by the implementation

## Options

### Option A: Keep folder state inside TPS front matter

Pros:

- no separate storage schema
- simple persistence

Cons:

- moving a file requires rewriting TPS text
- folder hierarchy becomes document content instead of library metadata
- bad fit for reusable library UI state
- still encourages view-level fallback hacks

Decision: reject.

### Option B: Extend stored script document with folder id and store folders separately

Pros:

- clear domain boundary between document content and library organization
- moving scripts does not rewrite TPS payload
- works well with localStorage and browser-only runtime
- makes folder tests and counts straightforward

Cons:

- requires storage schema update and migration path
- touches repository contracts and view models

Decision: choose this.

### Option C: Add a dedicated library repository/service with document + folder graph

Pros:

- best separation for long term growth
- avoids overloading `IScriptRepository`

Cons:

- larger refactor surface for this pass
- likely too much churn for the immediate need

Decision: defer. Option B can evolve into this later.

## Risks

- localStorage migration must preserve existing documents
- library UI parity with `new-design` can regress if structure changes carelessly
- test selectors must stay stable
- `LibraryPage.razor` is already too large and must be reduced while implementing the new behavior

## Recommended Direction

1. Extend stored document and summary models with folder metadata.
2. Add a browser-backed folder graph store with seeded default folders.
3. Move folder mapping and counts out of `LibraryPage.razor` hardcoded switches.
4. Add real create-folder and move-script flows with inline UI anchored to the existing design language.
5. Add bUnit and Playwright coverage for:
   - creating a top-level folder
   - creating a child folder under the selected folder
   - moving a script to a folder
   - persisted counts and visibility after reload/navigation
