using Microsoft.Extensions.AI;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptAgentToolProvider(
    ScriptDocumentEditService documentEditService,
    ScriptKnowledgeGraphService knowledgeGraphService)
{
    public IList<AITool> CreateTools(ScriptAgentContext? context)
    {
        var toolSet = new ScriptAgentToolSet(
            context?.ArticleContext ?? new ScriptArticleContext(),
            documentEditService,
            knowledgeGraphService);

        return
        [
            AIFunctionFactory.Create(
                toolSet.GetActivePrompterContext,
                "get_active_prompter_context",
                "Read the active PrompterOne route, editor selection, document revision, and graph summary."),
            AIFunctionFactory.Create(
                toolSet.ReadScriptRange,
                "read_script_range",
                "Read an exact UTF-16 range from the captured script document without changing it."),
            AIFunctionFactory.Create(
                toolSet.ProposeScriptReplacement,
                "propose_script_replacement",
                "Create a revision-bound replacement plan for an exact script range."),
            AIFunctionFactory.Create(
                toolSet.ApplyApprovedScriptReplacement,
                "apply_approved_script_replacement",
                "Apply an approved replacement to the captured script text and return the updated text."),
            AIFunctionFactory.Create(
                toolSet.BuildScriptGraphSummaryAsync,
                "build_script_graph_summary",
                "Build the script knowledge graph from the captured document and return a compact summary.")
        ];
    }

    private sealed class ScriptAgentToolSet(
        ScriptArticleContext context,
        ScriptDocumentEditService documentEditService,
        ScriptKnowledgeGraphService knowledgeGraphService)
    {
        public ScriptAgentContextSnapshot GetActivePrompterContext()
        {
            var content = GetContent();
            var revision = GetRevision(content);

            return new ScriptAgentContextSnapshot(
                context.Screen,
                context.Route,
                context.Title,
                context.Editor?.DocumentId,
                context.Editor?.DocumentTitle,
                revision,
                context.Editor?.SelectedRange,
                context.Editor?.SelectedLineNumbers ?? [],
                content.Length,
                context.Graph);
        }

        public ScriptAgentRangeReadResult ReadScriptRange(int start, int end)
        {
            var content = GetContent();
            var range = new ScriptDocumentRange(start, end);
            var text = documentEditService.ReadRange(content, range);

            return new ScriptAgentRangeReadResult(
                range,
                ScriptDocumentPosition.FromOffset(content, range.Start),
                ScriptDocumentPosition.FromOffset(content, range.End),
                text);
        }

        public ScriptAgentEditPreviewResult ProposeScriptReplacement(
            int start,
            int end,
            string replacementText,
            string? reason)
        {
            var content = GetContent();
            var plan = CreateReplacementPlan(content, start, end, replacementText);
            return new ScriptAgentEditPreviewResult(reason, plan);
        }

        public ScriptAgentAppliedEditPreviewResult ApplyApprovedScriptReplacement(
            int start,
            int end,
            string replacementText,
            string expectedRevision,
            string? reason)
        {
            var content = GetContent();
            var plan = CreateReplacementPlan(content, start, end, replacementText, expectedRevision);
            var result = documentEditService.Apply(content, plan);
            return new ScriptAgentAppliedEditPreviewResult(reason, result);
        }

        public async Task<ScriptAgentGraphSummaryResult> BuildScriptGraphSummaryAsync(
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
                    .Where(static node => node.Kind is "Section" or "Entity")
                    .Select(static node => node.Label)
                    .Distinct(StringComparer.Ordinal)
                    .Take(8)
                    .ToArray());
        }

        private ScriptDocumentEditPlan CreateReplacementPlan(
            string content,
            int start,
            int end,
            string replacementText,
            string? expectedRevision = null)
        {
            var range = new ScriptDocumentRange(start, end);
            range.ValidateWithin(content.Length);

            var revision = string.IsNullOrWhiteSpace(expectedRevision)
                ? GetRevision(content)
                : new ScriptDocumentRevision(expectedRevision);

            return new ScriptDocumentEditPlan(
                revision,
                [ScriptDocumentEditOperation.Replace(range, replacementText)],
                context.Editor?.DocumentId);
        }

        private string GetContent() =>
            context.Editor?.Content ?? context.Content ?? string.Empty;

        private ScriptDocumentRevision GetRevision(string content) =>
            context.Editor?.Revision ?? ScriptDocumentRevision.Create(content);
    }
}
