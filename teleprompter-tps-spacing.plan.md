# Teleprompter TPS Spacing Plan

## Goal

Verify and, where needed, complete `teleprompter` support for TPS speed formatting so the reading surface matches `new-design/teleprompter.html`, preserves TPS timing math, and renders subtle word-level letter-spacing cues for slower and faster spans without hurting readability.

## Scope

### In Scope

- inspect the current teleprompter runtime against `new-design/teleprompter.html` and the TPS specification at `https://tps.managed-code.com/`
- verify how compiled TPS speed metadata reaches `TeleprompterPage` word markup and CSS
- adjust production teleprompter rendering only if the current implementation misses TPS speed-spacing parity or skips supported TPS speed modes
- add or tighten automated tests so teleprompter coverage proves timing plus spacing behavior for TPS speed tags and inline WPM overrides
- update the reader feature doc if the verified behavior or verification contract changes

### Out Of Scope

- unrelated teleprompter layout redesigns
- editor authoring changes unless they are strictly required to expose or preserve the teleprompter runtime contract
- non-reader TPS parser refactors that are not needed for teleprompter parity

## Constraints And Risks

- `new-design/teleprompter.html` remains the visual source of truth for the reader UI.
- Teleprompter timing must stay anchored to effective WPM; visual spacing cues must not break playback duration math.
- Word-level spacing changes must not introduce layout jumps that fight the current active-word alignment behavior.
- Browser acceptance is the primary gate, so any reader-surface change must be checked in real Playwright flows in addition to bUnit.
- The user explicitly wants subtle spacing, not exaggerated stretching or squeezing.

## Testing Methodology

- Establish a full repo baseline before edits with the required solution `build` and `test` commands.
- Verify the teleprompter implementation layer-by-layer:
  - component-level DOM assertions for classes, style variables, titles, pronunciation, and effective WPM metadata
  - browser-level assertions for computed letter-spacing and live playback alignment
  - core-level assertions only when a TPS compile behavior gap is found
- Final quality bar:
  - teleprompter speed tags and inline WPM spans render with readable spacing cues in the DOM
  - timing metadata still reflects effective WPM correctly
  - `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror` passes
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` passes
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"` passes
  - `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` passes

## Ordered Steps

### 1. Freeze Discovery And Record The Intended Direction

- [x] Read the root and local `AGENTS.md` files for `PrompterOne.Shared`, `PrompterOne.App.Tests`, and `PrompterOne.App.UITests`.
- [x] Read `docs/Architecture.md`, `docs/Features/ReaderRuntime.md`, `new-design/teleprompter.html`, and the TPS specification reference page.
- [x] Locate the current teleprompter word rendering, spacing logic, and relevant tests in source and test projects.
- Verify before moving on:
  - the owning production files and test files are identified
  - the spec and design expectations for speed-derived spacing are explicit

### 2. Establish The Full Baseline Before Edits

- [x] Run `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`.
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Record every baseline failure below with symptom, suspected cause, and intended fix path before applying code or test changes.
- Verify before moving on:
  - baseline build status is recorded
  - baseline solution test status is recorded
  - every pre-existing failure, if any, is tracked below as a checklist item

## Baseline Failures

- [x] No baseline failures.
  - Symptom: none. The required solution `build` and `test` commands both passed before edits.
  - Suspected cause: not applicable.
  - Intended fix path: preserve the green baseline while tightening teleprompter TPS parity and coverage.

### 3. Compare Current Runtime Against The TPS Contract

- [x] Inspect compiled TPS metadata handling in `src/PrompterOne.Core/Tps/Services/ScriptCompiler.cs` only where needed to confirm support for `xslow`, `slow`, `normal`, `fast`, `xfast`, and inline `[NWPM]`.
- [x] Inspect `src/PrompterOne.Shared/Teleprompter/Pages/TeleprompterPage.ReaderContent.cs`, `TeleprompterPage.ReaderWordStyling.cs`, `TeleprompterPage.ReaderRendering.cs`, and the reader CSS to determine whether production changes are required.
- [x] Identify any gaps between the current implementation and the desired behavior from `new-design` and the TPS spec, including missing test coverage for specific speed modes.
- Verify before moving on:
  - the exact production gap list is concrete
  - if no production change is needed, the test-only scope is explicitly justified in this plan

Gap list discovered:

