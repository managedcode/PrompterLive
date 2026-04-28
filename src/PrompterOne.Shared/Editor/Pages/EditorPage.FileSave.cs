using System.Text.RegularExpressions;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private const string MarkdownExportDescription = "Markdown script";
    private const string MarkdownMimeType = "text/markdown";
    private const string DocxExportDescription = "Word document";
    private const string PlainTextExportDescription = "Plain text script";

    private static readonly IReadOnlyList<string> DocxExportFileNameSuffixes = [ScriptDocumentFileTypes.DocxExtension];
    private static readonly IReadOnlyList<string> MarkdownExportFileNameSuffixes = [".md"];
    private static readonly IReadOnlyList<string> PlainTextExportFileNameSuffixes = [".txt"];
    private static readonly Regex ExportCarriageReturnRegex = new(@"\r\n?", RegexOptions.Compiled);
    private static readonly Regex ExportExcessBlankLineRegex = new(@"\n{3,}", RegexOptions.Compiled);
    private static readonly Regex ExportSourceHeadingRegex = new(@"^\s{0,3}#{1,6}\s*\[[^\r\n]+\]\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex ExportSourceInlineTagRegex = new(@"\[[^\]\r\n]+\]", RegexOptions.Compiled);
    private static readonly Regex ExportWhitespaceBeforePunctuationRegex = new(@"\s+([,.!?;:])", RegexOptions.Compiled);

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
                if (format == EditorDocumentExportFormat.Docx)
                {
                    await FilePickerInterop.SaveBytesAsync(
                        ResolveExportDocumentName(format),
                        BuildDocxExportDocument(),
                        ScriptDocumentFileTypes.DocxMimeType,
                        DocxExportDescription,
                        DocxExportFileNameSuffixes);
                    return;
                }

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
        format switch
        {
            EditorDocumentExportFormat.Native => BuildPersistedDocument(_sourceText),
            EditorDocumentExportFormat.PlainText => BuildPlainTextExportDocument(),
            _ => _sourceText
        };

    private byte[] BuildDocxExportDocument() =>
        ScriptDocxDocumentService.Export(_screenTitle, BuildPlainTextExportDocument(includeHeadings: true));

    private string BuildPlainTextExportDocument(bool includeHeadings = false)
    {
        var compiledSegments = SessionService.State.CompiledScript?.Segments;
        if (compiledSegments is { Count: > 0 })
        {
            return BuildCompiledPlainText(compiledSegments, includeHeadings);
        }

        return BuildSourceDisplayText(_sourceText);
    }

    private static string BuildCompiledPlainText(
        IReadOnlyList<PrompterOne.Core.Models.CompiledScript.CompiledSegment> segments,
        bool includeHeadings)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var segment in segments)
        {
            if (includeHeadings && !string.IsNullOrWhiteSpace(segment.Name))
            {
                AppendParagraph(builder, string.Concat("## ", segment.Name.Trim()));
            }

            foreach (var block in segment.Blocks)
            {
                if (includeHeadings && !string.IsNullOrWhiteSpace(block.Name))
                {
                    AppendParagraph(builder, string.Concat("### ", block.Name.Trim()));
                }

                var blockText = BuildCompiledBlockText(block);
                if (!string.IsNullOrWhiteSpace(blockText))
                {
                    AppendParagraph(builder, blockText);
                }
            }
        }

        return builder.ToString().Trim();
    }

    private static void AppendParagraph(System.Text.StringBuilder builder, string text)
    {
        if (builder.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
        }

        builder.Append(text.Trim());
    }

    private static string BuildCompiledBlockText(CompiledBlock? block)
    {
        if (block?.Words is not { Count: > 0 } words)
        {
            return string.Empty;
        }

        var text = string.Join(
            ' ',
            words
                .Where(static word =>
                    word.Metadata is not { IsPause: true } &&
                    word.Metadata is not { IsEditPoint: true } &&
                    !string.IsNullOrWhiteSpace(word.CleanText))
                .Select(static word => word.CleanText.Trim()));

        return CleanExportText(text);
    }

    private static string BuildSourceDisplayText(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var normalized = ExportCarriageReturnRegex.Replace(source, "\n");
        var withoutHeadings = ExportSourceHeadingRegex.Replace(normalized, string.Empty);
        var withoutInlineTags = ExportSourceInlineTagRegex.Replace(withoutHeadings, string.Empty);
        return CleanExportText(withoutInlineTags);
    }

    private static string CleanExportText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = ExportCarriageReturnRegex.Replace(text, "\n");
        normalized = ExportWhitespaceBeforePunctuationRegex.Replace(normalized, "$1");
        normalized = ExportExcessBlankLineRegex.Replace(normalized, "\n\n");
        return normalized.Trim();
    }

    private static IReadOnlyList<string> ResolveExportFileNameSuffixes(EditorDocumentExportFormat format) =>
        format switch
        {
            EditorDocumentExportFormat.Docx => DocxExportFileNameSuffixes,
            EditorDocumentExportFormat.Markdown => MarkdownExportFileNameSuffixes,
            EditorDocumentExportFormat.PlainText => PlainTextExportFileNameSuffixes,
            _ => ScriptDocumentFileTypes.SaveSupportedFileNameSuffixes
        };

    private static string ResolveExportMimeType(EditorDocumentExportFormat format) =>
        format switch
        {
            EditorDocumentExportFormat.Docx => ScriptDocumentFileTypes.DocxMimeType,
            EditorDocumentExportFormat.Markdown => MarkdownMimeType,
            _ => ScriptDocumentFileTypes.TextMimeType
        };

    private static string ResolveExportPickerDescription(EditorDocumentExportFormat format) =>
        format switch
        {
            EditorDocumentExportFormat.Docx => DocxExportDescription,
            EditorDocumentExportFormat.Markdown => MarkdownExportDescription,
            EditorDocumentExportFormat.PlainText => PlainTextExportDescription,
            _ => ScriptDocumentFileTypes.SavePickerDescription
        };

    private string ResolveExportDocumentName(EditorDocumentExportFormat format)
    {
        var normalizedDocumentName = ScriptDocumentFileTypes.NormalizeFileName(SessionService.State.DocumentName);
        var supportedSuffix = format == EditorDocumentExportFormat.Native
            ? ResolveNativeExportSuffix(normalizedDocumentName)
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
            EditorDocumentExportFormat.Docx => ScriptDocumentFileTypes.DocxExtension,
            _ => ScriptDocumentFileTypes.ResolveSaveSupportedSuffix(ScriptWorkspaceState.UntitledScriptDocumentName)
                ?? ScriptDocumentFileTypes.DefaultExtension
        };

    private static string ResolveNativeExportSuffix(string normalizedDocumentName)
    {
        var suffix = ScriptDocumentFileTypes.ResolveSaveSupportedSuffix(normalizedDocumentName);
        return suffix is null
            ? ScriptDocumentFileTypes.DefaultExtension
            : suffix;
    }
}
