# Full Suite Stabilization

## Goal

Get the full local `PrompterOne` build and automated test baseline green across all projects, then publish the validated fix directly to `main` and monitor the downstream GitHub Actions release path until it is green or a concrete external blocker is identified.

## Scope

In scope:
- full local solution build and test verification
- fixes in test support, UI test suites, and production code only where required to remove real regressions
- direct push to `main` after local validation
- post-push CI and release monitoring, with follow-up fixes if the remote pipeline fails

Out of scope:
- unrelated user-owned or parallel in-flight changes that are not required for the failing baseline
- broad feature work unrelated to the failing tests or release pipeline

## Constraints And Risks

- Respect the root `AGENTS.md` command contract and use the solution-level build/test commands.
- Do not hide browser flakiness behind timeout inflation or reduced coverage.
- Keep mutating editor tests isolated per draft/script and extend that principle to other stateful browser scenarios if needed.
- Keep the diff limited to the test-support and editor test files required for stabilization.
- Pushing directly to `main` is explicitly requested, but only after the validated state is green locally.

## Testing Methodology

- Establish the real baseline with the required solution-level build and test commands.
- For every failing test:
  - reproduce locally
  - identify the owning suite and root cause
  - apply the narrowest durable fix
  - rerun the failing project or focused class first
  - rerun the broader required regression layer
- Before push:
  - rerun the full local solution test command
- After push:
  - monitor the relevant GitHub Actions/release run
  - if a remote failure appears, inspect the failing job/log and iterate until green or explicitly blocked

## Baseline And Tracked Failures

- [x] Run `dotnet build ./PrompterOne.slnx -warnaserror`.
- [x] Run `dotnet test @./tests/dotnet-test-progress.rsp --solution ./PrompterOne.slnx --max-parallel-test-modules 1`.

Tracked failing tests from the current baseline:

- [x] `EditorAiAvailabilityTests.EditorScreen_AiButtonsAreDisabled_WhenNoProviderIsConfigured`
  Symptom:
  - solution-level run observed the `editor-ai` action enabled when the test expected it to be disabled
  Root cause:
  - the disabled-path test relied on a "fresh enough" browser context, but a configured-provider test earlier in the suite left AI-related browser state that made this case order-dependent
  Fix path:
  - seed an explicit default `AiProviderSettings` payload for the disabled-path test before opening the editor so the test never depends on prior browser state
- [x] `EditorMinimapLayoutTests.EditorScreen_MonacoMinimapStaysVisibleInsideEditorStage`
  Symptom:
  - intermittent timeout waiting for `editor-page` before the minimap assertions began
  Root cause:
  - the test used the shorter default visible timeout before Monaco readiness and could fail under editor-suite startup pressure
  Fix path:
  - remove the redundant short page-visible gate, rely on the Monaco-ready helper, and allow isolated page retry on transient page/context closure
- [x] `EditorToolbarCoverageTests.EditorToolbar_FloatingCommandButton_MutatesSource(...editor-float-pause-short-menu...)`
  Symptom:
  - intermittent `Target page, context or browser has been closed` during `GotoAsync` inside `OpenEditorAsync`
  Root cause:
  - the reported failure was reproduced only while overlapping browser-suite `dotnet test` processes were fighting over the shared Playwright/browser runtime; the scenario itself passes in a clean single-process editor-suite run
  Fix path:
  - keep browser-suite execution to one local `dotnet test` process at a time and retain the isolated-page retry boundary for genuinely recoverable page/context closure during bootstrap
- [x] `EditorMonacoAssistanceFlowTests.EditorScreen_ProvidesMonacoTpsCompletionLabel(...)`
  Symptom:
  - the full editor-suite rerun now fails all `16` completion-label data cases while timing out inside `EditorIsolatedDraftDriver.WaitForAssignedScriptRouteAsync`
  Root cause:
  - the new isolated-draft helper waits for an autosaved persisted `?id=` route, but this completion scenario intentionally opens Monaco on the raw in-progress text `"["` where persistence is not the contract under test
  Fix path:
  - keep the helper strict by default, but opt this completion scenario out of persisted-route waiting and verify the data-driven case family goes green again