- `TpsParser` did not flatten nested `speed_offsets:` front matter from the TPS specification into usable metadata keys.
- `ScriptCompiler` applied hardcoded speed factors and ignored both spec-style `speed_offsets.*` metadata and the editor’s flat `*_offset` metadata.
- `TeleprompterPage` rebuilt per-block `TpsDocument` instances with only `base_wpm`, so custom front-matter speed offsets were dropped before reader rendering.
- Existing tests covered default `xslow` / `slow` / `fast` / `xfast` and inline `[NWPM]`, but they did not prove custom front-matter speed offsets or `[normal]` reset behavior on the live teleprompter surface.

### 4. Implement The Teleprompter Runtime Or Test Gaps

- [x] Update teleprompter production code in `src/PrompterOne.Shared/Teleprompter/Pages/*` only if the current speed-spacing behavior or TPS-mode coverage is incomplete.
- [x] Add or tighten bUnit coverage in `tests/PrompterOne.App.Tests/Teleprompter/*` so reader DOM assertions cover the needed TPS speed modes, inline WPM, pronunciation, and readable spacing rules.
- [x] Add or tighten Playwright coverage in `tests/PrompterOne.App.UITests/Teleprompter/*` so real-browser assertions cover computed letter-spacing and active playback behavior for TPS-formatted words.
- [x] Add or tighten core TPS tests in `tests/PrompterOne.Core.Tests/Tps/*` only if compile-time support for the targeted TPS markers needs correction or explicit proof.
- Verify before moving on:
  - every changed production contract has direct automated coverage
  - test selectors and repeated values come from existing shared constants or newly added constants

Implementation notes:

- `TpsParser` now flattens one-level nested YAML front matter such as `speed_offsets.xslow`.
- `ScriptCompiler` now resolves speed multipliers from both `speed_offsets.*` and legacy flat `*_offset` metadata before applying inline speed tags.
- `TeleprompterPage` now reparses the active TPS document metadata and forwards it into its synthetic per-block compiler pass, so reader timing and spacing keep custom front-matter speed offsets.
- Reader word styling now respects the minimum `new-design` spacing floors for `tps-slow`, `tps-fast`, `tps-xslow`, and `tps-xfast`, preventing subtle custom offsets from visually disappearing.

### 5. Update Durable Documentation

- [x] Update `docs/Features/ReaderRuntime.md` if the verified teleprompter TPS-spacing contract or its verification matrix changes.
- [x] Keep Mermaid content valid if the feature doc needs structural changes.
- Verify before moving on:
  - the reader feature doc matches the shipped behavior
  - documentation does not duplicate facts that already live in code comments or tests without adding durable value

### 6. Run Verification In Layers And Close The Plan

- [x] Run the smallest changed test set first, such as the touched teleprompter bUnit and browser test files.
- [x] Run `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`.
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"`.
- [x] Run `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Update this plan with the completed checklist, final outcomes, and any residual risk notes.
- Verify before moving on:
  - all required commands pass
  - the plan reflects the actual end state and any remaining risk

Final verification completed:

- Focused core regression: `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj --filter TpsRoundTripTests` passed.
- Focused reader DOM regression: `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/PrompterOne.App.Tests.csproj --filter TeleprompterFidelityTests` passed.
- Focused browser acceptance: `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/PrompterOne.App.UITests.csproj --filter FullyQualifiedName~TeleprompterFullFlowTests` passed with both the full product-launch scenario and the new custom-speed-offset reader scenario.
- Required repo build gate: `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror` passed.
- Required repo test gate: `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` passed.
- Required repo coverage gate: `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"` passed and emitted Cobertura reports for core, app tests, and UI tests.
- Required repo formatting gate: `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx` completed successfully; it reported that `IDE0060` has no automatic code fix, but the command exited green.

Residual risk notes:

- The editor metadata rail still authors flat `xslow_offset` / `slow_offset` / `fast_offset` / `xfast_offset` keys, while the parser now also accepts spec-style nested `speed_offsets.*` keys. This keeps runtime compatibility broad, but a future TPS-authoring cleanup could standardize the persisted shape if required.

## Final Validation Skills And Commands

1. `dotnet-blazor`
   - Action: verify the teleprompter Razor rendering and word-level style output stay aligned with the Blazor runtime structure.
   - Outcome: TPS speed metadata reaches DOM classes, style variables, and titles correctly.
2. `playwright`
   - Action: validate real-browser teleprompter rendering and playback behavior after any spacing adjustments.
   - Outcome: computed letter-spacing and focal-line alignment remain correct in the live browser harness.
3. `mcaf-testing`
   - Action: tighten automated coverage for TPS speed and teleprompter runtime contracts.
   - Outcome: new or updated tests prove the user-visible behavior rather than only internal implementation.
4. Repo quality commands
   - `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
   - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
   - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx --collect:"XPlat Code Coverage"`
   - `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
   - Reason: these are the mandatory repo-defined quality gates.
