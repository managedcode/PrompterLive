using System.Text.Json;
using Microsoft.JSInterop;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Library;
using PrompterLive.Core.Services.Samples;

namespace PrompterLive.Shared.Services;

public sealed class BrowserLibraryFolderRepository(IJSRuntime jsRuntime) : ILibraryFolderRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task InitializeAsync(IEnumerable<StoredLibraryFolder> seedFolders, CancellationToken cancellationToken = default)
    {
        var currentVersion = await _jsRuntime.InvokeAsync<string?>(
            BrowserStorageMethodNames.GetFolderSeedVersion,
            cancellationToken);
        var folders = await ListAsync(cancellationToken);

        if (!string.Equals(currentVersion, SampleLibraryFolderCatalog.SeedVersion, StringComparison.Ordinal))
        {
            foreach (var seedFolder in seedFolders)
            {
                await SaveInternalAsync(seedFolder, cancellationToken);
            }

            await _jsRuntime.InvokeVoidAsync(
                BrowserStorageMethodNames.SetFolderSeedVersion,
                cancellationToken,
                SampleLibraryFolderCatalog.SeedVersion);
            return;
        }

        var existingIds = folders
            .Select(folder => folder.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var seedFolder in seedFolders.Where(folder => !existingIds.Contains(folder.Id)))
        {
            await SaveInternalAsync(seedFolder, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<StoredLibraryFolder>> ListAsync(CancellationToken cancellationToken = default)
    {
        var json = await _jsRuntime.InvokeAsync<string>(
            BrowserStorageMethodNames.ListFoldersJson,
            cancellationToken);
        var folders = JsonSerializer.Deserialize<List<BrowserStoredLibraryFolderDto>>(json, JsonOptions) ?? [];

        return folders
            .Select(MapFolder)
            .OrderBy(folder => folder.DisplayOrder)
            .ThenBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<StoredLibraryFolder> CreateAsync(
        string name,
        string? parentId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        if (normalizedName.Length == 0)
        {
            throw new InvalidOperationException("Folder name is required.");
        }

        var folders = await ListAsync(cancellationToken);
        var folder = new StoredLibraryFolder(
            Id: BuildUniqueId(normalizedName, folders),
            Name: normalizedName,
            ParentId: string.IsNullOrWhiteSpace(parentId) ? null : parentId,
            DisplayOrder: ResolveDisplayOrder(parentId, folders),
            UpdatedAt: DateTimeOffset.UtcNow);

        return await SaveInternalAsync(folder, cancellationToken);
    }

    private async Task<StoredLibraryFolder> SaveInternalAsync(
        StoredLibraryFolder folder,
        CancellationToken cancellationToken)
    {
        var json = await _jsRuntime.InvokeAsync<string>(
            BrowserStorageMethodNames.SaveFolderJson,
            cancellationToken,
            ToDto(folder));
        var dto = JsonSerializer.Deserialize<BrowserStoredLibraryFolderDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Folder storage returned an empty payload.");

        return MapFolder(dto);
    }

    private static BrowserStoredLibraryFolderDto ToDto(StoredLibraryFolder folder) =>
        new()
        {
            Id = folder.Id,
            Name = folder.Name,
            ParentId = folder.ParentId,
            DisplayOrder = folder.DisplayOrder,
            UpdatedAt = folder.UpdatedAt
        };

    private static StoredLibraryFolder MapFolder(BrowserStoredLibraryFolderDto dto) =>
        new(
            Id: dto.Id ?? string.Empty,
            Name: dto.Name ?? "Untitled Folder",
            ParentId: string.IsNullOrWhiteSpace(dto.ParentId) ? null : dto.ParentId,
            DisplayOrder: dto.DisplayOrder,
            UpdatedAt: dto.UpdatedAt);

    private static int ResolveDisplayOrder(string? parentId, IEnumerable<StoredLibraryFolder> folders) =>
        folders
            .Where(folder => string.Equals(folder.ParentId, parentId, StringComparison.Ordinal))
            .Select(folder => folder.DisplayOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;

    private static string BuildUniqueId(string name, IEnumerable<StoredLibraryFolder> folders)
    {
        var usedIds = folders
            .Select(folder => folder.Id)
            .ToHashSet(StringComparer.Ordinal);
        var baseId = BrowserStorageSlugifier.Slugify(name);

        if (!usedIds.Contains(baseId))
        {
            return baseId;
        }

        var suffix = 2;
        var candidate = baseId;
        while (usedIds.Contains(candidate))
        {
            candidate = $"{baseId}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
