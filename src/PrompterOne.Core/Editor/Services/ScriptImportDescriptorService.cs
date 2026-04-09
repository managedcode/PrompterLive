using PrompterOne.Core.Models.Editor;

namespace PrompterOne.Core.Services.Editor;

public sealed class ScriptImportDescriptorService
{
    private const string UnsupportedFileNameMessage = "Only .tps, .tps.md, .md.tps, .md, and .txt files are supported.";

    private readonly TpsFrontMatterDocumentService _frontMatterService = new();

    public bool CanImport(string? fileName) =>
        ScriptDocumentFileTypes.CanDropIntoEditor(fileName);

    public ScriptImportDescriptor Build(string? fileName, string? text)
    {
        var normalizedFileName = NormalizeSupportedFileName(fileName);
        var normalizedText = text ?? string.Empty;
        var title = _frontMatterService.ResolveTitle(normalizedText, ResolveFallbackTitle(normalizedFileName));

        return new ScriptImportDescriptor(
            Title: title,
            Text: normalizedText,
            DocumentName: normalizedFileName);
    }

    private static string NormalizeSupportedFileName(string? fileName)
    {
        var normalizedFileName = ScriptDocumentFileTypes.NormalizeFileName(fileName);
        if (ScriptDocumentFileTypes.ResolveSaveSupportedSuffix(normalizedFileName) is null)
        {
            throw new ArgumentException(UnsupportedFileNameMessage, nameof(fileName));
        }

        return normalizedFileName;
    }

    private static string ResolveFallbackTitle(string fileName)
        => ScriptDocumentFileTypes.ResolveSaveTitle(fileName);

}
