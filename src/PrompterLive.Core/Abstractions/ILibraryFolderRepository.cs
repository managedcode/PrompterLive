using PrompterLive.Core.Models.Library;

namespace PrompterLive.Core.Abstractions;

public interface ILibraryFolderRepository
{
    Task InitializeAsync(IEnumerable<StoredLibraryFolder> seedFolders, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredLibraryFolder>> ListAsync(CancellationToken cancellationToken = default);

    Task<StoredLibraryFolder> CreateAsync(
        string name,
        string? parentId = null,
        CancellationToken cancellationToken = default);
}
