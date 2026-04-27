namespace PrompterOne.Shared.Services;

public enum EditorDocumentExportFormat
{
    Native,
    Markdown,
    PlainText,
    Docx
}

public sealed class EditorDocumentSaveCoordinator
{
    private Func<EditorDocumentExportFormat, CancellationToken, Task>? _saveHandler;

    public void Register(Func<EditorDocumentExportFormat, CancellationToken, Task> saveHandler)
    {
        ArgumentNullException.ThrowIfNull(saveHandler);
        _saveHandler = saveHandler;
    }

    public void Unregister(Func<EditorDocumentExportFormat, CancellationToken, Task> saveHandler)
    {
        ArgumentNullException.ThrowIfNull(saveHandler);

        if (_saveHandler == saveHandler)
        {
            _saveHandler = null;
        }
    }

    public Task RequestSaveAsync(CancellationToken cancellationToken = default) =>
        RequestExportAsync(EditorDocumentExportFormat.Native, cancellationToken);

    public Task RequestExportAsync(
        EditorDocumentExportFormat format,
        CancellationToken cancellationToken = default) =>
        _saveHandler is null
            ? Task.CompletedTask
            : _saveHandler(format, cancellationToken);
}
