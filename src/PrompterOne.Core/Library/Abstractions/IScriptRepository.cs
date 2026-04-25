using PrompterOne.Core.Models.Documents;

namespace PrompterOne.Core.Abstractions;

public interface IScriptRepository
{
    Task InitializeAsync(IEnumerable<StoredScriptDocument> initialDocuments, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredScriptSummary>> ListAsync(CancellationToken cancellationToken = default);

    Task<StoredScriptDocument?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<StoredScriptDocument> SaveAsync(
        string title,
        string text,
        string? documentName = null,
        string? existingId = null,
        string? folderId = null,
        CancellationToken cancellationToken = default);

    Task MoveToFolderAsync(string id, string? folderId, CancellationToken cancellationToken = default);

    Task SetFavoriteAsync(string id, bool isFavorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
