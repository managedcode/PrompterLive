using System.Reflection;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Library;

namespace PrompterOne.Shared.Services;

internal static class RuntimeLibrarySeedCatalog
{
    private const string ResourcePrefix = "PrompterOne.Shared.Library.SeedData.";
    private static readonly DateTimeOffset FolderSeedTimestamp = new(2026, 3, 31, 9, 0, 0, TimeSpan.Zero);

    private static readonly SeedFolderDefinition[] FolderDefinitions =
    [
        new("starter-internal", "Internal", null, 0)
    ];

    private static readonly SeedDocumentDefinition[] DocumentDefinitions =
    [
        new("starter-tps-cue-matrix-script", "TPS Cue Matrix", "starter-tps-cue-matrix.tps", "starter-internal")
    ];

    private static readonly DateTimeOffset[] DocumentTimestamps =
    [
        new(2026, 4, 16, 10, 0, 0, TimeSpan.Zero)
    ];

    public static IReadOnlyList<StoredLibraryFolder> CreateFolders() =>
        FolderDefinitions
            .Select(definition => new StoredLibraryFolder(
                Id: definition.Id,
                Name: definition.Name,
                ParentId: definition.ParentId,
                DisplayOrder: definition.DisplayOrder,
                UpdatedAt: FolderSeedTimestamp))
            .ToList();

    public static IReadOnlyList<StoredScriptDocument> CreateDocuments() =>
        DocumentDefinitions
            .Select((definition, index) => new StoredScriptDocument(
                Id: definition.Id,
                Title: definition.Title,
                Text: LoadScriptText(definition.ResourceFileName),
                DocumentName: definition.ResourceFileName,
                UpdatedAt: DocumentTimestamps[index],
                FolderId: definition.FolderId))
            .ToList();

    private static string LoadScriptText(string resourceFileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = string.Concat(ResourcePrefix, resourceFileName);
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Unable to locate runtime seed resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed record SeedFolderDefinition(string Id, string Name, string? ParentId, int DisplayOrder);

    private sealed record SeedDocumentDefinition(string Id, string Title, string ResourceFileName, string FolderId);
}
