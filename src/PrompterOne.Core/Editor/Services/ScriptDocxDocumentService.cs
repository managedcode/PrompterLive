using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PrompterOne.Core.Services.Editor;

public sealed class ScriptDocxDocumentService
{
    private const string Heading1StyleId = "Heading1";
    private const string Heading2StyleId = "Heading2";
    private const string TitleStyleId = "Title";

    public byte[] Export(string? title, string? text)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new Body();

            var normalizedTitle = NormalizeLine(title);
            if (!string.IsNullOrWhiteSpace(normalizedTitle))
            {
                body.Append(CreateParagraph(normalizedTitle, TitleStyleId));
            }

            foreach (var paragraph in EnumerateParagraphs(text))
            {
                body.Append(CreateParagraph(paragraph.Text, paragraph.StyleId));
            }

            if (!body.Elements<Paragraph>().Any())
            {
                body.Append(CreateParagraph(string.Empty));
            }

            mainPart.Document = new Document(body);
        }

        return stream.ToArray();
    }

    public string ImportMarkdown(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            var text = NormalizeLine(ReadParagraphText(paragraph));
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(FormatImportedParagraph(paragraph, text));
        }

        return builder.ToString();
    }

    private static Paragraph CreateParagraph(string text, string? styleId = null)
    {
        var paragraph = new Paragraph();
        if (!string.IsNullOrWhiteSpace(styleId))
        {
            paragraph.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId { Val = styleId });
        }

        paragraph.Append(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        return paragraph;
    }

    private static IEnumerable<(string Text, string? StyleId)> EnumerateParagraphs(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        foreach (var rawParagraph in NormalizeNewlines(text).Split("\n\n", StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(rawParagraph))
            {
                continue;
            }

            var normalized = rawParagraph.Trim();
            if (normalized.StartsWith("### ", StringComparison.Ordinal))
            {
                yield return (normalized[4..].Trim(), Heading2StyleId);
                continue;
            }

            if (normalized.StartsWith("## ", StringComparison.Ordinal))
            {
                yield return (normalized[3..].Trim(), Heading1StyleId);
                continue;
            }

            yield return (NormalizeLine(normalized), null);
        }
    }

    private static string FormatImportedParagraph(Paragraph paragraph, string text)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        return styleId switch
        {
            Heading1StyleId => string.Concat("# ", text),
            Heading2StyleId => string.Concat("## ", text),
            TitleStyleId => string.Concat("# ", text),
            _ => text
        };
    }

    private static string ReadParagraphText(Paragraph paragraph)
    {
        var builder = new StringBuilder();
        foreach (var child in paragraph.Descendants())
        {
            switch (child)
            {
                case Text text:
                    builder.Append(text.Text);
                    break;
                case TabChar:
                    builder.Append('\t');
                    break;
                case Break:
                    builder.Append('\n');
                    break;
            }
        }

        return builder.ToString();
    }

    private static string NormalizeLine(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : string.Join(' ', NormalizeNewlines(text).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeNewlines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
