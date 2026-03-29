namespace PrompterLive.Core.Models.Documents;

public sealed partial record StoredScriptDocument(
    string Id,
    string Title,
    string Text,
    string DocumentName,
    DateTimeOffset UpdatedAt,
    string? FolderId = null);
