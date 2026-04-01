# Go Live New Design Parity Plan

## Goal

Bring the routed `Go Live` screen in `src/PrompterOne.Shared` into exact structural and visual parity with `new-design/golive.html`, with special attention to:

- top bar layout and Go Live action placement
- panel widths and overall studio grid proportions
- canvas/program monitor sizing
- right sidebar sizing and composition
- preserving the existing live-session/runtime behavior while fixing the presentation layer
- keeping stable `data-testid` contracts and adding coverage for the corrected layout

## Scope

### In Scope

- compare `new-design/golive.html` and `new-design/styles-golive.css` against the routed Blazor `Go Live` surface
- refactor `GoLivePage` markup and its feature components to match the `gl-*` studio layout from the design reference
- port or adapt CSS so the runtime page uses the same sizing, spacing, and placement rules as the design
- preserve current live controls, room flow, source switching, and destination toggles while moving them into the design-faithful structure
- update `Go Live` docs if the runtime structure description changes materially
- add or update bUnit and Playwright tests that prove the corrected layout and interaction landmarks

### Out of Scope

- changing streaming providers, browser media composition, or live output runtime behavior unless a layout fix requires a minimal contract adjustment
- redesigning the `new-design` reference itself
- adding new `Go Live` product features unrelated to parity with `new-design/golive.html`

## Constraints And Risks

- `new-design/golive.html` is the visual source of truth; approximation is not acceptable.
- Current `Go Live` runtime behavior already works through existing services; avoid dragging logic into layout components.
- `data-testid` selectors used by browser tests must remain stable or be updated through shared constants in the same task.
- The browser suite is the primary acceptance gate and must not run concurrently with another `dotnet build` or `dotnet test`.
- The current page is split across several components; parity may require moving markup between `GoLivePage` and child components without breaking state flow.
- The existing feature CSS may conflict with imported `gl-*` layout rules; resolve conflicts explicitly instead of layering ad-hoc overrides.

## Testing Methodology

- First establish a real repo baseline with the required full build and full test suite.
- Use bUnit to verify structural landmarks and design-faithful layout containers/classes on the routed `Go Live` page.
- Use Playwright to verify the real browser `Go Live` screen renders the corrected studio shell, panel visibility behavior, and key layout landmarks.
- Keep assertions tied to stable `data-testid` contracts plus specific design classes where layout fidelity matters.
- Capture browser screenshot artifacts under `output/playwright/` for the updated major `Go Live` scenario.
- Finish with the required repo-wide build, test, coverage, and format commands.

## Ordered Implementation Plan

- [x] Step 1. Record the baseline and failure inventory.
  What: run the required repo-wide `build` and `test` commands before changing code.
  Where: solution root.
  Verify: record pass/fail status in this plan; if anything already fails, list each failing test below with symptom, suspected root cause, and fix status before touching production code.

- [x] Step 2. Map the exact design/runtime mismatch.
  What: compare `new-design/golive.html` and `new-design/styles-golive.css` against `src/PrompterOne.Shared/GoLive/Pages/GoLivePage.razor` and feature components/CSS to identify which runtime sections must move to the `gl-*` structure.
  Where: `new-design/`, `src/PrompterOne.Shared/GoLive/Pages/`, `src/PrompterOne.Shared/GoLive/Components/`.
  Verify: capture the target container mapping and the specific layout defects being fixed, then implement against that map instead of patching isolated sizes.

- [x] Step 3. Rebuild the routed `Go Live` page shell on top of the design structure.
  What: update the page-level Razor markup so the top bar, left sources panel, center canvas/program area, scenes/controls bars, and right sidebar match the `gl-*` studio hierarchy and placement from `new-design/golive.html`.
  Where: `src/PrompterOne.Shared/GoLive/Pages/GoLivePage.razor`.
  Verify: bUnit render shows the expected major design landmarks and panel containers; existing live controls still bind to the same actions/state.