- [x] `Release Pipeline` run `24199073318` reports a `Shell` bootstrap cluster (`44` failing tests)
  Symptom:
  - many unrelated shell, library, onboarding, settings, localization, diagnostics, and tooltip tests time out on their first routed-surface visibility assertion, most commonly `library-page`
  Root cause:
  - the browser-suite process starts parallel isolated contexts against a cold shared runtime, so the first routed page bootstrap can miss the standard per-test visibility window on CI even though the warmed local suite later passes
  Fix path:
  - add a one-time shared-runtime warmup path in the browser harness so the suite pays the cold-start cost before parallel tests begin
- [x] `Release Pipeline` run `24199073318` reports a `Studio` bootstrap cluster (`37` failing tests)
  Symptom:
  - go-live, settings, media, and studio workflow tests all time out on their first routed-surface visibility assertion, usually while `SeedGoLiveSceneForReuseAsync` waits for `library-page`
  Root cause:
  - the studio suite is hitting the same cold-start bootstrap pressure as `Shell`, but the failures fan out through GoLive/media helpers because those helpers seed state only after the first shell route becomes interactive
  Fix path:
  - reuse the shared-runtime warmup so the first library/settings/go-live routes are already booted before studio scenarios start seeding or asserting
- [x] `Release Pipeline` run `24203100281` still reports cold fresh-context bootstrap failures after the shared-runtime warmup
  Symptom:
  - `Shell` and `Studio` still fail on the first visible page assertion for a newly created browser context, and editor-only scenarios can still hit context/page closure while opening the first routed page inside a fresh context
  Root cause:
  - the earlier hypothesis was incomplete; the added CI-only sacrificial per-context warmup made CI bootstrap behavior diverge from local runs and added extra routed app startup work before every returned test page
  Fix path:
  - remove the CI-only per-context routed warmup, keep only the one-time shared-runtime warmup, and make every returned test page follow the same bootstrap path locally and on CI
- [ ] `StandaloneAppFixture` build break: `WarmUpContextPageIfNeededAsync` missing in `StandaloneAppFixture.cs`
  Symptom:
  - compiler reports `CS0103` for `WarmUpContextPageIfNeededAsync` from `tests/PrompterOne.Web.UITests/Infrastructure/StandaloneAppFixture.cs`
  Root cause:
  - verify whether the current workspace already contains the moved helper in `StandaloneAppFixture.Warmup.cs` and whether the failing build came from an older not-yet-published tree versus a real current partial-class mismatch
  Fix path:
  - confirm the helper is present in the compiled partial, keep the calling signature aligned, and re-run the required local build after the active full-suite run completes
- [ ] `Release Pipeline` run `24209085607` still fails on GitHub macOS despite the local full-suite baseline being green on the same `727d904` commit
  Symptom:
  - `Shell` and `Studio` fail remotely while local `dotnet build`, targeted suite runs, and the required solution-level `dotnet test` all pass on the exact pushed commit
  - downloaded CI screenshots show routed UI rendering successfully but staying on `library-page` for failing `Shell` scenarios instead of navigating into the requested editor/reader flow
  - `Studio` artifacts currently include intermediate scenario screenshots but not explicit `failure-*` captures, which suggests some failures still occur before the normal `AppUiTestBase` failure-capture path runs
  Root cause:
  - the current evidence points to a harness divergence and storage-coupling issue, not a broken visual tree or missing compiled assets
  - CI currently warms the returned isolated page to `Library` before the test sees it, while local runs return a blank primed page
  - mutable library/settings seed state is also being injected via `AddInitScript`, so every navigation can partially reseed browser storage instead of relying only on the explicit isolated reset-and-seed step
  Fix path:
  - preserve and inspect CI screenshot artifacts for each failing run, then harden the fresh-context bootstrap so local and CI both receive the same blank primed page
  - remove mutable library/settings seeding from `AddInitScript`, keep explicit isolated reset+seed only, and make blocked IndexedDB reset fail fast instead of silently succeeding
