using PrompterOne.Core.AI.Models;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Services;

internal static class AiSpotlightExecutionBuilder
{
    public static IReadOnlyList<AiSpotlightLogEntry> BuildRunningLog(
        ScriptArticleContext context,
        Func<UiTextKey, string> text,
        Func<UiTextKey, object[], string> format) =>
    [
        new(text(UiTextKey.AiSpotlightContextLoaded), context.Screen ?? text(UiTextKey.AiSpotlightContextRoute), true),
        new(text(UiTextKey.AiSpotlightGraphChecked), FormatGraphDetail(context.Graph, text, format), true),
        new(
            text(UiTextKey.AiSpotlightWaitingPoint),
            context.Editor?.SelectedRange is null
                ? text(UiTextKey.AiSpotlightReadyForNextInstruction)
                : text(UiTextKey.AiSpotlightApprovalRequiredBeforeChangingSelection))
    ];

    public static IReadOnlyList<AiSpotlightLogEntry> BuildAgentRunningLog(
        ScriptArticleContext context,
        Func<UiTextKey, string> text,
        Func<UiTextKey, object[], string> format) =>
    [
        new(text(UiTextKey.AiSpotlightContextLoaded), context.Screen ?? text(UiTextKey.AiSpotlightContextRoute), true),
        new(text(UiTextKey.AiSpotlightGraphChecked), FormatGraphDetail(context.Graph, text, format), true),
        new(
            text(UiTextKey.AiSpotlightAgentStarted),
            format(UiTextKey.AiSpotlightAgentRunningDetailFormat, [(context.AvailableTools ?? []).Count]))
    ];

    public static IReadOnlyList<AiSpotlightLogEntry> BuildApprovalLog(
        AiSpotlightApprovalRequest request,
        Func<UiTextKey, string> text,
        Func<UiTextKey, object[], string> format) =>
    [
        new(text(UiTextKey.AiSpotlightContextLoaded), format(UiTextKey.AiSpotlightSelectedRangeFormat, [request.Range.Start, request.Range.End]), true),
        new(text(UiTextKey.AiSpotlightPreparedRangeEdit), request.Reason, true),
        new(text(UiTextKey.AiSpotlightWaitingPoint), text(UiTextKey.AiSpotlightReviewEditBeforeApplying))
    ];

    public static IReadOnlyList<AiSpotlightLogEntry> AddLog(
        IReadOnlyList<AiSpotlightLogEntry> existing,
        AiSpotlightLogEntry entry) =>
        existing.Concat([entry]).ToArray();

    private static string FormatGraphDetail(
        ScriptKnowledgeGraphContext? graph,
        Func<UiTextKey, string> text,
        Func<UiTextKey, object[], string> format) =>
        graph is null || graph.IsEmpty
            ? text(UiTextKey.AiSpotlightNoGraphBuilt)
            : format(UiTextKey.AiSpotlightGraphSummaryFormat, [graph.NodeCount, graph.EdgeCount]);
}
