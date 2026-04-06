# AGENTS.md

## Project Purpose

`PrompterOne.Core.Tests` verifies the domain layer with TUnit and Shouldly-backed assertions.

## Entry Points

- `Tps/*`
- `Workspace/*`
- `Media/*`
- `Streaming/*`
- `Rsvp/*`
- `Editor/*`
- `Localization/*`
- `Support/*`

## Boundaries

- Cover public domain behavior, not Blazor rendering.
- Keep assertions on caller-visible contracts and serialized state.
- Mirror `PrompterOne.Core` slices where possible instead of accumulating flat test roots.
- Do not move browser concerns into this project.

## Project-Local Commands

- `dotnet test --project ./tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj`

## Applicable Skills

- no special skill is required; use root repo rules and architecture docs

## Local Risks Or Protected Areas

- Do not weaken TPS or RSVP regression coverage.
- If a production bug is fixed in `PrompterOne.Core`, add or tighten a regression test here first.
