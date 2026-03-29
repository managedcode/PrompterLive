# AGENTS.md

## Project Purpose

`PrompterLive.App` is the standalone Blazor WebAssembly host.

## Entry Points

- `Program.cs`
- `App.razor`
- `wwwroot/index.html`
- `Properties/launchSettings.json`

## Boundaries

- Keep this project as a thin host.
- Do not add a server runtime here.
- Prefer routed UI in `PrompterLive.Shared`.
- Do not move domain logic from `PrompterLive.Core` into the host.

## Project-Local Commands

- `dotnet build /Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.App/PrompterLive.App.csproj`
- `cd /Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.App && dotnet run`

## Applicable Skills

- `playwright` for real browser checks against the standalone host

## Local Risks Or Protected Areas

- Any dependency that assumes ASP.NET server hosting is a red flag.
- Keep static asset references aligned with `PrompterLive.Shared`.
- Keep the launch-settings origin stable. Do not teach the repo to run on random ports because browser media permissions are bound to origin.
- If a macOS embedded host returns later, it must use a persistent `WKWebViewConfiguration`, a stable trusted origin, and a `WKUIDelegate` that handles media-capture permission requests.
