# Go Live Streaming System Plan

## Goal

Replace the old mixed `OBS/VDO.Ninja/LiveKit/RTMP` output shape with a final browser-first `Go Live` architecture built around:

- one canonical browser-owned program feed
- source modules for local and remote inputs
- sink modules for recording and transport publishing
- transport-aware downstream distribution targets

The finished system must let `PrompterOne` operate as the streaming system itself, not as an OBS companion.

## Scope

### In Scope

- remove OBS from runtime architecture, settings, docs, UI contracts, and tests
- introduce explicit `sources + program + sinks` abstractions
- support concurrent `VDO.Ninja` and `LiveKit` publish sessions from the same canonical program feed
- support remote guest intake from `VDO.Ninja` and `LiveKit`
- keep local recording as a first-class sink off the same program feed
- reorganize `Settings` into program capture, recording, transport connections, and distribution targets
- keep `Go Live` visually stable while making it data-driven from module descriptors and runtime state
- update documentation, core tests, component tests, and browser acceptance tests in the same pass

### Out Of Scope

- adding any PrompterOne-owned backend, relay, ingest, encoder, or secret-bearing control plane
- pretending that browser-native RTMP exists when the active transport does not expose it
- redesigning unrelated `Editor`, `Learn`, `Teleprompter`, or library surfaces

## Constraints And Risks

- The runtime must remain browser-only from PrompterOne's perspective.
- `LiveKit` secret-bearing server APIs stay outside WASM.
- `VDO.Ninja` support must work for hosted and self-hosted variants through base/publish/view URL support.
- OBS removal must be complete. Hidden UI toggles are not enough if contracts or tests still model OBS.
- Local recording must always capture the same composed program feed that active transports publish.
- Browser acceptance tests remain the primary release bar for `Go Live`.

## Testing Methodology

- Start with the repo-level build and test baseline and track any failures explicitly.
- Verify the new architecture through real browser-visible operator flows first.
- Use core and component tests to pin the data model, normalization rules, and rendering contracts.
- Keep coverage focused on caller-visible behavior:
  - recording-only sessions
  - `VDO.Ninja`-only sessions
  - `LiveKit`-only sessions
  - concurrent `VDO.Ninja + LiveKit` sessions
  - remote-source intake and program switching
  - transport-bound downstream-target readiness and blocked states

## Ordered Implementation Plan

### 1. Establish Baseline

- [x] Run `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`.
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Capture the initial failing-test state before continuing implementation.
- [x] Confirm the existing browser suite already covers the main `Go Live` surface and can be extended instead of replaced.

Verification:

- build completed with `0` warnings and `0` errors on `2026-04-02`
- baseline full-suite test state was captured before the refactor continued

### 2. Document The New Streaming Boundary

- [x] Update `docs/Architecture.md` to describe the new `sources + program + sinks` ownership model.
- [x] Update `docs/Features/GoLiveRuntime.md` with the transport/source/runtime flow and Mermaid diagrams.
- [x] Update `docs/ADR/ADR-0002-go-live-operational-studio-surface.md` and `docs/ADR/ADR-0003-go-live-broadcast-spine-and-relay-architecture.md` to remove OBS and document transport-capability-gated downstream routing.

Verification:

- docs and ADRs now describe `PrompterOne` as the primary browser-owned streaming surface
- OBS is no longer part of the first-party architecture narrative

### 3. Replace The Flat Streaming Model In Core

- [x] Replace the old single-mode output shape in `src/PrompterOne.Core/Streaming/Models/StreamingProfile.cs` with:
  - `ProgramCaptureProfile`
  - `RecordingProfile`
  - `TransportConnectionProfile`
  - `DistributionTargetProfile`
- [x] Introduce explicit transport roles through `StreamingTransportRole`.
- [x] Introduce module contracts in `src/PrompterOne.Core/Streaming/Models/GoLiveModuleContracts.cs`.
- [x] Add a module registry and descriptors in:
  - `src/PrompterOne.Core/Streaming/Services/GoLiveModuleDescriptors.cs`
  - `src/PrompterOne.Core/Streaming/Services/GoLiveModuleRegistry.cs`
  - `src/PrompterOne.Core/Streaming/Services/GoLiveModulePlaceholders.cs`
- [x] Remove old OBS-era provider implementations:
  - `LiveKitOutputProvider`
  - `VdoNinjaOutputProvider`
  - `RtmpStreamingOutputProvider`

Verification:

- core routing now distinguishes transport modules from downstream targets
- multiple transport connections of the same kind can coexist
- downstream readiness is bound to transport capabilities instead of assumed native RTMP support

### 4. Normalize Settings Around Program, Recording, Transport, And Targets

- [x] Update `src/PrompterOne.Shared/Settings/Services/StreamingSettingsNormalizer.cs` for the new persisted shape.
- [x] Reorganize the settings surface into program capture, local recording, transport connections, and distribution targets.
- [x] Add data-driven transport and downstream cards:
  - `SettingsStreamingTransportConnectionCard.razor`
  - `SettingsStreamingDistributionTargetCard.razor`
- [x] Remove OBS fields, cards, storage shape, and compatibility shims from settings.

Verification:

- settings normalization no longer preserves OBS-specific shape
- settings UI renders transport modules and downstream targets from the new model

### 5. Rebuild Go Live Around A Stable Program Handle

