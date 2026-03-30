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
    LocalAi["Local AI helper actions"]
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
- toolbar AI and floating AI buttons execute deterministic local rewrite helpers without opening a separate editor panel
- speed-offset metadata fields persist into front matter during autosave
- `Duration` in the metadata rail now edits `display_duration` in front matter, stays hidden from the visible body editor, and survives reloads
- source edits refresh metadata, tree labels, preview overlay, and status
- metadata edits rewrite the persisted TPS document without surfacing YAML in the editor body
- typing stays local-first while autosave persists on a short debounce instead of every keystroke
- active typing temporarily shows the native textarea text layer while the highlight overlay catches up after a short idle window, keeping keystrokes off the full highlight rebuild path
- the styled TPS overlay remains visible while the real textarea owns caret and selection, so editing stays inline instead of switching to a plain-text mode
- the highlight overlay subtree is JS-owned while Blazor keeps workflow state, preventing per-keystroke DOM diff churn and keeping browser typing responsive
- plain `input` events no longer force a page-wide Blazor render; sidebar, metadata rail, and status refresh only after draft analysis or explicit selection/navigation interactions
- selection interop stays off the keystroke path and runs only for caret/selection workflows that actually need toolbar anchoring or focus moves
- the source highlight and textarea share the same wrapping metrics, preventing caret/text drift on multiline editing
- the authoring surface exposes a single vertical scroll container so the editor body does not fight a nested shell scrollbar
- the floating selection toolbar emotion affordance opens a real TPS emotion picker instead of applying a single hardcoded wrap
- the top toolbar shows direct TPS structure insert actions for `##` segments and `###` blocks, with the same actions still available from the insert dropdown
- native textarea clicks own caret placement; the stage shell no longer steals click-to-caret behavior
- `EditorSourcePanel` owns its editor-only CSS and browser support script, so the shell keeps loading support assets but the active editor surface no longer depends on global stylesheet or shell-script rules

## Verification

- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.Core.Tests/PrompterLive.Core.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --filter "FullyQualifiedName~EditorTypingTests|FullyQualifiedName~EditorSourceSyncTests|FullyQualifiedName~EditorInteractionTests"`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.Tests/PrompterLive.App.Tests.csproj --filter "FullyQualifiedName~Editor"`
- `dotnet test /Users/ksemenenko/Developer/PrompterLive/tests/PrompterLive.App.UITests/PrompterLive.App.UITests.csproj --no-build --filter "FullyQualifiedName~Editor"`
- `dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx -warnaserror`
