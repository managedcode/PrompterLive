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
        string knowledgeMarkdown,
        IReadOnlyDictionary<string, string> segmentTextById,
        IReadOnlyDictionary<string, string> blockTextById)
    {
        Compilation = compilation;
        DisplayText = displayText;
        DisplayMarkdown = displayMarkdown;
        KnowledgeMarkdown = knowledgeMarkdown;
        SegmentTextById = segmentTextById;
        BlockTextById = blockTextById;
    }

    public TpsCompilationResult Compilation { get; }

    public string DisplayText { get; }

    public string DisplayMarkdown { get; }

    public string KnowledgeMarkdown { get; }

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
        var knowledgeMarkdown = BuildKnowledgeMarkdown(compilation.Script, displayMarkdown, title);

        return new ScriptKnowledgeGraphCompiledDocument(
            compilation,
            displayText,
            displayMarkdown,
            knowledgeMarkdown,
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

    private static string BuildKnowledgeMarkdown(CompiledScript script, string displayMarkdown, string? title)
    {
        var frontMatter = BuildKnowledgeFrontMatter(script, title);
        return string.IsNullOrWhiteSpace(frontMatter)
            ? displayMarkdown
            : string.Concat(frontMatter, Environment.NewLine, displayMarkdown);
    }

    private static string BuildKnowledgeFrontMatter(CompiledScript script, string? title)
    {
        var metadata = CollectGraphMetadata(script, title);
        if (metadata.EntityHints.Count == 0 && metadata.Tags.Count == 0 && metadata.Groups.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("---");
        if (!string.IsNullOrWhiteSpace(title))
        {
            builder.Append("title: ").AppendLine(QuoteYaml(title.Trim()));
        }

        if (metadata.Tags.Count > 0)
        {
            builder.AppendLine("tags:");
            foreach (var tag in metadata.Tags)
            {
                builder.Append("  - ").AppendLine(QuoteYaml(tag));
            }
        }

        if (metadata.Groups.Count > 0)
        {
            builder.AppendLine("graph_groups:");
            foreach (var group in metadata.Groups)
            {
                builder.Append("  - ").AppendLine(QuoteYaml(group));
            }
        }

        if (metadata.EntityHints.Count > 0)
        {
            builder.AppendLine("graph_entities:");
            foreach (var hint in metadata.EntityHints)
            {
                builder.Append("  - label: ").AppendLine(QuoteYaml(hint.Label));
                builder.Append("    type: ").AppendLine(QuoteYaml(hint.Type));
            }
        }

        if (metadata.Related.Count > 0)
        {
            builder.AppendLine("graph_related:");
            foreach (var related in metadata.Related)
            {
                builder.Append("  - ").AppendLine(QuoteYaml(related));
            }
        }

        if (metadata.EntityHints.Count > 0)
        {
            builder.AppendLine("entity_hints:");
            foreach (var hint in metadata.EntityHints)
            {
                builder.Append("  - label: ").AppendLine(QuoteYaml(hint.Label));
                builder.Append("    type: ").AppendLine(QuoteYaml(hint.Type));
            }
        }

        builder.AppendLine("---");
        return builder.ToString().TrimEnd();
    }

    private static KnowledgeGraphFrontMatter CollectGraphMetadata(CompiledScript script, string? title)
    {
        var entityHints = new Dictionary<string, KnowledgeEntityHint>(StringComparer.OrdinalIgnoreCase);
        var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var groups = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PrompterOne scripts"
        };
        var related = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        AddValue(tags, "tps");
        AddValue(tags, "script");
        AddValue(groups, title);

        foreach (var segment in script.Segments)
        {
            AddEntityHint(entityHints, segment.Speaker, "schema:Person");
            AddEntityHint(entityHints, segment.Emotion, "schema:DefinedTerm");
            AddEntityHint(entityHints, segment.Archetype, "schema:DefinedTerm");
            AddValue(tags, segment.Emotion);
            AddValue(tags, segment.Archetype);
            AddValue(related, segment.Name);

            foreach (var block in segment.Blocks)
            {
                AddEntityHint(entityHints, block.Speaker, "schema:Person");
                AddEntityHint(entityHints, block.Emotion, "schema:DefinedTerm");
                AddEntityHint(entityHints, block.Archetype, "schema:DefinedTerm");
                AddValue(tags, block.Emotion);
                AddValue(tags, block.Archetype);
                AddValue(related, block.Name);
            }
        }

        return new KnowledgeGraphFrontMatter(
            entityHints.Values
                .OrderBy(static hint => hint.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static hint => hint.Label, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            tags.ToArray(),
            groups.ToArray(),
            related.Take(12).ToArray());
    }

    private static void AddEntityHint(IDictionary<string, KnowledgeEntityHint> hints, string? label, string type)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        var normalized = label.Trim();
        hints.TryAdd($"{type}:{normalized}", new KnowledgeEntityHint(normalized, type));
    }

    private static void AddValue(ISet<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value.Trim());
        }
    }

    private static string QuoteYaml(string value) =>
        "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string BuildWordText(IEnumerable<CompiledWord> words) =>
        string.Join(
            ' ',
            words
                .Where(static word => word.Metadata.IsPause == false)
                .Select(static word => word.CleanText)
                .Where(static text => !string.IsNullOrWhiteSpace(text)));

    private sealed record KnowledgeGraphFrontMatter(
        IReadOnlyList<KnowledgeEntityHint> EntityHints,
        IReadOnlyList<string> Tags,
        IReadOnlyList<string> Groups,
        IReadOnlyList<string> Related);

    private sealed record KnowledgeEntityHint(string Label, string Type);
}
