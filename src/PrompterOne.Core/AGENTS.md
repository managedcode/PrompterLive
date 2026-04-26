# AGENTS.md

## Project Purpose

`PrompterOne.Core` is the host-neutral domain layer.

It owns TPS parsing, compilation, export, RSVP helpers, preview and workspace state, media scene models, and streaming provider descriptors.

## Entry Points

- `Tps/*`
- `Editor/*`
- `Workspace/*`
- `Library/*`
- `Rsvp/*`
- `Media/*`
- `Streaming/*`
- `Localization/*`
- `AI/*`

## Boundaries

- No Blazor dependencies.
- No JavaScript interop.
- No browser or server runtime assumptions.
- Do not add browser-only, WebAssembly-only, or host-specific NuGet overrides here; keep those package pins in the runnable host project.
- Keep abstractions, models, preview helpers, and services colocated inside their owning feature slices.
- Keep types serializable and reusable from the WebAssembly app.

## Project-Local Commands

- `dotnet build ./src/PrompterOne.Core/PrompterOne.Core.csproj`
- `dotnet test ./tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj`

## Applicable Skills

- no special skill is required for most core work; follow the root repo policy and architecture map first

## Local Risks Or Protected Areas

- TPS compatibility matters more than cosmetic refactors.
- `Models/` and legacy `Services/` roots should not become new flat dumping grounds; prefer the owning feature slice first.
- Do not let UI-specific shortcuts leak into domain parsing or RSVP behavior.
- AI agent infrastructure belongs under `AI/*`; keep one focused class per concrete agent type, colocate that agent's system prompt and skill references with the agent class, prefer one factory that assembles predefined agents and workflows over separate catalog layers, and wire skills or article context through official `AgentSkillsProvider` or `AIContextProvider` support instead of hand-built prompt concatenation.
- AI script graph extraction must prioritize writer-facing knowledge from the document text: themes, terms, entities, characters, concepts, claims, recurring references, and block-to-block relationships should be explicit graph concepts, while TPS/source mechanics stay metadata unless they add semantic meaning. The graph model should help a scriptwriter understand what the script is about, how ideas and blocks connect, and where to jump back into the source.
- AI script graph labels, descriptions, semantic scopes, tokenizer chunks, and knowledge-graph markdown input must come from compiled TPS display text through the TPS SDK so they match the clean prompter text; raw TPS source is allowed only for source ranges and structural metadata, not post-hoc string-cleaned visible prose.
- AI script graph semantic extraction must use an available LLM/chat-client path as the primary extractor. Regex, stop-word, capitalization, keyword, or hardcoded-domain semantic heuristics are strictly forbidden; when no LLM graph extraction is configured, the app may either show no semantic graph or run only an explicit user-requested tokenizer/vector similarity fallback based on `Microsoft.ML.Tokenizers` token vectors and distance calculations.
- Script graph tokenizer/vector fallback must use the tokenizer/vector primitives exposed by `ManagedCode.MarkdownLd.Kb` when that package provides them; do not keep a duplicate Core-owned tokenizer implementation.
- Script graph extraction must track the current `ManagedCode.MarkdownLd.Kb` graph model and use its metadata/front matter primitives such as entity hints, graph groups, related links, next steps, focused graph/search, and token topics before adding PrompterOne-owned graph glue.
- Script graph output should feel like an Obsidian-style writer knowledge graph: documents, people, topics, backlinks, related ideas, and neighborhood metadata are primary; TPS mechanics remain source metadata unless they explain writing meaning.
- AI and agent services must not use ad-hoc language heuristics for semantic meaning, intent matching, or action similarity. Prefer LLM extraction; the only non-LLM fallback allowed for similarity or search is explicit tokenizer/vector similarity based on `Microsoft.ML.Tokenizers`.
- Respect root maintainability limits. Large parser/compiler edits need explicit decomposition or documented exceptions.
