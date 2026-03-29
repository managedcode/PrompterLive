using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Library;
using PrompterLive.Core.Services;
using PrompterLive.Core.Services.Preview;
using PrompterLive.Core.Services.Workspace;
using PrompterLive.Shared.Components.Library;
using PrompterLive.Shared.Services;
using PrompterLive.Shared.Services.Library;

namespace PrompterLive.Shared.Pages;

public partial class LibraryPage : ComponentBase
{
    private const string LibrarySettingsKey = "prompterlive.library";
    private const string SyncLibraryBreadcrumbMethod = "PrompterLiveDesign.setLibraryBreadcrumb";

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private BrowserSettingsStore SettingsStore { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ILibraryFolderRepository LibraryFolderRepository { get; set; } = null!;
    [Inject] private IScriptRepository ScriptRepository { get; set; } = null!;
    [Inject] private IScriptPreviewService PreviewService { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private TpsParser TpsParser { get; set; } = null!;

    private bool _loadLibrary = true;
    private bool _syncHeader;
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
    private HashSet<string> _expandedFolderIds = new(StringComparer.Ordinal);

    private bool IsAllSelected => string.Equals(_selectedFolderId, LibrarySelectionKeys.All, StringComparison.Ordinal);

    protected override Task OnParametersSetAsync()
    {
        _loadLibrary = true;
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loadLibrary)
        {
            _loadLibrary = false;
            await LoadLibraryAsync();
            StateHasChanged();
            return;
        }

        if (_syncHeader)
        {
            _syncHeader = false;
            await JS.InvokeVoidAsync(SyncLibraryBreadcrumbMethod, ResolveSelectedFolderLabel());
        }
    }

    private async Task LoadLibraryAsync()
    {
        await Bootstrapper.EnsureReadyAsync();

        await RestoreViewStateAsync();
        var folders = await LibraryFolderRepository.ListAsync();
        var summaries = await ScriptRepository.ListAsync();
        _folders = folders;
        _allCards = await LibraryCardFactory.BuildAsync(summaries, ScriptRepository, PreviewService, TpsParser);
        EnsureExpandedFolders();
        NormalizeRestoredState();
        RebuildLibraryView();
    }

    private void EnsureExpandedFolders()
    {
        var rootFoldersWithChildren = _folders
            .Where(folder => string.IsNullOrWhiteSpace(folder.ParentId))
            .Where(folder => _folders.Any(candidate => string.Equals(candidate.ParentId, folder.Id, StringComparison.Ordinal)))
            .Select(folder => folder.Id);

        foreach (var folderId in rootFoldersWithChildren)
        {
            _expandedFolderIds.Add(folderId);
        }
    }

    private async Task CreateScriptAsync()
    {
        await Bootstrapper.EnsureReadyAsync();
        await SessionService.NewAsync();
        Navigation.NavigateTo("/editor");
    }

    private async Task OpenScriptAsync(string id)
    {
        await Bootstrapper.EnsureReadyAsync();
        var document = await ScriptRepository.GetAsync(id);
        if (document is null)
        {
            return;
        }

        await SessionService.OpenAsync(document);
        Navigation.NavigateTo($"/editor?id={Uri.EscapeDataString(id)}");
    }

    private Task LearnScriptAsync(string id)
    {
        Navigation.NavigateTo($"/learn?id={Uri.EscapeDataString(id)}");
        return Task.CompletedTask;
    }

    private Task ReadScriptAsync(string id)
    {
        Navigation.NavigateTo($"/teleprompter?id={Uri.EscapeDataString(id)}");
        return Task.CompletedTask;
    }

    private async Task DuplicateScriptAsync(string id)
    {
        await Bootstrapper.EnsureReadyAsync();
        var document = await ScriptRepository.GetAsync(id);
        if (document is null)
        {
            return;
        }

        await ScriptRepository.SaveAsync(
            title: $"{document.Title} Copy",
            text: document.Text,
            documentName: null,
            existingId: null,
            folderId: document.FolderId);
        await LoadLibraryAsync();
    }

    private async Task MoveScriptAsync(LibraryMoveRequest request)
    {
        await Bootstrapper.EnsureReadyAsync();
        await ScriptRepository.MoveToFolderAsync(request.ScriptId, request.FolderId);
        await LoadLibraryAsync();
    }

    private async Task DeleteScriptAsync(string id)
    {
        await Bootstrapper.EnsureReadyAsync();
        await ScriptRepository.DeleteAsync(id);

        if (string.Equals(SessionService.State.ScriptId, id, StringComparison.Ordinal))
        {
            await SessionService.NewAsync();
        }

        await LoadLibraryAsync();
    }

    private async Task SelectFolder(string folderId)
    {
        _selectedFolderId = folderId;
        RebuildLibraryView();
        await PersistViewStateAsync();
    }

    private void StartCreateFolder()
    {
        _isCreatingFolder = true;
        _folderDraftName = string.Empty;
        _folderDraftParentId = ResolveDraftParentId();
    }

    private void CancelCreateFolder()
    {
        _isCreatingFolder = false;
        _folderDraftName = string.Empty;
        _folderDraftParentId = ResolveDraftParentId();
    }

    private void UpdateFolderDraftName(string name)
    {
        _folderDraftName = name;
    }

    private void UpdateFolderDraftParent(string parentId)
    {
        _folderDraftParentId = string.IsNullOrWhiteSpace(parentId)
            ? LibrarySelectionKeys.Root
            : parentId;
    }

    private async Task SubmitCreateFolderAsync()
    {
        var folderName = _folderDraftName.Trim();
        if (folderName.Length == 0)
        {
            return;
        }

        await Bootstrapper.EnsureReadyAsync();
        var parentId = NormalizeDraftParentId(_folderDraftParentId);
        var folder = await LibraryFolderRepository.CreateAsync(folderName, parentId);
        if (parentId is not null)
        {
            _expandedFolderIds.Add(parentId);
        }

        _expandedFolderIds.Add(folder.Id);
        _isCreatingFolder = false;
        _folderDraftName = string.Empty;
        _folderDraftParentId = ResolveDraftParentId();
        _selectedFolderId = folder.Id;
        await LoadLibraryAsync();
        await PersistViewStateAsync();
    }

    private async Task SetSortMode(LibrarySortMode sortMode)
    {
        _sortMode = sortMode;
        RebuildLibraryView();
        await PersistViewStateAsync();
    }

    private void RebuildLibraryView()
    {
        _folderNodes = LibraryFolderTreeBuilder.BuildTree(_folders, _allCards, _selectedFolderId, _expandedFolderIds);
        _folderOptions = LibraryFolderTreeBuilder.BuildOptions(_folders);
        _cards = SortCards(FilterCards()).ToList();
        _syncHeader = true;
    }

    private IEnumerable<LibraryCardViewModel> FilterCards()
    {
        if (IsAllSelected)
        {
            return _allCards;
        }

        var visibleFolderIds = CollectVisibleFolderIds(_selectedFolderId);
        return _allCards.Where(card => card.FolderId is not null && visibleFolderIds.Contains(card.FolderId));
    }

    private IEnumerable<LibraryCardViewModel> SortCards(IEnumerable<LibraryCardViewModel> cards) =>
        _sortMode switch
        {
            LibrarySortMode.Date => cards.OrderByDescending(card => card.UpdatedAt),
            LibrarySortMode.Duration => cards.OrderByDescending(card => card.Duration),
            LibrarySortMode.Wpm => cards.OrderByDescending(card => card.AverageWpm),
            _ => cards
                .OrderBy(card => card.DisplayOrder)
                .ThenBy(card => card.Title, StringComparer.OrdinalIgnoreCase)
        };

    private HashSet<string> CollectVisibleFolderIds(string selectedFolderId)
    {
        var visible = new HashSet<string>(StringComparer.Ordinal) { selectedFolderId };
        var pending = new Queue<string>();
        pending.Enqueue(selectedFolderId);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var child in _folders.Where(folder => string.Equals(folder.ParentId, current, StringComparison.Ordinal)))
            {
                if (visible.Add(child.Id))
                {
                    pending.Enqueue(child.Id);
                }
            }
        }

