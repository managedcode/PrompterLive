using ManagedCode.Tps;
using PrompterOne.Core.AI.Models;
using System.Text.RegularExpressions;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphTpsEnricher
{
    private const string TpsSegmentNodePrefix = "prompterone:tps:segment:";
    private const string TpsBlockNodePrefix = "prompterone:tps:block:";
    private const string TpsSpeakerNodePrefix = "prompterone:tps:speaker:";
    private const string TpsEmotionNodePrefix = "prompterone:tps:emotion:";
    private const string TpsArchetypeNodePrefix = "prompterone:tps:archetype:";

    public static void AddTpsGraph(
        string documentNodeId,
        string containsEdgeLabel,
        string content,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var result = TpsRuntime.Parse(content);
        var usedHeaderOffsets = new HashSet<int>();
        foreach (var segment in result.Document.Segments)
        {
            var segmentNodeId = TpsSegmentNodePrefix + SanitizeId(segment.Id);
            nodes.TryAdd(segmentNodeId, new ScriptKnowledgeGraphNode(segmentNodeId, segment.Name, "TpsSegment", "tps"));
            ScriptKnowledgeGraphEdges.Add(edges, documentNodeId, segmentNodeId, containsEdgeLabel);
            AddTpsHeaderRange(content, "## ", segment.Name, segmentNodeId, ranges, usedHeaderOffsets);
            AddTpsMetadataNodes(segmentNodeId, segment.Speaker, segment.Emotion, segment.Archetype, nodes, edges);

            foreach (var block in segment.Blocks)
            {
                var blockNodeId = TpsBlockNodePrefix + SanitizeId(block.Id);
                nodes.TryAdd(blockNodeId, new ScriptKnowledgeGraphNode(blockNodeId, block.Name, "TpsBlock", "tps"));
                ScriptKnowledgeGraphEdges.Add(edges, segmentNodeId, blockNodeId, containsEdgeLabel);
                AddTpsHeaderRange(content, "### ", block.Name, blockNodeId, ranges, usedHeaderOffsets);
                AddTpsMetadataNodes(
                    blockNodeId,
                    block.Speaker ?? segment.Speaker,
                    block.Emotion ?? segment.Emotion,
                    block.Archetype ?? segment.Archetype,
                    nodes,
                    edges);
            }
        }
    }

    private static void AddTpsMetadataNodes(
        string scopeNodeId,
        string? speaker,
        string? emotion,
        string? archetype,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges)
    {
        AddTpsMetadataNode(nodes, edges, scopeNodeId, speaker, TpsSpeakerNodePrefix, "Character", "spoken by");
        AddTpsMetadataNode(nodes, edges, scopeNodeId, emotion, TpsEmotionNodePrefix, "Theme", "uses emotion");
        AddTpsMetadataNode(nodes, edges, scopeNodeId, archetype, TpsArchetypeNodePrefix, "Archetype", "uses archetype");
    }

    private static void AddTpsMetadataNode(
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        string scopeNodeId,
        string? value,
        string prefix,
        string kind,
        string edgeLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var nodeId = prefix + SanitizeId(value);
        nodes.TryAdd(nodeId, new ScriptKnowledgeGraphNode(nodeId, value.Trim(), kind, "tps"));
        ScriptKnowledgeGraphEdges.Add(edges, scopeNodeId, nodeId, edgeLabel);
    }

    private static void AddTpsHeaderRange(
        string content,
        string prefix,
        string name,
        string nodeId,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges,
        ISet<int> usedHeaderOffsets)
    {
        foreach (var line in ScriptKnowledgeGraphSourceRanges.EnumerateLines(content))
        {
            var trimmed = line.Text.Trim();
            if (usedHeaderOffsets.Contains(line.Start) ||
                !trimmed.StartsWith(prefix, StringComparison.Ordinal) ||
                !trimmed.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            usedHeaderOffsets.Add(line.Start);
            ranges.TryAdd(
                nodeId,
                ScriptKnowledgeGraphSourceRanges.CreateSourceRange(nodeId, content, line.Start, line.End));
            return;
        }
    }

    private static string SanitizeId(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-", RegexOptions.CultureInvariant).Trim('-');
}
