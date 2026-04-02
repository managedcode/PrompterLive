# Go Live Runtime

## Scope

`Go Live` is the dedicated browser-only operational studio surface for switching real scene inputs, previewing the current on-air camera, and arming live destinations that are already configured in browser storage.

The current page layout is a production-style studio surface:

- the routed `Go Live` page owns its own studio chrome and suppresses the shared app header while the route is active, so `design/golive.html` remains the only topbar on that screen
- top session bar follows `design/golive.html`: back to Library, script title + session badge, centered session timer, panel toggles, mode switch, settings shortcut, REC, and the main stream action on the far right
- the studio shell follows the same three-column grid as `design/golive.html`: a compact left input rail, a dominant center canvas/program stage, and a dedicated right operational rail
- left input rail for scene cameras, add-camera action, utility sources, and microphone route status
- center program stage for the selected program source and current script/session state
- scene controls bar for scene chips, layout controls, transitions, and the primary `Take To Air` action
- right rail for the current live preview plus a larger stream, audio, and runtime panel that can comfortably show real output telemetry, metadata, and mix controls
- source-card `ON AIR` badges and the preview red live dot only turn on when recording or streaming is actually active; idle routing and armed sources stay visually non-live
- full-program mode collapses both side rails so the center canvas follows the design's focused monitor state
- destination cards are built only from the persisted browser-stored `ExternalDestinations` list; local recording is controlled from `REC` plus runtime metadata, not rendered as a fake destination row

The runtime now owns real browser media outputs for the composed program scene and the current audio bus:

- `Go Live` auto-seeds the first available browser camera into the scene when the scene is empty and the browser exposes a real camera list
- the center program stage always shows the currently selected scene camera, while the right preview rail shows the current program source and only marks it live once recording or streaming is active
- `Go Live` builds one browser-side program stream from the scene camera cards by drawing the selected primary camera full-frame; additional included cameras are layered as positioned overlays only when the operator explicitly chooses a multi-source layout instead of the default `Full` layout
- the scene `AudioBus` is mixed into one program audio track through `AudioContext`, delay, and gain nodes before the final program stream is published or recorded
- OBS browser output stays browser-only and exposes the composed program audio inside an OBS Browser Source environment
- `VDO.Ninja` publishing uses the vendored official browser SDK and publishes the same composed `MediaStream` that powers preview, recording, and OBS
- `LiveKit` publishing remains available only as an explicit optional managed-transport mode
- local recording uses the browser `MediaRecorder` API against the same composed program stream and prefers `showSaveFilePicker()` for real local file writing when the browser allows it, with download fallback otherwise
- saved local recording artifacts must decode into visible program video plus audible program audio; a black frame or silent file is a runtime regression even if metadata looks populated
- recording export profiles are honest browser probes only: the runtime checks `MediaRecorder.isTypeSupported()` and `MediaCapabilities.encodingInfo()` before choosing the resolved MIME type and falls back to a supported browser codec/container when the requested profile is unavailable
- stream start, stop, recording start, recording stop, and source switching update the active browser output session instead of only flipping local UI state
- the session timer and right-rail `Status` / `Runtime` cards are driven from live session/runtime state, not hardcoded demo telemetry
- active local recording surfaces resolved runtime metadata in the right rail, including the recording profile, save mode, and live captured file size when the browser exposes those details

Downstream destinations stay browser-honest configuration surfaces:

- YouTube, Twitch, custom RTMP, and similar targets still persist credentials and routing in browser storage
- `Go Live` only exposes quick arm/disarm toggles and readiness summaries for those targets
- detailed destination credentials, ingest URLs, and provider-specific configuration live in `Settings`
- direct browser publish is allowed only when the active standalone spine really supports it, with `VDO.Ninja` treated as the primary path for browser-driven `YouTube` / `Twitch` workflows
- custom RTMP remains visually constrained unless a real browser-compatible publish path is confirmed

## Broadcast Spine Decision

[ADR-0003](/Users/ksemenenko/Developer/PrompterOne/docs/ADR/ADR-0003-go-live-broadcast-spine-and-relay-architecture.md) locks the target architecture for this feature.

The important distinction is:

- the browser still composes one program feed
- local recording still uses that same program feed first
- one upstream browser transport spine is active for a session
- under the standalone rule, the browser remains the only PrompterOne runtime and does not rely on a PrompterOne-owned media server
- downstream publishing is limited to real browser-compatible paths, not assumed generic RTMP fan-out

Current implementation focus:

