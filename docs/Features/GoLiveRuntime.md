# Go Live Runtime

## Scope

`Go Live` is the browser-only operational studio for `PrompterOne`.

It owns:

- source switching for local scene cameras and remote guest sources
- one browser-owned program feed
- local recording from that same program feed
- live transport startup and stop for `VDO.Ninja` and `LiveKit`
- right-rail runtime telemetry and downstream target status

It does not own:

- provider credential editing
- source inventory or per-device sync offsets
- any PrompterOne-managed relay, ingest, encoder, or media server

`Settings` is the source of truth for:

- `ProgramCaptureProfile`
- `RecordingProfile`
- `TransportConnectionProfile`
- `DistributionTargetProfile`

## Runtime Shape

The runtime now uses explicit `sources + program + sinks` layers.

- `Source modules`
  - local browser scene sources
  - `VDO.Ninja` remote intake
  - `LiveKit` remote intake
- `Program capture`
  - one canonical composed `MediaStream`
  - owns canvas composition, overlay composition, and audio mix
- `Sink modules`
  - local recording
  - `VDO.Ninja` publish
  - `LiveKit` publish
  - downstream transport-aware targets

The browser compositor is the single source of truth. All sinks reuse that same program feed.

## Main Rules

- There is no legacy local-output path in the runtime architecture.
- There is no backward compatibility for the old local-output settings shape or the old local-output UI concepts.
- `VDO.Ninja` and `LiveKit` may both publish concurrently when both transport connections are armed.
- Local recording is a first-class sink and is not modeled as a fake external destination.
- Downstream targets such as `YouTube`, `Twitch`, and `Custom RTMP` are bound to transport connections and are only activatable when the chosen transport exposes that path honestly.
- Unsupported downstream paths must be shown as blocked, not silently degraded.

## Operator Surface

The routed `Go Live` page keeps the design shell:

- top session bar
- left source rail
- center program monitor
- scene controls bar
- right operational rail

The right rail now renders destination rows from two persisted collections:

- `TransportConnections`
- `DistributionTargets`

Local recording stays controlled by the `REC` action and runtime metadata instead of showing up as a fake destination row.

## Architecture

```mermaid
flowchart LR
    Settings["Settings<br/>capture, recording, transport, targets"]
    Sources["Source modules<br/>local scene + remote guests"]
    Program["Program capture<br/>one browser MediaStream"]
    Recording["Recording sink"]
    LiveKit["LiveKit output module"]
    Vdo["VDO.Ninja output module"]
    Targets["Distribution targets"]
    GoLive["Go Live rails"]

    Settings --> Sources
    Settings --> Program
    Settings --> Recording
    Settings --> LiveKit
    Settings --> Vdo
    Settings --> Targets
    Sources --> Program
    Program --> Recording
    Program --> LiveKit
    Program --> Vdo
    LiveKit --> Targets
    Vdo --> Targets
    GoLive --> Sources
    GoLive --> Program
    GoLive --> Recording
    GoLive --> LiveKit
    GoLive --> Vdo
    GoLive --> Targets
```

## Main Flow

```mermaid
sequenceDiagram
    participant User
    participant Settings as "SettingsPage"
    participant GoLive as "GoLivePage"
    participant Scene as "IMediaSceneService"
    participant Runtime as "GoLiveOutputRuntimeService"
    participant Browser as "go-live-output.js"
    participant Program as "Program capture"
    participant Recorder as "Recording sink"
    participant LiveKit as "LiveKit transport"
    participant Vdo as "VDO.Ninja transport"
    participant Targets as "Distribution targets"

    User->>Settings: Configure capture, recording, transport connections, targets
    Settings->>Scene: Persist local scene sources
    Settings->>GoLive: Persist streaming profiles in browser storage
    User->>GoLive: Open studio
    GoLive->>Scene: Load current scene
    GoLive->>Runtime: Build runtime request from scene + streaming settings
    Runtime->>Browser: Start or update browser session
    Browser->>Program: Compose canvas + audio mix
    User->>GoLive: Start recording
    Runtime->>Recorder: Record the canonical program stream
    User->>GoLive: Arm VDO.Ninja and/or LiveKit
    Runtime->>Vdo: Publish the canonical program stream
    Runtime->>LiveKit: Publish the canonical program stream
    LiveKit-->>Targets: Report relay-capable target state
    Vdo-->>Targets: Report direct browser target state when supported
    User->>GoLive: Switch source or layout
    Browser->>Program: Recompose the same program stream
```

