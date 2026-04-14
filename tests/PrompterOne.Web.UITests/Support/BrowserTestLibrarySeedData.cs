using System.Text.Json;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Web.UITests;

internal static class BrowserTestLibrarySeedData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TpsFrontMatterDocumentService FrontMatterService = new();

    public static string CreateInitializationScript()
    {
        var documentsJson = JsonSerializer.Serialize(CreateDocuments(), JsonOptions);
        var foldersJson = JsonSerializer.Serialize(CreateFolders(), JsonOptions);
        var settingsJson = JsonSerializer.Serialize(SettingsPagePreferences.Default with
        {
            HasSeenOnboarding = true
        }, JsonOptions);
        var documentLibraryKey = JsonSerializer.Serialize(BrowserStorageKeys.DocumentLibrary);
        var documentSeedVersionKey = JsonSerializer.Serialize(BrowserStorageKeys.DocumentSeedVersion);
        var folderLibraryKey = JsonSerializer.Serialize(BrowserStorageKeys.FolderLibrary);
        var folderSeedVersionKey = JsonSerializer.Serialize(BrowserStorageKeys.FolderSeedVersion);
        var settingsKey = JsonSerializer.Serialize(string.Concat(BrowserStorageKeys.SettingsPrefix, SettingsPagePreferences.StorageKey));
        var materializationVersion = JsonSerializer.Serialize(BrowserStorageKeys.LibraryMaterializationVersion);

        return $$"""
            (() => {
                const documentLibraryKey = {{documentLibraryKey}};
                const documentSeedVersionKey = {{documentSeedVersionKey}};
                const folderLibraryKey = {{folderLibraryKey}};
                const folderSeedVersionKey = {{folderSeedVersionKey}};
                const settingsKey = {{settingsKey}};
                const documentsJson = {{JsonSerializer.Serialize(documentsJson)}};
                const foldersJson = {{JsonSerializer.Serialize(foldersJson)}};
                const settingsJson = {{JsonSerializer.Serialize(settingsJson)}};
                const materializationVersion = {{materializationVersion}};

                if (!window.localStorage.getItem(folderLibraryKey)) {
                    window.localStorage.setItem(folderLibraryKey, foldersJson);
                }

                if (!window.localStorage.getItem(documentLibraryKey)) {
                    window.localStorage.setItem(documentLibraryKey, documentsJson);
                }

                if (!window.localStorage.getItem(settingsKey)) {
                    window.localStorage.setItem(settingsKey, settingsJson);
                }

                window.localStorage.setItem(documentSeedVersionKey, materializationVersion);
                window.localStorage.setItem(folderSeedVersionKey, materializationVersion);
            })();
            """;
    }

    public static string GetSeededScriptText(string scriptId) =>
        GetSeededScriptDocument(scriptId).Text
        ?? throw new InvalidOperationException($"The seeded script '{scriptId}' does not have text.");

    public static string GetSeededScriptTitle(string scriptId) =>
        GetSeededScriptDocument(scriptId).Title
        ?? throw new InvalidOperationException($"The seeded script '{scriptId}' does not have a title.");

    public static string GetSeededScriptVisibleText(string scriptId) =>
        FrontMatterService.Parse(GetSeededScriptText(scriptId)).Body;

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
            BrowserTestConstants.Scripts.HugeDraftId,
            BrowserTestConstants.Scripts.HugeDraftTitle,
            "test-huge-editor-draft.tps",
            EditorLargeDraftPerformanceTestData.BuildHugeDraft(),
            new DateTimeOffset(2026, 4, 3, 7, 30, 0, TimeSpan.Zero),
            "test-internal"),
        CreateDocument(
            BrowserTestConstants.Scripts.LargeDraftId,
            BrowserTestConstants.Scripts.LargeDraftTitle,
            "test-large-editor-draft.tps",
            EditorLargeDraftPerformanceTestData.BuildLargeDraft(),
            new DateTimeOffset(2026, 4, 3, 7, 0, 0, TimeSpan.Zero),
            "test-internal"),
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
            BrowserTestConstants.Scripts.TpsCueMatrixId,
            BrowserTestConstants.Scripts.TpsCueMatrixTitle,
            "test-tps-cue-matrix.tps",
            new DateTimeOffset(2026, 4, 14, 8, 0, 0, TimeSpan.Zero),
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
        string text,
        DateTimeOffset updatedAt,
        string? folderId)
    {
        return new BrowserStoredScriptDocumentDto
        {
            Id = id,
            Title = title,
            Text = text,
            DocumentName = documentName,
            UpdatedAt = updatedAt,
            FolderId = folderId
        };
    }

    private static BrowserStoredScriptDocumentDto GetSeededScriptDocument(string scriptId) =>
        CreateDocuments().FirstOrDefault(
            document => string.Equals(document.Id, scriptId, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Unable to resolve the seeded script '{scriptId}'.");

    private static BrowserStoredScriptDocumentDto CreateDocument(
        string id,
        string title,
        string documentName,
        DateTimeOffset updatedAt,
        string? folderId)
    {
        return CreateDocument(id, title, documentName, File.ReadAllText(GetScriptPath(documentName)), updatedAt, folderId);
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
