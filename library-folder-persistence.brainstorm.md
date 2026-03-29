# Library Folder Persistence Brainstorm

## Problem Framing

The library now supports real folders and move-to-folder behavior, but the acceptance bar is still incomplete until the browser runtime proves persistence across reloads and fresh app sessions.

Current gaps:

- no browser acceptance test that verifies created folders survive page reload
- no browser acceptance test that verifies moved scripts remain in their new folder after reload
- no explicit coverage that the stable self-hosted WASM origin keeps localStorage-backed library state consistent
- header breadcrumb and selected-folder view should be validated after re-entering the library, not only inside a single in-memory interaction

## Constraints

- standalone WASM only
- no backend
- keep `dotnet test` as the only required command for browser acceptance
- do not reintroduce hardcoded folder mapping or non-persistent UI state

## Options

### Option A: Browser-only acceptance coverage

Add Playwright reload tests that create a folder, move a script, hard reload the page, and verify the tree/card state.

Pros:

- closest to the real runtime contract
- validates localStorage + static host + route shell together

Cons:

- slower than adding only bUnit checks

### Option B: Add bUnit storage assertions only

Extend the bUnit harness to assert saved folder/document state in the fake repositories.

Pros:

- fast

Cons:

- does not prove the actual browser persistence path

### Option C: Do both, but keep browser flow as the source of truth

Add a real Playwright reload scenario and a small bUnit regression for selection state if needed.

Pros:

- strongest confidence
- catches runtime regressions and keeps fast local coverage for view rebuilding

Cons:

- more work than Option A

## Recommended Direction

Choose Option C.

Implementation outline:

1. add a browser reload scenario for folder creation + move persistence
2. persist and restore selected folder id in browser settings if the current UX benefits from reopening the same folder
3. add a small bUnit regression if view rebuilding needs protection
4. verify the flow through `dotnet test` only
