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

## Vertical Slice Layout

- `src/PrompterLive.Shared` keeps routed UI in feature slices: `AppShell`, `Diagnostics`, `Editor`, `Library`, `Learn`, `Teleprompter`, `GoLive`, `Settings`, and `Media`.
- `src/PrompterLive.Core` keeps host-neutral behavior in matching domain slices: `Tps`, `Editor`, `Workspace`, `Library`, `Rsvp`, `Media`, `Streaming`, `Localization`, and `Samples`.
- `tests/PrompterLive.Core.Tests`, `tests/PrompterLive.App.Tests`, and `tests/PrompterLive.App.UITests` mirror those feature slices and reserve `Support` or `Infrastructure` for shared harness code.

## Build Governance

- `Directory.Packages.props` is the canonical source for NuGet package versions.
- `Directory.Build.props` is the canonical source for shared target framework, analyzer policy, and assembly/app version settings.
- `global.json` pins the expected .NET SDK for local and CI builds.

## Runtime Boundaries

```mermaid
flowchart TD
    Browser["Browser"]
    WasmHost["PrompterLive.App"]
    Ui["PrompterLive.Shared"]
    Domain["PrompterLive.Core"]
    Localization["Culture bootstrap + localized UI catalog"]
    BrowserStorage["Browser storage adapters<br/>documents + folders + settings"]
    WebApis["localStorage / MediaDevices / Canvas / JS helpers"]

    Browser --> WasmHost
    WasmHost --> Ui
    Ui --> Domain
    WasmHost --> Localization
    Ui --> Localization
    Ui --> BrowserStorage
    BrowserStorage --> WebApis
    Ui --> WebApis
```

## Library Contracts

```mermaid
flowchart LR
    LibraryPage["LibraryPage + Library components"]
    FolderChips["LibraryFolderChips<br/>main-pane folder shortcuts"]
    ScriptRepo["IScriptRepository"]
    FolderRepo["ILibraryFolderRepository"]
    CardFactory["LibraryCardFactory"]
    TreeBuilder["LibraryFolderTreeBuilder"]
    LocalStorage["localStorage adapters"]

    LibraryPage --> FolderChips
    LibraryPage --> ScriptRepo
    LibraryPage --> FolderRepo
    LibraryPage --> CardFactory
    LibraryPage --> TreeBuilder
    ScriptRepo --> LocalStorage
    FolderRepo --> LocalStorage
```

## Editor Authoring Contracts

```mermaid
flowchart LR
    SourcePanel["EditorSourcePanel<br/>body-only source textarea + highlight overlay"]
    ToolbarCatalog["EditorToolbarCatalog<br/>descriptor-driven toolbar + floating bar"]
    StructureSidebar["EditorStructureSidebar<br/>tree navigation only"]
    MetadataRail["EditorMetadataRail<br/>front matter + speed offsets"]
    LocalAi["EditorLocalAssistant<br/>direct toolbar and floating-button rewrite helpers"]
    Page["EditorPage"]
    FrontMatter["TpsFrontMatterDocumentService"]
    TextEditor["TpsTextEditor<br/>wrap / insert / clear-color"]
    StructureEditor["TpsStructureEditor"]
    Session["IScriptSessionService"]

    ToolbarCatalog --> SourcePanel
    SourcePanel --> Page
    StructureSidebar --> Page
    MetadataRail --> Page
    Page --> LocalAi
    Page --> FrontMatter
    Page --> TextEditor
    Page --> StructureEditor
    Page --> Session
```

## Diagnostics Contracts

```mermaid
flowchart LR
    WasmHost["PrompterLive.App<br/>ILogger configuration"]
    Layout["MainLayout + DiagnosticsBanner"]
    Boundary["LoggingErrorBoundary"]
    Shell["index.html + app.css shell overlays"]
    ShellJs["prompterlive.js shell connectivity handlers"]
    Diagnostics["UiDiagnosticsService"]
    Pages["Library / Editor / Learn / Teleprompter / Go Live / Settings"]
    Browser["BrowserSettingsStore"]
    Session["ScriptSessionService"]

    WasmHost --> Shell
    WasmHost --> ShellJs
    WasmHost --> Diagnostics
    Pages --> Diagnostics
    Browser --> WasmHost
    Session --> WasmHost
    Diagnostics --> Layout
    Boundary --> Diagnostics
    ShellJs --> Shell
```

