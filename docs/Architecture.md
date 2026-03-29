# PrompterLive Architecture

## Intent

`PrompterLive` is a standalone Blazor WebAssembly teleprompter app.

The acceptance target is a browser-only runtime that:

- matches the local `new-design/` UI closely
- parses and exports TPS content
- supports RSVP learn mode and teleprompter reading mode
- keeps media, scene, and streaming state client-side
- ships all automated tests under `tests/`

There is no backend in the runtime architecture.

## Solution Layout

```mermaid
flowchart LR
    App["src/PrompterLive.App<br/>Standalone WASM host"]
    Shared["src/PrompterLive.Shared<br/>Razor pages, layout, CSS, JS interop"]
    Core["src/PrompterLive.Core<br/>TPS, RSVP, workspace, media, streaming"]
    NewDesign["new-design/<br/>HTML/CSS/JS reference + parser sources"]
    Tests["tests/*<br/>xUnit + bUnit + Playwright"]

    App --> Shared
    Shared --> Core
    Shared -. visual parity .-> NewDesign
    Core -. extracted logic .-> NewDesign
    Tests --> App
    Tests --> Shared
    Tests --> Core
```

## Runtime Boundaries

```mermaid
flowchart TD
    Browser["Browser"]
    WasmHost["PrompterLive.App"]
    Ui["PrompterLive.Shared"]
    Domain["PrompterLive.Core"]
    BrowserStorage["Browser storage adapters<br/>documents + folders + settings"]
    WebApis["localStorage / MediaDevices / Canvas / JS helpers"]

    Browser --> WasmHost
    WasmHost --> Ui
    Ui --> Domain
    Ui --> BrowserStorage
    BrowserStorage --> WebApis
    Ui --> WebApis
```

## Library Contracts

```mermaid
flowchart LR
    LibraryPage["LibraryPage + Library components"]
    ScriptRepo["IScriptRepository"]
    FolderRepo["ILibraryFolderRepository"]
    CardFactory["LibraryCardFactory"]
    TreeBuilder["LibraryFolderTreeBuilder"]
    LocalStorage["localStorage adapters"]

    LibraryPage --> ScriptRepo
    LibraryPage --> FolderRepo
    LibraryPage --> CardFactory
    LibraryPage --> TreeBuilder
    ScriptRepo --> LocalStorage
    FolderRepo --> LocalStorage
```

## Media Permission Model

- Browser-first WASM is the only active runtime today, so media access comes from browser origin permissions.
- Keep local development on the stable launch-settings origin. Do not rotate ports randomly because camera and microphone permissions are origin-bound.
- There is no server backend in the runtime path. `getUserMedia()` and device enumeration must stay client-side.

If a native embedded browser host returns later, media access must not rely on system permission alone. Follow the dedicated macOS note in [MacEmbeddedWebViewPermissions.md](./MacEmbeddedWebViewPermissions.md).

## Project Responsibilities

### `src/PrompterLive.App`

- standalone Blazor WebAssembly host
- serves the app shell and static asset references
- must stay free of server-only runtime dependencies

### `src/PrompterLive.Shared`

- routed Razor screens: `library`, `editor`, `learn`, `teleprompter`, `settings`
- exact design shell and imported `new-design` assets
- browser interop and app DI wiring
- dynamic library folder components and folder/document browser storage adapters

Rules:

- keep markup aligned with `new-design`
- do not move business logic here if it belongs in `Core`
- preserve `data-testid` selectors for browser tests

### `src/PrompterLive.Core`

- TPS parser, compiler, exporter
- RSVP helpers
- workspace state and preview generation
- media scene and streaming descriptor models

Rules:

- no Blazor dependencies
- no JS interop
- no host-specific APIs

## Main User Flows

```mermaid
sequenceDiagram
    participant User
    participant Library
    participant FolderStore
    participant ScriptStore
    participant Editor
    participant Learn
    participant Reader

    User->>Library: Open app
    Library->>FolderStore: Load folder tree
    Library->>ScriptStore: Load local scripts
    User->>Library: Create folder / move script
    Library->>FolderStore: Persist folder changes
    Library->>ScriptStore: Persist folder assignment
    Library->>Editor: Open or create script
    Editor->>Learn: Rehearse via RSVP
    Editor->>Reader: Enter teleprompter mode
    Reader->>Library: Return to library
```

```mermaid
sequenceDiagram
    participant Shared as PrompterLive.Shared
    participant Core as PrompterLive.Core
    participant Browser as Browser APIs

    Shared->>Core: Parse / compile TPS
    Shared->>Core: Calculate RSVP and reader state
    Shared->>Browser: Persist settings and local documents
    Shared->>Browser: Drive design interactions from app.js
```

## Test Topology

```mermaid
flowchart LR
    CoreTests["tests/PrompterLive.Core.Tests"]
    AppTests["tests/PrompterLive.App.Tests"]
    UiTests["tests/PrompterLive.App.UITests"]

    CoreTests --> Core["src/PrompterLive.Core"]
    AppTests --> Shared["src/PrompterLive.Shared"]
    UiTests --> App["src/PrompterLive.App"]
```

## Test Strategy

- `PrompterLive.Core.Tests`: domain correctness and regression tests
- `PrompterLive.App.Tests`: bUnit screen-shell coverage for the routed UI
- `PrompterLive.App.UITests`: Playwright browser flows that click real controls on every screen

## Constraints

- The runtime must remain backend-free.
- Visual fidelity should prefer copying the exact design classes and structure over inventing replacements.
- Browser tests require Playwright Chromium to be installed locally.
- If macOS embedding returns later, use a persistent `WKWebView` data store, a stable trusted origin, and explicit `requestMediaCapturePermissionFor` handling so camera and microphone prompts are not repeated on every launch.
