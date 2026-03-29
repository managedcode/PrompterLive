# Library Folder Create Fix Brainstorm

## Problem

The library folder creation overlay is visually too dark and the create flow is broken in the real browser. The user reports:

- the modal backdrop should be transparent / glassy, not black
- typing into the folder name field does not behave correctly
- automated tests must prove that folder creation actually works

## Current State

- `LibraryFolderCreateModal` renders through `LibraryPage`
- `LibraryPage.razor` passes `FolderDraftName` and `FolderDraftParentId` into the modal
- the overlay backdrop uses a dark gradient that reads as opaque black
- existing tests cover happy-path create flow but do not prove that the input starts empty, accepts typed text, preserves the typed value on rerender, or that the overlay backdrop remains translucent

## Root Cause

The modal is passed string literals instead of bound state:

- `FolderDraftName="_folderDraftName"`
- `FolderDraftParentId="_folderDraftParentId"`

For string parameters this passes the literal text, not the field value. That explains the visible `_folderDraftName` text and the broken typing behavior.

## Options

### Option A

Fix only the string parameter binding and leave the overlay visuals mostly unchanged.

Pros:

- smallest code change

Cons:

- does not address the reported visual bug

### Option B

Fix the string parameter binding, restyle the backdrop to a translucent glass overlay, and add focused component/browser regressions that validate input editing, submit, and persisted folder visibility.

Pros:

- fixes the functional bug and the reported visual bug together
- adds tests that catch the actual failure mode

Cons:

- slightly larger test surface

## Recommended Direction

Option B.

## Risks

- Playwright style assertions can be brittle if they depend on exact serialized CSS strings
- modal CSS changes can accidentally reduce contrast if overdone

## Testing Direction

- component test: modal input starts empty and holds typed text
- browser test: create overlay is translucent and folder creation works end-to-end
- browser test: created folder persists after reload
