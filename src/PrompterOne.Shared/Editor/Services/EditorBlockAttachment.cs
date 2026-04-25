namespace PrompterOne.Shared.Services.Editor;

public sealed record EditorBlockAttachment(
    string Id,
    string BlockKey,
    string FileName,
    string ContentType,
    long Size,
    string? PreviewDataUrl,
    DateTimeOffset AddedAt);
