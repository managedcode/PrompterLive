# AGENTS.md

Project: `PrompterLive`
Stack: `.NET 10`, Blazor WebAssembly, Razor Class Library, xUnit, bUnit, Playwright

## Current Shape

`PrompterLive` is now a standalone browser-first WebAssembly app.

- `src/PrompterLive.App` is the only runnable host.
- `src/PrompterLive.Shared` contains the routed Razor UI, exact `new-design` styling, and browser interop.
- `src/PrompterLive.Core` contains TPS, RSVP, preview, workspace, media-scene, and streaming domain logic.
- `tests/` contains all automated test projects.
- `new-design/` is the visual and interaction source of truth.

There is no backend in the runtime shape. The app must boot directly in the browser with `dotnet run` on the WebAssembly project.

## Mandatory Rules

- Keep the solution in `PrompterLive.slnx`.
- Keep all production projects under `src/`.
- Keep all test projects under `tests/`.
- Treat `new-design/index.html`, `new-design/tokens.css`, `new-design/components.css`, `new-design/styles.css`, and `new-design/app.js` as the exact design reference.
- Do not re-invent the UI when the answer should be “port the markup and classes from `new-design`”.
- Do not introduce a server host for the app runtime.
- Preserve stable `data-testid` selectors on core flows because the Playwright suite depends on them.

## Commands

- `dotnet build PrompterLive.slnx`
- `cd /Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.App && dotnet run`
- `dotnet test tests/PrompterLive.Core.Tests/PrompterLive.Core.Tests.csproj`
- `dotnet test tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
- `dotnet test tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
- `dotnet test PrompterLive.slnx`
- `dotnet format PrompterLive.slnx`
- `node tests/PrompterLive.App.UITests/bin/Debug/net10.0/.playwright/package/cli.js install chromium`

Do not override the app URL with `--urls` or random ports. Media permissions are origin-bound, so local development must stay on the stable launch-settings origin.

## Testing Policy

- `PrompterLive.Core.Tests` verifies TPS, RSVP, workspace, media-scene, and streaming domain behavior.
- `PrompterLive.App.Tests` verifies the routed Razor screens with bUnit.
- `PrompterLive.App.UITests` is the browser acceptance layer. It must launch the standalone WASM app, click real controls, and verify page-to-page flows.
- UI regressions are not done until the Playwright suite is green.

## Architecture Map

- Read [docs/Architecture.md](/Users/ksemenenko/Developer/PrompterLive/docs/Architecture.md) before non-trivial changes.
- If a native embedded browser host is ever reintroduced, also read [docs/MacEmbeddedWebViewPermissions.md](/Users/ksemenenko/Developer/PrompterLive/docs/MacEmbeddedWebViewPermissions.md) before touching macOS or BlazorWebView camera/microphone access.
- Read the nearest local `AGENTS.md` before editing inside that project.

## Local AGENTS

- [src/PrompterLive.App/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.App/AGENTS.md)
- [src/PrompterLive.Core/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.Core/AGENTS.md)
- [src/PrompterLive.Shared/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.Shared/AGENTS.md)
- [tests/PrompterLive.Core.Tests/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.Core.Tests/AGENTS.md)
- [tests/PrompterLive.App.Tests/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/AGENTS.md)
- [tests/PrompterLive.App.UITests/AGENTS.md](/Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/AGENTS.md)

## Preferred Skills

- `playwright` for browser verification and UI-flow debugging.
