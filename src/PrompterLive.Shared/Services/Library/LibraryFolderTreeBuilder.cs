using PrompterLive.Core.Models.Library;
using PrompterLive.Shared.Components.Library;

namespace PrompterLive.Shared.Services.Library;

internal static class LibraryFolderTreeBuilder
{
    public static IReadOnlyList<LibraryFolderNodeViewModel> BuildTree(
        IReadOnlyList<StoredLibraryFolder> folders,
        IReadOnlyCollection<LibraryCardViewModel> cards,
        string selectedFolderId,
        ISet<string> expandedFolderIds)
    {
        var cardCountByFolder = cards
            .Where(card => !string.IsNullOrWhiteSpace(card.FolderId))
            .GroupBy(card => card.FolderId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var childrenByParent = folders
            .OrderBy(folder => folder.DisplayOrder)
            .ThenBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
            .ToLookup(folder => folder.ParentId, StringComparer.Ordinal);

        return BuildNodes(parentId: null, depth: 0).ToList();

        IReadOnlyList<LibraryFolderNodeViewModel> BuildNodes(string? parentId, int depth)
        {
            var children = childrenByParent[parentId].ToList();
            if (children.Count == 0)
            {
                return [];
            }

            return children
                .Select(folder => BuildNode(folder, depth))
                .ToList();
        }

        LibraryFolderNodeViewModel BuildNode(StoredLibraryFolder folder, int depth)
        {
            var childNodes = BuildNodes(folder.Id, depth + 1);
            var descendantCount = childNodes.Sum(child => child.TotalCount);
            var totalCount = cardCountByFolder.GetValueOrDefault(folder.Id) + descendantCount;
            var hasSelectedDescendant = ContainsSelection(childNodes, selectedFolderId);
            var isExpanded = childNodes.Count > 0
                && (expandedFolderIds.Contains(folder.Id) || hasSelectedDescendant || depth == 0);

            return new LibraryFolderNodeViewModel(
                Id: folder.Id,
                Name: folder.Name,
                Depth: depth,
                TotalCount: totalCount,
                IsExpanded: isExpanded,
                IsSelected: string.Equals(folder.Id, selectedFolderId, StringComparison.Ordinal),
                ShowChevron: depth == 0,
                Children: childNodes);
        }
    }

    public static IReadOnlyList<LibraryFolderOptionViewModel> BuildOptions(IReadOnlyList<StoredLibraryFolder> folders)
    {
        var childrenByParent = folders
            .OrderBy(folder => folder.DisplayOrder)
            .ThenBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
            .ToLookup(folder => folder.ParentId, StringComparer.Ordinal);
        var options = new List<LibraryFolderOptionViewModel>();

        AppendOptions(parentId: null, prefix: string.Empty);
        return options;

        void AppendOptions(string? parentId, string prefix)
        {
            var children = childrenByParent[parentId].ToList();
            if (children.Count == 0)
            {
                return;
            }

            foreach (var child in children)
            {
                var label = string.IsNullOrWhiteSpace(prefix) ? child.Name : $"{prefix} / {child.Name}";
                options.Add(new LibraryFolderOptionViewModel(child.Id, label));
                AppendOptions(child.Id, label);
            }
        }
    }

    private static bool ContainsSelection(
        IEnumerable<LibraryFolderNodeViewModel> nodes,
        string selectedFolderId) =>
        nodes.Any(node =>
            string.Equals(node.Id, selectedFolderId, StringComparison.Ordinal)
            || ContainsSelection(node.Children, selectedFolderId));
}
