# Editor Authoring

## Intent

The `/editor` screen is a TPS-native authoring surface. The visible editor is body-only and styled inline, while metadata lives exclusively in the metadata rail and is composed back into the persisted TPS document during autosave.

## Main Flow

```mermaid
flowchart LR
    Source["Visible TPS body editor"]
    History["Undo/redo history"]
    FrontMatter["Front-matter metadata rail"]
    Structure["Segment/block structure editor"]
    LocalAi["Local AI helper panel"]
    Outline["Outline + status"]
    Highlight["Highlighted overlay"]
    Save["Autosave to script repository"]

    Source --> History
    FrontMatter --> Source
    Source --> Outline
    Source --> Highlight
    Structure --> Source
    LocalAi --> Source
    Source --> Save
    FrontMatter --> Save
```

## Structure Editing Contract

```mermaid
sequenceDiagram
    participant User
    participant Sidebar as "Structure Inspector"
    participant Page as "EditorPage"
    participant Core as "TpsStructureEditor"
    participant Session as "ScriptSessionService"

    User->>Sidebar: Edit segment/block fields
    Sidebar->>Page: Updated view model
    Page->>Core: Rewrite TPS header safely
    Core-->>Page: Updated source + selection
    Page->>Session: Persist draft
    Session-->>Page: Recompiled workspace state
    Page-->>Sidebar: Refreshed structure editor state
```

## Current Behavior

- floating selection toolbar supports formatting actions and stays anchored to the selection
- visible source input never shows front matter; metadata is edited only in the metadata rail
- active segment and block can be edited through the left sidebar inspector
- toolbar dropdowns open explicitly by click and expose stable test selectors
- local AI panel provides deterministic simplify, expand, and pause-format helpers without a backend
- speed-offset metadata fields persist into front matter during autosave
- source edits refresh metadata, outline, and status
- metadata and structure edits rewrite the source rather than bypassing it

## Verification

- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.Core.Tests/PrompterLive.Core.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
