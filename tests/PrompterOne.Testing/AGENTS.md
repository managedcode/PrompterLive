# AGENTS.md

## Project Purpose

`PrompterOne.Testing` is the shared test-support library for reusable assertion helpers, runner configuration, and cross-project test infrastructure.

## Entry Points

- `ShouldlyAssert.cs`
- `EnvironmentAwareParallelLimitBase.cs`
- `TestEnvironment.cs`

## Boundaries

- Keep only reusable test infrastructure here; do not move feature-specific test cases or app behavior assertions into this project.
- Shared helpers must stay test-only and must not leak into production projects.
- Prefer stable low-level helpers that reduce duplication across multiple test projects.

## Project-Local Commands

- `dotnet build ./tests/PrompterOne.Testing/PrompterOne.Testing.csproj`

## Applicable Skills

- no special skill is required; use root repo rules and nearest consuming test-project rules

## Local Risks Or Protected Areas

- Shared helpers affect multiple suites at once; keep API changes minimal and rerun every consuming suite after edits.
- Do not turn this project into a dumping ground for feature-specific assertions or fixtures.
