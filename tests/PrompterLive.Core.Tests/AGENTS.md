# AGENTS.md

## Project Purpose

`PrompterLive.Core.Tests` verifies the domain layer with xUnit.

## Entry Points

- `TpsRoundTripTests.cs`
- `ScriptSessionServiceTests.cs`
- `MediaSceneServiceTests.cs`
- `StreamingProviderTests.cs`
- `RsvpEmotionAnalyzerTests.cs`

## Boundaries

- Cover public domain behavior, not Blazor rendering.
- Keep assertions on caller-visible contracts and serialized state.
- Do not move browser concerns into this project.

## Project-Local Commands

- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.Core.Tests/PrompterLive.Core.Tests.csproj`

## Applicable Skills

- no special skill is required; use root repo rules and architecture docs

## Local Risks Or Protected Areas

- Do not weaken TPS or RSVP regression coverage.
- If a production bug is fixed in `PrompterLive.Core`, add or tighten a regression test here first.
