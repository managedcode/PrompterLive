# AGENTS.md

## Project Purpose

`PrompterLive.App.UITests` is the browser acceptance layer for the standalone WASM app.

## Entry Points

- `StandaloneAppFixture.cs`
- `StaticSpaServer.cs`
- `ScreenFlowTests.cs`

## Boundaries

- `dotnet test` must be enough to run this suite. Do not require env vars, custom ports, or manual app startup.
- The fixture self-hosts the built WASM assets on a stable local origin for Playwright.
- Verify routed flows in a real browser.
- Click real controls instead of only checking static HTML.

## Project-Local Commands

- `node /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`

## Applicable Skills

- `playwright`

## Local Risks Or Protected Areas

- Keep selectors stable via `data-testid` whenever possible.
- Stable origin and media permissions are part of the runtime contract. Do not reintroduce random ports or manual startup steps.
- Flaky browser tests are failures; fix the cause instead of weakening the assertion.