- browser-local recording from the composed program feed
- OBS browser output
- `VDO.Ninja` publishing in the current code path
- `LiveKit` kept only as an explicit optional managed mode
- no shipped universal browser-only path yet for every final platform target

Target rollout after this architecture pass:

- make the active upstream spine explicit in `Settings`
- keep `VDO.Ninja` as the primary standalone spine because it fits the director-room browser model
- keep `LiveKit` only as an explicit optional managed-transport mode
- expose downstream destination health only for targets that have a real browser-compatible publish path, with `YouTube` and `Twitch` treated as valid VDO-driven targets

```mermaid
flowchart LR
    Settings["Settings<br/>sources, delay, quality, spine choice"]
    Scene["Scene cameras + AudioBus"]
    Program["Browser Program Feed"]
    Record["Local Recording"]
    Spine["Active upstream spine"]
    Vdo["VDO.Ninja primary standalone mode"]
    LiveKit["LiveKit optional managed mode"]
    Targets["Browser-compatible publish targets"]
    Platforms["YouTube / Twitch / RTMP"]
    GoLive["Go Live rails<br/>left operate, right observe"]

    Settings --> Scene
    Scene --> Program
    Program --> Record
    Program --> Spine
    Spine --> Vdo
    Spine --> LiveKit
    Vdo --> Targets
    Targets --> Platforms
    GoLive --> Program
    GoLive --> Spine
    GoLive --> Targets
```

It is separate from:

- `Settings`, which owns device setup, provider credentials, ingest endpoints, and other detailed studio configuration such as camera selection, resolution, FPS, microphones, and audio sync
- `Teleprompter`, which owns the read experience and can run alongside the armed live configuration

## Main Flow

```mermaid
sequenceDiagram
    participant User
    participant Settings as "SettingsPage"
    participant GoLive as "GoLivePage"
    participant Sources as "GoLiveSourcesCard"
    participant Controls as "GoLiveSceneControls"
    participant Preview as "GoLiveCameraPreviewCard"
    participant Sidebar as "GoLiveStudioSidebar"
    participant Studio as "StudioSettingsStore"
    participant Scene as "IMediaSceneService"
    participant Runtime as "GoLiveOutputRuntimeService"
    participant Browser as "go-live-output.js"
    participant Spine as "Active upstream spine"
    participant Targets as "Browser-compatible publish targets"
    participant Reader as "TeleprompterPage"

    User->>Settings: Configure camera, FPS, mic, sync
    Settings->>Studio: Persist device preferences
    Settings->>Scene: Persist scene cameras and audio bus
    User->>Settings: Configure the active spine and browser-compatible publish targets
    Settings->>Studio: Persist external destination list and recording/export preferences
    User->>GoLive: Open Go Live
    GoLive->>Studio: Load live routing settings
    GoLive->>Scene: Load current scene sources
    GoLive->>Sources: Render real scene cameras and mic status
    GoLive->>Preview: Mount the current on-air scene camera
    GoLive->>Sidebar: Summarize persisted external destination readiness
    User->>GoLive: Arm one or more destinations
    GoLive->>Studio: Persist output targets
    User->>Sources: Select a different scene camera
    Sources->>GoLive: Update selected program source
    User->>Controls: Take selected source to air
    Controls->>GoLive: Promote selected source to active source
    User->>GoLive: Start stream
    GoLive->>Runtime: Build request from scene snapshot + audio bus + recording export prefs
    Runtime->>Browser: Start or update browser output session
    Browser->>Browser: Compose program canvas + mix audio bus
    Browser->>Spine: Publish the active upstream session / attach OBS browser audio
    Spine-->>Targets: Publish only where a real browser path exists
    User->>GoLive: Start recording
    GoLive->>Runtime: Build request from scene snapshot + audio bus + recording export prefs
    Runtime->>Browser: Start MediaRecorder on the composed program session
    User->>GoLive: Switch source
    GoLive->>Runtime: Update scene-backed program request
    Browser->>Browser: Recompose the same program stream with the new primary source
    User->>Reader: Open teleprompter
    Reader->>Scene: Reuse same scene cameras under text
```

## Contracts

