using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using PrompterOne.Core.AI.Abstractions;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptKnowledgeGraphLlmExtractor(IAgentRuntimeSettingsSource settingsSource) : IScriptKnowledgeGraphSemanticExtractor
{
    private const int MaximumScopeCount = 18;
    private const int MaximumScopeCharacters = 1800;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAgentRuntimeSettingsSource _settingsSource = settingsSource;

    public async Task<ScriptKnowledgeGraphSemanticExtraction?> ExtractAsync(
        ScriptKnowledgeGraphSemanticExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var runtimeSettings = (await _settingsSource.LoadAsync(cancellationToken).ConfigureAwait(false)).Normalize();
        if (!runtimeSettings.IsConfigured() ||
            string.Equals(runtimeSettings.ProviderId, AgentProviderIds.LlamaSharp, StringComparison.Ordinal))
        {
            return null;
        }

        var chatClient = ScriptChatClientFactory.Create(runtimeSettings);
        var response = await chatClient
            .GetResponseAsync(CreatePrompt(request), options: null, cancellationToken)
            .ConfigureAwait(false);
        return ParseResponse(response.Text);
    }

    private static string CreatePrompt(ScriptKnowledgeGraphSemanticExtractionRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Extract a writer-facing knowledge graph from this TPS/Markdown script.");
        builder.AppendLine("Return only JSON. Do not include markdown fences.");
        builder.AppendLine("Use meaningful graph concepts, not TPS syntax tokens or line numbers.");
        builder.AppendLine("Choose concise node kind names that fit the script content; do not force a predefined taxonomy.");
        builder.AppendLine("Each node must have kind, label, detail, scopeLabel, sourceQuote, confidence.");
        builder.AppendLine("Each link must have sourceLabel, targetLabel, label.");
        builder.AppendLine("JSON shape: {\"nodes\":[{\"kind\":\"writer concept\",\"label\":\"customer proof\",\"detail\":\"why it matters\",\"scopeLabel\":\"Opening\",\"sourceQuote\":\"exact short quote\",\"confidence\":0.82}],\"links\":[{\"sourceLabel\":\"customer proof\",\"targetLabel\":\"launch risk\",\"label\":\"affects\"}]}");
        builder.AppendLine(CultureInvariantLine("Title", request.Title));
        builder.AppendLine(CultureInvariantLine("DocumentId", request.DocumentId));
        builder.AppendLine("Scopes:");

        foreach (var scope in request.Scopes.Take(MaximumScopeCount))
        {
            builder.Append("- ");
            builder.AppendLine(scope.Label);
            builder.AppendLine(TrimForPrompt(scope.Content, MaximumScopeCharacters));
        }

        return builder.ToString();
    }

    private static string CultureInvariantLine(string name, string? value) =>
        $"{name}: {(string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim())}";

    private static string TrimForPrompt(string value, int maximumLength)
    {
        var normalized = string.Join(' ', value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= maximumLength
            ? normalized
            : string.Concat(normalized.AsSpan(0, maximumLength - 3).Trim(), "...");
    }

    private static ScriptKnowledgeGraphSemanticExtraction? ParseResponse(string? responseText)
    {
        var json = ExtractJsonObject(responseText);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var dto = JsonSerializer.Deserialize<ExtractionDto>(json, JsonOptions);
        if (dto is null)
        {
            return null;
        }

        return new ScriptKnowledgeGraphSemanticExtraction(
            dto.Nodes
                .Where(static node => !string.IsNullOrWhiteSpace(node.Kind) && !string.IsNullOrWhiteSpace(node.Label))
                .Select(static node => new ScriptKnowledgeGraphSemanticNode(
                    node.Kind.Trim(),
                    node.Label.Trim(),
                    node.Detail,
                    node.ScopeLabel,
                    node.SourceQuote,
                    node.Confidence))
                .ToArray(),
            dto.Links
                .Where(static link => !string.IsNullOrWhiteSpace(link.SourceLabel) &&
                                      !string.IsNullOrWhiteSpace(link.TargetLabel) &&
                                      !string.IsNullOrWhiteSpace(link.Label))
                .Select(static link => new ScriptKnowledgeGraphSemanticLink(
                    link.SourceLabel.Trim(),
                    link.TargetLabel.Trim(),
                    link.Label.Trim()))
                .ToArray());
    }

    private static string? ExtractJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var start = text.IndexOf('{', StringComparison.Ordinal);
        var end = text.LastIndexOf('}');
        return start < 0 || end <= start ? null : text[start..(end + 1)];
    }

    private sealed class ExtractionDto
    {
        public List<NodeDto> Nodes { get; set; } = [];

        public List<LinkDto> Links { get; set; } = [];
    }

    private sealed class NodeDto
    {
        public string Kind { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public string? ScopeLabel { get; set; }

        public string? SourceQuote { get; set; }

        public double? Confidence { get; set; }
    }

    private sealed class LinkDto
    {
        public string SourceLabel { get; set; } = string.Empty;

        public string TargetLabel { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
    }
}
