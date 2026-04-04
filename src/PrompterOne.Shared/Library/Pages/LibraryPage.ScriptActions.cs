using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class LibraryPage
{
    private Task CreateScriptAsync() =>
        RunLibraryOperationAsync(
            CreateScriptOperation,
            CreateScriptMessage,
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await SessionService.NewAsync();
                Navigation.NavigateTo("/editor");
            });

    private Task OpenScriptAsync(string id) =>
        RunLibraryOperationAsync(
            OpenScriptOperation,
            OpenScriptMessage,
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                var document = await ScriptRepository.GetAsync(id);
                if (document is null)
                {
                    return;
                }

                await SessionService.OpenAsync(document);
                Navigation.NavigateTo(AppRoutes.EditorWithId(id));
            });

    private Task LearnScriptAsync(string id)
    {
        Navigation.NavigateTo(AppRoutes.LearnWithId(id));
        return Task.CompletedTask;
    }

    private Task ReadScriptAsync(string id)
    {
        Navigation.NavigateTo(AppRoutes.TeleprompterWithId(id));
        return Task.CompletedTask;
    }

    private Task DuplicateScriptAsync(string id) =>
        RunLibraryOperationAsync(
            DuplicateScriptOperation,
            DuplicateScriptMessage,
            async () =>
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
                await LoadLibraryAsync(restoreViewState: false);
            });

    private Task MoveScriptAsync(LibraryMoveRequest request) =>
        RunLibraryOperationAsync(
            MoveScriptOperation,
            MoveScriptMessage,
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await ScriptRepository.MoveToFolderAsync(request.ScriptId, request.FolderId);
                ApplyMovedScript(request);
                await PersistViewStateAsync();
            });

    private Task DeleteScriptAsync(string id) =>
        RunLibraryOperationAsync(
            DeleteScriptOperation,
            DeleteScriptMessage,
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await ScriptRepository.DeleteAsync(id);

                if (string.Equals(SessionService.State.ScriptId, id, StringComparison.Ordinal))
                {
                    await SessionService.NewAsync();
                }

                await LoadLibraryAsync(restoreViewState: false);
            });

    private void ApplyMovedScript(LibraryMoveRequest request)
    {
        _allCards = _allCards
            .Select(card => string.Equals(card.Id, request.ScriptId, StringComparison.Ordinal)
                ? card with { FolderId = request.FolderId }
                : card)
            .ToList();

        RebuildLibraryView();
    }
}
