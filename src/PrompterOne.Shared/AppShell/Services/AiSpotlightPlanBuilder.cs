using PrompterOne.Core.AI.Models;

namespace PrompterOne.Shared.Services;

internal static class AiSpotlightPlanBuilder
{
    public static IReadOnlyList<AiSpotlightPlanItem> BuildPlan(ScriptArticleContext context)
    {
        var editorDetail = context.Editor?.SelectedRange is { } range
            ? $"Selected range {range.Start}-{range.End}"
            : context.Editor is not null
                ? "Active editor document"
                : "Current route";

        return
        [
            new("Read context", editorDetail),
            new("Inspect graph", FormatGraphDetail(context.Graph)),
            new("Prepare action", "Use range-based edits and ask before applying changes")
        ];
    }

    public static IReadOnlyList<AiSpotlightLogEntry> BuildRunningLog(ScriptArticleContext context) =>
    [
        new("Context loaded", context.Screen ?? "Route", true),
        new("Graph checked", FormatGraphDetail(context.Graph), true),
        new(
            "Waiting point",
            context.Editor?.SelectedRange is null
                ? "Ready for the next instruction"
                : "Approval required before changing selected text")
    ];

    public static IReadOnlyList<AiSpotlightLogEntry> BuildApprovalLog(AiSpotlightApprovalRequest request) =>
    [
        new("Context loaded", $"Selected range {request.Range.Start}-{request.Range.End}", true),
        new("Prepared range edit", request.Reason, true),
        new("Waiting point", "Review the current and proposed text before applying this edit")
    ];

    public static IReadOnlyList<AiSpotlightLogEntry> AddLog(
        IReadOnlyList<AiSpotlightLogEntry> existing,
        AiSpotlightLogEntry entry) =>
        existing.Concat([entry]).ToArray();

    private static string FormatGraphDetail(ScriptKnowledgeGraphContext? graph) =>
        graph is null || graph.IsEmpty
            ? "No script graph has been built yet"
            : $"{graph.NodeCount} nodes, {graph.EdgeCount} links";
}
