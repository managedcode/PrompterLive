using PrompterOne.Core.AI.Models;

namespace PrompterOne.Shared.Services;

public enum AiSpotlightMode
{
    Idle,
    Running,
    Approval
}

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
    IReadOnlyList<AiSpotlightLogEntry> Log,
    bool RequiresApproval,
    AiSpotlightApprovalRequest? ApprovalRequest = null,
    string? AgentOutput = null,
    string? ErrorMessage = null)
{
    public static AiSpotlightState Closed { get; } = new(
        IsOpen: false,
        Mode: AiSpotlightMode.Idle,
        Prompt: string.Empty,
        Context: new ScriptArticleContext(),
        Log: [],
        RequiresApproval: false);
}
