# AGENTS.md

## Project Purpose

`PrompterOne.Web.UITests.Studio` is the runnable browser acceptance suite for GoLive, media, and end-to-end studio/live workflow flows.

## Entry Points

- `GoLive/*`
- `Media/*`
- `Scenarios/*`

## Boundaries

- `dotnet test` must be enough to run this suite. Do not require env vars, custom ports, or manual app startup.
- Reuse the shared browser harness from `tests/PrompterOne.Web.UITests`; do not fork or locally duplicate fixture, host, or driver infrastructure.
- Keep studio/browser-media specs focused on GoLive, live-state transitions, media harnesses, recording, and end-to-end studio workflows.
- Use dedicated `data-test` selectors and named constants only.

## Project-Local Commands

- `node ./tests/PrompterOne.Web.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- `dotnet test --project ./tests/PrompterOne.Web.UITests.Studio/PrompterOne.Web.UITests.Studio.csproj`

## Applicable Skills

- `playwright`

## Local Risks Or Protected Areas

- Media and GoLive flows must keep the deterministic synthetic-media harness intact.
- Recording, preview, and live-state assertions must stay production-shaped and screenshot-backed where already required.
- Keep live/studio helpers isolated here unless they are genuinely shared by multiple suites.
