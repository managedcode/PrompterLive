namespace PrompterLive.Core.Models.Library;

public sealed record StoredLibraryFolder(
    string Id,
    string Name,
    string? ParentId,
    int DisplayOrder,
    DateTimeOffset UpdatedAt);
