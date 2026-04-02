# AGENTS.md

## Project Purpose

`PrompterOne.App` is the standalone Blazor WebAssembly host.

## Entry Points

- `Program.cs`
- `App.razor`
- `wwwroot/index.html`
- `Properties/launchSettings.json`

## Boundaries

- Keep this project as a thin host.
- Do not add a server runtime here.
- Prefer routed UI in `PrompterOne.Shared`.
- Do not move domain logic from `PrompterOne.Core` into the host.

## Project-Local Commands

- `dotnet build ./src/PrompterOne.App/PrompterOne.App.csproj`
- `cd ./src/PrompterOne.App && dotnet run`
- Always run the project build immediately before `dotnet run` so the standalone host cannot boot stale client assets.

## Applicable Skills

- `playwright` for real browser checks against the standalone host

## Local Risks Or Protected Areas

- Any dependency that assumes ASP.NET server hosting is a red flag.
- Keep static asset references aligned with `PrompterOne.Shared`.
- Keep the launch-settings origin stable. Do not teach the repo to run on random ports because browser media permissions are bound to origin.
- If the launch-settings port is already occupied, stop the stale dev-server process. Do not move the standalone app host to a new port as a workaround.
- Keep browser WASM debugging opt-in. Default launch profiles must not inject `inspectUri`; add explicit debug-only profiles instead.
- If a macOS embedded host returns later, it must use a persistent `WKWebViewConfiguration`, a stable trusted origin, and a `WKUIDelegate` that handles media-capture permission requests.
