using Microsoft.Agents.AI;
using PrompterOne.Core.AI.Agents;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Workflows;

public sealed class AssistantScriptWorkflow : ScriptWorkflow
{
    public const string WorkflowId = "assistant";

    private static readonly IReadOnlyList<string> WorkflowAgentIds = [AssistantScriptAgent.AgentId];

    public override string Id => WorkflowId;

    public override string Name => "Script Assistant";

    public override string Description => "Runs one route-aware assistant agent with PrompterOne context and tools.";

    public override ScriptWorkflowKind Kind => ScriptWorkflowKind.Sequential;

    public override IReadOnlyList<string> AgentIds => WorkflowAgentIds;

    protected override AIAgent BuildWorkflowAgent(IReadOnlyList<AIAgent> agents) =>
        agents.Count == 1
            ? agents[0]
            : throw new InvalidOperationException("The assistant workflow requires exactly one agent.");
}
