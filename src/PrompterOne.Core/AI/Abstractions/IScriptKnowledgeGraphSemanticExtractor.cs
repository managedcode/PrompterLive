using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Abstractions;

public interface IScriptKnowledgeGraphSemanticExtractor
{
    Task<ScriptKnowledgeGraphSemanticExtraction?> ExtractAsync(
        ScriptKnowledgeGraphSemanticExtractionRequest request,
        CancellationToken cancellationToken = default);
}
