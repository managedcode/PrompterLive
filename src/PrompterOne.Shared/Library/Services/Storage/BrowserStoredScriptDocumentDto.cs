namespace PrompterOne.Shared.Services;

public sealed class BrowserStoredScriptDocumentDto
{
    public string? Id { get; set; }

    public string? Title { get; set; }

    public string? Text { get; set; }

    public string? DocumentName { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? FolderId { get; set; }

    public bool IsFavorite { get; set; }
}
