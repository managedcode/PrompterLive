using PrompterOne.Core.Models.Editor;
using PrompterOne.Core.Services.Editor;

namespace PrompterOne.Core.Tests;

public sealed class TpsEditorServicesTests
{
    private readonly TpsFrontMatterDocumentService _frontMatter = new();
    private readonly TpsTextEditor _textEditor = new();

    [Test]
    public void WrapSelection_WrapsExistingSelectionWithProvidedTokens()
    {
        var source = "Good morning everyone";
        var start = source.IndexOf("morning", StringComparison.Ordinal);
        var result = _textEditor.WrapSelection(
            source,
            new EditorSelectionRange(start, start + "morning".Length),
            "[emphasis]",
            "[/emphasis]",
            "text");

        Assert.Equal("Good [emphasis]morning[/emphasis] everyone", result.Text);
        Assert.Equal(start + "[emphasis]".Length, result.Selection.Start);
        Assert.Equal(result.Selection.Start + "morning".Length, result.Selection.End);
    }

    [Test]
    public void WrapSelection_UsesPlaceholderWhenNoSelectionExists()
    {
        var result = _textEditor.WrapSelection(
            "Hello",
            new EditorSelectionRange(5, 5),
            "[highlight]",
            "[/highlight]",
            "text");

        Assert.Equal("Hello[highlight]text[/highlight]", result.Text);
        Assert.Equal(5 + "[highlight]".Length, result.Selection.Start);
        Assert.Equal(result.Selection.Start + "text".Length, result.Selection.End);
    }

    [Test]
    public void InsertAtSelection_ReplacesCurrentSelectionAndMovesCaret()
    {
        var source = "Hello welcome";
        var start = source.IndexOf("welcome", StringComparison.Ordinal);
        var result = _textEditor.InsertAtSelection(
            source,
            new EditorSelectionRange(start, start + "welcome".Length),
            "[pause:2s]");

        Assert.Equal("Hello [pause:2s]", result.Text);
        Assert.Equal(result.Selection.Start, result.Selection.End);
        Assert.Equal("Hello [pause:2s]".Length, result.Selection.Start);
    }

    [Test]
    public void Upsert_RewritesCanonicalFrontMatterAndPreservesBody()
    {
        var source =
            """
            ---
            title: "Product Launch"
            base_wpm: 140
            speed_offsets:
              slow: -20
              fast: 25
            ---

            ## [Intro|Speaker:Alex|warm]
            Body text.
            """;

        var updated = _frontMatter.Upsert(
            source,
            new Dictionary<string, string?>
            {
                [TpsFrontMatterDocumentService.MetadataKeys.Profile] = "Actor",
                [TpsFrontMatterDocumentService.MetadataKeys.Author] = "Editor Test",
                [TpsFrontMatterDocumentService.MetadataKeys.Duration] = "09:30",
                [TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetSlow] = "-10"
            });
        var document = _frontMatter.Parse(updated);

        Assert.Equal("Product Launch", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.Title]);
        Assert.Equal("Actor", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.Profile]);
        Assert.Equal("Editor Test", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.Author]);
        Assert.Equal("09:30", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.Duration]);
        Assert.Equal("-10", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetSlow]);
        Assert.Contains("## [Intro|Speaker:Alex|warm]", document.Body, StringComparison.Ordinal);
    }

    [Test]
    public void Parse_ReadsCanonicalDurationAndNestedSpeedOffsetsOnly()
    {
        var source =
            """
            ---
            title: "System Design"
            profile: "Actor"
            duration: "145:00"
            base_wpm: 140
            speed_offsets:
              xslow: -40
              slow: -14
              fast: 14
              xfast: 50
            xslow_offset: -99
            ---

            ## [Architecture Intro|focused]
            ### [Structure Block]
            Keep the body in the visible editor only.
            """;

        var document = _frontMatter.Parse(source);

        Assert.Equal("145:00", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.Duration]);
        Assert.Equal("-40", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetXslow]);
        Assert.Equal("-14", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetSlow]);
        Assert.Equal("14", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetFast]);
        Assert.Equal("50", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetXfast]);
        Assert.DoesNotContain("xslow_offset", document.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("title:", document.Body, StringComparison.Ordinal);
        Assert.StartsWith("## [Architecture Intro|focused]", document.Body, StringComparison.Ordinal);
    }
}