```mermaid
sequenceDiagram
    participant User
    participant Page as "Routed page"
    participant Diagnostics as "UiDiagnosticsService"
    participant Logger as "ILogger"
    participant Boundary as "LoggingErrorBoundary"
    participant Shell as "Shell overlay"

    User->>Page: Trigger load/save/device action
    Page->>Diagnostics: RunAsync(operation, message, action)
    Diagnostics->>Logger: Information start/success
    alt Recoverable failure
        Diagnostics->>Logger: Error with exception
        Diagnostics-->>Page: Current recoverable entry
        Page-->>User: Banner with dismiss action
    else Unhandled render failure
        Boundary->>Logger: Critical exception
        Boundary->>Diagnostics: ReportFatal(...)
        Boundary-->>User: Fatal fallback with retry/library actions
    else Browser offline or bootstrap failure
        Shell-->>User: Styled reconnect or bootstrap error overlay
    end
```

## Media Permission Model

- Browser-first WASM is the only active runtime today, so media access comes from browser origin permissions.
- Keep local development on the stable launch-settings origin. Do not rotate ports randomly because camera and microphone permissions are origin-bound.
- The Playwright browser-test harness is a separate synthetic environment. It may bind to a dynamic loopback origin, but it must pass the resolved origin into the Playwright browser context and permission grants.
- There is no server backend in the runtime path. `getUserMedia()` and device enumeration must stay client-side.

## Browser Media Test Harness

```mermaid
flowchart LR
    UITests["PrompterLive.App.UITests"]
    Fixture["StandaloneAppFixture"]
    InitScript["synthetic-media-harness.js<br/>BrowserContext.addInitScript"]
    Browser["Chromium context<br/>dynamic loopback origin + granted permissions"]
    MediaApis["navigator.mediaDevices<br/>enumerateDevices + getUserMedia"]
    Synthetic["Synthetic cameras + microphone<br/>canvas.captureStream + Web Audio"]
    Reader["Teleprompter runtime"]
    GoLive["Go Live preview/runtime"]

    UITests --> Fixture
    Fixture --> InitScript
    Fixture --> Browser
    InitScript --> MediaApis
    MediaApis --> Synthetic
    Reader --> MediaApis
    GoLive --> MediaApis
```

- Browser acceptance now installs a deterministic synthetic media harness before page scripts run.
- The static SPA host now binds to a dynamic loopback HTTP port and exposes the resolved origin through the fixture.
- The harness overrides `enumerateDevices()` and `getUserMedia()` inside the Playwright browser context only.
- Synthetic video comes from `canvas.captureStream()`.
- Synthetic audio comes from `AudioContext.createMediaStreamDestination()`.
- `teleprompter`, `settings`, and `go-live` tests assert real `MediaStream` attachment through `video.srcObject`, not only CSS state.

If a native embedded browser host returns later, media access must not rely on system permission alone. Follow the dedicated macOS note in [MacEmbeddedWebViewPermissions.md](./MacEmbeddedWebViewPermissions.md).

## Project Responsibilities

### `src/PrompterLive.App`

- standalone Blazor WebAssembly host
- serves the app shell and static asset references
- applies browser-language culture selection before the WASM runtime starts rendering routed UI
- must stay free of server-only runtime dependencies

### `src/PrompterLive.Shared`

