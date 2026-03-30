# Diagnostics Shell

## Scope

`PrompterLive` uses three branded diagnostics surfaces:

- recoverable in-app banner via `DiagnosticsBanner`
- fatal UI crash fallback via `LoggingErrorBoundary`
- bootstrap fallback markup in `index.html` for host-level failures before Blazor is interactive
- browser connectivity overlay in routed Blazor UI via `BrowserConnectivityService` and `ConnectivityOverlay`

## Flow

```mermaid
flowchart TD
    Action["User action / browser state"]
    Service["UiDiagnosticsService"]
    Banner["DiagnosticsBanner"]
    Boundary["LoggingErrorBoundary"]
    Connectivity["BrowserConnectivityService"]
    Overlay["ConnectivityOverlay"]
    Bootstrap["index.html bootstrap shell"]

    Action --> Service
    Service --> Banner
    Action --> Boundary
    Boundary --> Service
    Action --> Connectivity
    Connectivity --> Overlay
    Action --> Bootstrap
```

## Rules

- `#blazor-error-ui` keeps the standard Blazor id, but it must render in PrompterLive styling.
- Standalone WASM has no server reconnect modal, so browser connectivity states are surfaced through the branded Blazor overlay instead of a global shell script.
- Runtime diagnostics must expose stable `data-testid` hooks for browser coverage.
- Fatal and recoverable diagnostics labels must come from the shared localization catalog.
