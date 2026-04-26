using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class LibraryPage
{
    private Task CreateScriptAsync() =>
        RunLibraryOperationAsync(
            CreateScriptOperation,
            Text(UiTextKey.LibraryCreateScriptMessage),
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await SessionService.NewAsync();
                Navigation.NavigateTo("/editor");
            });

    private Task OpenScriptAsync(string id) =>
        RunLibraryOperationAsync(
            OpenScriptOperation,
            Text(UiTextKey.LibraryOpenScriptMessage),
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

    private Task PrepScriptAsync(string id)
    {
        Navigation.NavigateTo(AppRoutes.PrepWithId(id));
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
            Text(UiTextKey.LibraryDuplicateScriptMessage),
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                var document = await ScriptRepository.GetAsync(id);
                if (document is null)
                {
                    return;
                }

                await ScriptRepository.SaveAsync(
                    title: Format(UiTextKey.LibraryDuplicateTitleFormat, document.Title),
                    text: document.Text,
                    documentName: null,
                    existingId: null,
                    folderId: document.FolderId);
                await LoadLibraryAsync(restoreViewState: false);
            });

    private Task MoveScriptAsync(LibraryMoveRequest request) =>
        RunLibraryOperationAsync(
            MoveScriptOperation,
            Text(UiTextKey.LibraryMoveScriptMessage),
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await ScriptRepository.MoveToFolderAsync(request.ScriptId, request.FolderId);
                ApplyMovedScript(request);
                await PersistViewStateAsync();
            });

    private Task ToggleFavoriteAsync(string id) =>
        RunLibraryOperationAsync(
            MoveScriptOperation,
            Text(UiTextKey.LibraryMoveScriptMessage),
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                var card = _allCards.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
                if (card is null)
                {
                    return;
                }

                var nextFavoriteState = !card.IsFavorite;
                await ScriptRepository.SetFavoriteAsync(id, nextFavoriteState);
                ApplyFavoriteScript(id, nextFavoriteState);
            });

    private Task DeleteScriptAsync(string id) =>
        RunLibraryOperationAsync(
            DeleteScriptOperation,
            Text(UiTextKey.LibraryDeleteScriptMessage),
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

    private void ApplyFavoriteScript(string id, bool isFavorite)
    {
        _allCards = _allCards
            .Select(card => string.Equals(card.Id, id, StringComparison.Ordinal)
                ? card with { IsFavorite = isFavorite }
                : card)
            .ToList();

        RebuildLibraryView();
    }
}
