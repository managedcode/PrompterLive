using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphEdges
{
    public static void Add(
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        string sourceId,
        string targetId,
        string label,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        var id = $"{sourceId}|{label}|{targetId}";
        edges.TryAdd(id, new ScriptKnowledgeGraphEdge(id, sourceId, targetId, label, attributes));
    }
}
