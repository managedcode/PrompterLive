# AGENTS.md

## Project Purpose

`PrompterOne.Web.UITests.Reader` is the runnable browser acceptance suite for Learn, Reader, Teleprompter, and responsive reading-surface flows.

## Entry Points

- `Learn/*`
- `Reader/*`
- `Responsive/*`
- `Teleprompter/*`

## Boundaries

- `dotnet test` must be enough to run this suite. Do not require env vars, custom ports, or manual app startup.
- Reuse the shared browser harness from `tests/PrompterOne.Web.UITests`; do not fork or locally duplicate fixture, host, or driver infrastructure.
- Keep reader-facing specs focused on routed reading surfaces, playback, responsive visibility, and screenshot-backed fidelity checks.
- Use dedicated `data-test` selectors and named constants only.

## Project-Local Commands

- `node ./tests/PrompterOne.Web.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- `dotnet test --project ./tests/PrompterOne.Web.UITests.Reader/PrompterOne.Web.UITests.Reader.csproj`

## Applicable Skills

- `playwright`

## Local Risks Or Protected Areas

- Reader and teleprompter flows must keep real-browser fidelity checks and screenshot capture.
- Responsive checks stay mandatory for routed reading surfaces; do not narrow the viewport matrix casually.
- Keep reading-flow helpers shared only when they are reused across suites.
