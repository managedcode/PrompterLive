using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private const string MarkdownExportDescription = "Markdown script";
    private const string MarkdownMimeType = "text/markdown";
    private const string PlainTextExportDescription = "Plain text script";

    private static readonly IReadOnlyList<string> MarkdownExportFileNameSuffixes = [".md"];
    private static readonly IReadOnlyList<string> PlainTextExportFileNameSuffixes = [".txt"];

    private async Task HandleSaveFileRequestedAsync(
        EditorDocumentExportFormat format,
        CancellationToken cancellationToken)
    {
        CancelDraftAnalysis();
        CancelAutosave();
        var revision = PrepareDraftPersistence(_sourceText);

        await Diagnostics.RunAsync(
            SaveFileOperation,
            Text(UiTextKey.EditorSaveFileMessage),
            async () =>
            {
                await PersistDraftStateCoreAsync(persistDocument: true, cancellationToken, revision);
                await FilePickerInterop.SaveTextAsync(
                    ResolveExportDocumentName(format),
                    BuildExportDocument(format),
                    ResolveExportMimeType(format),
                    ResolveExportPickerDescription(format),
                    ResolveExportFileNameSuffixes(format));
            },
            clearRecoverableOnSuccess: string.IsNullOrWhiteSpace(SessionService.State.ErrorMessage));
    }

    private string BuildExportDocument(EditorDocumentExportFormat format) =>
        format == EditorDocumentExportFormat.Native
            ? BuildPersistedDocument(_sourceText)
            : _sourceText;

    private static IReadOnlyList<string> ResolveExportFileNameSuffixes(EditorDocumentExportFormat format) =>
        format switch
        {
            EditorDocumentExportFormat.Markdown => MarkdownExportFileNameSuffixes,
            EditorDocumentExportFormat.PlainText => PlainTextExportFileNameSuffixes,
            _ => ScriptDocumentFileTypes.SaveSupportedFileNameSuffixes
        };

    private static string ResolveExportMimeType(EditorDocumentExportFormat format) =>
        format switch
        {
            EditorDocumentExportFormat.Markdown => MarkdownMimeType,
            _ => ScriptDocumentFileTypes.TextMimeType
        };

    private static string ResolveExportPickerDescription(EditorDocumentExportFormat format) =>
        format switch
        {
            EditorDocumentExportFormat.Markdown => MarkdownExportDescription,
            EditorDocumentExportFormat.PlainText => PlainTextExportDescription,
            _ => ScriptDocumentFileTypes.SavePickerDescription
        };

    private string ResolveExportDocumentName(EditorDocumentExportFormat format)
    {
        var normalizedDocumentName = ScriptDocumentFileTypes.NormalizeFileName(SessionService.State.DocumentName);
        var supportedSuffix = format == EditorDocumentExportFormat.Native
            ? ScriptDocumentFileTypes.ResolveSaveSupportedSuffix(normalizedDocumentName) ?? ScriptDocumentFileTypes.DefaultExtension
            : ResolveExportFileNameSuffix(format);

        if (!string.IsNullOrWhiteSpace(normalizedDocumentName)
            && !string.Equals(
                normalizedDocumentName,
                ScriptWorkspaceState.UntitledScriptDocumentName,
                StringComparison.OrdinalIgnoreCase))
        {
            return format == EditorDocumentExportFormat.Native
                ? normalizedDocumentName
                : ResolveExportFileName(normalizedDocumentName, supportedSuffix);
        }

        return string.Concat(BrowserStorageSlugifier.Slugify(_screenTitle), supportedSuffix);
    }

    private static string ResolveExportFileName(string normalizedDocumentName, string suffix)
    {
        var currentSuffix = ScriptDocumentFileTypes.ResolveSaveSupportedSuffix(normalizedDocumentName);
        if (string.IsNullOrWhiteSpace(currentSuffix))
        {
            return string.Concat(normalizedDocumentName, suffix);
        }

        var stem = normalizedDocumentName[..^currentSuffix.Length].Trim();
        return string.Concat(stem, suffix);
    }

    private static string ResolveExportFileNameSuffix(EditorDocumentExportFormat format) =>
        format switch
        {
            EditorDocumentExportFormat.Markdown => ".md",
            EditorDocumentExportFormat.PlainText => ".txt",
            _ => ScriptDocumentFileTypes.ResolveSaveSupportedSuffix(ScriptWorkspaceState.UntitledScriptDocumentName)
                ?? ScriptDocumentFileTypes.DefaultExtension
        };
}
