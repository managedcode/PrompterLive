using System.Text;
using ManagedCode.Tps;
using ManagedCode.Tps.Models;

namespace PrompterOne.Core.AI.Services;

internal sealed class ScriptKnowledgeGraphCompiledDocument
{
    private ScriptKnowledgeGraphCompiledDocument(
        TpsCompilationResult compilation,
        string displayText,
        string displayMarkdown,
        IReadOnlyDictionary<string, string> segmentTextById,
        IReadOnlyDictionary<string, string> blockTextById)
    {
        Compilation = compilation;
        DisplayText = displayText;
        DisplayMarkdown = displayMarkdown;
        SegmentTextById = segmentTextById;
        BlockTextById = blockTextById;
    }

    public TpsCompilationResult Compilation { get; }

    public string DisplayText { get; }

    public string DisplayMarkdown { get; }

    public IReadOnlyDictionary<string, string> SegmentTextById { get; }

    public IReadOnlyDictionary<string, string> BlockTextById { get; }

    public static ScriptKnowledgeGraphCompiledDocument Create(string sourceContent, string? title)
    {
        var compilation = TpsRuntime.Compile(sourceContent);
        var segmentTextById = compilation.Script.Segments.ToDictionary(
            static segment => segment.Id,
            static segment => BuildWordText(segment.Words),
            StringComparer.Ordinal);
        var blockTextById = compilation.Script.Segments
            .SelectMany(static segment => segment.Blocks)
            .ToDictionary(
                static block => block.Id,
                static block => BuildWordText(block.Words),
                StringComparer.Ordinal);
        var displayText = BuildWordText(compilation.Script.Words);
        var displayMarkdown = BuildDisplayMarkdown(compilation.Script, title);

        return new ScriptKnowledgeGraphCompiledDocument(
            compilation,
            displayText,
            displayMarkdown,
            segmentTextById,
            blockTextById);
    }

    public string GetSegmentText(string segmentId) =>
        SegmentTextById.TryGetValue(segmentId, out var text) ? text : string.Empty;

    public string GetBlockText(string blockId) =>
        BlockTextById.TryGetValue(blockId, out var text) ? text : string.Empty;

    private static string BuildDisplayMarkdown(CompiledScript script, string? title)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title))
        {
            builder.Append("# ").AppendLine(title.Trim());
        }

        foreach (var segment in script.Segments)
        {
            builder.Append("## ").AppendLine(segment.Name);
            if (segment.Blocks.Count == 0)
            {
                AppendScopeText(builder, BuildWordText(segment.Words));
                continue;
            }

            foreach (var block in segment.Blocks)
            {
                builder.Append("### ").AppendLine(block.Name);
                AppendScopeText(builder, BuildWordText(block.Words));
            }
        }

        return builder.ToString().Trim();
    }

    private static void AppendScopeText(StringBuilder builder, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        builder.AppendLine(text).AppendLine();
    }

    private static string BuildWordText(IEnumerable<CompiledWord> words) =>
        string.Join(
            ' ',
            words
                .Where(static word => word.Metadata.IsPause == false)
                .Select(static word => word.CleanText)
                .Where(static text => !string.IsNullOrWhiteSpace(text)));
}
