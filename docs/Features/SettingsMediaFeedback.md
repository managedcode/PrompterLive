# Settings Media Feedback

## Scope

`Settings` owns device setup for the standalone browser runtime.

The camera and microphone sections now provide live feedback directly on the setup screen so the user can confirm:

- the selected camera is the one currently producing video
- mirror settings are visually obvious before opening Reader or Go Live
- the selected microphone is actively producing input signal in the browser

This feature stays separate from `Go Live` routing and from teleprompter playback.

## Main Flow

```mermaid
sequenceDiagram
    participant User
    participant Settings as "SettingsPage"
    participant CameraCard as "SettingsCameraPreviewCard"
    participant MicCard as "SettingsMicrophoneLevelCard"
    participant Permissions as "IMediaPermissionService"
    participant Devices as "IMediaDeviceService"
    participant CameraInterop as "CameraPreviewInterop"
    participant MicInterop as "MicrophoneLevelInterop"
    participant Browser as "navigator.mediaDevices + AudioContext"

    User->>Settings: Open Cameras or Microphones
    Settings->>Permissions: Query or request browser access
    Settings->>Devices: Load visible camera and microphone devices
    Settings->>CameraCard: Pass selected camera + active section state
    Settings->>MicCard: Pass selected microphone + active section state
    CameraCard->>CameraInterop: Attach or detach preview stream
    MicCard->>MicInterop: Start or stop live level monitor
    CameraInterop->>Browser: getUserMedia(video)
    MicInterop->>Browser: getUserMedia(audio) + analyser
    Browser-->>CameraCard: Live video stream
    Browser-->>MicCard: Live signal level
    User->>Settings: Adjust camera mirror, gain, or default devices
    Settings->>CameraCard: Refresh preview target or transform
    Settings->>MicCard: Refresh active microphone target
```

## Contracts

```mermaid
flowchart LR
    Page["SettingsPage"]
    CameraCard["SettingsCameraPreviewCard"]
    MicCard["SettingsMicrophoneLevelCard"]
    CameraInterop["CameraPreviewInterop"]
    MicInterop["MicrophoneLevelInterop"]
    Scene["IMediaSceneService"]
    Studio["StudioSettingsStore"]
    Browser["browser-media.js thin bridge"]

    Page --> CameraCard
    Page --> MicCard
    Page --> Scene
    Page --> Studio
    CameraCard --> CameraInterop
    MicCard --> MicInterop
    CameraInterop --> Browser
    MicInterop --> Browser
```

## Rules

- live camera preview must stay in `Settings`, not in the shared header or `Go Live`
- live microphone level must reflect real browser input, not the stored gain percentage alone
- preview and monitor lifecycles must stop when their settings section is no longer active
- the microphone meter UI must stay Blazor-owned; JS may sample browser audio and report numeric levels only
- UI contracts for the feedback cards must use stable shared `UiTestIds` and `UiDomIds`
- browser acceptance must verify real synthetic media attachment and live activity through the deterministic media harness
- this document is about setup feedback only; routing remains documented in [GoLiveRuntime.md](./GoLiveRuntime.md)
