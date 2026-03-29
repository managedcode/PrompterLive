# logging-diagnostics.plan

Chosen brainstorm: `logging-diagnostics.brainstorm.md`

## Goal

Add structured logging and actionable UI error handling so failures in the standalone WASM app are visible, traceable, and easier to debug.

## Scope

In scope:
- configure `ILogger` for the WASM host
- add a shared diagnostics service for logged UI operations and surfaced recoverable errors
- add a global error boundary for unhandled UI exceptions
- instrument key routed pages with the diagnostics service
- add automated tests for the diagnostics flow
- update architecture and feature docs

Out of scope:
- remote log shipping
- backend observability
- full instrumentation of every core service

## Constraints And Risks

- keep the app backend-free
- preserve `new-design` visual fidelity while adding an error surface
- do not turn pages into giant nested `try/catch` blocks

## Testing Methodology

Flows covered:
- recoverable page operation failure is logged and shown to the user
- unhandled render exception is caught by a global boundary and surfaced
- normal library/editor/runtime flows stay green

How they are tested:
- xUnit/bUnit for diagnostics service and rendered error UI
- Playwright for browser-visible diagnostics behavior when possible

Quality bar:
- the app logs through `ILogger`
- recoverable failures produce a visible message and a structured log entry
- unhandled UI exceptions do not fail silently

## Ordered Plan

- [x] Step 1. Write the brainstorm and choose the direction.
- [x] Step 2. Write this plan.
- [x] Step 3. Run the baseline verification suite and record results.
  Verification:
  - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet test /Users/ksemenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
  - `dotnet test /Users/ksemenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build`
- [x] Step 4. Add the shared diagnostics and error-boundary architecture.
  Verification: run focused component tests for diagnostics behavior.
- [x] Step 5. Instrument key UI operations and add automated tests.
  Verification: rerun focused component tests and relevant Playwright flows.
- [x] Step 6. Update architecture and feature docs.
  Verification: inspect rendered docs and ensure diagrams still match the code.
- [x] Step 7. Run final validation.
  Verification:
  - `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
  - `dotnet test /Users/ksemenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
  - `dotnet test /Users/ksemenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build`
  - `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`

## Baseline Failures

- None recorded. Focused diagnostics baseline was green before and after instrumentation.
