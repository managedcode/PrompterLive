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
        new("starter-presentations", "Presentations", null, 0),
        new("starter-product", "Product", "starter-presentations", 0),
        new("starter-investors", "Investors", "starter-presentations", 1),
        new("starter-podcasts", "Podcasts", null, 1),
        new("starter-news-reports", "News Reports", null, 2),
        new("starter-ted-talks", "TED Talks", null, 3),
        new("starter-internal", "Internal", null, 4)
    ];

    private static readonly SeedDocumentDefinition[] DocumentDefinitions =
    [
        new("starter-product-launch-script", "Product Launch", "starter-product-launch.tps", "starter-product"),
        new("starter-security-incident-script", "Security Incident", "starter-security-incident.tps", "starter-news-reports"),
        new("starter-ted-leadership-script", "TED: Leadership", "starter-ted-leadership.tps", "starter-ted-talks"),
        new("starter-green-architecture-script", "Green Architecture", "starter-green-architecture.tps", "starter-investors"),
        new("starter-quantum-computing-script", "Quantum Computing", "starter-quantum-computing.tps", "starter-internal"),
        new("starter-tps-cue-matrix-script", "TPS Cue Matrix", "starter-tps-cue-matrix.tps", "starter-internal")
    ];

    private static readonly DateTimeOffset[] DocumentTimestamps =
    [
        new(2026, 3, 25, 9, 0, 0, TimeSpan.Zero),
        new(2026, 3, 24, 8, 30, 0, TimeSpan.Zero),
        new(2026, 3, 20, 12, 0, 0, TimeSpan.Zero),
        new(2026, 3, 18, 10, 15, 0, TimeSpan.Zero),
        new(2026, 3, 15, 16, 45, 0, TimeSpan.Zero),
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
