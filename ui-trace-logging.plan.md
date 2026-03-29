# UI Trace Logging Plan

Reference brainstorm: `ui-trace-logging.brainstorm.md`

## Goal

Add focused `ILogger` trace points for SPA navigation and library folder interactions, with automated tests that prove the logs are emitted.

## Scope

In scope:

- `MainLayout` navigation lifecycle logs
- `LibraryPage` folder-selection and folder-overlay logs
- component tests with `RecordingLoggerProvider`

Out of scope:

- verbose per-keystroke logging
- browser console instrumentation
- remote telemetry

## Testing Methodology

- use bUnit with the existing `RecordingLoggerProvider`
- verify that meaningful UI actions emit expected log categories/messages
- keep assertions at the event level rather than the full exact formatted line

## Ordered Plan

- [x] Add `ILogger` instrumentation to `MainLayout`.
- [x] Add `ILogger` instrumentation to `LibraryPage`.
- [x] Add component tests for navigation logs.
- [x] Add component tests for folder overlay and folder selection logs.
- [x] Run focused diagnostics/component tests.
- [x] Run broader app component tests if focused regressions are green.

## Validation Notes

- `MainLayout` now logs SPA bridge attach, client navigation requests, and route changes.
- `LibraryPage` now logs folder selection, folder overlay open/cancel, and created folder ids.
- Focused log assertions are green:
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj --filter "FullyQualifiedName~UiTraceLoggingTests"`
- Broader component validation is green:
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj --no-build`
  - Result: `35` tests passed
- Browser acceptance remains green after the logging changes:
  - `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build`
  - Result: `20` tests passed in `3 m 36 s`
- `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` is green.
- `dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx` completed successfully, while reporting existing analyzer items without automatic fixes.