## Contracts

The browser runtime is driven by these contracts:

- `IGoLiveProgramCaptureService`
- `IGoLiveSourceModule`
- `IGoLiveOutputModule`
- `IGoLiveModuleRegistry`

The persisted settings model is:

- `ProgramCaptureProfile`
- `RecordingProfile`
- `TransportConnectionProfile`
- `DistributionTargetProfile`

```mermaid
flowchart LR
    Page["GoLivePage"]
    Registry["IGoLiveModuleRegistry"]
    Source["IGoLiveSourceModule"]
    Capture["IGoLiveProgramCaptureService"]
    Output["IGoLiveOutputModule"]
    Request["GoLiveOutputRuntimeRequest"]
    Browser["go-live-output.js"]
    Vdo["go-live-output-vdo-ninja.js"]
    LiveKit["vendored livekit-client"]

    Page --> Registry
    Registry --> Source
    Registry --> Output
    Page --> Capture
    Page --> Request
    Request --> Browser
    Browser --> Vdo
    Browser --> LiveKit
    Capture --> Browser
    Output --> Browser
```

## Browser Pipeline

```mermaid
flowchart LR
    Scene["Scene cameras + transforms"]
    Audio["Audio bus inputs + delay/gain"]
    Factory["GoLiveOutputRequestFactory"]
    Runtime["GoLiveOutputRuntimeService"]
    Support["go-live-output-support.js"]
    Browser["go-live-output.js"]
    Program["Composed MediaStream"]
    Recording["MediaRecorder"]
    LiveKit["LiveKit publish session"]
    Vdo["VDO.Ninja publish session"]
    Targets["Distribution targets"]

    Scene --> Factory
    Audio --> Factory
    Factory --> Runtime
    Runtime --> Support
    Runtime --> Browser
    Browser --> Program
    Program --> Recording
    Program --> LiveKit
    Program --> Vdo
    LiveKit --> Targets
    Vdo --> Targets
```

## Destination Semantics

- `Transport connections`
  - can ingest remote sources, publish the program, or both
  - own room, server, token, base URL, publish URL, and view URL fields
- `Distribution targets`
  - own RTMP-style target data and bound transport connection ids
  - do not imply native browser RTMP support by themselves

Current capability model:

- `LiveKit`
  - can ingest remote sources
  - can publish the program
  - can expose downstream-target capability
- `VDO.Ninja`
  - can ingest remote sources
  - can publish the program
  - supports hosted and self-hosted base/publish/view URL paths
  - does not claim generic downstream relay capability in the current implementation

## Testing Methodology

- Browser UI verification is the primary acceptance bar.
- Component and core tests prove settings normalization, routing, and runtime-request shaping.
- Go Live browser scenarios must prove:
  - local recording can start from the composed program feed
  - `VDO.Ninja` publish can start from the composed program feed
  - `LiveKit` publish can start from the composed program feed
  - both transports can be active in one session
  - source switching updates the live program state
  - blocked downstream targets are shown honestly

## Rules

- `Settings` owns source inventory, per-device sync, program-capture defaults, recording defaults, transport connections, and downstream targets.
- `Go Live` operates those persisted settings; it must not reintroduce inline provider credential editors.
- The browser runtime must not pretend to publish generic RTMP directly unless a real transport path exists.
- The browser runtime must not invent telemetry values that the active transport or recorder does not provide.
- Local recording must continue to capture the same composed program feed that live publish uses.
- Remote guest intake and live publish must stay modular; the UI shell should not change when a module is swapped.

## Verification

- `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
- `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/PrompterOne.App.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/PrompterOne.App.UITests.csproj --no-build --filter "FullyQualifiedName~GoLive"`
