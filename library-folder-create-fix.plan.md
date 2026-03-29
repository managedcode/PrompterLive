# Library Folder Create Fix Plan

Reference brainstorm: `library-folder-create-fix.brainstorm.md`

## Goal

Fix the broken library folder creation modal so it:

- uses real bound draft state instead of literal variable names
- renders with a translucent glass backdrop instead of a dark opaque overlay
- is covered by automated tests that prove typing and creation work in the browser

## Scope

In scope:

- `LibraryPage` modal parameter binding
- folder overlay styling
- focused component and Playwright regressions for create flow

Out of scope:

- broader library redesign
- unrelated editor or teleprompter issues

## Constraints And Risks

- existing tracked changes in the repo must not be reverted
- tests must use the real routed UI flow
- style assertions should verify translucency without overfitting to exact CSS serialization

## Testing Methodology

- Component tests will exercise the rendered `LibraryPage` through bUnit and assert caller-visible DOM state.
- Browser tests will use Playwright to type into the real modal, verify the typed value survives rerender, submit the folder, and verify persistence after reload.
- Visual overlay verification will use computed style inspection to ensure the backdrop alpha remains translucent.

## Baseline

- [x] Run focused component baseline: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj --filter "FullyQualifiedName~LibraryFolder"`
- [x] Run focused UI baseline: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --filter "FullyQualifiedName~LibraryScreen_CreatesFolderAndMovesScript"`

Already failing tests:

- [x] `PrompterLive.App.Tests.LibraryFolderOverlayTests.LibraryPage_FolderOverlay_StartsWithEmptyDraft_AndKeepsTypedValue`
  Symptom: the input starts with `_folderDraftName` instead of an empty draft value.
  Suspected cause: `LibraryPage.razor` passes a literal string instead of the backing field into `LibraryFolderCreateModal`.
  Fix path: bind `FolderDraftName` and `FolderDraftParentId` with Razor expressions.
- [x] `PrompterLive.App.UITests.LibraryFolderOverlayFlowTests.FolderOverlay_IsTranslucent_AndCreationAcceptsTypedInput`
  Symptom: the real browser input starts with `_folderDraftName` and the overlay still uses an opaque dark gradient.
  Suspected cause: the same broken string binding plus overly dark backdrop styling.
  Fix path: fix modal bindings and replace the dark linear gradient with a translucent glass overlay.

## Ordered Steps

- [x] Fix `LibraryPage` modal bindings so string parameters use real component state.
- [x] Restyle the modal backdrop to a translucent glass overlay that matches `new-design`.
- [x] Add a component regression that proves the modal field starts empty and accepts typed input without snapping back to a variable name.
- [x] Add a browser regression that proves the overlay is translucent and the folder create flow works end-to-end.
- [x] Re-run focused tests after each change until green.
- [ ] Run final validation commands in order:
  - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` because build is a separate gate
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj` because the library modal is covered here
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj` because the real browser flow is the acceptance bar
  - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` because repo policy requires formatting
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx --collect:"XPlat Code Coverage"` because repo policy requires coverage collection

Validation notes:

- Focused red baseline reproduced on both new regressions before the fix.
- `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` is green after the fix.
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj` is green after the fix.
- Focused browser acceptance is green via `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build --filter "FullyQualifiedName~LibraryFolderOverlayFlowTests|FullyQualifiedName~LibraryScreen_CreatesFolderAndMovesScript"`.
- `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` completed successfully, with existing unfixed analyzer items reported by the formatter.
- Full `PrompterLive.App.UITests` and solution-wide coverage collection remain separate because the broader UI suite has an existing long-running behavior outside this folder-overlay fix.

## Done Criteria

- [x] Folder modal no longer displays `_folderDraftName` or `_folderDraftParentId`
- [x] User can type into the folder name input normally
- [x] Submitting creates the folder and selects it
- [x] Folder is still present after reload
- [x] Overlay backdrop is translucent, not a dark opaque layer
- [ ] Focused and final validation commands are green
