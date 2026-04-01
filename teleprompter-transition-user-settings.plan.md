# Teleprompter Transition And User Settings Plan

## Goal

Fix teleprompter card transitions so block changes always animate in one consistent upward direction, and introduce a platform-agnostic user-settings abstraction that persists teleprompter width and vertical text position across sessions while establishing the same abstraction for broader user preferences.

## Scope

### In Scope

- Teleprompter reader card transition direction and related rendering state in `src/PrompterOne.Shared/Teleprompter/Pages/*`.
- Persistent teleprompter width and focal-point settings in the reader runtime and stored reader settings model.
- A reusable user-settings abstraction for persisted browser preferences, with the browser implementation backed by local storage.
- Updating existing settings consumers in `src/PrompterOne.Shared` to depend on the new abstraction where appropriate.
- Automated regression coverage in core, bUnit, and browser UI tests for persisted reader settings and transition behavior.
- Feature and architecture documentation updates if ownership or discovery changes.

### Out Of Scope

- New platform-specific persistence implementations beyond the browser-backed implementation.
- Redesigning teleprompter visuals beyond restoring the intended motion direction and persistence behavior.
- Changing unrelated TPS parsing, learn-mode timing, or cloud-storage behavior except where the new settings abstraction must preserve current contracts.

## Constraints And Risks

- Preserve exact `new-design` teleprompter feel; do not introduce new motion directions or extra jumpiness.
- Keep `PrompterOne.Core` host-neutral; the new abstraction contract may live in core, but browser storage implementation must stay in `PrompterOne.Shared`.
- Do not break existing persisted keys for reader, learn, scene, theme, or studio settings unless a migration-compatible path is provided.
- Preserve stable `data-testid` contracts and prefer shared constants in new tests.
- Keep changed files within repo maintainability limits; extract helper types/services rather than extending already-large partial classes without control.
- The worktree is already dirty from previous tasks; do not revert or overwrite unrelated changes.

## Testing Methodology

- Use a failing-first workflow for the user-visible regressions touched here.
- Prove teleprompter persistence through real reader flows:
  - component coverage for state restoration and persistence triggers
  - browser coverage for reload-surviving width/focal settings and visual transition direction contracts
- Verify settings abstraction changes through caller-visible save/load behavior rather than internal implementation detail.
- Run the full repo baseline first, then targeted suites while implementing, then the required final repo-wide quality pass.
- Browser coverage remains the primary acceptance bar for the teleprompter behavior.

## Ordered Implementation Plan

- [x] Step 1. Record the baseline before code changes.
  - What: run the required repo `build` and `test` commands from the root without changing code.
  - Where: repository root using `PrompterOne.slnx`.
  - Verify: capture whether the current baseline is green or enumerate each failing test in the tracked-failures section below before any production changes proceed.
  - Done criteria: the plan file lists the actual baseline outcome and every pre-existing failure is tracked explicitly.

- [x] Step 2. Define the cross-platform user-settings contract and browser-backed implementation wiring.
  - What: add a host-neutral user-settings interface and key contract, then wire the existing browser local-storage service to implement it through DI without breaking existing saved-key behavior.
  - Where: `src/PrompterOne.Core/*` for the abstraction contract and `src/PrompterOne.Shared/Settings/Services/*`, `src/PrompterOne.Shared/AppShell/Services/*`, and related callers for the browser implementation and registrations.
  - Verify: targeted build plus focused tests confirm existing settings consumers still resolve and the browser-backed store still round-trips values under the same storage keys.
  - Done criteria: browser settings callers can depend on the interface, not the concrete browser store, and current persisted contracts still load/save successfully.

- [x] Step 3. Extend reader settings persistence for teleprompter width and vertical focal position.
  - What: add the missing persisted reader settings fields, restore them during teleprompter bootstrap/population, and save them whenever the user changes width or focal controls.
  - Where: `src/PrompterOne.Core/Workspace/Models/*`, `src/PrompterOne.Shared/Teleprompter/Pages/*`, bootstrap/settings persistence services, and any shared key/contract files needed.
  - Verify: failing-first component or browser tests reproduce the current non-persisted behavior, then pass after implementation by confirming settings survive a fresh page load.
  - Done criteria: teleprompter width and focal point restore from persisted settings and update persistence immediately after the user changes them.

