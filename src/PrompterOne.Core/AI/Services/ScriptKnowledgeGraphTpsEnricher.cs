using ManagedCode.Tps;
using PrompterOne.Core.AI.Models;
using System.Text.RegularExpressions;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphTpsEnricher
{
    private const string TpsSegmentNodePrefix = "prompterone:tps:segment:";
    private const string TpsBlockNodePrefix = "prompterone:tps:block:";
    private const string TpsSpeakerNodePrefix = "prompterone:character:";

    public static void AddTpsGraph(
        string documentNodeId,
        string containsEdgeLabel,
        string content,
        ICollection<ScriptKnowledgeGraphSemanticScope> scopes,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var result = TpsRuntime.Compile(content);
        var compiledSegments = result.Script.Segments.ToDictionary(static segment => segment.Id, StringComparer.Ordinal);
        var usedHeaderOffsets = new HashSet<int>();
        foreach (var segment in result.Document.Segments)
        {
            compiledSegments.TryGetValue(segment.Id, out var compiledSegment);
            var segmentNodeId = TpsSegmentNodePrefix + SanitizeId(segment.Id);
            var header = FindAndReserveTpsHeader(content, "## ", segment.Name, usedHeaderOffsets);
            nodes.TryAdd(
                segmentNodeId,
                new ScriptKnowledgeGraphNode(
                    segmentNodeId,
                    segment.Name,
                    "TpsSegment",
                    "tps",
                    CreateReadableDetail(segment.Content, segment.Name),
                    CreateScopeAttributes(
                        "segment",
                        compiledSegment?.TargetWpm ?? segment.TargetWpm,
                        compiledSegment?.Emotion ?? segment.Emotion,
                        compiledSegment?.Speaker ?? segment.Speaker,
                        compiledSegment?.Archetype ?? segment.Archetype,
                        compiledSegment?.Timing ?? segment.Timing,
                        header?.LineNumber)));
            ScriptKnowledgeGraphEdges.Add(edges, documentNodeId, segmentNodeId, containsEdgeLabel);
            AddTpsHeaderRange(content, segmentNodeId, ranges, header);
            AddTpsMetadataNodes(
                segmentNodeId,
                compiledSegment?.Speaker ?? segment.Speaker,
                nodes,
                edges);
            scopes.Add(new ScriptKnowledgeGraphSemanticScope(segmentNodeId, segment.Name, segment.Content));

            var compiledBlocks = compiledSegment?.Blocks.ToDictionary(static block => block.Id, StringComparer.Ordinal)
                ?? new Dictionary<string, ManagedCode.Tps.Models.CompiledBlock>(StringComparer.Ordinal);

            foreach (var block in segment.Blocks)
            {
                compiledBlocks.TryGetValue(block.Id, out var compiledBlock);
                var blockNodeId = TpsBlockNodePrefix + SanitizeId(block.Id);
                header = FindAndReserveTpsHeader(content, "### ", block.Name, usedHeaderOffsets);
                nodes.TryAdd(
                    blockNodeId,
                    new ScriptKnowledgeGraphNode(
                        blockNodeId,
                        block.Name,
                        "TpsBlock",
                        "tps",
                        CreateReadableDetail(block.Content, block.Name),
                        CreateScopeAttributes(
                            "block",
                            compiledBlock?.TargetWpm ?? block.TargetWpm ?? compiledSegment?.TargetWpm ?? segment.TargetWpm,
                            compiledBlock?.Emotion ?? block.Emotion ?? compiledSegment?.Emotion ?? segment.Emotion,
                            compiledBlock?.Speaker ?? block.Speaker ?? compiledSegment?.Speaker ?? segment.Speaker,
                            compiledBlock?.Archetype ?? block.Archetype ?? compiledSegment?.Archetype ?? segment.Archetype,
                            null,
                            header?.LineNumber)));
                ScriptKnowledgeGraphEdges.Add(edges, segmentNodeId, blockNodeId, containsEdgeLabel);
                AddTpsHeaderRange(content, blockNodeId, ranges, header);
                AddTpsMetadataNodes(
                    blockNodeId,
                    compiledBlock?.Speaker ?? block.Speaker ?? compiledSegment?.Speaker ?? segment.Speaker,
                    nodes,
                    edges);
                scopes.Add(new ScriptKnowledgeGraphSemanticScope(blockNodeId, block.Name, block.Content));
            }
        }
    }

    private static void AddTpsMetadataNodes(
        string scopeNodeId,
        string? speaker,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges)
    {
        AddTpsMetadataNode(nodes, edges, scopeNodeId, speaker, TpsSpeakerNodePrefix, "Character", "spoken by", "speaker");
    }

    private static void AddTpsMetadataNode(
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        string scopeNodeId,
        string? value,
        string prefix,
        string kind,
        string edgeLabel,
        string valueType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalizedValue = value.Trim();
        var nodeId = prefix + SanitizeId(normalizedValue);
        nodes.TryAdd(
            nodeId,
            new ScriptKnowledgeGraphNode(
                nodeId,
                normalizedValue,
                kind,
                "tps",
                $"{valueType}: {normalizedValue}",
                CreateValueAttributes(valueType, normalizedValue)));
        ScriptKnowledgeGraphEdges.Add(edges, scopeNodeId, nodeId, edgeLabel);
    }

    private static void AddTpsHeaderRange(
        string content,
        string nodeId,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges,
        TpsHeaderLine? header)
    {
        if (header is null)
        {
            return;
        }

        ranges.TryAdd(
            nodeId,
            ScriptKnowledgeGraphSourceRanges.CreateSourceRange(nodeId, content, header.Start, header.End));
    }

    private static TpsHeaderLine? FindAndReserveTpsHeader(
        string content,
        string prefix,
        string name,
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
            return new TpsHeaderLine(line.Number, line.Text, line.Start, line.End);
        }

        return null;
    }

    private static string SanitizeId(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-", RegexOptions.CultureInvariant).Trim('-');

    private static string CreateReadableDetail(string content, string fallback)
    {
        var text = Regex.Replace(content, @"(?m)^\s*@[\p{L}_][^\r\n]*$", " ", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, @"\[[^\]\r\n|]+:[^\]\r\n]*\]|\[/[^\]\r\n]+\]|\[[^\]\r\n|]+\]", string.Empty, RegexOptions.CultureInvariant);
        text = Regex.Replace(text, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        return text.Length <= 220 ? text : string.Concat(text.AsSpan(0, 217).Trim(), "...");
    }

    private static IReadOnlyDictionary<string, string> CreateScopeAttributes(
        string scope,
        int? targetWpm,
        string? emotion,
        string? speaker,
        string? archetype,
        string? timing,
        int? lineNumber)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["scope"] = scope
        };
        AddAttribute(attributes, "wpm", targetWpm?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AddAttribute(attributes, "emotion", emotion);
        AddAttribute(attributes, "speaker", speaker);
        AddAttribute(attributes, "archetype", archetype);
        AddAttribute(attributes, "timing", timing);
        AddAttribute(attributes, "line", lineNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return attributes;
    }

    private static IReadOnlyDictionary<string, string> CreateValueAttributes(string valueType, string value) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["valueType"] = valueType,
            ["value"] = value
        };

    private static void AddAttribute(IDictionary<string, string> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            attributes[key] = value.Trim();
        }
    }

    private sealed record TpsHeaderLine(int LineNumber, string Text, int Start, int End);
}