```mermaid
flowchart LR
    Page["GoLivePage"]
    SessionBar["session bar"]
    PreviewRail["preview + studio rail"]
    Preview["GoLiveCameraPreviewCard"]
    Program["GoLiveProgramFeedCard"]
    Sources["GoLiveSourcesCard"]
    SceneControls["GoLiveSceneControls"]
    Sidebar["GoLiveStudioSidebar"]
    Studio["StreamStudioSettings"]
    Scene["MediaSceneState"]
    AudioBus["AudioBusState"]
    CameraInterop["CameraPreviewInterop"]
    Runtime["GoLiveOutputRuntimeService"]
    RuntimeInterop["GoLiveOutputInterop"]
    OutputSupport["go-live-output-support.js"]
    BrowserRuntime["go-live-output.js"]
    Composer["go-live-media-compositor.js"]
    Spine["Active upstream spine"]
    LiveKit["LiveKitOutputProvider"]
    Vdo["VdoNinjaOutputProvider<br/>primary standalone mode"]
    Targets["Browser-compatible publish targets"]
    LiveKitSdk["vendored livekit-client"]
    ObsBrowser["OBS Browser Source"]

    Page --> SessionBar
    Page --> PreviewRail
    Page --> Preview
    Page --> Program
    Page --> Sources
    Page --> SceneControls
    Page --> Sidebar
    Page --> Studio
    Page --> Scene
    Page --> AudioBus
    Page --> Runtime
    Preview --> CameraInterop
    Runtime --> RuntimeInterop
    RuntimeInterop --> OutputSupport
    RuntimeInterop --> BrowserRuntime
    BrowserRuntime --> Composer
    BrowserRuntime --> ObsBrowser
    Page --> Spine
    Sidebar --> Spine
    Spine --> Vdo
    Spine --> LiveKit
    LiveKit --> LiveKitSdk
    Vdo --> Targets
```

## Runtime Pipeline

```mermaid
flowchart LR
    Scene["Scene cameras + transforms"]
    Audio["Audio bus inputs + gains + delay"]
    Factory["GoLiveOutputRequestFactory"]
    Runtime["GoLiveOutputRuntimeService"]
    Support["go-live-output-support.js"]
    Composer["go-live-media-compositor.js"]
    Canvas["canvas.captureStream()"]
    Mix["AudioContext + MediaStreamDestination"]
    Program["Composed MediaStream"]
    Recorder["MediaRecorder"]
    Save["File picker or download fallback"]
    Spine["Active upstream spine"]
    Vdo["VDO.Ninja publish / WHIP"]
    LiveKit["LiveKit publishTrack(...)"]
    Targets["Browser-compatible publish targets"]
    Platforms["YouTube / Twitch / RTMP<br/>only when the active spine exposes a real browser path"]
    Obs["OBS browser audio bridge"]

    Scene --> Factory
    Audio --> Factory
    Factory --> Runtime
    Runtime --> Support
    Runtime --> Composer
    Composer --> Canvas
    Composer --> Mix
    Canvas --> Program
    Mix --> Program
    Program --> Recorder
    Recorder --> Save
    Program --> Spine
    Spine --> Vdo
    Spine --> LiveKit
    Vdo --> Targets
    Targets --> Platforms
    Program --> Obs
```

## Rules

