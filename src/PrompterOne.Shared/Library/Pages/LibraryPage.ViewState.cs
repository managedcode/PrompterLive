using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services.Library;

namespace PrompterOne.Shared.Pages;

public partial class LibraryPage
{
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
        Shell.ShowLibrary(ResolveSelectedFolderLabel());
    }

    private IEnumerable<LibraryCardViewModel> FilterCards()
    {
        var cards = IsAllSelected
            ? _allCards
            : _allCards.Where(card => card.FolderId is not null && CollectVisibleFolderIds(_selectedFolderId).Contains(card.FolderId));

        return ApplySearchFilter(cards);
    }

    private IEnumerable<LibraryCardViewModel> ApplySearchFilter(IEnumerable<LibraryCardViewModel> cards)
    {
        var query = Shell.State.SearchText.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return cards;
        }

        return cards.Where(card =>
            card.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            card.Author.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

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

    private string? GetSortClass(LibrarySortMode sortMode) => _sortMode == sortMode ? "active" : null;

    private string ResolveSelectedFolderLabel()
    {
        if (IsAllSelected)
        {
            return Text(UiTextKey.LibraryAllScripts);
        }

        return _folders
            .FirstOrDefault(folder => string.Equals(folder.Id, _selectedFolderId, StringComparison.Ordinal))
            ?.Name
            ?? Text(UiTextKey.LibraryAllScripts);
    }

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
}