- routed Razor screens: `library`, `editor`, `learn`, `teleprompter`, `go-live`, `settings`
- vertical slices own their routed pages, components, renderers, and feature-local services under folders such as `Editor/`, `Library/`, `Teleprompter/`, and `GoLive/`
- only true cross-cutting UI assets stay outside feature slices: `Contracts/`, `Localization/`, `wwwroot/`, root bootstrap files, and `AppShell/`
- exact design shell and imported `new-design` assets
- shared UI localization catalog for supported browser cultures
- browser interop and app DI wiring
- dynamic library folder components and folder/document browser storage adapters
- UI diagnostics banner and global error boundary
- debounced editor autosave and body-only TPS source authoring
- centered RSVP ORP playback in `learn`
- single background camera layer under text in `teleprompter`
- dedicated `go-live` routing surface that arms multiple live destinations while reusing the same browser-composed scene
- settings split between device setup (`settings`) and destination routing (`go-live`)

Rules:

- keep markup aligned with `new-design`
- do not move business logic here if it belongs in `Core`
- preserve `data-testid` selectors for browser tests

### `src/PrompterLive.Core`

- feature slices keep related abstractions, models, previews, and services together under `Tps/`, `Editor/`, `Workspace/`, `Library/`, `Rsvp/`, `Media/`, and `Streaming/`
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
    participant Live as "Go Live"

    User->>Library: Open app
    Library->>FolderStore: Load folder tree
    Library->>ScriptStore: Load local scripts
    User->>Library: Create folder / move script
    Library->>FolderStore: Persist folder changes
    Library->>ScriptStore: Persist folder assignment
    Library->>Editor: Open or create script
    Editor->>Learn: Rehearse via RSVP
    Editor->>Live: Arm destinations
    Editor->>Reader: Enter teleprompter mode
    Live->>Reader: Keep teleprompter available in parallel
    Reader->>Library: Return to library
```

## Go Live Contracts

```mermaid
flowchart LR
    Settings["SettingsPage<br/>device setup only"]
    GoLive["GoLivePage<br/>destination routing"]
    Hero["GoLiveHero"]
    Preview["GoLiveCameraPreviewCard"]
    Program["GoLiveProgramFeedCard"]
    Sources["GoLiveSourcesCard"]
    TargetSources["GoLiveDestinationSourcePicker"]
    Studio["StudioSettingsStore"]
    Routing["GoLiveDestinationRouting"]
    Scene["IMediaSceneService"]
    CameraInterop["CameraPreviewInterop"]
    Providers["IStreamingOutputProvider[]"]
    Reader["TeleprompterPage"]

    Settings --> Studio
    Settings --> Scene
    Settings --> GoLive
    GoLive --> Studio
    GoLive --> Scene
    GoLive --> Routing
    GoLive --> Providers
    GoLive --> Reader
    GoLive --> Hero
    GoLive --> Preview
    GoLive --> Program
    GoLive --> Sources
    GoLive --> TargetSources
    Preview --> CameraInterop
    Preview --> Scene
    Routing --> Studio
    Routing --> Scene
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

- `PrompterLive.Core.Tests`: domain correctness and regression tests grouped by core slice plus `Support/`
- `PrompterLive.App.Tests`: bUnit coverage grouped by routed feature slice plus `Support/`
- `PrompterLive.App.UITests`: Playwright browser flows grouped by browser feature slice plus `Infrastructure/`, `Scenarios/`, `Media/`, and `Support/`

## Constraints

- The runtime must remain backend-free.
- Browser-language localization must default to English and support `en`, `uk`, `fr`, `es`, `pt`, and `it`.
- Russian is intentionally unsupported and must fall back to English.
- Visual fidelity should prefer copying the exact design classes and structure over inventing replacements.
- Browser tests require Playwright Chromium to be installed locally.
- Build verification is expected to pass with `-warnaserror`.
- Editor metadata belongs in the right metadata rail and must not be rendered as visible front matter in the source editor.
- `learn` must keep the ORP letter aligned to the center guide while stepping words.
- `teleprompter` must render camera only as a background layer; overlay camera boxes are not part of the current reference UI.
- If macOS embedding returns later, use a persistent `WKWebView` data store, a stable trusted origin, and explicit `requestMediaCapturePermissionFor` handling so camera and microphone prompts are not repeated on every launch.
