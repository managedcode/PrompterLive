using PrompterOne.Core.AI.Models;

namespace PrompterOne.Shared.Services;

public enum AiSpotlightMode
{
    Idle,
    Plan,
    Running,
    Approval
}

public sealed record AiSpotlightPlanItem(string Label, string Detail, bool IsComplete = false);

public sealed record AiSpotlightLogEntry(string Label, string Detail, bool IsComplete = false);

public sealed record AiSpotlightApprovalRequest(
    string Reason,
    ScriptDocumentEditPlan Plan,
    ScriptDocumentRange Range,
    ScriptDocumentPosition Start,
    ScriptDocumentPosition End,
    string CurrentText,
    string ProposedText);

public sealed record AiSpotlightState(
    bool IsOpen,
    AiSpotlightMode Mode,
    string Prompt,
    ScriptArticleContext Context,
    IReadOnlyList<AiSpotlightPlanItem> Plan,
    IReadOnlyList<AiSpotlightLogEntry> Log,
    bool RequiresApproval,
    AiSpotlightApprovalRequest? ApprovalRequest = null,
    string? ErrorMessage = null)
{
    public static AiSpotlightState Closed { get; } = new(
        IsOpen: false,
        Mode: AiSpotlightMode.Idle,
        Prompt: string.Empty,
        Context: new ScriptArticleContext(),
        Plan: [],
        Log: [],
        RequiresApproval: false);
}
