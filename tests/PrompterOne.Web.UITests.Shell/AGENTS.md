# AGENTS.md

## Project Purpose

`PrompterOne.Web.UITests.Shell` is the runnable browser acceptance suite for shell, library, settings, diagnostics, localization, and browser-host infrastructure flows.

## Entry Points

- `AppShell/*`
- `Diagnostics/*`
- `Infrastructure/*`
- `Library/*`
- `Localization/*`
- `Settings/*`
- `Shared/*`

## Boundaries

- `dotnet test` must be enough to run this suite. Do not require env vars, custom ports, or manual app startup.
- Reuse the shared browser harness from `tests/PrompterOne.Web.UITests`; do not fork or locally duplicate fixture, host, or driver infrastructure.
- Keep shell/browser-host specs focused on app bootstrap, navigation, settings, localization, diagnostics, and cross-cutting shell chrome.
- Use dedicated `data-test` selectors and named constants only.

## Project-Local Commands

- `node ./tests/PrompterOne.Web.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- `dotnet test --project ./tests/PrompterOne.Web.UITests.Shell/PrompterOne.Web.UITests.Shell.csproj`

## Applicable Skills

- `playwright`

## Local Risks Or Protected Areas

- Keep browser-host and shell checks isolated here; do not drift them into reader or studio suites.
- Cross-tab, localization, and storage-backed flows must stay real-browser and production-shaped.
- Infrastructure smoke checks should stay lightweight and deterministic.
