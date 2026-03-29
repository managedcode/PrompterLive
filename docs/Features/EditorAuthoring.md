# Editor Authoring

## Intent

The `/editor` screen is a TPS-native authoring surface. The visible editor is body-only and styled inline, while metadata lives exclusively in the metadata rail and is composed back into the persisted TPS document during autosave.

## Main Flow

```mermaid
flowchart LR
    Source["Visible TPS body editor"]
    History["Undo/redo history"]
    Toolbar["Descriptor-driven toolbar catalog"]
    FrontMatter["Front-matter metadata rail"]
    Outline["Segment/block tree navigation"]
    LocalAi["Local AI helper panel"]
    Status["Status bar + outline state"]
    Highlight["Highlighted overlay"]
    TextMutations["TpsTextEditor mutations"]
    Save["Debounced autosave to script repository"]

    Toolbar --> Source
    Source --> History
    FrontMatter --> Source
    Source --> Status
    Source --> Highlight
    Outline --> Source
    LocalAi --> Source
    Source --> TextMutations
    Source --> Save
    FrontMatter --> Save
```

## Source And Navigation Contract

```mermaid
sequenceDiagram
    participant User
    participant Sidebar as "Structure Tree"
    participant Page as "EditorPage"
    participant Core as "TpsTextEditor / TpsStructureEditor"
    participant Session as "ScriptSessionService"

    User->>Sidebar: Navigate to segment or block
    Sidebar->>Page: Selection target
    User->>Page: Type or invoke toolbar action
    Page->>Core: Rewrite TPS body text safely
    Core-->>Page: Updated source + selection
    Page->>Session: Debounced autosave
    Session-->>Page: Recompiled workspace state
    Page-->>Sidebar: Refreshed tree labels + active state
```

## Current Behavior

- floating selection toolbar supports formatting actions and stays anchored to the selection
- toolbar and floating-bar actions are rendered from a shared descriptor catalog instead of duplicated hardcoded markup
- visible source input never shows front matter; metadata is edited only in the metadata rail
- the left sidebar is tree-only and no longer renders the legacy `ACTIVE SEGMENT` / `ACTIVE BLOCK` inspector
- direct source header edits refresh the structure tree after reparse
- toolbar dropdowns open explicitly by click and expose stable test selectors
- color formatting includes a deterministic `remove color` action that strips TPS color tags from the selected region
- local AI panel provides deterministic simplify, expand, and pause-format helpers without a backend
- floating AI from the selection toolbar survives the immediate follow-up selection event instead of closing instantly
- speed-offset metadata fields persist into front matter during autosave
- source edits refresh metadata, tree labels, preview overlay, and status
- metadata edits rewrite the persisted TPS document without surfacing YAML in the editor body
- typing stays local-first while autosave persists on a short debounce instead of every keystroke

## Verification

- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.Core.Tests/PrompterLive.Core.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --filter "FullyQualifiedName~EditorTypingTests|FullyQualifiedName~EditorSourceSyncTests|FullyQualifiedName~EditorInteractionTests"`
