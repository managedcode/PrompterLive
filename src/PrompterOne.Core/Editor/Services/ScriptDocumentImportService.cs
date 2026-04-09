using System.Text;
using MarkItDown;
using PrompterOne.Core.Models.Editor;

namespace PrompterOne.Core.Services.Editor;

public sealed class ScriptDocumentImportService(ScriptImportDescriptorService descriptorService)
{
    private const string UnsupportedFileNameMessage = "Only supported script and document files can be imported.";

    private readonly ScriptImportDescriptorService _descriptorService = descriptorService;

    public bool CanImport(string? fileName) =>
        ScriptDocumentFileTypes.CanImportFromPicker(fileName);

    public async Task<ScriptImportDescriptor> ImportAsync(
        Stream stream,
        string? fileName,
        string? mimeType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var normalizedFileName = ScriptDocumentFileTypes.NormalizeFileName(fileName);
        if (!CanImport(normalizedFileName))
        {
            throw new ArgumentException(UnsupportedFileNameMessage, nameof(fileName));
        }

        var text = ScriptDocumentFileTypes.CanReadAsText(normalizedFileName)
            ? await ReadTextAsync(stream, cancellationToken)
            : await ConvertToMarkdownAsync(stream, normalizedFileName, mimeType, cancellationToken);
        var importedDocumentName = ScriptDocumentFileTypes.BuildImportedDocumentName(normalizedFileName);
        var descriptor = _descriptorService.Build(importedDocumentName, text);

        return descriptor with
        {
            Title = ScriptDocumentFileTypes.ResolvePickerTitle(normalizedFileName)
        };
    }

    private static async Task<string> ConvertToMarkdownAsync(
        Stream stream,
        string fileName,
        string? mimeType,
        CancellationToken cancellationToken)
    {
        var extension = ScriptDocumentFileTypes.ResolvePickerSupportedSuffix(fileName) ?? Path.GetExtension(fileName);
        var streamInfo = new StreamInfo(
            mimeType: string.IsNullOrWhiteSpace(mimeType) ? null : mimeType,
            extension: extension,
            fileName: fileName);

        var client = new MarkItDownClient();
        await using var result = await client.ConvertAsync(stream, streamInfo, cancellationToken: cancellationToken);
        return NormalizeMarkdown(result.Markdown);
    }

    private static string NormalizeMarkdown(string? markdown) =>
        string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private static async Task<string> ReadTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
