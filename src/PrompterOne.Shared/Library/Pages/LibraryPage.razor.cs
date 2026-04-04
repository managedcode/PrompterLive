using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Library;
using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;

namespace PrompterOne.Shared.Pages;

public partial class LibraryPage : ComponentBase, IDisposable
{
    private const string LibrarySettingsKey = "prompterone.library";
    private const string LoadLibraryOperation = "Library load";
    private const string LoadLibraryMessage = "Unable to load the library right now.";
    private const string CreateScriptOperation = "Library create script";
    private const string CreateScriptMessage = "Unable to create a new script.";
    private const string OpenScriptOperation = "Library open script";
    private const string OpenScriptMessage = "Unable to open this script.";
    private const string DuplicateScriptOperation = "Library duplicate script";
    private const string DuplicateScriptMessage = "Unable to duplicate this script.";
    private const string MoveScriptOperation = "Library move script";
    private const string MoveScriptMessage = "Unable to move this script.";
    private const string DeleteScriptOperation = "Library delete script";
    private const string DeleteScriptMessage = "Unable to delete this script.";
    private const string CreateFolderOperation = "Library create folder";
    private const string CreateFolderMessage = "Unable to create this folder.";
    private const string SelectFolderLogTemplate = "Selecting library folder {FolderId}.";
    private const string StartCreateFolderLogTemplate = "Opening library folder overlay with parent {ParentId}.";
    private const string CancelCreateFolderLogMessage = "Cancelling library folder overlay.";
    private const string FolderCreatedLogTemplate = "Created library folder {FolderId} under {ParentId}.";

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private IUserSettingsStore SettingsStore { get; set; } = null!;
    [Inject] private UiDiagnosticsService Diagnostics { get; set; } = null!;
    [Inject] private ILogger<LibraryPage> Logger { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private ILibraryFolderRepository LibraryFolderRepository { get; set; } = null!;
    [Inject] private IScriptRepository ScriptRepository { get; set; } = null!;
    [Inject] private IScriptPreviewService PreviewService { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private TpsParser TpsParser { get; set; } = null!;

    private bool _loadLibrary = true;
    private bool _isCreatingFolder;
    private string _folderDraftName = string.Empty;
    private string _folderDraftParentId = LibrarySelectionKeys.Root;
    private string _selectedFolderId = LibrarySelectionKeys.All;
    private LibrarySortMode _sortMode = LibrarySortMode.Name;
    private IReadOnlyList<StoredLibraryFolder> _folders = [];
    private IReadOnlyList<LibraryCardViewModel> _allCards = [];
    private IReadOnlyList<LibraryCardViewModel> _cards = [];
    private IReadOnlyList<LibraryFolderNodeViewModel> _folderNodes = [];
    private IReadOnlyList<LibraryFolderOptionViewModel> _folderOptions = [];
    private LibraryCardsGrid? _cardsGrid;
    private HashSet<string> _expandedFolderIds = new(StringComparer.Ordinal);

    private bool IsAllSelected => string.Equals(_selectedFolderId, LibrarySelectionKeys.All, StringComparison.Ordinal);

    private void HandleLibrarySurfaceClick() => _cardsGrid?.DismissOpenCardMenu();
}
