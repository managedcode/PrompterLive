using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Library;

namespace PrompterLive.Core.Tests;

internal sealed class InMemoryLibraryFolderRepository : ILibraryFolderRepository
{
    private readonly Dictionary<string, StoredLibraryFolder> _folders = new(StringComparer.Ordinal);

    public Task InitializeAsync(IEnumerable<StoredLibraryFolder> seedFolders, CancellationToken cancellationToken = default)
    {
        foreach (var folder in seedFolders)
        {
            _folders[folder.Id] = folder;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredLibraryFolder>> ListAsync(CancellationToken cancellationToken = default)
    {
        var folders = _folders.Values
            .OrderBy(folder => folder.DisplayOrder)
            .ThenBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<StoredLibraryFolder>>(folders);
    }

    public Task<StoredLibraryFolder> CreateAsync(
        string name,
        string? parentId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var nextFolder = new StoredLibraryFolder(
            Id: BuildUniqueId(normalizedName),
            Name: normalizedName,
            ParentId: string.IsNullOrWhiteSpace(parentId) ? null : parentId,
            DisplayOrder: ResolveDisplayOrder(parentId),
            UpdatedAt: DateTimeOffset.UtcNow);

        _folders[nextFolder.Id] = nextFolder;
        return Task.FromResult(nextFolder);
    }

    private string BuildUniqueId(string name)
    {
        var baseId = Slugify(name);
        if (!_folders.ContainsKey(baseId))
        {
            return baseId;
        }

        var suffix = 2;
        var candidate = baseId;
        while (_folders.ContainsKey(candidate))
        {
            candidate = $"{baseId}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private int ResolveDisplayOrder(string? parentId) =>
        _folders.Values
            .Where(folder => string.Equals(folder.ParentId, parentId, StringComparison.Ordinal))
            .Select(folder => folder.DisplayOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;

    private static string Slugify(string value)
    {
        var slug = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "untitled-folder" : slug;
    }
}
