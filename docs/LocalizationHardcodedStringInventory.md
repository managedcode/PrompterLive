# Localization Hardcoded String Inventory

## Goal

This file records the user-facing hardcoded string audit for `PrompterOne.Shared` so localization cleanup stays explicit and reviewable.

## Audit Method

- Regex scan for visible Razor text nodes
- Regex scan for user-facing `Title`, `Label`, `Placeholder`, and tooltip strings in `.razor` and `.cs`
- Manual review to exclude implementation literals such as storage keys, route fragments, CSS classes, JS interop identifiers, and test ids

## Remaining Audited Files

### Editor

- `src/PrompterOne.Shared/Editor/Components/EditorSourcePanel.razor`
  - toolbar chip labels such as `History`, `Format`, `Voice`, `Emotion`, `Pause`, `Speed`, `Insert`, `AI`
  - status-bar labels such as `Ln`, `Col`, `Segments`, `Words`, `TPS v...`
- `src/PrompterOne.Shared/Editor/Components/EditorSourcePanel.razor.cs`
  - `Start writing in TPS.`
- `src/PrompterOne.Shared/Editor/Components/EditorToolbarCatalog.cs`
  - toolbar section labels
  - dropdown group labels
  - editor toolbar tooltip copy
- `src/PrompterOne.Shared/Editor/Components/EditorFloatingToolbarCatalog.cs`
  - floating-toolbar labels
  - floating-menu group labels
  - floating-toolbar tooltip copy
- `src/PrompterOne.Shared/Editor/Components/EditorMetadataRail.razor`
  - metadata rail headings, labels, and local-history status copy
- `src/PrompterOne.Shared/Editor/Components/EditorStructureInspector.razor`
  - active-segment and active-block field labels
- `src/PrompterOne.Shared/Editor/Components/EditorStructureSidebar.razor`
  - structure heading and split feedback copy such as `+N more in Library`
- `src/PrompterOne.Shared/Editor/Pages/EditorPage.Layout.cs`
  - metadata rail toggle labels
- `src/PrompterOne.Shared/Editor/Pages/EditorPage.DocumentSplit.cs`
  - split completion copy and destination notes

### Settings

- `src/PrompterOne.Shared/Settings/Pages/SettingsPage.Cameras.cs`
  - camera connection labels and resolution summaries
- `src/PrompterOne.Shared/Settings/Pages/SettingsPage.Microphones.cs`
  - microphone connection, channel, and sample-rate labels
- `src/PrompterOne.Shared/Settings/Pages/SettingsPage.MediaState.cs`
  - media-access CTA labels and diagnostics copy
- `src/PrompterOne.Shared/Settings/Models/SettingsNavigationText.cs`
  - navigation labels such as `AI Provider`, `Cloud Sync`, and `File Storage`

## Resolved In The Current Pass

- `src/PrompterOne.Shared/GoLive/Models/GoLiveText.cs`
  - routed Go Live chrome, source-card, sidebar, destination, and session copy now resolves through shared localization keys; only technical/internal constants remain inline by design
- `src/PrompterOne.Shared/GoLive/Pages/*`
  - Go Live page chrome, preview states, session badges, destination summaries, participant/runtime labels, and local camera empty-state copy now resolve through `SharedResource`
- `src/PrompterOne.Shared/GoLive/Components/GoLiveStudioSidebar.razor`
  - sidebar tab and room/runtime labels localized
- `src/PrompterOne.Shared/GoLive/Components/GoLiveSourcesCard.razor`
  - source-card title, empty state, microphone title, and action labels localized
- `src/PrompterOne.Shared/Settings/Models/SettingsStreamingText.cs`
  - streaming labels now live behind localization keys instead of hardcoded display strings
- `src/PrompterOne.Shared/Settings/Models/SettingsStreamingLocalTargetCatalog.cs`
  - local recording target now uses localized name, account, and description keys
- `src/PrompterOne.Shared/Settings/Components/SettingsStreamingPanel.razor`
  - local recording card now resolves localized catalog values
- `src/PrompterOne.Shared/Settings/Components/SettingsAboutSection.razor.cs`
  - About section titles, link descriptions, disclosure labels, and footer copy localized
- `src/PrompterOne.Shared/Library/Components/LibraryCard.razor`
  - card badge and CTA labels now resolve through localization keys
- `src/PrompterOne.Shared/Learn/Pages/LearnPage.razor`
  - reader labels such as `Next` and `WPM` now resolve through localization keys
- `src/PrompterOne.Shared/AppShell/Routes.razor`
  - not-found shell copy localized
- `src/PrompterOne.Shared/Settings/Components/SettingsFilesSection.razor`
  - storage headings, toggles, and detail labels localized
- `src/PrompterOne.Shared/Settings/Components/SettingsAiSection.razor`
  - provider headings and common field labels localized
- `src/PrompterOne.Shared/Settings/Components/SettingsAiSection.razor.cs`
  - provider subtitle and save/clear messages localized
- `src/PrompterOne.Shared/Settings/Components/SettingsCloudSection.razor.cs`
  - connection status and provider disconnect copy localized

## Intentional Non-Localization Exceptions

- `PrompterOne`
- `TPS`
- provider and protocol brand names such as `OpenAI`, `Claude`, `Ollama`, `LiveKit`, `VDO.Ninja`, `RTMP`, `RTMPS`
- raw model identifiers such as `gpt-4o`
- technical values such as `WPM`, `FPS`, `MP4`, `WEBM`

These terms may still appear in the UI, but they should be emitted by localized owners or explicitly documented value catalogs instead of scattered hardcoded display strings.
