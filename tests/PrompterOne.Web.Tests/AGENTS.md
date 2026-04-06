# AGENTS.md

## Project Purpose

`PrompterOne.Web.Tests` verifies routed Razor screens with bUnit under TUnit.

## Entry Points

- `AppShell/*`
- `Diagnostics/*`
- `Editor/*`
- `GoLive/*`
- `Library/*`
- `Localization/*`
- `Reader/*`
- `Settings/*`
- `Teleprompter/*`
- `Support/*`

## Boundaries

- Cover rendered UI structure and meaningful user-visible interactions.
- Keep the harness close to the real shared-service registration shape.
- Mirror `PrompterOne.Shared` feature slices instead of collecting unrelated tests in one root.
- Do not add smoke-only placeholders; every test here must assert a meaningful UI contract.
- Do not replace browser acceptance tests; this project complements them.
- Prefer shared `PrompterOne.Shared.Contracts` constants over inline test ids, route literals, and DOM ids.
- Use dedicated test-attribute selectors via helpers instead of repeating raw selector strings in each test. Prefer `data-test-id`, allow `data-test`, and keep existing `data-testid` contracts stable until an intentional migration replaces them.
- Inline magic numbers and seeded values are forbidden; use named constants.

## Project-Local Commands

- `dotnet test --project ./tests/PrompterOne.Web.Tests/PrompterOne.Web.Tests.csproj`

## Applicable Skills

- `playwright` is usually not needed here; use it only when a bUnit failure needs correlation with real browser behavior

## Local Risks Or Protected Areas

- Keep dedicated test selectors and exact design landmarks stable.
- Test support wiring must stay compatible with `PrompterOne.Shared` service registration and saved browser-state behavior.
- If a bUnit assertion needs a new stable UI landmark, add a dedicated test attribute to production code instead of locking onto brittle markup structure.
