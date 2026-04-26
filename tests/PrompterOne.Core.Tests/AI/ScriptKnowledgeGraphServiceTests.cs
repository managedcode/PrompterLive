using PrompterOne.Core.AI.Abstractions;
using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;

namespace PrompterOne.Core.Tests;

public sealed class ScriptKnowledgeGraphServiceTests
{
    private const double ModelConfidence = .91;
    private const string ModelClaimLabel = "Evidence changes launch confidence";
    private const string ModelLinkLabel = "supports";
    private const string ModelThemeLabel = "Audience trust";

    private const string Script = """
    # Product Launch
    ## [Intro|Speaker:Alex|140WPM|focused]
    Alex mentions [RDF](https://example.com/rdf) and graph thinking because connected knowledge reduces context loss by 50%.
    @plot: Connected Context
    @location: Demo Stage
    @pov: Alex
    @entity: Launch Council
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
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "Line");
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "Theme" && node.Group == "tps");
        Assert.DoesNotContain(result.Nodes, static node => node.Group == "knowledge" && node.Label.Contains('|', StringComparison.Ordinal));
        Assert.DoesNotContain(result.Nodes, static node => node.Group == "knowledge" && node.Label.StartsWith("schema:", StringComparison.Ordinal));
        Assert.Contains(result.Edges, static edge => edge.Label == "contains");
        Assert.Contains(result.Edges, static edge => edge.Label == "spoken by");
        Assert.DoesNotContain(result.Edges, static edge => edge.Label == "uses emotion");
        Assert.DoesNotContain(result.Nodes, static node => node.Group == "story");
        Assert.DoesNotContain(result.Edges, static edge => edge.Label == "states");
        Assert.DoesNotContain(result.Edges, static edge => edge.Label == "claims");
        Assert.DoesNotContain(result.Edges, static edge => edge.Label == "about");
        Assert.DoesNotContain(result.Edges, static edge => edge.Label == "mentions");
        Assert.DoesNotContain(result.Edges, static edge => edge.Label == "point of view");
        Assert.Equal(ScriptKnowledgeGraphSemanticStatus.ModelUnavailable, result.SemanticStatus);
        Assert.Equal(ScriptKnowledgeGraphSemanticMode.ModelOnly, result.SemanticMode);
        Assert.Contains(result.Nodes, static node =>
            node.Kind == "Character" &&
            node.Attributes is not null &&
            node.Attributes.ContainsKey("centrality"));
        Assert.Contains(result.Nodes, static node =>
            node.Kind == "Character" &&
            node.Label == "Alex" &&
            node.Attributes?.TryGetValue("source", out var source) == true &&
            source == "markdown-ld-kb" &&
            node.Attributes.TryGetValue("entityType", out var entityType) &&
            entityType == "schema:Person");
        Assert.Contains(result.Nodes, static node =>
            node.Kind == "Term" &&
            node.Label == "focused" &&
            node.Attributes?.TryGetValue("source", out var source) == true &&
            source == "markdown-ld-kb" &&
            node.Attributes.TryGetValue("entityType", out var entityType) &&
            entityType == "schema:DefinedTerm");
        Assert.Contains(result.Edges, static edge =>
            edge.Label == "kb:memberOf" ||
            edge.Label == "memberOf");
        Assert.Contains("Product Launch", result.JsonLd, StringComparison.Ordinal);
        Assert.Contains("Product Launch", result.Turtle, StringComparison.Ordinal);
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
        var launchNode = result.Nodes.Single(node => node.Kind == "Section" && node.Label.Contains("Product Launch", StringComparison.Ordinal));
        var range = result.SourceRanges.Single(range => range.NodeId == launchNode.Id);

        Assert.Equal(1, range.Start.Line);
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
        Assert.Equal(8, questionRange.Start.Line);
    }

    [Test]
    public async Task BuildAsync_ProjectsTpsAttributesIntoSectionMetadataWithoutGraphNoise()
    {
        const string source = """
        # Product Launch
        ## [Intro|Speaker:Alex|Archetype:Coach|warm|0:00-0:30]
        ### [Proof|Speaker:Jordan|180WPM|focused]
        [highlight]Ship[/highlight] this launch with customer proof. Because 80% of buyers need evidence, this launch must lead with proof [pause:500ms]
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
        Assert.DoesNotContain("## [Intro", segment.Detail ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("block", block.Attributes!["scope"]);
        Assert.Equal("180", block.Attributes["wpm"]);
        Assert.Equal("Jordan", block.Attributes["speaker"]);
        Assert.Equal("focused", block.Attributes["emotion"]);
        Assert.Equal("3", block.Attributes["line"]);
        Assert.Contains("Ship", block.Detail ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("180WPM", block.Detail ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(result.Nodes, static node =>
            node.Kind == "Character" &&
            node.Label == "Alex");
        Assert.Contains(result.Edges, static edge => edge.Label == "spoken by");
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "Pace");
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "Timing");
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "Cue");
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "Line");
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "Theme" && node.Group == "tps");
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "Archetype" && node.Group == "tps");
        Assert.DoesNotContain(result.Nodes, static node => node.Group == "story");
        Assert.DoesNotContain(result.Nodes, static node => node.Attributes?.ContainsKey("claimScore") == true);
        Assert.Equal(ScriptKnowledgeGraphSemanticStatus.ModelUnavailable, result.SemanticStatus);
        Assert.DoesNotContain(result.Nodes, static node => node.Group == "knowledge" && node.Label.Contains('|', StringComparison.Ordinal));
        Assert.DoesNotContain(result.Edges, static edge => edge.SourceId.Contains('|', StringComparison.Ordinal) || edge.TargetId.Contains('|', StringComparison.Ordinal));
    }

    [Test]
    public async Task BuildAsync_CoOccurrenceAndWeightsConnectModelWriterConcepts()
    {
        var extractor = FakeSemanticExtractor.Returning(new ScriptKnowledgeGraphSemanticExtraction(
            [
                new ScriptKnowledgeGraphSemanticNode("Entity", "Launch Council", "Review group", "Opening", "Launch Council", ModelConfidence),
                new ScriptKnowledgeGraphSemanticNode("Theme", "customer proof", "Evidence theme", "Opening", "customer proof", ModelConfidence),
                new ScriptKnowledgeGraphSemanticNode("Entity", "Launch Council", "Review group", "Evidence", "Launch Council", ModelConfidence),
                new ScriptKnowledgeGraphSemanticNode("Theme", "customer proof", "Evidence theme", "Evidence", "Customer proof", ModelConfidence)
            ],
            []));
        var service = new ScriptKnowledgeGraphService(extractor);
        const string source = """
        # Product Launch
        ## [Intro|Speaker:Alex|140WPM|focused]
        ### [Opening|Speaker:Alex|140WPM|focused]
        @entity: Launch Council
        @theme: customer proof
        Alex says customer proof reduces launch risk because 80% of buyers need evidence.
        ### [Evidence|Speaker:Alex|140WPM|focused]
        @entity: Launch Council
        @theme: customer proof
        Customer proof connects the launch story to decision confidence.
        """;

        var result = await service.BuildAsync(CreateRequest(source));
        var launchCouncil = result.Nodes.Single(static node => node.Kind == "Entity" && node.Label == "Launch Council");
        var customerProof = result.Nodes.Single(static node => node.Kind == "Theme" && node.Label == "customer proof");

        Assert.Contains(result.Edges, edge =>
            edge.Label == "co-occurs" &&
            ((edge.SourceId == launchCouncil.Id && edge.TargetId == customerProof.Id) ||
             (edge.SourceId == customerProof.Id && edge.TargetId == launchCouncil.Id)));
        Assert.Equal("2", launchCouncil.Attributes?["weight"]);
        Assert.Equal("2", customerProof.Attributes?["weight"]);
        Assert.True(int.Parse(launchCouncil.Attributes!["centrality"], System.Globalization.CultureInfo.InvariantCulture) > 1);
    }

    [Test]
    public async Task BuildAsync_UsesConfiguredSemanticExtractorBeforeTokenizerFallback()
    {
        var extractor = FakeSemanticExtractor.Returning(new ScriptKnowledgeGraphSemanticExtraction(
            [
                new ScriptKnowledgeGraphSemanticNode("Theme", ModelThemeLabel, "Trust topic", "Opening", "customer proof", ModelConfidence),
                new ScriptKnowledgeGraphSemanticNode("Claim", ModelClaimLabel, "Proof affects confidence", "Opening", "because 80% of buyers need evidence", ModelConfidence)
            ],
            [
                new ScriptKnowledgeGraphSemanticLink(ModelClaimLabel, ModelThemeLabel, ModelLinkLabel)
            ]));
        var service = new ScriptKnowledgeGraphService(extractor);
        const string source = """
        # Product Launch
        ## [Intro|Speaker:Alex|140WPM|focused]
        ### [Opening|Speaker:Alex|140WPM|focused]
        [highlight]Customer proof[/highlight] reduces launch risk because [emphasis]80% of buyers[/emphasis] need evidence [pause:500ms].
        """;

        var result = await service.BuildAsync(CreateRequest(source));

        Assert.Equal(1, extractor.Calls);
        Assert.Contains("Customer proof reduces launch risk", extractor.LastRequest!.Content, StringComparison.Ordinal);
        AssertNoVisibleTpsMarkup(extractor.LastRequest.Content);
        Assert.Contains(extractor.LastRequest!.Scopes, static scope => scope.Label == "Document");
        Assert.Contains(extractor.LastRequest.Scopes, static scope => scope.Label == "Opening");
        var openingScope = extractor.LastRequest.Scopes.Single(static scope => scope.Label == "Opening");
        Assert.Contains("Customer proof reduces launch risk", openingScope.Content, StringComparison.Ordinal);
        AssertNoVisibleTpsMarkup(openingScope.Content);
        Assert.Contains(result.Nodes, static node =>
            node.Label == ModelThemeLabel &&
            node.Kind == "Theme" &&
            node.Attributes is not null &&
            node.Attributes["source"] == "llm" &&
            node.Attributes["confidence"] == "0.91");
        Assert.Contains(result.Edges, static edge => edge.Label == ModelLinkLabel);
        Assert.DoesNotContain(result.Nodes, static node => node.Attributes?.ContainsKey("claimScore") == true);
    }

    [Test]
    public async Task BuildAsync_DoesNotCreateSemanticGraphWhenSemanticExtractorFails()
    {
        var extractor = FakeSemanticExtractor.Throwing();
        var service = new ScriptKnowledgeGraphService(extractor);
        const string source = """
        # Product Launch
        ## [Intro|Speaker:Alex|140WPM|focused]
        ### [Opening|Speaker:Alex|140WPM|focused]
        Customer proof reduces launch risk because 80% of buyers need evidence.
        """;

        var result = await service.BuildAsync(CreateRequest(source));

        Assert.Equal(1, extractor.Calls);
        Assert.Equal(ScriptKnowledgeGraphSemanticStatus.ModelFailed, result.SemanticStatus);
        Assert.DoesNotContain(result.Nodes, static node => node.Attributes?.ContainsKey("claimScore") == true);
        Assert.DoesNotContain(result.Nodes, static node =>
            (node.Kind is "Idea" or "Claim" or "Term" or "Theme") &&
            node.Group == "story");
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "SimilarityChunk");
    }

    [Test]
    public async Task BuildAsync_StructuralOnlySkipsSemanticExtractorAndTokenizerFallback()
    {
        var extractor = FakeSemanticExtractor.Throwing();
        var service = new ScriptKnowledgeGraphService(extractor);
        const string source = """
        # Product Launch
        ## [Intro|Speaker:Alex|140WPM|focused]
        ### [Opening|Speaker:Alex|140WPM|focused]
        Customer proof reduces launch risk because 80% of buyers need evidence.
        """;

        var result = await service.BuildAsync(CreateRequest(source, ScriptKnowledgeGraphSemanticMode.StructuralOnly));

        Assert.Equal(0, extractor.Calls);
        Assert.Equal(ScriptKnowledgeGraphSemanticStatus.StructuralOnly, result.SemanticStatus);
        Assert.Equal(ScriptKnowledgeGraphSemanticMode.StructuralOnly, result.SemanticMode);
        Assert.DoesNotContain(result.Nodes, static node => node.Kind == "SimilarityChunk");
        Assert.DoesNotContain(result.Nodes, static node => node.Attributes?.ContainsKey("claimScore") == true);
    }

    [Test]
    public async Task BuildAsync_RunsTokenizerSimilarityOnlyWhenExplicitlyRequested()
    {
        var extractor = FakeSemanticExtractor.Throwing();
        var service = new ScriptKnowledgeGraphService(extractor);
        const string source = """
        # Product Launch
        ## [Intro|Speaker:Alex|140WPM|focused]
        ### [Proof One|Speaker:Alex|140WPM|focused]
        [highlight]Customer proof launch confidence[/highlight] repeats customer proof evidence for launch confidence [pause:500ms].
        ### [Proof Two|Speaker:Alex|140WPM|focused]
        [emphasis]Customer proof launch confidence[/emphasis] creates evidence for launch readiness [pause:1s].
        ### [Operations|Speaker:Alex|140WPM|focused]
        Orchard weather packing crates describe an unrelated logistics rehearsal.
        """;

        var result = await service.BuildAsync(CreateRequest(source, ScriptKnowledgeGraphSemanticMode.TokenizerSimilarity));

        Assert.Equal(1, extractor.Calls);
        Assert.Equal(ScriptKnowledgeGraphSemanticStatus.TokenizerSimilarity, result.SemanticStatus);
        Assert.Contains(result.Nodes, static node =>
            node.Kind == "SimilarityChunk" &&
            node.Attributes?["source"] == "markdown-ld-kb" &&
            node.Attributes.ContainsKey("entityType"));
        Assert.Contains(result.Nodes, static node =>
            node.Kind == "Term" &&
            node.Attributes?["source"] == "markdown-ld-kb");
        Assert.Contains(result.Edges, static edge =>
            edge.Label == "token similarity" &&
            edge.Attributes?["source"] == "markdown-ld-kb" &&
            edge.Attributes.ContainsKey("confidence"));
        var similarityNodes = result.Nodes.Where(static node => node.Kind == "SimilarityChunk").ToArray();
        Assert.Contains(similarityNodes, static node =>
            node.Label.Contains("Customer proof launch confidence", StringComparison.Ordinal) ||
            (node.Detail?.Contains("Customer proof launch confidence", StringComparison.Ordinal) ?? false));
        foreach (var node in similarityNodes)
        {
            AssertNoVisibleTpsMarkup(node.Label);
            AssertNoVisibleTpsMarkup(node.Detail ?? string.Empty);
        }

        Assert.DoesNotContain(result.Nodes, static node => node.Attributes?.ContainsKey("claimScore") == true);
    }

    private static void AssertNoVisibleTpsMarkup(string value)
    {
        Assert.DoesNotContain("[", value, StringComparison.Ordinal);
        Assert.DoesNotContain("]", value, StringComparison.Ordinal);
        Assert.DoesNotContain("WPM", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pause:", value, StringComparison.OrdinalIgnoreCase);
    }

    private static ScriptKnowledgeGraphBuildRequest CreateRequest(
        string source,
        ScriptKnowledgeGraphSemanticMode semanticMode = ScriptKnowledgeGraphSemanticMode.ModelOnly) =>
        new("launch", "Product Launch", source, ScriptDocumentRevision.Create(source), semanticMode);

    private sealed class FakeSemanticExtractor : IScriptKnowledgeGraphSemanticExtractor
    {
        private readonly ScriptKnowledgeGraphSemanticExtraction? _extraction;
        private readonly bool _throws;

        private FakeSemanticExtractor(ScriptKnowledgeGraphSemanticExtraction? extraction, bool throws)
        {
            _extraction = extraction;
            _throws = throws;
        }

        public int Calls { get; private set; }

        public ScriptKnowledgeGraphSemanticExtractionRequest? LastRequest { get; private set; }

        public static FakeSemanticExtractor Returning(ScriptKnowledgeGraphSemanticExtraction? extraction) =>
            new(extraction, throws: false);

        public static FakeSemanticExtractor Throwing() =>
            new(null, throws: true);

        public Task<ScriptKnowledgeGraphSemanticExtraction?> ExtractAsync(
            ScriptKnowledgeGraphSemanticExtractionRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastRequest = request;
            return _throws
                ? throw new InvalidOperationException("Model extraction failed.")
                : Task.FromResult(_extraction);
        }
    }
}