- [x] Step 4. Align child components with the design composition.
  What: reshape `GoLiveSourcesCard`, `GoLiveProgramFeedCard`, `GoLiveSceneControls`, `GoLiveCameraPreviewCard`, and `GoLiveStudioSidebar` so their markup fits the design shell without oversized cards or misplaced controls.
  Where: `src/PrompterOne.Shared/GoLive/Components/`.
  Verify: the components render inside the new shell with correct labels, controls, and `data-testid` landmarks preserved or intentionally updated through shared constants.

- [x] Step 5. Port or replace CSS to match the design proportions.
  What: replace the current `go-live-*` layout sizing with design-faithful `gl-*` rules or equivalent adapted CSS, including top bar sizing, three-column grid widths, canvas monitor height, sidebar sizing, and full-program/hide-panel behavior.
  Where: `src/PrompterOne.Shared/GoLive/Pages/GoLivePage.razor.css` and affected component CSS files.
  Verify: the routed page uses the same spatial model as `new-design`; panel collapse states still work and do not distort the program monitor.

- [x] Step 6. Add structural regression coverage in bUnit.
  What: update or add component tests that assert the `Go Live` page renders the design-faithful top bar, studio grid, canvas section, scenes/controls bars, and right rail landmarks.
  Where: `tests/PrompterOne.App.Tests/GoLive/`.
  Verify: focused `PrompterOne.App.Tests` runs green and new assertions fail on the old layout.

- [x] Step 7. Add browser parity coverage and artifact capture.
  What: update or add Playwright coverage that opens `Go Live`, verifies the corrected shell in a real browser, toggles panel visibility, and captures a screenshot artifact for the corrected page.
  Where: `tests/PrompterOne.App.UITests/GoLive/`.
  Verify: focused UI tests pass, screenshot artifacts are written under `output/playwright/`, and the major layout regressions are covered through stable selectors/contracts.

- [x] Step 8. Update durable docs for the corrected studio surface.
  What: refresh feature documentation if the runtime layout description or diagrams need to reflect the design-faithful shell.
  Where: `docs/Features/GoLiveRuntime.md` and related docs only if needed.
  Verify: docs remain accurate, non-duplicative, and diagrams still render.

- [x] Step 9. Run final validation and close the task.
  What: run focused tests first, then required repo-wide validation commands.
  Where: solution root.
  Verify: all commands pass and each checklist item in this plan is complete.

## Baseline Failure Inventory

- [x] Baseline build status recorded
- [x] Baseline full test status recorded

Baseline results:

- `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror` passed with `0` warnings and `0` errors.
- `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` passed.
  Details:
  `PrompterOne.Core.Tests`: `36` passed.
  `PrompterOne.App.Tests`: `103` passed.
  `PrompterOne.App.UITests`: `84` passed.

Pre-existing failing tests:

- None. Baseline is clean.

## Final Validation Skills And Commands

1. `dotnet-blazor`
   Action: verify the routed Blazor page structure and component ownership stay inside `PrompterOne.Shared` and align with the design reference.
   Outcome required: the final `Go Live` screen uses a coherent Blazor-owned layout rather than ad-hoc CSS patches.

2. `mcaf-testing`
   Action: verify that new or updated automated tests cover the corrected user-visible behavior.
   Outcome required: bUnit and Playwright regressions prove the page structure and browser-visible parity.

3. `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
   Reason: required repo build gate.

4. `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
   Reason: required repo full test gate.

5. `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"`
   Reason: required coverage gate and regression gap check.

6. `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
   Reason: required formatting/analyzer gate.

## Final Validation Results

- Focused `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror` passed after the Go Live rewrite.
- Focused `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/PrompterOne.App.Tests.csproj` passed.
- Focused `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/PrompterOne.App.UITests.csproj --no-build` passed, including the new layout parity browser scenario and screenshot artifact capture.
- Required `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` passed.
- Required `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"` passed and produced coverage attachments for all three test projects.
- Required `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` exited successfully and reported the existing `Unable to fix IDE0060. No associated code fix found.` message only.
