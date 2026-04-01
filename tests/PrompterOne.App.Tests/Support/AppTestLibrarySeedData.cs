using PrompterOne.App.Tests;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Library;

namespace PrompterOne.Shared.Tests;

internal static class AppTestLibrarySeedData
{
    private static readonly DateTimeOffset FolderSeedTimestamp = new(2026, 3, 29, 10, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<StoredScriptDocument> CreateDocuments() =>
    [
        CreateDocument(
            AppTestData.Scripts.DemoId,
            AppTestData.Scripts.DemoTitle,
            "test-product-launch.tps",
            new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero),
            AppTestData.Folders.ProductId),
        CreateDocument(
            AppTestData.Scripts.LeadershipId,
            AppTestData.Scripts.TedLeadershipTitle,
            "test-ted-leadership.tps",
            new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero),
            AppTestData.Folders.TedTalksId),
        CreateDocument(
            AppTestData.Scripts.SecurityIncidentId,
            AppTestData.Scripts.SecurityIncidentTitle,
            "test-security-incident.tps",
            new DateTimeOffset(2026, 3, 24, 8, 30, 0, TimeSpan.Zero),
            AppTestData.Folders.NewsReportsId),
        CreateDocument(
            AppTestData.Scripts.SpeedOffsetsId,
            AppTestData.Scripts.SpeedOffsetsTitle,
            "test-tps-speed-offsets.tps",
            new DateTimeOffset(2026, 3, 27, 11, 15, 0, TimeSpan.Zero),
            AppTestData.Folders.InternalId),
        CreateDocument(
            AppTestData.Scripts.ArchitectureId,
            AppTestData.Scripts.GreenArchitectureTitle,
            "test-green-architecture.tps",
            new DateTimeOffset(2026, 3, 18, 10, 15, 0, TimeSpan.Zero),
            AppTestData.Folders.InvestorsId),
        CreateDocument(
            AppTestData.Scripts.QuantumId,
            AppTestData.Scripts.QuantumTitle,
            "test-quantum-computing.tps",
            new DateTimeOffset(2026, 3, 15, 16, 45, 0, TimeSpan.Zero),
            AppTestData.Folders.InternalId)
    ];

    public static IReadOnlyList<StoredLibraryFolder> CreateFolders() =>
    [
        CreateFolder(AppTestData.Folders.PresentationsId, AppTestData.Folders.PresentationsName, null, 0),
        CreateFolder(AppTestData.Folders.ProductId, AppTestData.Folders.ProductName, AppTestData.Folders.PresentationsId, 0),
        CreateFolder(AppTestData.Folders.TedTalksId, AppTestData.Folders.TedTalksName, null, 1),
        CreateFolder(AppTestData.Folders.NewsReportsId, AppTestData.Folders.NewsReportsName, null, 2),
        CreateFolder(AppTestData.Folders.InvestorsId, AppTestData.Folders.InvestorsName, null, 3),
        CreateFolder(AppTestData.Folders.InternalId, AppTestData.Folders.InternalName, null, 4)
    ];

    private static StoredScriptDocument CreateDocument(
        string id,
        string title,
        string documentName,
        DateTimeOffset updatedAt,
        string? folderId)
    {
        return new StoredScriptDocument(
            Id: id,
            Title: title,
            Text: File.ReadAllText(GetScriptPath(documentName)),
            DocumentName: documentName,
            UpdatedAt: updatedAt,
            FolderId: folderId);
    }

    private static StoredLibraryFolder CreateFolder(string id, string name, string? parentId, int displayOrder)
    {
        return new StoredLibraryFolder(
            Id: id,
            Name: name,
            ParentId: parentId,
            DisplayOrder: displayOrder,
            UpdatedAt: FolderSeedTimestamp);
    }

    private static string GetScriptPath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../tests/TestData/Scripts",
            fileName));
    }
}
