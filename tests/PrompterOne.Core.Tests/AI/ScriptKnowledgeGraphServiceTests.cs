using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;

namespace PrompterOne.Core.Tests;

public sealed class ScriptKnowledgeGraphServiceTests
{
    private const string Script = """
    # Product Launch
    ## [Intro|Speaker:Alex|140WPM|focused]
    Alex mentions [RDF](https://example.com/rdf) and graph thinking.
    ### [Audience Question|Speaker:Jordan|130WPM|curious]
    Jordan asks about connected context.
    """;

    private readonly ScriptKnowledgeGraphService _service = new();

    [Test]
    public async Task BuildAsync_CreatesScriptAndKnowledgeGraphNodesWithSourceRanges()
    {
        var request = new ScriptKnowledgeGraphBuildRequest(
            "launch",
            "Product Launch",
            Script,
            ScriptDocumentRevision.Create(Script));

        var result = await _service.BuildAsync(request);

        Assert.Contains(result.Nodes, static node => node.Kind == "Document");
        Assert.Contains(result.Nodes, static node => node.Kind == "TpsSegment" && node.Label == "Intro");
        Assert.Contains(result.Nodes, static node => node.Kind == "TpsBlock" && node.Label == "Audience Question");
        Assert.Contains(result.Nodes, static node => node.Kind == "Character" && node.Label == "Alex");
        Assert.Contains(result.Nodes, static node => node.Kind == "Character" && node.Label == "Jordan");
        Assert.Contains(result.Nodes, static node => node.Kind == "Theme" && node.Label == "focused");
        Assert.Contains(result.Nodes, static node => node.Kind == "Line" && node.Label.Contains("Line 3:", StringComparison.Ordinal));
        Assert.Contains(result.Edges, static edge => edge.Label == "contains");
        Assert.Contains(result.Edges, static edge => edge.Label == "spoken by");
        Assert.Contains(result.Edges, static edge => edge.Label == "uses emotion");
        Assert.Contains(result.Nodes, static node => node.Label.Contains("RDF", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("RDF", result.JsonLd, StringComparison.Ordinal);
        Assert.Contains("RDF", result.Turtle, StringComparison.Ordinal);
    }

    [Test]
    public async Task BuildAsync_MapsSectionNodesBackToEditorLineNumbers()
    {
        var request = new ScriptKnowledgeGraphBuildRequest(
            "launch",
            "Product Launch",
            Script,
            ScriptDocumentRevision.Create(Script));

        var result = await _service.BuildAsync(request);
        var introNode = result.Nodes.Single(node => node.Kind == "Section" && node.Label.Contains("Intro", StringComparison.Ordinal));
        var range = result.SourceRanges.Single(range => range.NodeId == introNode.Id);

        Assert.Equal(2, range.Start.Line);
        Assert.True(range.Range.Length > 0);
    }

    [Test]
    public async Task BuildAsync_MapsTpsSegmentAndBlockNodesBackToEditorLineNumbers()
    {
        var request = new ScriptKnowledgeGraphBuildRequest(
            "launch",
            "Product Launch",
            Script,
            ScriptDocumentRevision.Create(Script));

        var result = await _service.BuildAsync(request);
        var introNode = result.Nodes.Single(static node => node.Kind == "TpsSegment" && node.Label == "Intro");
        var questionNode = result.Nodes.Single(static node => node.Kind == "TpsBlock" && node.Label == "Audience Question");
        var introRange = result.SourceRanges.Single(range => range.NodeId == introNode.Id);
        var questionRange = result.SourceRanges.Single(range => range.NodeId == questionNode.Id);

        Assert.Equal(2, introRange.Start.Line);
        Assert.Equal(4, questionRange.Start.Line);
    }

    [Test]
    public async Task BuildAsync_ProjectsTpsAttributesAndCueValuesIntoGraphNodes()
    {
        const string source = """
        # Product Launch
        ## [Intro|Speaker:Alex|Archetype:Coach|warm|0:00-0:30]
        ### [Proof|Speaker:Jordan|180WPM|focused]
        [highlight]Ship[/highlight] this [pause:500ms]
        """;

        var request = new ScriptKnowledgeGraphBuildRequest(
            "launch",
            "Product Launch",
            source,
            ScriptDocumentRevision.Create(source));

        var result = await _service.BuildAsync(request);
        var segment = result.Nodes.Single(static node => node.Kind == "TpsSegment" && node.Label == "Intro");
        var block = result.Nodes.Single(static node => node.Kind == "TpsBlock" && node.Label == "Proof");

        Assert.Equal("segment", segment.Attributes!["scope"]);
        Assert.Equal("Alex", segment.Attributes["speaker"]);
        Assert.Equal("coach", segment.Attributes["archetype"]);
        Assert.Equal("warm", segment.Attributes["emotion"]);
        Assert.Equal("0:00-0:30", segment.Attributes["timing"]);
        Assert.Equal("2", segment.Attributes["line"]);
        Assert.Contains("## [Intro", segment.Detail ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("block", block.Attributes!["scope"]);
        Assert.Equal("180", block.Attributes["wpm"]);
        Assert.Equal("Jordan", block.Attributes["speaker"]);
        Assert.Equal("focused", block.Attributes["emotion"]);
        Assert.Equal("3", block.Attributes["line"]);
        Assert.Contains(result.Nodes, static node =>
            node.Kind == "Pace" &&
            node.Attributes is not null &&
            node.Attributes.TryGetValue("valueType", out var valueType) &&
            string.Equals(valueType, "wpm", StringComparison.Ordinal) &&
            node.Attributes.TryGetValue("value", out var value) &&
            string.Equals(value, "180", StringComparison.Ordinal));
        Assert.Contains(result.Nodes, static node => node.Kind == "Timing" && node.Label == "0:00-0:30");
        Assert.Contains(result.Nodes, static node => node.Kind == "Cue" && node.Label == "highlight");
        Assert.Contains(result.Nodes, static node => node.Kind == "Cue" && node.Label == "pause:500ms");
    }
}
