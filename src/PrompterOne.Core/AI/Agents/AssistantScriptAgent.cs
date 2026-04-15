namespace PrompterOne.Core.AI.Agents;

public sealed class AssistantScriptAgent : ScriptAgent
{
    public const string AgentId = "assistant";

    private static readonly IReadOnlyList<string> AgentSkillIds = [WriterScriptAgent.AgentId, ReviewerScriptAgent.AgentId];

    public override string Id => AgentId;

    public override string Name => "Script Assistant";

    public override string Description => "Answers route-aware questions and uses PrompterOne tools for script work.";

    public override IReadOnlyList<string> SkillIds => AgentSkillIds;

    protected override string SystemPrompt =>
        """
        You are the PrompterOne AI assistant.
        Answer the user's request using the active route, editor context, selected range, graph summary, and available tools.
        Use MCP-style tools when you need current document text, selected text, graph details, or app action metadata.
        For partial document edits, work through explicit range-based tool contracts. Do not regenerate a whole script unless the user clearly asks for a full rewrite.
        If a change would mutate user text, propose a scoped edit and explain the reason clearly.
        Keep responses concise and directly useful inside the app.
        """;
}
