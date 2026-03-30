using PrompterLive.Core.Models.Library;

namespace PrompterLive.Core.Samples;

public static class SampleLibraryFolderCatalog
{
    public const string SeedVersion = "2026-03-29-library-folders-v1";
    public const string PresentationsFolderId = "presentations";
    public const string ProductFolderId = "product";
    public const string InvestorsFolderId = "investors";
    public const string PodcastsFolderId = "podcasts";
    public const string NewsReportsFolderId = "news";
    public const string TedTalksFolderId = "ted";
    public const string InternalFolderId = "internal";

    private static readonly DateTimeOffset SeedTimestamp = new(2026, 3, 29, 10, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<StoredLibraryFolder> CreateSeedFolders() =>
    [
        CreateFolder(PresentationsFolderId, "Presentations", parentId: null, displayOrder: 0),
        CreateFolder(ProductFolderId, "Product", PresentationsFolderId, 0),
        CreateFolder(InvestorsFolderId, "Investors", PresentationsFolderId, 1),
        CreateFolder(PodcastsFolderId, "Podcasts", parentId: null, displayOrder: 1),
        CreateFolder(NewsReportsFolderId, "News Reports", parentId: null, displayOrder: 2),
        CreateFolder(TedTalksFolderId, "TED Talks", parentId: null, displayOrder: 3),
        CreateFolder(InternalFolderId, "Internal", parentId: null, displayOrder: 4)
    ];

    private static StoredLibraryFolder CreateFolder(string id, string name, string? parentId, int displayOrder) =>
        new(
            Id: id,
            Name: name,
            ParentId: parentId,
            DisplayOrder: displayOrder,
            UpdatedAt: SeedTimestamp);
}
