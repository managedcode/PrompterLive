using Microsoft.Extensions.Logging;
using PrompterOne.Core.Models.Library;
using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class LibraryPage
{
    private async Task SelectFolder(string folderId)
    {
        Logger.LogInformation(SelectFolderLogTemplate, folderId);
        _selectedFolderId = folderId;
        UpdateExpandedFolderState(folderId);
        RebuildLibraryView();
        await PersistViewStateAsync();
    }

    private void StartCreateFolder()
    {
        var draftParentId = ResolveDraftParentId();
        Logger.LogInformation(StartCreateFolderLogTemplate, draftParentId);
        _isCreatingFolder = true;
        _folderDraftName = string.Empty;
        _folderDraftParentId = draftParentId;
    }

    private void CancelCreateFolder()
    {
        Logger.LogInformation(CancelCreateFolderLogMessage);
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

    private Task SubmitCreateFolderAsync()
    {
        var folderName = _folderDraftName.Trim();
        if (folderName.Length == 0)
        {
            return Task.CompletedTask;
        }

        return RunLibraryOperationAsync(
            CreateFolderOperation,
            Text(UiTextKey.LibraryCreateFolderMessage),
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                var parentId = NormalizeDraftParentId(_folderDraftParentId);
                var folder = await LibraryFolderRepository.CreateAsync(folderName, parentId);
                ExpandCreatedFolderPath(folder.Id, parentId);
                ResetFolderDraftState(folder.Id);
                ApplyCreatedFolder(folder);
                Logger.LogInformation(FolderCreatedLogTemplate, folder.Id, parentId ?? LibrarySelectionKeys.Root);
                await PersistViewStateAsync();
            });
    }

    private void ExpandCreatedFolderPath(string folderId, string? parentId)
    {
        if (parentId is not null)
        {
            _expandedFolderIds.Add(parentId);
        }

        _expandedFolderIds.Add(folderId);
    }

    private void ResetFolderDraftState(string selectedFolderId)
    {
        _isCreatingFolder = false;
        _folderDraftName = string.Empty;
        _folderDraftParentId = ResolveDraftParentId();
        _selectedFolderId = selectedFolderId;
    }

    private void ApplyCreatedFolder(StoredLibraryFolder folder)
    {
        _folders = _folders
            .Where(existingFolder => !string.Equals(existingFolder.Id, folder.Id, StringComparison.Ordinal))
            .Append(folder)
            .OrderBy(existingFolder => existingFolder.DisplayOrder)
            .ThenBy(existingFolder => existingFolder.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RebuildLibraryView();
    }

    private string ResolveDraftParentId() =>
        NormalizeDraftParentId(_selectedFolderId) ?? LibrarySelectionKeys.Root;

    private static string? NormalizeDraftParentId(string? folderId) =>
        string.IsNullOrWhiteSpace(folderId) || string.Equals(folderId, LibrarySelectionKeys.All, StringComparison.Ordinal)
            ? null
            : string.Equals(folderId, LibrarySelectionKeys.Root, StringComparison.Ordinal)
                ? null
                : folderId;

    private void UpdateExpandedFolderState(string folderId)
    {
        if (string.IsNullOrWhiteSpace(folderId)
            || string.Equals(folderId, LibrarySelectionKeys.All, StringComparison.Ordinal)
            || string.Equals(folderId, LibrarySelectionKeys.Favorites, StringComparison.Ordinal)
            || string.Equals(folderId, LibrarySelectionKeys.Root, StringComparison.Ordinal))
        {
            return;
        }

        var foldersById = _folders.ToDictionary(folder => folder.Id, StringComparer.Ordinal);
        if (!foldersById.ContainsKey(folderId))
        {
            return;
        }

        var hasChildren = _folders.Any(folder => string.Equals(folder.ParentId, folderId, StringComparison.Ordinal));
        if (hasChildren && _expandedFolderIds.Contains(folderId))
        {
            CollapseFolderBranch(folderId);
            return;
        }

        ExpandFolderPath(folderId, foldersById);
    }

    private void ExpandFolderPath(string folderId, IReadOnlyDictionary<string, StoredLibraryFolder> foldersById)
    {
        var currentFolderId = folderId;

        while (foldersById.TryGetValue(currentFolderId, out var folder))
        {
            _expandedFolderIds.Add(currentFolderId);

            if (string.IsNullOrWhiteSpace(folder.ParentId))
            {
                break;
            }

            currentFolderId = folder.ParentId;
        }
    }

    private void CollapseFolderBranch(string folderId)
    {
        var pendingFolderIds = new Queue<string>();
        pendingFolderIds.Enqueue(folderId);

        while (pendingFolderIds.Count > 0)
        {
            var currentFolderId = pendingFolderIds.Dequeue();
            _expandedFolderIds.Remove(currentFolderId);

            foreach (var childFolder in _folders.Where(folder => string.Equals(folder.ParentId, currentFolderId, StringComparison.Ordinal)))
            {
                pendingFolderIds.Enqueue(childFolder.Id);
            }
        }
    }
}
