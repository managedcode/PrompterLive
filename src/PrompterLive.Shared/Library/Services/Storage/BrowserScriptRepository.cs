using System.Text.Json;
using Microsoft.JSInterop;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Documents;
using PrompterLive.Core.Samples;

namespace PrompterLive.Shared.Services;

public sealed class BrowserScriptRepository(IJSRuntime jsRuntime) : IScriptRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public Task InitializeAsync(IEnumerable<StoredScriptDocument> seedDocuments, CancellationToken cancellationToken = default)
    {
        return InitializeSeedDataAsync(seedDocuments, cancellationToken);
    }

    public async Task<IReadOnlyList<StoredScriptSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var documents = await LoadDocumentsAsync(cancellationToken);

        return documents
            .Select(ToDocument)
            .OrderByDescending(document => document.UpdatedAt)
            .Select(document => new StoredScriptSummary(
                document.Id,
                document.Title,
                document.DocumentName,
                document.UpdatedAt,
                CountWords(document.Text),
                document.FolderId))
            .ToList();
    }

    public Task<StoredScriptDocument?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return GetMappedAsync(id, cancellationToken);
    }

    public async Task<StoredScriptDocument> SaveAsync(
        string title,
        string text,
        string? documentName = null,
        string? existingId = null,
        string? folderId = null,
        CancellationToken cancellationToken = default)
    {
        title = string.IsNullOrWhiteSpace(title) ? "Untitled Script" : title.Trim();
        documentName = string.IsNullOrWhiteSpace(documentName)
            ? $"{BrowserStorageSlugifier.Slugify(title)}.tps"
            : documentName;
        var persistedFolderId = await ResolveFolderIdAsync(existingId, folderId, cancellationToken);

        var document = new StoredScriptDocument(
            Id: string.IsNullOrWhiteSpace(existingId) ? BrowserStorageSlugifier.Slugify(Path.GetFileNameWithoutExtension(documentName)) : existingId,
            Title: title,
            Text: text ?? string.Empty,
            DocumentName: documentName,
            UpdatedAt: DateTimeOffset.UtcNow,
            FolderId: persistedFolderId);

        var documents = await LoadDocumentsAsync(cancellationToken);
        var nextDocument = ToDto(document);
        var existingIndex = documents.FindIndex(item => string.Equals(item.Id, nextDocument.Id, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            documents[existingIndex] = nextDocument;
        }
        else
        {
            documents.Add(nextDocument);
        }

        await SaveDocumentsAsync(documents, cancellationToken);
        return document;
    }

    public async Task MoveToFolderAsync(string id, string? folderId, CancellationToken cancellationToken = default)
    {
        var document = await GetMappedAsync(id, cancellationToken);
        if (document is null)
        {
            return;
        }

        var updated = document with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            FolderId = string.IsNullOrWhiteSpace(folderId) ? null : folderId
        };

        var documents = await LoadDocumentsAsync(cancellationToken);
        var updatedDto = ToDto(updated);
        var existingIndex = documents.FindIndex(item => string.Equals(item.Id, updatedDto.Id, StringComparison.Ordinal));
        if (existingIndex < 0)
        {
            return;
        }

        documents[existingIndex] = updatedDto;
        await SaveDocumentsAsync(documents, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var documents = await LoadDocumentsAsync(cancellationToken);
        documents.RemoveAll(document => string.Equals(document.Id, id, StringComparison.Ordinal));
        await SaveDocumentsAsync(documents, cancellationToken);
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private async Task<StoredScriptDocument?> GetMappedAsync(string id, CancellationToken cancellationToken)
    {
        var dto = (await LoadDocumentsAsync(cancellationToken))
            .FirstOrDefault(document => string.Equals(document.Id, id, StringComparison.Ordinal));
        return dto is null ? null : ToDocument(dto);
    }

    private async Task InitializeSeedDataAsync(IEnumerable<StoredScriptDocument> seedDocuments, CancellationToken cancellationToken)
    {
        var seedList = seedDocuments.ToList();
        var existing = await ListAsync(cancellationToken);
        var seedVersion = await LoadStorageValueAsync(BrowserStorageKeys.DocumentSeedVersion, cancellationToken);
        var forceRefresh = !string.Equals(seedVersion, SampleScriptCatalog.SeedVersion, StringComparison.Ordinal);

        if (forceRefresh)
        {
            var replaceIds = existing
                .Where(SampleScriptCatalog.ShouldReplaceOnSeedRefresh)
                .Select(document => document.Id)
                .ToHashSet(StringComparer.Ordinal);
            var nextDocuments = (await LoadDocumentsAsync(cancellationToken))
                .Where(document => !replaceIds.Contains(document.Id ?? string.Empty))
                .ToList();

            foreach (var seedDocument in seedList.Select(ToDto))
            {
                nextDocuments.RemoveAll(document => string.Equals(document.Id, seedDocument.Id, StringComparison.Ordinal));
                nextDocuments.Add(seedDocument);
            }

            await SaveDocumentsAsync(nextDocuments, cancellationToken);
            await SaveStorageValueAsync(BrowserStorageKeys.DocumentSeedVersion, SampleScriptCatalog.SeedVersion, cancellationToken);
            return;
        }

        var documents = await LoadDocumentsAsync(cancellationToken);
        var existingIds = documents
            .Select(document => document.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var document in seedList.Where(document => !existingIds.Contains(document.Id)))
        {
            documents.Add(ToDto(document));
        }

        await SaveDocumentsAsync(documents, cancellationToken);
    }

    private static StoredScriptDocument ToDocument(BrowserStoredScriptDocumentDto dto)
    {
        return new StoredScriptDocument(
            dto.Id ?? string.Empty,
            dto.Title ?? "Untitled Script",
            dto.Text ?? string.Empty,
            dto.DocumentName ?? "untitled-script.tps",
            dto.UpdatedAt,
            string.IsNullOrWhiteSpace(dto.FolderId) ? null : dto.FolderId);
    }

    private static BrowserStoredScriptDocumentDto ToDto(StoredScriptDocument document)
    {
        return new BrowserStoredScriptDocumentDto
        {
            Id = document.Id,
            Title = document.Title,
            Text = document.Text,
            DocumentName = document.DocumentName,
            UpdatedAt = document.UpdatedAt,
            FolderId = document.FolderId
        };
    }

    private async Task<string?> ResolveFolderIdAsync(
        string? existingId,
        string? folderId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(folderId))
        {
            return folderId;
        }

        if (string.IsNullOrWhiteSpace(existingId))
        {
            return null;
        }

        var existingDocument = await GetMappedAsync(existingId, cancellationToken);
        return existingDocument?.FolderId;
    }

    private async Task<List<BrowserStoredScriptDocumentDto>> LoadDocumentsAsync(CancellationToken cancellationToken)
    {
        var json = await LoadStorageValueAsync(BrowserStorageKeys.DocumentLibrary, cancellationToken);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<BrowserStoredScriptDocumentDto>>(json, JsonOptions) ?? [];
    }

    private Task SaveDocumentsAsync(
        List<BrowserStoredScriptDocumentDto> documents,
        CancellationToken cancellationToken)
    {
        return SaveStorageValueAsync(
            BrowserStorageKeys.DocumentLibrary,
            JsonSerializer.Serialize(documents, JsonOptions),
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