- [x] Step 4. Fix teleprompter block-transition direction so every block change moves upward consistently.
  - What: replace index-only card-state direction logic with transition-aware rendering state so outgoing cards always move upward and incoming cards rise from below in the same direction contract, including manual block changes.
  - Where: `src/PrompterOne.Shared/Teleprompter/Pages/*` and `src/PrompterOne.Shared/wwwroot/design/modules/reader/*` if CSS adjustments are required.
  - Verify: add a failing regression that exercises backward/forward/manual block changes and asserts consistent card-state direction or computed transforms in the browser.
  - Done criteria: reader transitions no longer alternate between upward and downward motion across block changes.

- [x] Step 5. Expand automated coverage for the new settings abstraction and teleprompter regressions.
  - What: add or update core, bUnit, and browser tests for settings round-trips, teleprompter width/focal persistence, and transition consistency.
  - Where: `tests/PrompterOne.Core.Tests/*`, `tests/PrompterOne.App.Tests/*`, and `tests/PrompterOne.App.UITests/Teleprompter/*`.
  - Verify: run focused test projects after each added regression until the new coverage is stable and deterministic.
  - Done criteria: each user-visible behavior changed in this task has automated regression coverage at the appropriate layer, with browser tests covering the primary acceptance path.

- [x] Step 6. Update architecture/feature documentation if the settings ownership or reader runtime contract changed.
  - What: document the new settings abstraction ownership and the persisted teleprompter behavior in the canonical docs.
  - Where: `docs/Architecture.md` and the relevant feature doc under `docs/Features/`.
  - Verify: docs clearly describe where user settings live, how teleprompter persistence works, and include valid Mermaid diagrams if the changed document requires them.
  - Done criteria: contributor-facing docs match the implemented ownership and runtime behavior.

- [x] Step 7. Run final validation and close the task only after all checks are green.
  - What: run focused suites, full repo build/test, coverage, and format in the required order after implementation is complete.
  - Where: repository root and any focused test project roots as needed.
  - Verify: every command succeeds and no planned checklist item remains incomplete.
  - Done criteria: the final validation section is fully checked off with passing commands and the tracked-failures section shows no unresolved task-related failures.

## Baseline Failures Tracking

- [x] `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror` passed with 0 warnings and 0 errors.
- [x] `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` passed.
  - `PrompterOne.Core.Tests`: 35 passed, 0 failed.
  - `PrompterOne.App.Tests`: 101 passed, 0 failed.
  - `PrompterOne.App.UITests`: 82 passed, 0 failed.
- [x] No pre-existing failing tests were present in the baseline, so subsequent failures introduced during implementation are task-related and must be fixed before completion.

## Final Validation Skills And Commands

1. `dotnet-blazor`
   Reason: validate that the routed Blazor teleprompter/settings changes preserve the intended UI/runtime ownership.
2. `mcaf-testing`
   Reason: ensure the regression coverage and verification layers match the repo testing policy.
3. `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
   Reason: enforce the repo-wide compile and analyzer gate before broader verification.
4. `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/PrompterOne.App.Tests.csproj`
   Reason: verify the routed component-level teleprompter/settings contracts affected by the changes.
5. `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/PrompterOne.App.UITests.csproj --no-build`
   Reason: prove the primary browser acceptance flows for teleprompter persistence and transition behavior.
6. `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
   Reason: ensure the full repository regression suite remains green.
7. `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:\"XPlat Code Coverage\"`
   Reason: confirm coverage does not regress after production changes.
8. `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
   Reason: apply the repo-required formatting and analyzer fix pass after tests succeed.

## Final Validation Results

- [x] `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
- [x] `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj`
- [x] `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/PrompterOne.App.Tests.csproj`
- [x] `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/PrompterOne.App.UITests.csproj --no-build`
- [x] `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
- [x] `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"`
- [x] `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
  - CLI note: `Unable to fix IDE0060. No associated code fix found.`
