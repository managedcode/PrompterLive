# AGENTS.md

## Project Purpose

`PrompterOne.Shared` contains the routed Razor UI, shipped runtime CSS assets, thin browser interop, and browser-side service wiring.

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

- Keep markup aligned with the shipped routed UI patterns and shared runtime CSS assets.
- Do not add or depend on the deleted root `design/` prototype folder. Final routed UI must be authored as Blazor components and C#-owned state in this project.
- For parity work, fix the whole routed screen structure and intended interactions in Blazor instead of approximating only selected sections.
- Keep routed pages, feature components, renderers, and feature-local services inside their owning slice folders.
- Keep app-specific UI logic here, but keep business rules in `PrompterOne.Core`.
- User-facing library, editor, learn, and reader summaries must come from real TPS-derived metrics, never from `display_*` metadata or other presentation-only hardcoded overrides.
- Keep feature styles owned by their routed screen; `Learn` and `Teleprompter` must not share one feature stylesheet manifest.
- Preserve dedicated test selectors used by Playwright. Prefer `data-test-id` for new markup, allow `data-test`, and do not churn existing `data-testid` contracts unless the task is an intentional migration.
- Do not add server-only dependencies.
- Keep keyboard shortcuts, screen ids/selectors, and reusable UI constants in Blazor/C# contracts when possible; leave JS only with unavoidable browser API interop and direct DOM work that Blazor cannot replace cleanly.
- Prefer removing JS files that only orchestrate UI behavior, reader state, or shell interactions; keep those flows in Razor components or C# services and let JS expose only the minimal browser or SDK call surface.
- TPS 1.1.0 removed legacy inline color tags, so editor, reader, menus, examples, and tests in this project must not expose or insert `[red]`, `[green]`, or other deprecated color-tag authoring paths.
- Editor dropdowns and tooltips must read as structured surfaces: item rows need a consistent visual rhythm with aligned columns or spacing, and overlay surfaces need border contrast strong enough to separate them clearly from the editor background.
- Editor dropdown rows must stay compact menu rows, not stacks of tall rounded mini-cards; overlays may feel premium, but menu items still need fast scannable list rhythm.
- Tooltip surfaces across the app must feel intentional and premium: compact, aligned, clearly separated from the background, and positioned so they do not clip, overlap, or awkwardly fight the control that owns them.
- Editor gutter line numbers must read as editor chrome, not as part of the script content; their color and emphasis must stay clearly separated from source text.
- Editor wrap/format commands must not partially capture TPS tag syntax or create accidental nested wrappers by spanning across existing tag boundaries; if the selection touches tag markup, normalize or reject it instead of producing broken mixed-tag output.
- File-creating editor actions such as document split must show explicit in-app feedback about what was created and where to find it; silent success states behind generic buttons are not user-friendly enough.

## Project-Local Commands

- `dotnet build ./src/PrompterOne.Shared/PrompterOne.Shared.csproj`
- `dotnet test ./tests/PrompterOne.Web.Tests/PrompterOne.Web.Tests.csproj`
- `dotnet test ./tests/PrompterOne.Web.UITests/PrompterOne.Web.UITests.csproj`

## Applicable Skills

- `playwright` for routed UI verification and interaction debugging

## Local Risks Or Protected Areas

- Small class-name changes can break visual fidelity badly because the shared runtime CSS is cross-cutting.
- `AppShell`, `Contracts`, `Localization`, and `wwwroot` are cross-cutting; do not turn them back into dumping grounds for feature code.
- Routed shell and page navigation belong in Blazor; keep remaining `wwwroot` JavaScript limited to browser/runtime interop.
- JS interop and saved browser state are part of the real runtime contract; do not treat them as decorative.
