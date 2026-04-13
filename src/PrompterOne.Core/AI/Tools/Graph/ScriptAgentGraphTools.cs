using System.ComponentModel;
using ModelContextProtocol.Server;
using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;

namespace PrompterOne.Core.AI.Tools;

[McpServerToolType]
internal sealed class ScriptAgentGraphTools(
    ScriptArticleContext context,
    ScriptKnowledgeGraphService knowledgeGraphService)
{
    [McpServerTool(
        Name = ScriptAgentToolNames.BuildScriptGraphSummary,
        Title = "Build script graph summary",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = false)]
    [Description("Build the script knowledge graph from the captured document and return a compact summary.")]
    public async Task<ScriptAgentGraphSummaryResult> BuildScriptGraphSummaryAsync(
        [Description("A cancellation token for the graph build.")]
        CancellationToken cancellationToken = default)
    {
        var content = GetContent();
        var revision = GetRevision(content);
        var artifact = await knowledgeGraphService.BuildAsync(
            new ScriptKnowledgeGraphBuildRequest(
                context.Editor?.DocumentId,
                context.Editor?.DocumentTitle ?? context.Title,
                content,
                revision),
            cancellationToken);

        return new ScriptAgentGraphSummaryResult(
            artifact.Revision,
            artifact.Nodes.Count,
            artifact.Edges.Count,
            artifact.Nodes
                .Where(static node => node.Kind is "Section" or "TpsSegment" or "TpsBlock" or "Entity" or "Character" or "Theme" or "Idea")
                .Select(static node => node.Label)
                .Distinct(StringComparer.Ordinal)
                .Take(8)
                .ToArray());
    }

    private string GetContent() =>
        context.Editor?.Content ?? context.Content ?? string.Empty;

    private ScriptDocumentRevision GetRevision(string content) =>
        context.Editor?.Revision ?? ScriptDocumentRevision.Create(content);
}