- [ ] `Release Pipeline` run `24218745809` fails in `Shell` (`27` tests) and `Studio` (`5` tests) after the direct `main` push on commit `4bea2cd`
  Symptom:
  - `Shell` now fails broadly on the very first routed page visibility checks for `library-page` and `settings-page`, with screenshots showing the shell UI eventually rendered after the assertion already timed out
  - `Studio` fails on initial `library-page` visibility in go-live setup plus a smaller cluster of route-return clicks that stay on `/go-live?id=...`
  Root cause:
  - the CI-only sacrificial per-context warmup still makes every new context pay extra routed bootstrap work that local runs never execute
  - `Shell` still opens many routes with raw `GotoAsync(...)` plus one-shot `ToBeVisibleAsync(...)` checks instead of the shared retrying route driver, so slower CI bootstrap falls straight into suite-wide flakes
  - `Studio` route-open and go-live-return flows still rely on root-page readiness alone instead of a stronger page-ready contract before clicking or waiting for downstream session state
  Fix path:
  - remove the per-context routed warmup so local and CI use the same isolated-page bootstrap path
  - introduce shared shell route-open helpers and migrate raw shell route opens onto `BrowserRouteDriver`
  - strengthen go-live route readiness in the studio helpers before interaction-heavy assertions
- [x] `Release Pipeline` run `24199073318` reports a `Reader` bootstrap cluster (`163` failing tests)
  Symptom:
  - learn, teleprompter, responsive, and route-visibility tests all fail at the initial `learn-page`, `teleprompter-page`, `settings-page`, `go-live-page`, `editor-page`, or `library-page` visibility gate
  Root cause:
  - the reader suite opens many isolated contexts directly against a cold runtime, so route bootstrap dominates the first assertion and cascades into nearly every reader-facing scenario
  Fix path:
  - preload the shared runtime once across the core routed surfaces so reader tests keep their existing assertions while avoiding cold-boot CI starvation
- [x] `TeleprompterAlignmentTooltipFlowTests.TeleprompterScreen_LeftRailTooltips_AppearOnlyAfterDelayAndStayOutsideButtons`
  Symptom:
  - the tooltip still remains visible intermittently during the dismiss assertion in the solution-level run
  Root cause:
  - the current hover-clear path still depends on incidental hover behavior; moving to `teleprompter-stage` was not deterministic enough under full-suite pressure
  Fix path:
  - replace the dismiss step with a deterministic pointer move to a measured non-trigger point on a production-owned surface and prove it under the reader project plus solution-level run
- [x] `TeleprompterAlignmentTooltipFlowTests.TeleprompterScreen_RightRailTooltips_AppearOnlyAfterDelayAndStayOutsideSliders`
  Symptom:
  - the width-slider tooltip still remains visible intermittently during the dismiss assertion in the solution-level run
  Root cause:
  - the right-rail tooltip test shares the same non-deterministic hover-clear path as the left-rail case
  Fix path:
  - reuse the deterministic measured pointer-move dismissal path so the test exits the tooltip anchor through a known safe region
- [x] `ReaderPlaybackTimingTests.LearnTimingProbe_UserSpeedChange_ChangesWordByWordTiming`
  Symptom:
  - the detailed slow/fast per-word timing assertions pass, but the final aggregate playback-span comparison sometimes reports fast playback completing later than slow playback
  Root cause:
  - the current total-span comparison includes startup and tail jitter that is looser than the already-proven per-word timing checks
  Fix path:
  - compare a more stable derived timing measure from the recorded samples so the assertion proves speed ordering without depending on first/last-sample jitter
