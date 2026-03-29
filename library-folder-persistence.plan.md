# Library Folder Persistence Plan

Chosen brainstorm: `library-folder-persistence.brainstorm.md`

## Goal

Prove that library folders and folder assignments persist across reloads in the standalone WASM app and keep the library shell synchronized after reload.

## Scope

### In Scope

- browser persistence for created folders
- browser persistence for moved scripts
- selected folder restoration if implemented
- bUnit/browser regression coverage for reload-safe library rebuilding
- architecture doc update only if the persistence boundary changes

### Out Of Scope

- sync across devices
- folder rename/delete
- recent/favorites implementation

## Constraints And Risks

- `dotnet test` must remain enough for UI tests
- localStorage-backed runtime behavior is the real contract
- reload assertions must not be flaky

## Testing Methodology

Flows to cover:

- create folder
- move script into folder
- reload page
- reopen folder view
- verify folder still exists and script is still inside it

How they will be tested:

- Playwright for the real reload/browser persistence flow
- bUnit only if view rebuilding needs a direct regression guard
- full solution regression after changes

Quality bar:

- persistence flow proven in a real browser
- no regression in existing library/editor/learn/teleprompter/settings suites

## Ordered Plan

- [x] Read `docs/Architecture.md` and local `AGENTS.md` files for the affected modules.
- [x] Inspect current browser persistence behavior and identify whether selected-folder state also needs restoration.
  - Verification: run or inspect the existing UI flow.
- [x] Add/adjust runtime code for reload-safe folder persistence and selected-folder restoration if needed.
  - Verification: focused bUnit and/or targeted browser test.
- [x] Add a Playwright reload acceptance scenario for folder persistence.
  - Verification: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
- [x] Add a focused bUnit regression only if view rebuild logic changes materially.
  - Verification: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
- [x] Run final verification stack.
  - Verification commands:
    - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`

## Full-Test Baseline

- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` before implementation to record the new baseline.
  - Result: passed, `32` tests green, `0` baseline failures.

## Existing Failing Tests At Baseline

- [x] No failing tests at baseline.

## Root-Cause Tracking

- [x] `LibraryPage_RestoresPersistedFolderSelectionAfterReload` initially failed because the test moved a sample script before the in-memory repository had been seeded.
  - Failure symptom: restored folder selection rendered an empty folder and the expected `Product Launch` card never appeared.
  - Root cause: the regression test called `MoveToFolderAsync` before seeding sample documents, so the move operation had no target document to update.
  - Intended fix path: seed the in-memory repository and folder repository before creating the custom folder and persisted view state.
  - Current status: fixed and verified with `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`.

## Final Validation Skills

- `playwright`
  - Reason: persistence on reload must be proven in a real browser.
