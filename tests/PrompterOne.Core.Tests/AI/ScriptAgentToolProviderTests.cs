using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;

namespace PrompterOne.Core.Tests;

public sealed class ScriptAgentToolProviderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    static ScriptAgentToolProviderTests() =>
        JsonOptions.Converters.Add(new JsonStringEnumConverter<ScriptDocumentEditKind>());

    private const string Script = """
    # Launch
    ## [Intro|Speaker:Alex]
    Keep this.
    Rewrite this line.
    Keep that.
    """;

    private readonly ScriptAgentToolProvider _provider = new(
        new ScriptDocumentEditService(),
        new ScriptKnowledgeGraphService());

    [Test]
    public void CreateTools_ExposesPredefinedSafeToolSet()
    {
        var tools = _provider.CreateTools(CreateContext());

        Assert.Contains(tools, static tool => tool.Name == "get_active_prompter_context");
        Assert.Contains(tools, static tool => tool.Name == "read_script_range");
        Assert.Contains(tools, static tool => tool.Name == "propose_script_replacement");
        Assert.Contains(tools, static tool => tool.Name == "apply_approved_script_replacement");
        Assert.Contains(tools, static tool => tool.Name == "build_script_graph_summary");
    }

    [Test]
    public async Task ReadScriptRange_ReturnsExactRangeFromEditorContext()
    {
        var start = Script.IndexOf("Rewrite", StringComparison.Ordinal);
        var end = start + "Rewrite this line.".Length;
        var readRange = GetFunction(CreateContext(), "read_script_range");

        var result = ToResult<ScriptAgentRangeReadResult>(await readRange.InvokeAsync(new AIFunctionArguments(
            new Dictionary<string, object?>
            {
                ["start"] = start,
                ["end"] = end
            })));

        Assert.Equal("Rewrite this line.", result.Text);
        Assert.Equal(new ScriptDocumentRange(start, end), result.Range);
    }

    [Test]
    public async Task ApplyApprovedScriptReplacement_OnlyMutatesApprovedRange()
    {
        var start = Script.IndexOf("Rewrite", StringComparison.Ordinal);
        var end = start + "Rewrite this line.".Length;
        var applyReplacement = GetFunction(CreateContext(), "apply_approved_script_replacement");

        var result = ToResult<ScriptAgentAppliedEditPreviewResult>(
            await applyReplacement.InvokeAsync(new AIFunctionArguments(
                new Dictionary<string, object?>
                {
                    ["start"] = start,
                    ["end"] = end,
                    ["replacementText"] = "Polish this line.",
                    ["expectedRevision"] = ScriptDocumentRevision.Create(Script).Value,
                    ["reason"] = "User selected this line only."
                })));

        Assert.Contains("Keep this.", result.Result.Text, StringComparison.Ordinal);
        Assert.Contains("Polish this line.", result.Result.Text, StringComparison.Ordinal);
        Assert.Contains("Keep that.", result.Result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Rewrite this line.", result.Result.Text, StringComparison.Ordinal);
    }

    [Test]
    public async Task BuildScriptGraphSummary_ReturnsCapturedDocumentGraph()
    {
        var graphSummary = GetFunction(CreateContext(), "build_script_graph_summary");

        var result = ToResult<ScriptAgentGraphSummaryResult>(await graphSummary.InvokeAsync());

        Assert.True(result.NodeCount > 0);
        Assert.True(result.EdgeCount > 0);
        Assert.Contains(result.FocusLabels, static label => label.Contains("Intro", StringComparison.Ordinal));
    }

    private static AIFunction GetFunction(ScriptAgentContext context, string name) =>
        (AIFunction)new ScriptAgentToolProvider(new ScriptDocumentEditService(), new ScriptKnowledgeGraphService())
            .CreateTools(context)
            .Single(tool => tool.Name == name);

    private static ScriptAgentContext CreateContext()
    {
        var start = Script.IndexOf("Rewrite", StringComparison.Ordinal);
        var end = start + "Rewrite this line.".Length;

        return new ScriptAgentContext(
            "conversation",
            new ScriptArticleContext(
                Title: "Launch",
                Route: "/editor?id=launch",
                Screen: "Editor",
                Editor: new ScriptEditorContext(
                    DocumentId: "launch",
                    DocumentTitle: "Launch",
                    Content: Script,
                    Revision: ScriptDocumentRevision.Create(Script),
                    Cursor: ScriptDocumentPosition.FromOffset(Script, start),
                    SelectedRange: new ScriptDocumentRange(start, end),
                    SelectedText: "Rewrite this line.",
                    SelectedLineNumbers: [4])));
    }

    private static T ToResult<T>(object? result)
    {
        if (result is T typed)
        {
            return typed;
        }

        if (result is JsonElement json)
        {
            return json.Deserialize<T>(JsonOptions) ??
                throw new InvalidOperationException($"Function returned an empty {typeof(T).Name} payload.");
        }

        throw new InvalidOperationException($"Function returned unexpected payload type {result?.GetType().Name ?? "null"}.");
    }
}
