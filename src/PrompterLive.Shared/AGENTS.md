# AGENTS.md

## Project Purpose

`PrompterLive.Shared` contains the routed Razor UI, exact design shell, CSS/JS assets, and browser-side service wiring.

## Entry Points

- `Routes.razor`
- `Layout/MainLayout.razor`
- `Pages/*`
- `Services/PrompterLiveServiceCollectionExtensions.cs`
- `wwwroot/design/*`
- `wwwroot/prompterlive.js`

## Boundaries

- Keep markup aligned with `new-design`.
- Keep app-specific UI logic here, but keep business rules in `PrompterLive.Core`.
- Preserve `data-testid` selectors used by Playwright.
- Do not add server-only dependencies.

## Project-Local Commands

- `dotnet build /Users/ksemenenko/Developer/PrompterLive/src/PrompterLive.Shared/PrompterLive.Shared.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`

## Applicable Skills

- `playwright` for routed UI verification and interaction debugging

## Local Risks Or Protected Areas

- Small class-name changes can break design fidelity badly because the CSS comes from `new-design`.
- Header behavior is driven by `wwwroot/design/app.js`; keep route mapping consistent with page ids there.
- JS interop and saved browser state are part of the real runtime contract; do not treat them as decorative.
