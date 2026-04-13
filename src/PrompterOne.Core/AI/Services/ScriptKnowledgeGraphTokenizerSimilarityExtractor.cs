using Microsoft.ML.Tokenizers;
using PrompterOne.Core.AI.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptKnowledgeGraphTokenizerSimilarityExtractor
{
    private const int MinimumChunkCharacters = 24;
    private const int MaximumLabelCharacters = 84;
    private const int MaximumDetailCharacters = 220;
    private const int MaximumChunks = 32;
    private const int MaximumNeighborsPerChunk = 3;
    private const double MinimumSimilarity = .18;
    private const string SimilarityNodePrefix = "prompterone:similarity:";
    private const string SimilarityEdgeLabel = "token similarity";

    private static readonly Lazy<Tokenizer> Tokenizer = new(
        static () => TiktokenTokenizer.CreateForModel("gpt-5"));

    public bool AddTokenizerSimilarity(
        string content,
        IReadOnlyList<ScriptKnowledgeGraphSemanticScope> scopes,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var chunks = CreateChunks(content, scopes, ranges).Take(MaximumChunks).ToArray();
        if (chunks.Length < 2)
        {
            return false;
        }

        var vectors = chunks
            .Select(static chunk => new TokenVector(chunk, Tokenizer.Value.EncodeToIds(chunk.Text)))
            .Where(static vector => vector.Magnitude > 0)
            .ToArray();
        if (vectors.Length < 2)
        {
            return false;
        }

        AddChunkNodes(content, vectors, nodes, edges, ranges);
        AddSimilarityEdges(vectors, edges);
        return true;
    }

    private static IEnumerable<ScriptSimilarityChunk> CreateChunks(
        string content,
        IReadOnlyList<ScriptKnowledgeGraphSemanticScope> scopes,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        foreach (var scope in scopes.Where(static scope => !string.Equals(scope.Label, "Document", StringComparison.Ordinal)))
        {
            var text = NormalizeWhitespace(scope.Content);
            if (text.Length >= MinimumChunkCharacters)
            {
                yield return CreateScopeChunk(content, scope, text, ranges);
            }
        }

        if (scopes.Count > 1)
        {
            yield break;
        }

        foreach (var chunk in CreateLineChunks(content))
        {
            yield return chunk;
        }
    }

    private static ScriptSimilarityChunk CreateScopeChunk(
        string content,
        ScriptKnowledgeGraphSemanticScope scope,
        string text,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var start = content.IndexOf(scope.Content, StringComparison.OrdinalIgnoreCase);
        if (start >= 0)
        {
            return new ScriptSimilarityChunk(scope.NodeId, scope.Label, text, start, start + scope.Content.Length);
        }

        return ranges.TryGetValue(scope.NodeId, out var range)
            ? new ScriptSimilarityChunk(scope.NodeId, scope.Label, text, range.Range.Start, range.Range.End)
            : new ScriptSimilarityChunk(scope.NodeId, scope.Label, text, null, null);
    }

    private static IEnumerable<ScriptSimilarityChunk> CreateLineChunks(string content)
    {
        foreach (var line in ScriptKnowledgeGraphSourceRanges.EnumerateLines(content))
        {
            var text = NormalizeWhitespace(line.Text);
            if (text.Length >= MinimumChunkCharacters)
            {
                yield return new ScriptSimilarityChunk(
                    null,
                    CreateLabel(text),
                    text,
                    line.Start,
                    line.End);
            }
        }
    }

    private static void AddChunkNodes(
        string content,
        IReadOnlyList<TokenVector> vectors,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        foreach (var vector in vectors)
        {
            var nodeId = CreateSimilarityNodeId(vector.Chunk);
            nodes.TryAdd(
                nodeId,
                new ScriptKnowledgeGraphNode(
                    nodeId,
                    CreateLabel(vector.Chunk.Text),
                    "SimilarityChunk",
                    "similarity",
                    TrimDetail(vector.Chunk.Text),
                    CreateNodeAttributes(vector)));
            if (!string.IsNullOrWhiteSpace(vector.Chunk.ScopeNodeId) && nodes.ContainsKey(vector.Chunk.ScopeNodeId))
            {
                ScriptKnowledgeGraphEdges.Add(edges, vector.Chunk.ScopeNodeId, nodeId, "contains");
            }

            AddSourceRange(content, nodeId, vector.Chunk, ranges);
        }
    }

    private static void AddSimilarityEdges(
        IReadOnlyList<TokenVector> vectors,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges)
    {
        var candidates = CreateSimilarityCandidates(vectors)
            .Where(static candidate => candidate.Similarity >= MinimumSimilarity)
            .GroupBy(static candidate => candidate.Left.Id, StringComparer.Ordinal)
            .SelectMany(static group => group
                .OrderByDescending(static candidate => candidate.Similarity)
                .Take(MaximumNeighborsPerChunk))
            .ToArray();
        var added = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            var key = string.CompareOrdinal(candidate.Left.Id, candidate.Right.Id) <= 0
                ? $"{candidate.Left.Id}|{candidate.Right.Id}"
                : $"{candidate.Right.Id}|{candidate.Left.Id}";
            if (!added.Add(key))
            {
                continue;
            }

            ScriptKnowledgeGraphEdges.Add(
                edges,
                candidate.Left.Id,
                candidate.Right.Id,
                SimilarityEdgeLabel,
                CreateEdgeAttributes(candidate));
        }
    }

    private static IEnumerable<SimilarityCandidate> CreateSimilarityCandidates(IReadOnlyList<TokenVector> vectors)
    {
        for (var left = 0; left < vectors.Count; left++)
        {
            for (var right = left + 1; right < vectors.Count; right++)
            {
                var similarity = CosineSimilarity(vectors[left], vectors[right]);
                yield return new SimilarityCandidate(vectors[left], vectors[right], similarity);
                yield return new SimilarityCandidate(vectors[right], vectors[left], similarity);
            }
        }
    }

    private static double CosineSimilarity(TokenVector left, TokenVector right)
    {
        var dot = 0d;
        foreach (var (tokenId, leftWeight) in left.Weights)
        {
            if (right.Weights.TryGetValue(tokenId, out var rightWeight))
            {
                dot += leftWeight * rightWeight;
            }
        }

        return dot <= 0 ? 0 : dot / (left.Magnitude * right.Magnitude);
    }

    private static IReadOnlyDictionary<string, string> CreateNodeAttributes(TokenVector vector) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = "tokenizer",
            ["category"] = "token-similarity",
            ["scopeLabel"] = vector.Chunk.ScopeLabel,
            ["tokenCount"] = vector.TokenCount.ToString(CultureInfo.InvariantCulture)
        };

    private static IReadOnlyDictionary<string, string> CreateEdgeAttributes(SimilarityCandidate candidate) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = "tokenizer",
            ["similarity"] = candidate.Similarity.ToString("0.###", CultureInfo.InvariantCulture),
            ["distance"] = (1 - candidate.Similarity).ToString("0.###", CultureInfo.InvariantCulture)
        };

    private static void AddSourceRange(
        string content,
        string nodeId,
        ScriptSimilarityChunk chunk,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        if (chunk.Start is not { } start || chunk.End is not { } end || end <= start || end > content.Length)
        {
            return;
        }

        ranges.TryAdd(nodeId, ScriptKnowledgeGraphSourceRanges.CreateSourceRange(nodeId, content, start, end));
    }

    private static string CreateSimilarityNodeId(ScriptSimilarityChunk chunk) =>
        SimilarityNodePrefix + StableHash($"{chunk.ScopeNodeId}:{chunk.Text}");

    private static string CreateLabel(string text)
    {
        var normalized = NormalizeWhitespace(text);
        return normalized.Length <= MaximumLabelCharacters
            ? normalized
            : string.Concat(normalized.AsSpan(0, MaximumLabelCharacters - 3).Trim(), "...");
    }

    private static string TrimDetail(string text)
    {
        var normalized = NormalizeWhitespace(text);
        return normalized.Length <= MaximumDetailCharacters
            ? normalized
            : string.Concat(normalized.AsSpan(0, MaximumDetailCharacters - 3).Trim(), "...");
    }

    private static string NormalizeWhitespace(string? text) =>
        string.Join(' ', (text ?? string.Empty).Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    private static string StableHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16];

    private sealed record ScriptSimilarityChunk(
        string? ScopeNodeId,
        string ScopeLabel,
        string Text,
        int? Start,
        int? End);

    private sealed class TokenVector
    {
        public TokenVector(ScriptSimilarityChunk chunk, IReadOnlyList<int> tokenIds)
        {
            Chunk = chunk;
            TokenCount = tokenIds.Count;
            Weights = tokenIds
                .GroupBy(static tokenId => tokenId)
                .ToDictionary(static group => group.Key, static group => (double)group.Count());
            Magnitude = Math.Sqrt(Weights.Values.Sum(static value => value * value));
            Id = CreateSimilarityNodeId(chunk);
        }

        public string Id { get; }

        public ScriptSimilarityChunk Chunk { get; }

        public int TokenCount { get; }

        public IReadOnlyDictionary<int, double> Weights { get; }

        public double Magnitude { get; }
    }

    private sealed record SimilarityCandidate(TokenVector Left, TokenVector Right, double Similarity);
}
