namespace PrompterLive.Shared.Services;

public sealed class BrowserStoredLibraryFolderDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? ParentId { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
