using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Providers;

namespace PrompterOne.Core.Tests;

public sealed class ArticleContextProviderTests
{
    [Test]
    public void BuildInstructions_IncludesRouteEditorSelectionAndGraphContext()
    {
        const string selectedText = "Selected paragraph.";
        var context = new ScriptArticleContext(
            Title: "Launch Script",
            Source: "library",
            Route: "/editor?id=launch",
            Screen: "Editor",
            Editor: new ScriptEditorContext(
                DocumentId: "launch",
                DocumentTitle: "Launch Script",
                Content: "Do not include this when a selection exists.",
                Revision: new ScriptDocumentRevision("rev-1"),
                Cursor: new ScriptDocumentPosition(3, 8, 42),
                SelectedRange: new ScriptDocumentRange(30, 49),
                SelectedText: selectedText,
                SelectedLineNumbers: [3]),
            Graph: new ScriptKnowledgeGraphContext(
                new ScriptDocumentRevision("rev-1"),
                NodeCount: 7,
                EdgeCount: 9,
                FocusLabels: ["Alex", "Launch theme"]));

        var instructions = ArticleContextProvider.BuildInstructions(context);

        Assert.Contains("Active screen: Editor", instructions, StringComparison.Ordinal);
        Assert.Contains("Active route: /editor?id=launch", instructions, StringComparison.Ordinal);
        Assert.Contains("Selected lines: 3", instructions, StringComparison.Ordinal);
        Assert.Contains(selectedText, instructions, StringComparison.Ordinal);
        Assert.DoesNotContain("Do not include this", instructions, StringComparison.Ordinal);
        Assert.Contains("Graph nodes: 7", instructions, StringComparison.Ordinal);
        Assert.Contains("Graph focus: Alex, Launch theme", instructions, StringComparison.Ordinal);
    }

    [Test]
    public void IsEmpty_ReturnsFalseWhenOnlyEditorContextExists()
    {
        var context = new ScriptArticleContext(Editor: new ScriptEditorContext(DocumentId: "draft"));

        Assert.False(context.IsEmpty);
    }
}
