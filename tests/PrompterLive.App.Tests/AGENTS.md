# AGENTS.md

## Project Purpose

`PrompterLive.App.Tests` verifies routed Razor screens with bUnit.

## Entry Points

- `ScreenShellContractTests.cs`
- `SettingsInteractionTests.cs`
- `EditorMetadataInteractionTests.cs`
- `EditorMarkupRendererTests.cs`
- `TeleprompterSceneTests.cs`
- `TestSupport.cs`

## Boundaries

- Cover rendered UI structure and meaningful user-visible interactions.
- Keep the harness close to the real shared-service registration shape.
- Do not add smoke-only placeholders; every test here must assert a meaningful UI contract.
- Do not replace browser acceptance tests; this project complements them.
- Prefer shared `PrompterLive.Shared.Contracts` constants over inline test ids, route literals, and DOM ids.
- Use `data-testid` selectors via helpers instead of repeating raw selector strings in each test.
- Inline magic numbers and seeded values are forbidden; use named constants.

## Project-Local Commands

- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`

## Applicable Skills

- `playwright` is usually not needed here; use it only when a bUnit failure needs correlation with real browser behavior

## Local Risks Or Protected Areas

- Keep `data-testid` and exact design landmarks stable.
- Test support wiring must stay compatible with `PrompterLive.Shared` service registration and saved browser-state behavior.
- If a bUnit assertion needs a new stable UI landmark, add the `data-testid` to production code instead of locking onto brittle markup structure.
