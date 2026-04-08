# AGENTS.md

## Project Purpose

`PrompterOne.Web.UITests.Editor` is the runnable browser acceptance suite for editor authoring, Monaco assistance, toolbar, layout, and editor performance flows.

## Entry Points

- `Editor/*`

## Boundaries

- `dotnet test` must be enough to run this suite. Do not require env vars, custom ports, or manual app startup.
- Reuse the shared browser harness from `tests/PrompterOne.Web.UITests`; do not fork or locally duplicate fixture, host, or driver infrastructure.
- Keep editor browser specs grouped by authoring or editor-surface concern.
- Verify real editor interactions in a real browser, including Monaco typing, selection, menus, tooltips, and screenshot artifacts.
- Use dedicated `data-test` selectors and named constants only.

## Project-Local Commands

- `node ./tests/PrompterOne.Web.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- `dotnet test --project ./tests/PrompterOne.Web.UITests.Editor/PrompterOne.Web.UITests.Editor.csproj`

## Applicable Skills

- `playwright`

## Local Risks Or Protected Areas

- Editor typing and authoring regressions must be exercised through the live browser surface, not only helper abstractions.
- Keep editor-only helpers or assets inside this suite unless they are truly shared across multiple browser suites.
- Do not weaken editor performance or authoring assertions to hide slowness.
