using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private const string LoadEditorOperation = "Editor load";
    private const string LoadEditorMessage = "Unable to load the editor right now.";
    private const string PersistDraftOperation = "Editor save draft";
    private const string PersistDraftMessage = "Unable to save the current draft.";
    private const string EditorSyntaxOperation = "Editor syntax";
    private const string EditorSyntaxMessage = "The TPS draft has a syntax issue. Fix it and keep writing.";
    private const int DraftAnalysisDelayMilliseconds = 1_000;
    private const int AutosaveDelayMilliseconds = 1_500;
    private const int LargeDraftAnalysisDelayMilliseconds = 3_000;
    private const int LargeDraftAutosaveDelayMilliseconds = 4_000;
    private const int LargeDraftDebounceThreshold = 16_000;
    private const int UntitledDraftAutosaveDelayMilliseconds = 1_500;
    private const int UntitledDraftAutosaveCharacterThreshold = 2;
    private const string DefaultAuthor = "PrompterOne";
    private const string DefaultProfileActor = "Actor";
    private const string DefaultProfileRsvp = "RSVP";
    private const string DefaultVersion = "1.1.0";

    private readonly EditorDocumentHistory _history = new();
    private readonly TpsFrontMatterDocumentService _frontMatterService = new();
    private CancellationTokenSource? _draftAnalysisCancellationSource;
    private CancellationTokenSource? _autosaveCancellationSource;
    private long _draftRevision;
    private bool _isEditorReady;
    private bool _loadState = true;
    private int? _activeBlockIndex;
    private int _activeSegmentIndex;
    private string _author = DefaultAuthor;
    private int _baseWpm = 140;
    private string _createdDate = DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    private string _displayDuration = "0:00";
    private string? _errorMessage;
    private string _profile = DefaultProfileActor;
    private string _screenTitle = ScriptWorkspaceState.UntitledScriptTitle;
    private EditorSelectionViewModel _selection = EditorSelectionViewModel.Empty;
    private IReadOnlyList<EditorOutlineSegmentViewModel> _segments = [];
    private bool _skipNextRenderFromTyping;
    private EditorSourcePanel? _sourcePanel;
    private string _sourceText = string.Empty;
    private EditorDraftMetrics _draftMetrics = EditorDraftMetrics.Empty;
    private EditorStatusViewModel _status = new(1, 1, "Actor", 140, 0, 0, 0, "0:00", DefaultVersion);
    private string _version = DefaultVersion;

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private UiDiagnosticsService Diagnostics { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private EditorOutlineBuilder OutlineBuilder { get; set; } = null!;
    [Inject] private EditorLocalAssistant LocalAssistant { get; set; } = null!;
    [Inject] private IScriptRepository ScriptRepository { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private TpsParser TpsParser { get; set; } = null!;
    [Inject] private TpsStructureEditor StructureEditor { get; set; } = null!;
    [Inject] private TpsTextEditor TextEditor { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "id")]
    public string? ScriptId { get; set; }

    protected override bool ShouldRender()
    {
        if (!_skipNextRenderFromTyping)
        {
            return true;
        }

        _skipNextRenderFromTyping = false;
        return false;
    }
}
