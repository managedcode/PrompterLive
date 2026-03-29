# Diagnostics

## Intent

PrompterLive stays browser-only, so local debugging has to be visible inside the UI. Recoverable failures should surface as a dismissible banner, while unhandled render exceptions should land in a fatal fallback instead of failing silently.

## Main Flow

```mermaid
flowchart LR
    Page["Routed page action"]
    Service["UiDiagnosticsService"]
    Banner["DiagnosticsBanner"]
    Boundary["LoggingErrorBoundary"]
    Logger["ILogger"]

    Page --> Service
    Service --> Logger
    Service --> Banner
    Boundary --> Logger
    Boundary --> Service
```

## Behavior

- recoverable page operations run through `UiDiagnosticsService.RunAsync`
- `UiDiagnosticsService` logs `Information` at operation start/success and `Error` on failure
- recoverable failures render a top-level banner with the user-facing message and exception detail
- unhandled render exceptions go through `LoggingErrorBoundary`, log `Critical`, and render a retry/library fallback
- editor TPS syntax issues surface as recoverable diagnostics without leaking front matter into the visible editor body

## Verification

- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj --filter "FullyQualifiedName~DiagnosticsTests"`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --filter "FullyQualifiedName~DiagnosticsUiTests" --no-build`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx`
