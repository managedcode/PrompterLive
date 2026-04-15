using Microsoft.Agents.AI;
using PrompterOne.Core.AI.Abstractions;
using PrompterOne.Core.AI.Agents;
using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Workflows;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptAgentRuntime(
    IEnumerable<ScriptAgent> agents,
    IEnumerable<ScriptWorkflow> workflows,
    IScriptAgentFactory agentFactory) : IScriptAgentRuntime
{
    private readonly IReadOnlyDictionary<string, ScriptAgent> _agentsById = agents.ToDictionary(
        static agent => agent.Id,
        StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyList<ScriptWorkflow> _workflows = workflows.ToArray();
    private readonly IReadOnlyDictionary<string, ScriptWorkflow> _workflowsById = workflows.ToDictionary(
        static workflow => workflow.Id,
        StringComparer.OrdinalIgnoreCase);
    private readonly IScriptAgentFactory _agentFactory = agentFactory;

    public async Task<AIAgent> CreateWorkflowAgentAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var workflow = GetRequiredWorkflow(workflowId);
        return await workflow.CreateWorkflowAgentAsync(_agentFactory, cancellationToken: cancellationToken);
    }

    public async Task<ScriptAgentRunResult> RunAsync(ScriptAgentRunRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var workflow = GetRequiredWorkflow(request.WorkflowId);
        var agentContext = new ScriptAgentContext(request.ConversationId, request.ArticleContext);
        var originalInput = request.Input?.Trim() ?? string.Empty;
        if (originalInput.Length == 0)
        {
            throw new InvalidOperationException("Script agent workflow input is required.");
        }

        return workflow.Kind switch
        {
            ScriptWorkflowKind.Sequential => await RunSequentialAsync(workflow, agentContext, originalInput, cancellationToken),
            ScriptWorkflowKind.GroupChat => await RunWorkflowAgentAsync(workflow, agentContext, originalInput, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported script workflow kind '{workflow.Kind}'.")
        };
    }

    private static string BuildNextInput(string originalInput, string latestOutput) =>
        $$"""
        Original request:
        {{originalInput}}

        Current draft:
        {{latestOutput}}
        """;

    private static string ExtractOutput(AgentResponse response)
    {
        var messages = response.Messages
            .Select(static message => message.Text?.Trim())
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .Cast<string>()
            .ToArray();

        if (messages.Length == 0)
        {
            throw new InvalidOperationException("The agent completed without returning any text.");
        }

        return string.Join(Environment.NewLine + Environment.NewLine, messages);
    }

    private ScriptAgent GetRequiredAgent(string agentId) =>
        _agentsById.TryGetValue(agentId, out var agent)
            ? agent
            : throw new InvalidOperationException($"Unknown script agent '{agentId}'.");

    private ScriptWorkflow GetRequiredWorkflow(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
        {
            return _workflows.Count == 0
                ? throw new InvalidOperationException("No script workflows are registered.")
                : _workflows[0];
        }

        return _workflowsById.TryGetValue(workflowId, out var workflow)
            ? workflow
            : throw new InvalidOperationException($"Unknown script workflow '{workflowId}'.");
    }

    private async Task<ScriptAgentRunResult> RunSequentialAsync(
        ScriptWorkflow workflow,
        ScriptAgentContext agentContext,
        string originalInput,
        CancellationToken cancellationToken)
    {
        var steps = new List<ScriptAgentStepResult>(workflow.AgentIds.Count);
        var nextInput = originalInput;

        foreach (var agentId in workflow.AgentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var definition = GetRequiredAgent(agentId);
            var agent = await _agentFactory.CreateRequiredAsync(agentId, agentContext, cancellationToken);
            var session = await agent.CreateSessionAsync(cancellationToken);
            var response = await agent.RunAsync(nextInput, session, new AgentRunOptions(), cancellationToken);
            var output = ExtractOutput(response);

            steps.Add(new ScriptAgentStepResult(definition.Id, definition.Name, nextInput, output));
            nextInput = BuildNextInput(originalInput, output);
        }

        return new ScriptAgentRunResult(
            workflow.Id,
            workflow.Name,
            originalInput,
            steps,
            steps.Count == 0 ? string.Empty : steps[^1].Output);
    }

    private async Task<ScriptAgentRunResult> RunWorkflowAgentAsync(
        ScriptWorkflow workflow,
        ScriptAgentContext agentContext,
        string originalInput,
        CancellationToken cancellationToken)
    {
        var workflowAgent = await workflow.CreateWorkflowAgentAsync(_agentFactory, agentContext, cancellationToken);
        var session = await workflowAgent.CreateSessionAsync(cancellationToken);
        var response = await workflowAgent.RunAsync(originalInput, session, new AgentRunOptions(), cancellationToken);
        var output = ExtractOutput(response);

        return new ScriptAgentRunResult(
            workflow.Id,
            workflow.Name,
            originalInput,
            [new ScriptAgentStepResult(workflow.Id, workflow.Name, originalInput, output)],
            output);
    }
}