- `Settings` must not own live destination routing anymore.
- `Settings` owns provider credentials, ingest endpoints, detailed streaming configuration, source inventory, per-device delay/sync offsets, and the active upstream-spine choice. `Go Live` may only arm or disarm those persisted targets and link back to `Settings` for setup.
- `Settings` must expose a visible CTA into `Go Live` so device setup and live routing stay discoverable as separate flows.
- the shared header shell must keep `Go Live` reachable from every non-`Go Live` routed page because it is a primary studio action
- `Go Live` may arm local recording and downstream relay targets at the same time, but only one upstream browser transport spine may be active for a session.
- hardcoded destination instances are forbidden; the right-rail destination list must come from persisted external destinations only and may contain zero, one, or many platform entries
- `Go Live` must reuse the browser-composed scene and not invent a separate media graph.
- `Go Live` must auto-seed the first available browser camera into the scene when the scene is empty and devices are available.
- `Go Live` must show the selected program source in the center monitor and the currently on-air source in the right preview rail until the operator explicitly takes the selected source live.
- `Go Live` left rail stays the operational source-control surface, and the right rail stays the runtime output panel for honest metadata, telemetry, and mix controls.
- `Go Live` must not render `ON AIR` source badges or red preview live dots while the session is idle; those indicators only represent active recording or streaming.
- `Go Live` must show a stable empty preview state instead of mounting camera interop when the current scene has no cameras.
- the routed `Go Live` page must not stack the shared app header above the studio topbar; the studio topbar is the only route chrome on that screen
- any shared `Go Live` localized copy must come from `PrompterOne.Shared.Localization.UiTextCatalog`, so supported browser cultures localize the studio surface without feature-local string copies.
- quick destination cards must only expose honest readiness summaries and arm/disarm toggles; fake in-page credential editors are forbidden on the operational studio surface
- legacy streaming settings must normalize to the current included program cameras and migrate legacy provider fields into the canonical external destination list so existing browser storage keeps working
- `VirtualCamera` mode normalizes to OBS armed by default, so browser sessions keep the legacy desktop-capture workflow unless the user explicitly turns OBS off
- Camera source inclusion is persisted through `MediaSceneState`.
- Destination credentials and endpoints are persisted only in browser storage for this standalone runtime.
- `VDO.Ninja` browser publishing must use the vendored official SDK shipped in the repo, not a CDN copy.
- LiveKit browser publishing must use the vendored SDK shipped in the repo, not a CDN copy.
- `VDO.Ninja` is the default production upstream spine for the strict standalone browser mode; `LiveKit` is reserved for explicit optional managed-transport modes and must not quietly become a second co-equal default publish path.
- OBS browser integration must stay a thin browser bridge; no server relay or backend media graph is introduced.
- local recording must stay browser-local and use the same active program media session as OBS / `VDO.Ninja` / optional `LiveKit` so record and source switching stay in sync
- local recording must prefer real local file writing through the File System Access API when the browser exposes it, but must fall back to browser download instead of pretending save-to-disk is universally available
- recording codec/container export must never advertise unsupported browser encoders as if they are guaranteed; the runtime must probe support and choose a real fallback profile
- local recording metadata shown in `Go Live` must come from browser runtime state, including honest file-size/save-mode/profile details when available, instead of placeholder labels or blank cards
- browser acceptance for local recording must verify the saved artifact itself, not only the in-page runtime metadata, so regressions such as black video or silent audio cannot hide behind populated telemetry
- the `Audio` tab must project real browser telemetry: the microphone row comes from a direct browser microphone monitor, while `Program` and `Recording` come from the mixed program audio feed and must fall back to idle instead of seeded percentages when no signal is active
- right-rail telemetry must never show fake packet-loss, jitter, ping, or upload metrics when the browser runtime does not actually own those measurements
- downstream platforms such as YouTube and Twitch may be shown as first-class live-publish targets when the chosen VDO-driven standalone path supports them; custom RTMP must stay visibly constrained unless a real browser-compatible path is confirmed
- remote room UI must not render fake guest personas; when no real remote guest transport exists, it may only show honest local-host state and persisted room identity
- Browser acceptance verifies `Go Live` preview and source switching against deterministic synthetic cameras, not only against static DOM state.
- Browser acceptance for the active upstream spine and OBS verifies real `getUserMedia` audio/video requests and runtime session state, not only button labels.

## Exception Notes

- `src/PrompterOne.Shared/wwwroot/media/go-live-media-compositor.js` temporarily exceeds the root `file_max_loc` limit because canvas compositing, shared device capture, and audio-bus graph ownership are tightly coupled around browser-only APIs. Scope: only the browser program graph. Removal plan: split video composition and audio graph helpers once the pipeline stabilizes and the recording/export profile surface is no longer moving.
- `src/PrompterOne.Shared/wwwroot/media/go-live-output-support.js` temporarily exceeds the root `file_max_loc` limit because request normalization, codec probing, and local-save recording helpers must currently stay aligned with the browser runtime contract in one place. Scope: request normalization and browser recording/export helpers. Removal plan: split request normalization from recording/export helpers after the profile mapping rules settle.
- `src/PrompterOne.Shared/GoLive/Pages/GoLivePage.razor.css` temporarily exceeds the root `file_max_loc` limit because the routed parity pass keeps the top session bar, left/right rail toggles, full-program layout states, and responsive shell rules in one isolated stylesheet to stay visually locked to `design/golive.html`. Scope: `Go Live` routed shell only. Removal plan: extract the topbar shell into a dedicated routed-shell component stylesheet once the layout stops moving.
- `src/PrompterOne.Shared/GoLive/Components/GoLiveStudioSidebar.razor.css` temporarily exceeds the root `file_max_loc` limit because stream destinations, the audio mixer treatment, and room empty/active states still share one right-rail component while the design parity pass settles. Scope: `Go Live` right rail only. Removal plan: split the sidebar into stream, audio, and room subcomponents with isolated styles after the parity work is accepted.
