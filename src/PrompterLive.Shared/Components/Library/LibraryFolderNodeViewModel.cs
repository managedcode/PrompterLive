namespace PrompterLive.Shared.Components.Library;

public sealed record LibraryFolderNodeViewModel(
    string Id,
    string Name,
    int Depth,
    int TotalCount,
    bool IsExpanded,
    bool IsSelected,
    bool ShowChevron,
    IReadOnlyList<LibraryFolderNodeViewModel> Children)
{
    public bool HasChildren => Children.Count > 0;

    public string TestId => $"library-folder-{Id}";
}
