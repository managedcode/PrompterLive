# AGENTS.md

## Purpose

`PrompterLive.App.UITests` is the browser acceptance layer for the standalone WASM app.

## Rules

- `dotnet test` must be enough to run this suite. Do not require env vars, custom ports, or manual app startup.
- The fixture self-hosts the built WASM assets on a stable local origin for Playwright.
- Verify routed flows in a real browser.
- Click real controls instead of only checking static HTML.
- Keep selectors stable when possible via `data-testid`.

## Commands

- `node /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
