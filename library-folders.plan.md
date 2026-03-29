# Library Folders Plan

Chosen brainstorm: `library-folders.brainstorm.md`

## Goal

Replace hardcoded library folders with a real persistent folder system for the standalone WASM app, including folder creation, moving scripts into folders, and automated coverage for the new flows.

## Scope

### In Scope

- persistent folder model and script folder assignment
- dynamic folder tree rendering
- folder creation from the library UI
- moving scripts into folders
- bUnit and Playwright verification for these flows
- architecture doc update if changed boundaries or contracts need to be reflected

### Out Of Scope

- backend sync
- favorites/recent behavior
- drag and drop
- folder deletion and rename unless needed during implementation

## Constraints And Risks

- browser-only runtime must remain intact
- localStorage migration must preserve existing documents
- `new-design` fidelity must remain high
- stable `data-testid` selectors must be preserved or replaced intentionally with updated tests
- root maintainability limits apply; split large logic instead of growing `LibraryPage.razor`

## Testing Methodology

Flows to cover:

- library loads seeded folders dynamically
- user creates a new top-level folder
- user creates a child folder under a selected folder
- user moves a script into a different folder
- counts and filtered cards update immediately
- folder state persists across navigation/reload

How they will be tested:

- bUnit for rendering and local interaction state transitions
- Playwright for real browser flows with clicks and persisted localStorage behavior
- full repo test suite for regression coverage

Quality bar:

- changed folder behavior covered by both component and browser acceptance layers
- all relevant tests green
- no loss of existing library/editor/learn/teleprompter/settings coverage

## Ordered Plan

- [x] Read `docs/Architecture.md` and nearest local `AGENTS.md` files.
- [x] Read current library page, repository, and document models to identify hardcoded folder behavior.
- [x] Create real folder/document storage model and migration-safe browser storage shape.
  - Verification: build the touched projects.
- [x] Refactor the library UI to use dynamic folders and remove hardcoded folder switches/counts.
  - Verification: add/update bUnit tests for folder rendering and counts.
- [x] Implement folder creation flow in the library UI.
  - Verification: bUnit test for creating a folder and seeing it rendered.
- [x] Implement moving scripts into folders.
  - Verification: bUnit test for move action and filtered results.
- [x] Add browser acceptance coverage for create-folder and move-to-folder flows.
  - Verification: Playwright tests pass on the standalone WASM app.
- [x] Update `docs/Architecture.md` if new library contracts or boundaries are introduced.
  - Verification: architecture doc reflects the new library storage boundary.
- [x] Run final verification stack.
  - Verification commands:
    - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
    - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx --collect:"XPlat Code Coverage"`

## Full-Test Baseline

- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` before implementation to establish the baseline.
  - Result: passed, `29` tests green, `0` known failures at baseline.

## Existing Failing Tests At Baseline

- [x] No failing tests at baseline.

## Root-Cause Tracking

- [x] Baseline status recorded.
- [x] `PageSmokeTests.LibraryPage_RendersExactDesignShell` and new folder bUnit tests failed with `ArgumentNullException` while building the folder tree.
  - Failure symptom: `LibraryFolderTreeBuilder.BuildTree()` threw when root `ParentId` was `null`.
  - Root cause: used `ToDictionary` with a nullable grouping key for root folders.
  - Intended fix path: switch tree and option building to `Lookup`-based traversal that accepts `null` parents.
  - Current status: fixed and verified via `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`.
- [x] `ScreenFlowTests.LibraryScreen_CreatesFolderAndMovesScript` initially failed because UI tests served stale WASM assets.
  - Failure symptom: static browser host loaded outdated bundles, so new `data-testid` selectors never appeared.
  - Root cause: `PrompterLive.App.UITests` did not build `PrompterLive.App` before starting `StaticSpaServer`.
  - Intended fix path: add a `ProjectReference` from UI tests to the standalone app so `dotnet test` always builds fresh browser assets first.
  - Current status: fixed and verified via `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`.
- [x] `ScreenFlowTests.LibraryScreen_CreatesFolderAndMovesScript` then failed after folder creation because the newly created folder was auto-selected.
  - Failure symptom: the test could not find `library-card-menu-rsvp-tech-demo` immediately after creating an empty folder.
  - Root cause: the app correctly switched context into the new empty folder, but the test still expected the card list from `All Scripts`.
  - Intended fix path: update the browser scenario to return to `All Scripts` before moving the script into the new folder.
  - Current status: fixed and verified in the Playwright suite.

## Final Validation Skills

- `playwright`
  - Reason: validate real folder interactions and persistence in a browser, not only component rendering.
