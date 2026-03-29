using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Documents;

namespace PrompterLive.Core.Tests;

internal sealed class InMemoryScriptRepository : IScriptRepository
{
    private readonly Dictionary<string, StoredScriptDocument> _documents = new(StringComparer.Ordinal);

    public Task InitializeAsync(IEnumerable<StoredScriptDocument> seedDocuments, CancellationToken cancellationToken = default)
    {
        foreach (var document in seedDocuments)
        {
            _documents.TryAdd(document.Id, document);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredScriptSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var summaries = _documents.Values
            .OrderByDescending(document => document.UpdatedAt)
            .Select(document => new StoredScriptSummary(
                document.Id,
                document.Title,
                document.DocumentName,
                document.UpdatedAt,
                CountWords(document.Text)))
            .ToList();

        return Task.FromResult<IReadOnlyList<StoredScriptSummary>>(summaries);
    }

    public Task<StoredScriptDocument?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(id, out var document);
        return Task.FromResult(document);
    }

    public Task<StoredScriptDocument> SaveAsync(
        string title,
        string text,
        string? documentName = null,
        string? existingId = null,
        string? folderId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? "Untitled Script" : title.Trim();
        var normalizedDocumentName = string.IsNullOrWhiteSpace(documentName)
            ? $"{Slugify(normalizedTitle)}.tps"
            : documentName;
        var id = string.IsNullOrWhiteSpace(existingId)
            ? Slugify(Path.GetFileNameWithoutExtension(normalizedDocumentName))
            : existingId;
        var persistedFolderId = ResolveFolderId(existingId, folderId);

        var document = new StoredScriptDocument(
            id,
            normalizedTitle,
            text ?? string.Empty,
            normalizedDocumentName,
            DateTimeOffset.UtcNow,
            persistedFolderId);

        _documents[id] = document;
        return Task.FromResult(document);
    }

    public Task MoveToFolderAsync(string id, string? folderId, CancellationToken cancellationToken = default)
    {
        if (_documents.TryGetValue(id, out var document))
        {
            _documents[id] = document with
            {
                UpdatedAt = DateTimeOffset.UtcNow,
                FolderId = string.IsNullOrWhiteSpace(folderId) ? null : folderId
            };
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _documents.Remove(id);
        return Task.CompletedTask;
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

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
        return string.IsNullOrWhiteSpace(slug) ? "untitled-script" : slug;
    }

    private string? ResolveFolderId(string? existingId, string? folderId)
    {
        if (!string.IsNullOrWhiteSpace(folderId))
        {
            return folderId;
        }

        if (string.IsNullOrWhiteSpace(existingId))
        {
            return null;
        }

        return _documents.TryGetValue(existingId, out var existingDocument)
            ? existingDocument.FolderId
            : null;
    }
}