        return visible;
    }

    private string? GetSortClass(LibrarySortMode sortMode) => _sortMode == sortMode ? "active" : null;

    private string ResolveSelectedFolderLabel()
    {
        if (IsAllSelected)
        {
            return _folderNodes.FirstOrDefault()?.Name ?? "All Scripts";
        }

        return _folders
            .FirstOrDefault(folder => string.Equals(folder.Id, _selectedFolderId, StringComparison.Ordinal))
            ?.Name
            ?? "All Scripts";
    }

    private string ResolveDraftParentId() =>
        NormalizeDraftParentId(_selectedFolderId) ?? LibrarySelectionKeys.Root;

    private void NormalizeRestoredState()
    {
        var validFolderIds = _folders
            .Select(folder => folder.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (!string.Equals(_selectedFolderId, LibrarySelectionKeys.All, StringComparison.Ordinal)
            && !validFolderIds.Contains(_selectedFolderId))
        {
            _selectedFolderId = LibrarySelectionKeys.All;
        }

        _expandedFolderIds = _expandedFolderIds
            .Where(validFolderIds.Contains)
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task RestoreViewStateAsync()
    {
        var storedState = await SettingsStore.LoadAsync<LibraryViewState>(LibrarySettingsKey);
        if (storedState is null)
        {
            return;
        }

        _selectedFolderId = string.IsNullOrWhiteSpace(storedState.SelectedFolderId)
            ? LibrarySelectionKeys.All
            : storedState.SelectedFolderId;
        _sortMode = storedState.SortMode;
        _expandedFolderIds = storedState.ExpandedFolderIds
            .Where(folderId => !string.IsNullOrWhiteSpace(folderId))
            .ToHashSet(StringComparer.Ordinal);
    }

    private Task PersistViewStateAsync()
    {
        var state = new LibraryViewState(
            SelectedFolderId: _selectedFolderId,
            SortMode: _sortMode,
            ExpandedFolderIds: _expandedFolderIds.OrderBy(value => value, StringComparer.Ordinal).ToArray());

        return SettingsStore.SaveAsync(LibrarySettingsKey, state);
    }

    private static string? NormalizeDraftParentId(string? folderId) =>
        string.IsNullOrWhiteSpace(folderId) || string.Equals(folderId, LibrarySelectionKeys.All, StringComparison.Ordinal)
            ? null
            : string.Equals(folderId, LibrarySelectionKeys.Root, StringComparison.Ordinal)
                ? null
                : folderId;
}