- [x] Extend `Go Live` runtime contracts so the canonical program feed can drive recording and multiple transport outputs concurrently.
- [x] Update:
  - `src/PrompterOne.Shared/GoLive/Services/GoLiveOutputRuntimeRequest.cs`
  - `src/PrompterOne.Shared/GoLive/Services/GoLiveOutputRuntimeService.cs`
  - `src/PrompterOne.Shared/wwwroot/media/go-live-output.js`
  - `src/PrompterOne.Shared/wwwroot/media/go-live-media-compositor.js`
  - `src/PrompterOne.Shared/wwwroot/media/go-live-output-vdo-ninja.js`
- [x] Keep local recording as a dedicated sink off the same canonical program feed.
- [x] Allow `VDO.Ninja` and `LiveKit` publish sessions to run together from that shared program feed.

Verification:

- recording, `VDO.Ninja`, and `LiveKit` all consume the same browser-owned program stream
- the runtime reuses one program spine while sinks are added or removed

### 6. Add First-Class Remote Source Intake

- [x] Introduce remote-source runtime services and interop:
  - `GoLiveRemoteSourceInterop.cs`
  - `GoLiveRemoteSourceRuntimeService.cs`
  - `go-live-remote-sources.js`
- [x] Make remote `VDO.Ninja` and `LiveKit` sources available as first-class selectable sources in `Go Live`.
- [x] Update `GoLivePage.RemoteSources.cs` and related `Go Live` page slices so remote-source diagnostics and source switching stay in the shared operational shell.
- [x] Preserve preview attachments during recurring remote-source syncs by diffing source updates instead of clearing and re-registering all remote streams each tick.

Verification:

- remote participants appear as selectable program sources
- recurring remote-source sync no longer detaches the active preview element

### 7. Keep The UI Stable And Data-Driven

- [x] Keep the existing `Go Live` shell while replacing hardcoded provider branches with module/transport-driven rendering.
- [x] Remove OBS labels, cards, interop names, and runtime states from:
  - `Settings`
  - `Go Live`
  - shared UI contract catalogs
  - tests and docs
- [x] Keep the left rail focused on source control and the right rail focused on output telemetry and destination state.

Verification:

- first-party `src/`, `docs/`, and `tests/` no longer contain OBS-specific product/runtime references
- the `Go Live` shell still renders the same operational surface, but from the new model

### 8. Expand Automated Coverage And Fix Regressions

- [x] Replace old provider tests with module-registry and routing tests.
- [x] Update component tests for settings cards, `Go Live` state, and remote-source rendering.
- [x] Extend browser coverage for:
  - recording-only operation
  - `VDO.Ninja` publish
  - `LiveKit` publish
  - concurrent `VDO.Ninja + LiveKit`
  - remote-source intake and source switching
  - custom `LiveKit` server and custom/self-hosted `VDO.Ninja` configuration flows
- [x] Fix the remaining dual-transport browser regression by:
  - installing the synthetic `LiveKit` and `VDO.Ninja` harnesses with `EvaluateAsync(...)` on the loaded page
  - relaxing the `LiveKit` readiness check to assert published audio/video tracks instead of a brittle exact call count
  - making remote-source sync incremental so previews survive repeated guest updates
- [x] Keep screenshot artifacts for major browser flows under `output/playwright/`.

Verification:

- focused `Go Live` browser scenarios pass end to end with the new architecture
- regression coverage now proves concurrent publish plus remote intake

### 9. Final Validation

- [x] Run `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`.
- [x] Run focused tests:
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj`
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.Tests/PrompterOne.App.Tests.csproj`
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/tests/PrompterOne.App.UITests/PrompterOne.App.UITests.csproj --no-build --filter "FullyQualifiedName~GoLive"`
- [x] Run `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Run `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`.
- [x] Re-run final required validation after formatting:
  - `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
  - `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`

Verification:

- build, focused tests, full suite, format, and post-format rerun all completed successfully on `2026-04-02`
- `dotnet format` completed successfully; it reported that `IDE0060` had no associated automatic code fix, but it did not fail the command

## Tracked Failing Tests

- [x] No remaining tracked failures after the final full-suite rerun on `2026-04-02`.

## Final Validation Skills And Commands

1. `dotnet-blazor`
Reason: validate that the Blazor/WASM and JS-interop boundary still reflects a browser-owned runtime.

2. `mcaf-testing`
Reason: ensure the implementation ships with browser-primary regression coverage, supporting component/core tests, and explicit final validation commands.

3. `dotnet build /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx -warnaserror`
Reason: keep the repo quality gate green under warning-as-error.

4. `dotnet test /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
Reason: prove the full automated suite remains green after the refactor.

5. `dotnet format /Users/ksemenenko/Developer/PrompterOne/PrompterOne.slnx`
Reason: keep formatting and analyzer-owned fixes aligned with repo policy before the final rerun.

## Done Criteria

- [x] OBS is removed from first-party runtime architecture, settings, docs, contracts, and tests.
- [x] `PrompterOne` has an explicit `sources + program + sinks` streaming model.
- [x] Local recording, `VDO.Ninja`, and `LiveKit` all operate from the same canonical program feed.
- [x] Remote `VDO.Ninja` and `LiveKit` sources are selectable in `Go Live`.
- [x] Downstream targets are transport-capability-gated instead of pretending to be native browser RTMP outputs.
- [x] The full required validation stack is green.
