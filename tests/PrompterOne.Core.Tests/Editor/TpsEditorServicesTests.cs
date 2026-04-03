using PrompterOne.Core.Models.Editor;
using PrompterOne.Core.Services.Editor;

namespace PrompterOne.Core.Tests;

public sealed class TpsEditorServicesTests
{
    private readonly TpsFrontMatterDocumentService _frontMatter = new();
    private readonly TpsTextEditor _textEditor = new();

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void ClearColorFormatting_RemovesEnclosingColorTagsFromSelection()
    {
        const string source = "Hello [green]welcome[/green] friend";
        var start = source.IndexOf("welcome", StringComparison.Ordinal);
        var end = start + "welcome".Length;

        var result = _textEditor.ClearColorFormatting(source, new EditorSelectionRange(start, end));

        Assert.Equal("Hello welcome friend", result.Text);
        Assert.Equal(start - "[green]".Length, result.Selection.Start);
        Assert.Equal(result.Selection.Start + "welcome".Length, result.Selection.End);
    }

    [Fact]
    public void Upsert_RewritesFrontMatterAndPreservesBody()
    {
        var source =
            """
            ---
            title: "Product Launch"
            base_wpm: 140
            ---

            ## [Intro|140WPM|warm]
            Body text.
            """;

        var updated = _frontMatter.Upsert(
            source,
            new Dictionary<string, string?>
            {
                [TpsFrontMatterDocumentService.MetadataKeys.Profile] = "RSVP",
                [TpsFrontMatterDocumentService.MetadataKeys.Author] = "Editor Test"
            });
        var document = _frontMatter.Parse(updated);

        Assert.Equal("Product Launch", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.Title]);
        Assert.Equal("RSVP", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.Profile]);
        Assert.Equal("Editor Test", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.Author]);
        Assert.Contains("## [Intro|140WPM|warm]", document.Body);
    }

    [Fact]
    public void Parse_NormalizesDurationAliasAndNestedPresetSpeeds()
    {
        var source =
            """
            ---
            title: "System Design and Software Architecture for Vibe Coders"
            profile: Actor
            duration: "145:00"
            base_wpm: 140
            presets:
              slow: 120
              fast: 160
            author: "Konstantin Semenenko"
            created: "2026-03-25"
            version: "1.0"
            ---

            ## [Architecture Intro|140WPM|focused]
            ### [Structure Block|140WPM]
            Keep the body in the visible editor only.
            """;

        var document = _frontMatter.Parse(source);

        Assert.Equal("145:00", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.DisplayDuration]);
        Assert.Equal("-14", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.SlowOffset]);
        Assert.Equal("14", document.Metadata[TpsFrontMatterDocumentService.MetadataKeys.FastOffset]);
        Assert.Equal("120", document.Metadata["presets.slow"]);
        Assert.Equal("160", document.Metadata["presets.fast"]);
        Assert.DoesNotContain("title:", document.Body, StringComparison.Ordinal);
        Assert.StartsWith("## [Architecture Intro|140WPM|focused]", document.Body, StringComparison.Ordinal);
    }
}
