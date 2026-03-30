# AGENTS.md

## Project Purpose

`PrompterLive.Shared` contains the routed Razor UI, exact design shell, CSS/JS assets, and browser-side service wiring.

## Entry Points

- `AppShell/Routes.razor`
- `AppShell/Layout/MainLayout.razor`
- `AppShell/Services/PrompterLiveServiceCollectionExtensions.cs`
- `Editor/*`
- `Library/*`
- `Learn/*`
- `Teleprompter/*`
- `GoLive/*`
- `Settings/*`
- `Diagnostics/*`
- `Media/*`
- `wwwroot/design/*`
- `wwwroot/prompterlive.js`

## Boundaries

- Keep markup aligned with `new-design`.
- Keep routed pages, feature components, renderers, and feature-local services inside their owning slice folders.
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
- `AppShell`, `Contracts`, `Localization`, and `wwwroot` are cross-cutting; do not turn them back into dumping grounds for feature code.
- Routed shell and page navigation belong in Blazor; keep `wwwroot/design/app.js` limited to browser/runtime interop.
- JS interop and saved browser state are part of the real runtime contract; do not treat them as decorative.
