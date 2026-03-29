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
- This suite is intentionally non-parallel and owns `http://localhost:5051` while it runs.
- Do not keep separate concurrent `dotnet build` or `dotnet test` processes alive against the same test assets.
- Prefer `PrompterLive.Shared.Contracts.AppRoutes`, `UiTestIds`, and other named constants over inline route or selector strings.
- Use `data-testid` first. Raw text, role-name, and CSS selectors are allowed only when no stable `data-testid` contract exists yet and the missing contract is fixed in the same task.
- Magic numbers in waits, delays, seeded values, and timeouts must be named constants.

## Project-Local Commands

- `node /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`

## Applicable Skills

- `playwright`

## Local Risks Or Protected Areas

- Keep selectors stable via `data-testid` whenever possible.
- Stable origin and media permissions are part of the runtime contract. Do not reintroduce random ports or manual startup steps.
- Flaky browser tests are failures; fix the cause instead of weakening the assertion.
- Do not duplicate route strings, test ids, or storage keys across tests; centralize them.
