# Go Live Runtime

## Scope

`Go Live` is the dedicated browser-only operational studio surface for switching real scene inputs, previewing the current on-air camera, and arming live destinations that are already configured in browser storage.

The current page layout is a production-style studio surface:

- top session bar follows `new-design/golive.html`: back to Read, script title + session badge, centered session timer, panel toggles, mode switch, settings shortcut, REC, and the main stream action on the far right
- the studio shell follows the same three-column grid as `new-design/golive.html`: a compact left input rail, a dominant center canvas/program stage, and a dedicated right operational rail
- left input rail for scene cameras, add-camera action, utility sources, and microphone route status
- center program stage for the selected program source and current script/session state
- scene controls bar for scene chips, layout controls, transitions, and the primary `Take To Air` action
- right rail for the current live preview plus compact stream, audio, and room/runtime panels
- full-program mode collapses both side rails so the center canvas follows the design's focused monitor state
- destination cards that arm OBS, recording, LiveKit, and YouTube from persisted settings instead of editing credentials inline

The runtime now owns real browser media outputs for the composed program scene and the current audio bus:

- `Go Live` auto-seeds the first available browser camera into the scene when the scene is empty and the browser exposes a real camera list
- the center program stage always shows the currently selected scene camera, while the right preview rail shows the currently on-air camera until the operator takes the selected source live
- `Go Live` builds one browser-side program stream from the scene camera cards by drawing the selected primary camera full-frame and then layering additional included cameras as positioned overlays on a canvas
- the scene `AudioBus` is mixed into one program audio track through `AudioContext`, delay, and gain nodes before the final program stream is published or recorded
- OBS browser output stays browser-only and exposes the composed program audio inside an OBS Browser Source environment
- LiveKit publishing uses the vendored browser SDK and publishes real composed `MediaStreamTrack` objects for video and audio
- local recording uses the browser `MediaRecorder` API against the same composed program stream and prefers `showSaveFilePicker()` for real local file writing when the browser allows it, with download fallback otherwise
- recording export profiles are honest browser probes only: the runtime checks `MediaRecorder.isTypeSupported()` and `MediaCapabilities.encodingInfo()` before choosing the resolved MIME type and falls back to a supported browser codec/container when the requested profile is unavailable
- stream start, stop, recording start, recording stop, and source switching update the active browser output session instead of only flipping local UI state
- the session timer and right-rail `Status` / `Runtime` cards are driven from live session/runtime state, not hardcoded demo telemetry

Relay-only destinations stay configuration surfaces:

- YouTube, Twitch, custom RTMP, and similar RTMP-style targets still persist credentials and routing in browser storage
- `Go Live` only exposes quick arm/disarm toggles and readiness summaries for those targets
- detailed destination credentials, ingest URLs, and provider-specific configuration live in `Settings`
- these targets do not publish directly from the browser runtime; they require an external relay or ingest layer outside this standalone WASM app

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
    participant Providers as "LiveKit / OBS runtime"
    participant Reader as "TeleprompterPage"

    User->>Settings: Configure camera, FPS, mic, sync
    Settings->>Studio: Persist device preferences
    Settings->>Scene: Persist scene cameras and audio bus
    User->>Settings: Configure LiveKit / YouTube / recording settings
    Settings->>Studio: Persist provider credentials and destinations
    User->>GoLive: Open Go Live
    GoLive->>Studio: Load live routing settings
    GoLive->>Scene: Load current scene sources
    GoLive->>Sources: Render real scene cameras and mic status
    GoLive->>Preview: Mount the current on-air scene camera
    GoLive->>Sidebar: Summarize persisted destination readiness
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
    Browser->>Providers: Publish LiveKit tracks / attach OBS browser audio
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
    LiveKit["LiveKitOutputProvider"]
    Vdo["VdoNinjaOutputProvider"]
    Rtmp["RtmpStreamingOutputProvider"]
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
    BrowserRuntime --> LiveKitSdk
    BrowserRuntime --> ObsBrowser
    Page --> LiveKit
    Page --> Vdo
    Page --> Rtmp
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
    LiveKit["LiveKit publishTrack(...)"]
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
    Program --> LiveKit
    Program --> Obs
```

## Rules

- `Settings` must not own live destination routing anymore.
- `Settings` owns provider credentials, ingest endpoints, and detailed streaming configuration. `Go Live` may only arm or disarm those persisted targets and link back to `Settings` for setup.
- `Settings` must expose a visible CTA into `Go Live` so device setup and live routing stay discoverable as separate flows.
- the shared header shell must keep `Go Live` reachable from every non-`Go Live` routed page because it is a primary studio action
- `Go Live` may arm multiple destinations at the same time.
- `Go Live` must reuse the browser-composed scene and not invent a separate media graph.
- `Go Live` must auto-seed the first available browser camera into the scene when the scene is empty and devices are available.
- `Go Live` must show the selected program source in the center monitor and the currently on-air source in the right preview rail until the operator explicitly takes the selected source live.
- `Go Live` must show a stable empty preview state instead of mounting camera interop when the current scene has no cameras.
- any shared `Go Live` localized copy must come from `PrompterOne.Shared.Localization.UiTextCatalog`, so supported browser cultures localize the studio surface without feature-local string copies.
- quick destination cards must only expose honest readiness summaries and arm/disarm toggles; fake in-page credential editors are forbidden on the operational studio surface
- legacy streaming settings must normalize to the current included program cameras so existing browser storage keeps working
- `VirtualCamera` mode normalizes to OBS armed by default, so browser sessions keep the legacy desktop-capture workflow unless the user explicitly turns OBS off
- Camera source inclusion is persisted through `MediaSceneState`.
- Destination credentials and endpoints are persisted only in browser storage for this standalone runtime.
- LiveKit browser publishing must use the vendored SDK shipped in the repo, not a CDN copy.
- OBS browser integration must stay a thin browser bridge; no server relay or backend media graph is introduced.
- local recording must stay browser-local and use the same active program media session as OBS / LiveKit so record and source switching stay in sync
- local recording must prefer real local file writing through the File System Access API when the browser exposes it, but must fall back to browser download instead of pretending save-to-disk is universally available
- recording codec/container export must never advertise unsupported browser encoders as if they are guaranteed; the runtime must probe support and choose a real fallback profile
- right-rail telemetry must never show fake packet-loss, jitter, ping, or upload metrics when the browser runtime does not actually own those measurements
- remote room UI must not render fake guest personas; when no real remote guest transport exists, it may only show honest local-host state and persisted room identity
- Browser acceptance verifies `Go Live` preview and source switching against deterministic synthetic cameras, not only against static DOM state.
- Browser acceptance for LiveKit and OBS verifies real `getUserMedia` audio/video requests and runtime session state, not only button labels.

## Exception Notes

- `src/PrompterOne.Shared/wwwroot/media/go-live-media-compositor.js` temporarily exceeds the root `file_max_loc` limit because canvas compositing, shared device capture, and audio-bus graph ownership are tightly coupled around browser-only APIs. Scope: only the browser program graph. Removal plan: split video composition and audio graph helpers once the pipeline stabilizes and the recording/export profile surface is no longer moving.
- `src/PrompterOne.Shared/wwwroot/media/go-live-output-support.js` temporarily exceeds the root `file_max_loc` limit because request normalization, codec probing, and local-save recording helpers must currently stay aligned with the browser runtime contract in one place. Scope: request normalization and browser recording/export helpers. Removal plan: split request normalization from recording/export helpers after the profile mapping rules settle.
