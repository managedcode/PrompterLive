using System.Text.Json;
using PrompterOne.Shared.Services;

namespace PrompterOne.App.UITests;

internal static class BrowserTestLibrarySeedData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string CreateInitializationScript()
    {
        var documentsJson = JsonSerializer.Serialize(CreateDocuments(), JsonOptions);
        var foldersJson = JsonSerializer.Serialize(CreateFolders(), JsonOptions);
        var documentLibraryKey = JsonSerializer.Serialize("prompterone.library.v1");
        var documentSeedVersionKey = JsonSerializer.Serialize("prompterone.library.seed-version");
        var folderLibraryKey = JsonSerializer.Serialize("prompterone.folders.v1");
        var folderSeedVersionKey = JsonSerializer.Serialize("prompterone.folders.seed-version");
        var materializationVersion = JsonSerializer.Serialize("2026-04-01-browser-library-materialized-v1");

        return $$"""
            (() => {
                const documentLibraryKey = {{documentLibraryKey}};
                const documentSeedVersionKey = {{documentSeedVersionKey}};
                const folderLibraryKey = {{folderLibraryKey}};
                const folderSeedVersionKey = {{folderSeedVersionKey}};
                const documentsJson = {{JsonSerializer.Serialize(documentsJson)}};
                const foldersJson = {{JsonSerializer.Serialize(foldersJson)}};
                const materializationVersion = {{materializationVersion}};

                if (!window.localStorage.getItem(folderLibraryKey)) {
                    window.localStorage.setItem(folderLibraryKey, foldersJson);
                }

                if (!window.localStorage.getItem(documentLibraryKey)) {
                    window.localStorage.setItem(documentLibraryKey, documentsJson);
                }

                window.localStorage.setItem(documentSeedVersionKey, materializationVersion);
                window.localStorage.setItem(folderSeedVersionKey, materializationVersion);
            })();
            """;
    }

    private static IReadOnlyList<BrowserStoredLibraryFolderDto> CreateFolders() =>
    [
        CreateFolder(BrowserTestConstants.Folders.PresentationsId, "Presentations", null, 0),
        CreateFolder("test-product", "Product", BrowserTestConstants.Folders.PresentationsId, 0),
        CreateFolder(BrowserTestConstants.Folders.TedTalksId, BrowserTestConstants.Folders.TedTalksName, null, 1),
        CreateFolder("test-news-reports", "News Reports", null, 2),
        CreateFolder("test-investors", "Investors", null, 3),
        CreateFolder("test-internal", "Internal", null, 4)
    ];

    private static IReadOnlyList<BrowserStoredScriptDocumentDto> CreateDocuments() =>
    [
        CreateDocument(
            BrowserTestConstants.Scripts.DemoId,
            BrowserTestConstants.Scripts.ProductLaunchTitle,
            "test-product-launch.tps",
            new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero),
            "test-product"),
        CreateDocument(
            BrowserTestConstants.Scripts.LeadershipId,
            BrowserTestConstants.Scripts.LeadershipTitle,
            "test-ted-leadership.tps",
            new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero),
            BrowserTestConstants.Folders.TedTalksId),
        CreateDocument(
            BrowserTestConstants.Scripts.SecurityIncidentId,
            BrowserTestConstants.Scripts.SecurityIncidentTitle,
            "test-security-incident.tps",
            new DateTimeOffset(2026, 3, 24, 8, 30, 0, TimeSpan.Zero),
            "test-news-reports"),
        CreateDocument(
            BrowserTestConstants.Scripts.SpeedOffsetsId,
            BrowserTestConstants.Scripts.SpeedOffsetsTitle,
            "test-tps-speed-offsets.tps",
            new DateTimeOffset(2026, 3, 27, 11, 15, 0, TimeSpan.Zero),
            "test-internal"),
        CreateDocument(
            BrowserTestConstants.Scripts.ReaderTimingId,
            BrowserTestConstants.Scripts.ReaderTimingTitle,
            "test-reader-timing.tps",
            new DateTimeOffset(2026, 4, 1, 19, 0, 0, TimeSpan.Zero),
            "test-internal"),
        CreateDocument(
            BrowserTestConstants.Scripts.LearnWpmBoundaryId,
            BrowserTestConstants.Scripts.LearnWpmBoundaryTitle,
            "test-learn-wpm-boundary.tps",
            new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero),
            "test-internal"),
        CreateDocument(
            BrowserTestConstants.Scripts.QuantumId,
            BrowserTestConstants.Scripts.QuantumTitle,
            "test-quantum-computing.tps",
            new DateTimeOffset(2026, 3, 15, 16, 45, 0, TimeSpan.Zero),
            "test-internal")
    ];

    private static BrowserStoredScriptDocumentDto CreateDocument(
        string id,
        string title,
        string documentName,
        DateTimeOffset updatedAt,
        string? folderId)
    {
        return new BrowserStoredScriptDocumentDto
        {
            Id = id,
            Title = title,
            Text = File.ReadAllText(GetScriptPath(documentName)),
            DocumentName = documentName,
            UpdatedAt = updatedAt,
            FolderId = folderId
        };
    }

    private static BrowserStoredLibraryFolderDto CreateFolder(string id, string name, string? parentId, int displayOrder)
    {
        return new BrowserStoredLibraryFolderDto
        {
            Id = id,
            Name = name,
            ParentId = parentId,
            DisplayOrder = displayOrder,
            UpdatedAt = new DateTimeOffset(2026, 3, 29, 10, 0, 0, TimeSpan.Zero)
        };
    }

    private static string GetScriptPath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../tests/TestData/Scripts",
            fileName));
    }
}
