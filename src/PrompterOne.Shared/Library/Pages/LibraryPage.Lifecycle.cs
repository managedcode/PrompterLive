using PrompterOne.Shared.Services.Library;

namespace PrompterOne.Shared.Pages;

public partial class LibraryPage
{
    protected override void OnInitialized()
    {
        Shell.LibrarySearchChanged += HandleLibrarySearchChanged;
    }

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
            await RunLibraryLoadAsync();
        }
    }

    private Task RunLibraryLoadAsync() =>
        RunLibraryOperationAsync(
            LoadLibraryOperation,
            LoadLibraryMessage,
            async () =>
            {
                await LoadLibraryAsync();
                StateHasChanged();
            });

    private async Task LoadLibraryAsync(bool restoreViewState = true)
    {
        await Bootstrapper.EnsureReadyAsync();

        if (restoreViewState)
        {
            await RestoreViewStateAsync();
        }

        _folders = await LibraryFolderRepository.ListAsync();

        var summaries = await ScriptRepository.ListAsync();
        _allCards = await LibraryCardFactory.BuildAsync(summaries, ScriptRepository, PreviewService, TpsDocumentReader, ScriptCompiler, Localizer);

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

    private Task RunLibraryOperationAsync(string operation, string message, Func<Task> action) =>
        Diagnostics.RunAsync(operation, message, action);

    private async void HandleLibrarySearchChanged(string searchText)
    {
        RebuildLibraryView();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Shell.LibrarySearchChanged -= HandleLibrarySearchChanged;
    }
}
