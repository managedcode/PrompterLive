using Microsoft.JSInterop;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Documents;
using PrompterLive.Core.Services.Samples;
using System.Text.Json;

namespace PrompterLive.Shared.Services;

public sealed class BrowserScriptRepository : IScriptRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _jsRuntime;

    public BrowserScriptRepository(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public Task InitializeAsync(IEnumerable<StoredScriptDocument> seedDocuments, CancellationToken cancellationToken = default)
    {
        return InitializeSeedDataAsync(seedDocuments, cancellationToken);
    }

    public async Task<IReadOnlyList<StoredScriptSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var json = await _jsRuntime.InvokeAsync<string>(
            BrowserStorageMethodNames.ListDocumentsJson,
            cancellationToken);
        var documents = JsonSerializer.Deserialize<List<BrowserStoredScriptDocumentDto>>(json, JsonOptions) ?? [];

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

        var json = await _jsRuntime.InvokeAsync<string>(
            BrowserStorageMethodNames.SaveDocumentJson,
            cancellationToken,
            ToDto(document));
        var dto = JsonSerializer.Deserialize<BrowserStoredScriptDocumentDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Storage returned an empty document payload.");

        return ToDocument(dto);
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

        await _jsRuntime.InvokeAsync<string>(
            BrowserStorageMethodNames.SaveDocumentJson,
            cancellationToken,
            ToDto(updated));
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return _jsRuntime.InvokeVoidAsync(BrowserStorageMethodNames.DeleteDocument, cancellationToken, id).AsTask();
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private async Task<StoredScriptDocument?> GetMappedAsync(string id, CancellationToken cancellationToken)
    {
        var json = await _jsRuntime.InvokeAsync<string>(
            BrowserStorageMethodNames.GetDocumentJson,
            cancellationToken,
            id);
        var dto = JsonSerializer.Deserialize<BrowserStoredScriptDocumentDto?>(json, JsonOptions);
        return dto is null ? null : ToDocument(dto);
    }

    private async Task InitializeSeedDataAsync(IEnumerable<StoredScriptDocument> seedDocuments, CancellationToken cancellationToken)
    {
        var seedList = seedDocuments.ToList();
        var existing = await ListAsync(cancellationToken);
        var seedVersion = await _jsRuntime.InvokeAsync<string?>(
            BrowserStorageMethodNames.GetSeedVersion,
            cancellationToken);
        var forceRefresh = !string.Equals(seedVersion, SampleScriptCatalog.SeedVersion, StringComparison.Ordinal);

        if (forceRefresh)
        {
            foreach (var existingDocument in existing.Where(SampleScriptCatalog.ShouldReplaceOnSeedRefresh))
            {
                await DeleteAsync(existingDocument.Id, cancellationToken);
            }

            foreach (var document in seedList)
            {
                await _jsRuntime.InvokeAsync<string>(
                    BrowserStorageMethodNames.SaveDocumentJson,
                    cancellationToken,
                    ToDto(document));
            }

            await _jsRuntime.InvokeVoidAsync(
                BrowserStorageMethodNames.SetSeedVersion,
                cancellationToken,
                SampleScriptCatalog.SeedVersion);
            return;
        }

        var existingIds = existing
            .Select(document => document.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var document in seedList.Where(document => !existingIds.Contains(document.Id)))
        {
            await _jsRuntime.InvokeAsync<string>(
                BrowserStorageMethodNames.SaveDocumentJson,
                cancellationToken,
                ToDto(document));
        }
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

}
