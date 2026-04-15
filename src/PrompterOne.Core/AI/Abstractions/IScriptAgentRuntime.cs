using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Abstractions;

public interface IScriptAgentRuntime
{
    Task<ScriptAgentRunResult> RunAsync(
        ScriptAgentRunRequest request,
        CancellationToken cancellationToken = default);
}
