# AGENTS.md

## Project Purpose

`PrompterOne.Web.UITests` is the browser acceptance layer for the standalone WASM app.

## Entry Points

- `Infrastructure/*`
- `AppShell/*`
- `Diagnostics/*`
- `Editor/*`
- `GoLive/*`
- `Learn/*`
- `Library/*`
- `Localization/*`
- `Media/*`
- `Scenarios/*`
- `Teleprompter/*`
- `Support/*`

## Boundaries

- `dotnet test` must be enough to run this suite. Do not require env vars, custom ports, or manual app startup.
- The fixture self-hosts the built WASM assets on a dynamically assigned local loopback origin for Playwright.
- Each fixture startup MUST request a fresh OS-assigned loopback port. Never hardcode or reuse a fixed browser-test port across runs.
- The fixture also injects a deterministic synthetic media harness before page scripts run, so browser tests can verify camera and microphone flows without real hardware.
- Keep browser specs grouped by routed feature or cross-cutting acceptance concern instead of returning to a flat file dump.
- Verify routed flows in a real browser.
- Click real controls instead of only checking static HTML.
- This suite is the primary acceptance gate for the product.
- Major workflows must be covered by long scenario tests, not only narrow regression tests.
- Major scenario tests must save screenshots under `output/playwright/`.
- This suite resolves its origin at runtime and uses one `dotnet test` process with up to `4` local parallel TUnit workers; lower the CI worker cap only when repeated full-suite runs prove resource contention.
- Do not keep separate concurrent `dotnet build` or `dotnet test` processes alive against the same test assets.
- Prefer `PrompterOne.Shared.Contracts.AppRoutes`, `UiTestIds`, and other named constants over inline route or selector strings.
- Use one dedicated test-attribute format only: `data-test`.
- CSS-class selectors, style-driven selectors, and DOM-shape selectors are forbidden in browser assertions; add or reuse a dedicated `data-test` hook instead.
- Browser-test JavaScript snippets must receive dedicated `data-test` ids from C# constants and resolve nodes from those contracts, never from raw style/class selectors.
- Magic numbers in waits, delays, seeded values, and timeouts must be named constants.

## Project-Local Commands

- `node ./tests/PrompterOne.Web.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- `dotnet test --project ./tests/PrompterOne.Web.UITests/PrompterOne.Web.UITests.csproj`

## Applicable Skills

- `playwright`

## Local Risks Or Protected Areas

- Keep selectors stable via dedicated test attributes whenever possible.
- Do not introduce or keep CSS-class selector dependencies in this suite.
- Production media permissions still depend on the stable launch-settings origin, but this synthetic browser harness must use the fixture-resolved dynamic loopback origin. Do not hardcode ports or require manual startup steps.
- Flaky browser tests are failures; fix the cause instead of weakening the assertion.
- Do not duplicate route strings, test ids, or storage keys across tests; centralize them.
