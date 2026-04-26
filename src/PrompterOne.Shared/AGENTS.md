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
- Preserve dedicated test selectors used by Playwright and keep one selector format only: `data-test`.
- Do not add server-only dependencies.
- Keep keyboard shortcuts, screen ids/selectors, and reusable UI constants in Blazor/C# contracts when possible; leave JS only with unavoidable browser API interop and direct DOM work that Blazor cannot replace cleanly.
- Graph and canvas viewer JavaScript must stay a thin renderer interop layer. Blazor/C# components must own graph view modes, labels, app color token mapping, and product UI vocabulary, then pass that configuration into JS instead of hardcoding those rules in script files.
- Script graph visuals must prioritize readable story knowledge: node labels and descriptions should be visible and meaningful, and line-number nodes, source-line link clutter, or clipped TPS attribute strings must not be presented as primary graph knowledge.
- Script graph labels, tooltips, semantic scopes, tokenizer chunks, and graph markdown input must render compiled TPS display text from the Core TPS SDK path, matching the clean prompter text; raw TPS markup is only allowed as hidden source-range/metadata input and must not be cleaned after the fact for visible prose.
- Script graph renderer code must delete obsolete visual branches for source-only or TPS-attribute-only node kinds such as raw `Line`, `Pace`, `Timing`, or `Cue`; those values belong in source ranges, metadata, or tooltips only when they add meaning, not as primary graph nodes or edge clutter.
- Script graph tooltips must anchor beside the hovered node or edge using pointer/object geometry; never let graph tooltips fall back to random top-left or detached viewport positions.
- Script graph colors must use subdued app-owned tokens for readable dark-surface graph nodes with semantic accents; avoid pale, saturated, or randomly mixed blobs that fight the PrompterOne palette.
- Script graph content must behave like a writer knowledge map: include meaningful ideas, themes, people, story references, and document structure that navigate back to the editor text, not only technical TPS headers or graph-layout placeholders.
- Script graph UI must expose an intentional analysis path when richer extraction is needed, and every graph mode should answer a concrete writer question such as "what is this about", "how are blocks connected", "which terms recur", "which references matter", or "where should I edit this idea".
- Script graph UI must not silently fall back to regex/keyword semantic extraction when LLM graph extraction is unavailable; regex, stop-word, capitalization, keyword, and hardcoded-domain semantic heuristics are strictly forbidden. In that state, show an explicit action to connect AI or run tokenizer/vector similarity analysis; the tokenizer path must be user-visible as a lower-fidelity fallback, not disguised as LLM semantic understanding.
- Script graph UI should surface tokenizer/vector similarity through the `ManagedCode.MarkdownLd.Kb` tokenizer path when available; do not add a separate browser or Shared-owned tokenizer implementation for the same fallback.
- Assistant spotlight suggestions and script graph UI must not use ad-hoc language heuristics for intent or similarity. Use LLM-backed semantics when available, or explicit tokenizer/vector similarity over localized action descriptions and the user query when a non-LLM fallback is needed.
- Script graph editing must support a split source/graph workflow: the user should be able to inspect the graph beside the editor, click a meaningful graph node, and have the owning source range revealed or highlighted without leaving the graph workspace.
- Script graph split mode must expose a user-resizable divider between source and graph panes, and graph view must also support a graph-only workspace mode for focused exploration.
- Script graph canvas must fill the graph panel's remaining workspace in split and graph-only modes; keep the G6 host and rendered canvas synchronized to the panel size after render, resize, and divider changes so the graph is not clipped unless the user intentionally pans or zooms.
- Script graph nodes must explain article/book meaning through themes, terms, entities, characters, concepts, claims, and references; do not let generic layout groupings or unexplained extracted snippets dominate the graph.
- Script graph controls must expose an explicit auto-layout action so users can recover a readable layout after a broken preset, resize, drag, pan, or zoom interaction without rebuilding the document.
- Script graph layout presets must remain readable with PrompterOne's rectangular text nodes; do not add or keep a G6 layout mode without explicit node-size spacing, overlap prevention, or a deterministic fallback that prevents stacked or clipped node labels.
- Script graph node shapes must fit the graph-reading task, not default to square UI cards everywhere: use graph-native visual language such as topic bubbles, entity dots, document anchors, cluster hulls, and weighted compact labels by semantic role and zoom level.
- Prefer removing JS files that only orchestrate UI behavior, reader state, or shell interactions; keep those flows in Razor components or C# services and let JS expose only the minimal browser or SDK call surface.
- TPS 1.1.0 removed legacy inline color tags, so editor, reader, menus, examples, and tests in this project must not expose or insert `[red]`, `[green]`, or other deprecated color-tag authoring paths.
- Editor dropdowns and tooltips must read as structured surfaces: item rows need a consistent visual rhythm with aligned columns or spacing, and overlay surfaces need border contrast strong enough to separate them clearly from the editor background.
- Editor dropdown rows must stay compact menu rows, not stacks of tall rounded mini-cards; overlays may feel premium, but menu items still need fast scannable list rhythm.
- Dropdown item content across `PrompterOne.Shared` must align from the left edge as one readable cluster; do not push tags, shortcuts, or meta copy to a fake right column inside menu rows.
- Editor Cards view must behave like a real card board: script cards should support drag-and-drop reordering, expose clear drag handles, and persist the resulting source order instead of relying only on up/down buttons.
- Editor Cards view must use the available workspace as a multi-column scenario board: segments act as swimlanes or grouped sections, and block cards must wrap into multiple visible columns when a segment contains many blocks instead of collapsing into one narrow vertical list.
- Editor Cards drag-and-drop must provide visible interaction feedback, including lifted-card state, drop target/position affordance, and smooth movement/settle animation so reordering reads like a real board interaction.
- Editor Cards view must be theme-aware across light and dark themes; do not hardcode white card/page backgrounds or dark-only text that bypasses shared app color tokens.
- Editor workspace authoring modes must be explicit: `Raw` is the canonical TPS source editor, `Rendered` is the visual editable authoring mode. Do not label the rendered authoring mode as generic cards when the user needs to edit without knowing Markdown or TPS syntax.
- Editor Rendered mode must expose visible cue affordances directly on the rendered blocks and segments, including tone, pace, pause, energy, volume, delivery, pronunciation, and similar TPS intent, so non-technical authors can understand and revise the script without reading raw tags.
- Editor header export actions must use one clear `Export` entry point with format choices inside it; do not render separate top-level buttons for each export format because they read as duplicated actions and crowd the shell chrome.
- Tooltip surfaces across the app must feel intentional and premium: compact, aligned, clearly separated from the background, and positioned so they do not clip, overlap, or awkwardly fight the control that owns them.
- Editor header toolbar tooltips must reveal more slowly than dropdown intent so quick menu interactions open the dropdown without competing hover tooltip paint.
- Repeated menus, dropdowns, tooltips, badges, icon rows, image wrappers, and similar visual chrome must be standardized as reusable Blazor components with owning styles; routed pages and catalog files must compose those components instead of embedding bespoke markup or inline visual logic.
- Shared shell chrome such as the global `Go Live` entry must live behind one reusable Blazor component; routed pages must not fork their own spacing, icon color, or idle/active variants.
- Editor TPS menus in `PrompterOne.Shared` must use one spec-driven grouping and command inventory across top toolbar, floating toolbar, and Monaco authoring help; if a TPS tag or header pattern is supported, every relevant editor command surface must expose it coherently.
- `PrompterOne.Shared` must provide a localized first-run onboarding flow that walks new users through PrompterOne, TPS, RSVP, Editor, Learn, Teleprompter, and Go Live, and it must persist completion or dismissal in browser-owned settings.
- `PrompterOne.Shared` onboarding must give TPS its own dedicated explainer step or page, separate from the editor overview, so new users understand what TPS is and why PrompterOne centers it.
- The TPS onboarding content in `PrompterOne.Shared` must speak to beginners directly: define TPS, explain why plain-text teleprompter markup matters, show how PrompterOne carries that source into rehearsal/reading/live flows, and point users to official TPS documentation or glossary paths for deeper orientation.
- Library and editor routed surfaces in `PrompterOne.Shared` must expose real search affordances for script names and script content instead of relying only on manual browsing.
- Teleprompter routed surfaces in `PrompterOne.Shared` must expose a direct in-reader text-size control, owned by the reader UI itself, so operators can tune readability during playback without leaving the route.
- Teleprompter width controls in `PrompterOne.Shared` must scale relative to the active viewport or screen size and persist that adaptive ratio; do not clamp the readable lane around a fixed pixel-only desktop maximum.
- Teleprompter width controls in `PrompterOne.Shared` must map to the real visible text lane; when the user drives `Read Width` to `100%`, the reader must not keep extra inner shell padding or centered shrink-wrap gaps inside that lane.
- Teleprompter speed display mode must apply consistently to every reader speed readout: when the user switches between `WPM` and multiplier display, inline cue labels and the bottom transport speed label must change together.
- Editor gutter line numbers must read as editor chrome, not as part of the script content; their color and emphasis must stay clearly separated from source text.
- Editor wrap/format commands must not partially capture TPS tag syntax or create accidental nested wrappers by spanning across existing tag boundaries; if the selection touches tag markup, normalize or reject it instead of producing broken mixed-tag output.
- File-creating editor actions such as document split must show explicit in-app feedback about what was created and where to find it; silent success states behind generic buttons are not user-friendly enough.
- Settings panels and provider cards in `PrompterOne.Shared` must render collapsed by default on first open; do not auto-expand AI, cloud, appearance, about, or similar settings cards unless the user explicitly opens them or a task-specific interaction requires it after load.
- Desktop settings layout in `PrompterOne.Shared` must use the available main content width instead of constraining routed sections to a narrow centered column with a mid-screen scrollbar; settings forms should expand across the settings workspace unless a specific sub-surface documents a narrower contract.

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
