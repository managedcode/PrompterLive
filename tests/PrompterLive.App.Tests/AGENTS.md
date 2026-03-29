# AGENTS.md

## Project Purpose

`PrompterLive.App.Tests` verifies routed Razor screens with bUnit.

## Entry Points

- `PageSmokeTests.cs`
- `SettingsInteractionTests.cs`
- `EditorMetadataInteractionTests.cs`
- `EditorMarkupRendererTests.cs`
- `TeleprompterSceneTests.cs`
- `TestSupport.cs`

## Boundaries

- Cover rendered UI structure and meaningful user-visible interactions.
- Keep the harness close to the real shared-service registration shape.
- Do not replace browser acceptance tests; this project complements them.

## Project-Local Commands

- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`

## Applicable Skills

- `playwright` is usually not needed here; use it only when a bUnit failure needs correlation with real browser behavior

## Local Risks Or Protected Areas

- Keep `data-testid` and exact design landmarks stable.
- Test support wiring must stay compatible with `PrompterLive.Shared` service registration and saved browser-state behavior.
