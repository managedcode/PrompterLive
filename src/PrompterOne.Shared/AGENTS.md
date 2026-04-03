# AGENTS.md

## Project Purpose

`PrompterOne.Shared` contains the routed Razor UI, exact design shell, CSS assets, thin browser interop, and browser-side service wiring.

## Entry Points

- `AppShell/Routes.razor`
- `AppShell/Layout/MainLayout.razor`
- `AppShell/Services/PrompterOneServiceCollectionExtensions.cs`
- `Editor/*`
- `Library/*`
- `Learn/*`
- `Teleprompter/*`
- `GoLive/*`
- `Settings/*`
- `Diagnostics/*`
- `Media/*`
- `wwwroot/design/*`
- `Diagnostics/Services/BrowserConnectivityService.cs`
- `Diagnostics/Components/ConnectivityOverlay.razor`
- `wwwroot/media/browser-media.js`

## Boundaries

- Keep markup aligned with `design`.
- Use `design` only as a static HTML/CSS reference. Final routed UI must be authored as Blazor components and C#-owned state in this project.
- When a screen has a `design/*.html` counterpart, parity work must port the whole screen structure and intended interactions into Blazor instead of approximating only selected sections.
- Keep routed pages, feature components, renderers, and feature-local services inside their owning slice folders.
- Keep app-specific UI logic here, but keep business rules in `PrompterOne.Core`.
- Keep feature styles owned by their routed screen; `Learn` and `Teleprompter` must not share one feature stylesheet manifest.
- Preserve `data-testid` selectors used by Playwright.
- Do not add server-only dependencies.
- Keep keyboard shortcuts, screen ids/selectors, and reusable UI constants in Blazor/C# contracts when possible; leave JS only with unavoidable browser API interop and direct DOM work that Blazor cannot replace cleanly.
- Prefer removing JS files that only orchestrate UI behavior, reader state, or shell interactions; keep those flows in Razor components or C# services and let JS expose only the minimal browser or SDK call surface.
- TPS 1.1.0 removed legacy inline color tags, so editor, reader, menus, examples, and tests in this project must not expose or insert `[red]`, `[green]`, or other deprecated color-tag authoring paths.

## Project-Local Commands

- `dotnet build ./src/PrompterOne.Shared/PrompterOne.Shared.csproj`
- `dotnet test ./tests/PrompterOne.App.Tests/PrompterOne.App.Tests.csproj`
- `dotnet test ./tests/PrompterOne.App.UITests/PrompterOne.App.UITests.csproj`

## Applicable Skills

- `playwright` for routed UI verification and interaction debugging

## Local Risks Or Protected Areas

- Small class-name changes can break design fidelity badly because the CSS comes from `design`.
- `AppShell`, `Contracts`, `Localization`, and `wwwroot` are cross-cutting; do not turn them back into dumping grounds for feature code.
- Routed shell and page navigation belong in Blazor; keep remaining `wwwroot` JavaScript limited to browser/runtime interop.
- JS interop and saved browser state are part of the real runtime contract; do not treat them as decorative.
