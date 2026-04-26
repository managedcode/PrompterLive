using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Layout;

public partial class MainLayout
{
    [Inject] private EditorDocumentSaveCoordinator EditorDocumentSaveCoordinator { get; set; } = null!;

    private Task HandleSaveFileClickAsync() =>
        EditorDocumentSaveCoordinator.RequestExportAsync(EditorDocumentExportFormat.Native);

    private Task HandleExportMarkdownClickAsync() =>
        EditorDocumentSaveCoordinator.RequestExportAsync(EditorDocumentExportFormat.Markdown);

    private Task HandleExportPlainTextClickAsync() =>
        EditorDocumentSaveCoordinator.RequestExportAsync(EditorDocumentExportFormat.PlainText);
}
