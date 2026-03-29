# Editor Visual Authoring Plan

Reference brainstorm: `editor-visual-authoring.brainstorm.md`

## Goal

Turn the current TPS editor into a body-only visual authoring surface where metadata is edited only in the metadata block, toolbar/menu actions are explicitly interactive, and the UI suite covers the actionable editor controls.

## Scope

### In Scope

- hide front matter from the editor source surface
- compose persisted TPS files from metadata + body
- make toolbar dropdowns explicit-click and testable
- make AI buttons open a local authoring panel
- add broad UI regression coverage for toolbar/menu actions

### Out Of Scope

- remote AI execution
- server runtime
- non-TPS storage

## Constraints And Risks

- no backend
- no random-port workflow changes
- preserve current autosave/history flows
- keep `new-design` visual structure

## Testing Methodology

- bUnit for metadata-hidden rendering and persistence composition
- Playwright for real selection, dropdown open/close, menu clicks, and visible reactions
- full solution gates after focused suites

## Baseline

- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
- [x] Record any already failing tests here before implementation

Already failing tests:

- [x] None recorded at baseline

## Ordered Plan

- [x] Add failing component tests proving front matter is hidden from the visible editor surface while persistence still includes metadata
  Done criteria: visible editor source shows only body content; saved session text still contains front matter.
  Verification: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
  Result: covered by `EditorVisualSourceTests`, `EditorMetadataInteractionTests`, and `EditorSourceInteractionTests`.

- [x] Add failing UI tests for explicit toolbar/dropdown interactions and AI panel visibility
  Done criteria: dropdown triggers open by click, menu buttons can be clicked in browser automation, and AI buttons show a local panel.
  Verification: `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
  Result: covered by `EditorInteractionTests`.

- [x] Implement body-only editor composition and persistence wiring
  Done criteria: metadata no longer appears in the visible editor source, but saved TPS still contains front matter.
  Verification: focused component tests and full app tests
  Result: visible editor now uses TPS body only; persistence is assembled through `EditorPage.DocumentComposition.cs`.

- [x] Implement explicit toolbar menu state, stable selectors, and local AI panel behavior
  Done criteria: actionable toolbar items are explicitly interactive and testable.
  Verification: Playwright and component tests
  Result: toolbar dropdowns use explicit open state, buttons have stable `data-testid`s, and local AI actions are handled by `EditorLocalAssistant`.

- [x] Re-run focused suites and fix regressions
  Done criteria: editor-focused tests are green.
  Verification:
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
  Result:
  - focused component editor tests green
  - focused UI editor tests green
  - full UI suite green

- [x] Run final quality pass
  Done criteria: build, test, format, and coverage pass.
  Verification:
  - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx --collect:"XPlat Code Coverage"`
  Result:
  - build green: `0 warnings`, `0 errors`
  - test green: `57` tests passed (`20` core, `26` component, `11` UI)
  - format completed; repo-wide analyzer issues remain without auto-fixes for `IDE0060`, `CA1305`, and `CA1826`
  - coverage collector generated Cobertura artifacts for `Core`, `App.Tests`, and `App.UITests`

## Root-Cause Tracking

- [x] No known failing baseline tests
  Root cause: full baseline run was green before the red-test additions for this task.
  Intended fix path: keep baseline green while adding editor authoring coverage.

- [x] Visible editor leaked TPS front matter
  Root cause: editor source was bound to the raw persisted TPS document instead of the TPS body.
  Intended fix path: split visible body editing from persisted metadata composition in the editor page.

- [x] Toolbar menus were hover-like decorations instead of explicit interactive controls
  Root cause: the editor surface lacked persistent menu state and stable UI selectors.
  Intended fix path: add explicit toolbar state, test ids, and browser-covered click flows.

- [x] AI actions had no local runtime behavior in standalone WASM
  Root cause: the design had AI affordances, but the runtime had no deterministic local action layer.
  Intended fix path: add a local assistant service with explicit panel actions and test them through UI flows.

## Final Validation Skills And Commands

- [x] `playwright`
  Reason: verify the visual editor surface and dropdown/button behavior in a real browser.

- [x] `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
  Reason: fast regression feedback for composition and rendering rules.

- [x] `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
  Reason: browser-realistic interaction coverage for toolbar/menu actions.

- [x] `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  Reason: repo-wide regression gate.

## Coverage Notes

- `tests/PrompterLive.Core.Tests/TestResults/fc844b9f-15a4-4fc8-8987-5e01d55d4aea/coverage.cobertura.xml`: `46.30%` line, `31.55%` branch
- `tests/PrompterLive.App.Tests/TestResults/ce253cfd-0de3-4957-b1bb-833199471477/coverage.cobertura.xml`: `54.47%` line, `43.91%` branch
- `tests/PrompterLive.App.UITests/TestResults/a50ba6ee-a0a5-416e-97a3-94be11f8fe33/coverage.cobertura.xml`: `0.00%` line, `100.00%` branch

These are collector outputs for the current repo baseline. This task added editor-specific regression coverage, but the repo still needs a separate coverage-raising pass if it wants to enforce the aspirational AGENTS thresholds module-wide.
