using System.Text.Json;
using Microsoft.JSInterop;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Library;
using PrompterLive.Core.Samples;

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
        var currentVersion = await LoadStorageValueAsync(BrowserStorageKeys.FolderSeedVersion, cancellationToken);
        var folders = await ListAsync(cancellationToken);

        if (!string.Equals(currentVersion, SampleLibraryFolderCatalog.SeedVersion, StringComparison.Ordinal))
        {
            var nextFolders = await LoadFoldersAsync(cancellationToken);
            foreach (var seedFolder in seedFolders)
            {
                var seedFolderDto = ToDto(seedFolder);
                nextFolders.RemoveAll(folder => string.Equals(folder.Id, seedFolderDto.Id, StringComparison.Ordinal));
                nextFolders.Add(seedFolderDto);
            }

            await SaveFoldersAsync(nextFolders, cancellationToken);
            await SaveStorageValueAsync(BrowserStorageKeys.FolderSeedVersion, SampleLibraryFolderCatalog.SeedVersion, cancellationToken);
            return;
        }

        var nextFolderDtos = await LoadFoldersAsync(cancellationToken);
        var existingIds = folders
            .Select(folder => folder.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var seedFolder in seedFolders.Where(folder => !existingIds.Contains(folder.Id)))
        {
            nextFolderDtos.Add(ToDto(seedFolder));
        }

        await SaveFoldersAsync(nextFolderDtos, cancellationToken);
    }

    public async Task<IReadOnlyList<StoredLibraryFolder>> ListAsync(CancellationToken cancellationToken = default)
    {
        var folders = await LoadFoldersAsync(cancellationToken);

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
        var folders = await LoadFoldersAsync(cancellationToken);
        var dto = ToDto(folder);
        var existingIndex = folders.FindIndex(item => string.Equals(item.Id, dto.Id, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            folders[existingIndex] = dto;
        }
        else
        {
            folders.Add(dto);
        }

        await SaveFoldersAsync(folders, cancellationToken);
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

    private async Task<List<BrowserStoredLibraryFolderDto>> LoadFoldersAsync(CancellationToken cancellationToken)
    {
        var json = await LoadStorageValueAsync(BrowserStorageKeys.FolderLibrary, cancellationToken);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<BrowserStoredLibraryFolderDto>>(json, JsonOptions) ?? [];
    }

    private Task SaveFoldersAsync(
        List<BrowserStoredLibraryFolderDto> folders,
        CancellationToken cancellationToken)
    {
        return SaveStorageValueAsync(
            BrowserStorageKeys.FolderLibrary,
            JsonSerializer.Serialize(folders, JsonOptions),
            cancellationToken);
    }

    private Task<string?> LoadStorageValueAsync(string key, CancellationToken cancellationToken)
    {
        return _jsRuntime.InvokeAsync<string?>(
            BrowserStorageMethodNames.LoadStorageValue,
            cancellationToken,
            key).AsTask();
    }

    private Task SaveStorageValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        return _jsRuntime.InvokeVoidAsync(
            BrowserStorageMethodNames.SaveStorageValue,
            cancellationToken,
            key,
            value).AsTask();
    }
}
