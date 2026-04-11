using System.Globalization;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Providers;

public sealed class ArticleContextProvider(ScriptArticleContext? articleContext)
    : AIContextProvider(PassThroughMessages, PassThroughMessages, PassThroughMessages)
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(articleContext is null || articleContext.IsEmpty
            ? new AIContext()
            : new AIContext
            {
                Instructions = BuildInstructions(articleContext)
            });
    }

    public static string BuildInstructions(ScriptArticleContext articleContext)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Script context is available for this run.");

        if (!string.IsNullOrWhiteSpace(articleContext.Screen))
        {
            builder.Append("Active screen: ");
            builder.AppendLine(articleContext.Screen.Trim());
        }

        if (!string.IsNullOrWhiteSpace(articleContext.Route))
        {
            builder.Append("Active route: ");
            builder.AppendLine(articleContext.Route.Trim());
        }

        if (!string.IsNullOrWhiteSpace(articleContext.Title))
        {
            builder.Append("Title: ");
            builder.AppendLine(articleContext.Title.Trim());
        }

        if (!string.IsNullOrWhiteSpace(articleContext.Source))
        {
            builder.Append("Source: ");
            builder.AppendLine(articleContext.Source.Trim());
        }

        if (!string.IsNullOrWhiteSpace(articleContext.Summary))
        {
            builder.AppendLine();
            builder.AppendLine("Summary:");
            builder.AppendLine(articleContext.Summary.Trim());
        }

        if (!string.IsNullOrWhiteSpace(articleContext.Content))
        {
            builder.AppendLine();
            builder.AppendLine("Article content:");
            builder.AppendLine(articleContext.Content.Trim());
        }

        AppendEditorContext(builder, articleContext.Editor);
        AppendGraphContext(builder, articleContext.Graph);

        return builder.ToString().Trim();
    }

    private static void AppendEditorContext(StringBuilder builder, ScriptEditorContext? editorContext)
    {
        if (editorContext is null || editorContext.IsEmpty)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Editor context:");
        AppendTrimmedLine(builder, "Document id", editorContext.DocumentId);
        AppendTrimmedLine(builder, "Document title", editorContext.DocumentTitle);
        AppendTrimmedLine(builder, "Document revision", editorContext.Revision?.Value);
        AppendPosition(builder, "Cursor", editorContext.Cursor);
        AppendRange(builder, "Selected range", editorContext.SelectedRange);
        AppendSelectedLines(builder, editorContext.SelectedLineNumbers);
        AppendSelectedOrFullText(builder, editorContext);
    }

    private static void AppendGraphContext(StringBuilder builder, ScriptKnowledgeGraphContext? graphContext)
    {
        if (graphContext is null || graphContext.IsEmpty)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Script graph context:");
        AppendTrimmedLine(builder, "Graph revision", graphContext.Revision?.Value);
        builder.Append("Graph nodes: ");
        builder.AppendLine(graphContext.NodeCount.ToString(CultureInfo.InvariantCulture));
        builder.Append("Graph edges: ");
        builder.AppendLine(graphContext.EdgeCount.ToString(CultureInfo.InvariantCulture));
        AppendFocusLabels(builder, graphContext.FocusLabels);
    }

    private static void AppendSelectedOrFullText(StringBuilder builder, ScriptEditorContext editorContext)
    {
        if (!string.IsNullOrWhiteSpace(editorContext.SelectedText))
        {
            builder.AppendLine("Selected text:");
            builder.AppendLine(editorContext.SelectedText.Trim());
            return;
        }

        if (!string.IsNullOrWhiteSpace(editorContext.Content))
        {
            builder.AppendLine("Script content:");
            builder.AppendLine(editorContext.Content.Trim());
        }
    }

    private static void AppendPosition(StringBuilder builder, string label, ScriptDocumentPosition? position)
    {
        if (position is null)
        {
            return;
        }

        builder.Append(label);
        builder.Append(": line ");
        builder.Append(position.Value.Line.ToString(CultureInfo.InvariantCulture));
        builder.Append(", column ");
        builder.Append(position.Value.Column.ToString(CultureInfo.InvariantCulture));
        builder.Append(", offset ");
        builder.AppendLine(position.Value.Offset.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendRange(StringBuilder builder, string label, ScriptDocumentRange? range)
    {
        if (range is null)
        {
            return;
        }

        builder.Append(label);
        builder.Append(": [");
        builder.Append(range.Value.Start.ToString(CultureInfo.InvariantCulture));
        builder.Append(", ");
        builder.Append(range.Value.End.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine(")");
    }

    private static void AppendSelectedLines(StringBuilder builder, IReadOnlyList<int>? selectedLineNumbers)
    {
        if (selectedLineNumbers is null || selectedLineNumbers.Count == 0)
        {
            return;
        }

        builder.Append("Selected lines: ");
        builder.AppendLine(string.Join(
            ", ",
            selectedLineNumbers.Select(static line => line.ToString(CultureInfo.InvariantCulture))));
    }

    private static void AppendFocusLabels(StringBuilder builder, IReadOnlyList<string>? focusLabels)
    {
        var labels = focusLabels?
            .Where(static label => !string.IsNullOrWhiteSpace(label))
            .Select(static label => label.Trim())
            .ToArray();
        if (labels is null || labels.Length == 0)
        {
            return;
        }

        builder.Append("Graph focus: ");
        builder.AppendLine(string.Join(", ", labels));
    }

    private static void AppendTrimmedLine(StringBuilder builder, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(label);
        builder.Append(": ");
        builder.AppendLine(value.Trim());
    }

    private static IEnumerable<ChatMessage> PassThroughMessages(IEnumerable<ChatMessage> messages) => messages;
}