- [x] `EditorThemeFlowTests.EditorScreen_LightTheme_EmotionMenu_UsesReadableDropdownAndCustomTooltipOnly`
  Symptom:
  - the solution-level run can observe the custom editor tooltip while it is still fading in, so the tooltip is visible but opacity remains below the required fully-rendered threshold
  Root cause:
  - the test relies on a fixed settle delay before asserting final tooltip opacity instead of waiting for the tooltip to finish its CSS fade-in under slower full-suite conditions
  Fix path:
  - replace the fixed-delay final tooltip assertion with a deterministic wait that polls the dedicated tooltip `data-test` surface until opacity reaches the required threshold
- [x] `EditorToolbarTooltipFlowTests.EditorScreen_DropdownTooltip_StaysOutsideMenuAndDoesNotBlockAction`
  Symptom:
  - the solution-level run observed the "early tooltip opacity" check after the tooltip had already started fading in, even though the same project-level editor run still passed
  Root cause:
  - the test mixed a fixed wall-clock sleep with a CSS-delay contract, so slower solution-level execution could consume enough time between `HoverAsync` and the assertion for the tooltip fade-in to have already started
  Fix path:
  - assert the low-opacity state immediately after hover, then rely on the existing tooltip driver to wait for the real visible state instead of stacking extra fixed settle sleeps
- [x] `Reader` route bootstrap/readiness hardening after the `24218745809` CI failures
  Symptom:
  - remote reader failures clustered around first-route `learn-page` visibility timeouts while the same commit stayed green locally
  Root cause:
  - the shared reader route helper treated the routed page root as "ready" too early, so slower CI could advance into shortcut/playback assertions before the interactive learn or teleprompter controls were actually present
  Fix path:
  - strengthen `ReaderRouteDriver` to wait for production-owned interactive sentinels (`learn` progress/play controls, teleprompter stage/play toggle, settings title) instead of stopping at the top-level page shell

## Ordered Plan

- [x] Step 1. Capture the full local build baseline.
  Actions:
  - run the required solution build command
  - record any build failures with exact file ownership
  Verification:
  - build completes successfully or every blocking error is captured in the tracked-failures section

- [x] Step 2. Capture the full local test baseline.
  Actions:
  - run the required solution-level `dotnet test` command
  - record each failing project/test name in the tracked-failures section with a short root-cause note
  Verification:
  - the plan contains an explicit checklist entry for every current failing test or failing project

- [x] Step 3. Stabilize failing suites one by one.
  Actions:
  - fix the first failing suite starting from the smallest reproducible scope
  - keep a running root-cause note and intended fix path for each failure
  - repeat until all tracked failures are closed
  Verification:
  - repaired `Reader` and `Editor` projects pass locally; remaining open work is the full solution rerun plus any remote-only CI fallout

- [x] Step 4. Re-run the full local solution verification.
  Actions:
  - rerun the required solution build and solution test commands
  - rerun `dotnet format ./PrompterOne.slnx`
  Verification:
  - local build passes
  - local full test pass count is green across all projects
  - formatting completes cleanly
  Result:
  - `dotnet format ./PrompterOne.slnx` passed
  - `dotnet build ./PrompterOne.slnx -warnaserror` passed
  - `dotnet test @./tests/dotnet-test-progress.rsp --solution ./PrompterOne.slnx --max-parallel-test-modules 1` passed with `1162/1162` green in `7m 42.943s`

- [ ] Step 5. Publish directly to `main`.
  Actions:
  - stage only the task-scoped stabilization files
  - commit the validated fix
  - push to `origin/main`
  Verification:
  - the remote push succeeds

- [ ] Step 6. Watch the remote CI/release path and react to failures.
  Actions:
  - monitor the relevant GitHub Actions run(s) for the pushed commit
  - download and inspect Playwright screenshot artifacts from failing browser jobs while the run is still active so each follow-up fix is grounded in the actual remote UI state
  - if any job fails, inspect logs, apply the fix, rerun local validation, and push the follow-up
  Verification:
  - the release path is green or an explicit external blocker is documented
