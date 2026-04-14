using Bunit;
using Microsoft.JSInterop;
using PrompterOne.Core.AI.Models;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class ScriptGraphPanelTests : BunitContext
{
    private const string NodeId = "prompterone:idea:test";
    private const string Source = "Graph thinking connects the script.";
    private const string GraphModulePath = "./_content/PrompterOne.Shared/app/script-graph-viewer.js";
    private readonly AppHarness _harness;

    public ScriptGraphPanelTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public async Task ScriptGraphPanel_NodeRequestInvokesSourceRangeCallback()
    {
        var requested = new List<ScriptKnowledgeGraphSourceRange>();
        var range = new ScriptKnowledgeGraphSourceRange(
            NodeId,
            new ScriptDocumentRange(0, Source.Length),
            ScriptDocumentPosition.FromOffset(Source, 0),
            ScriptDocumentPosition.FromOffset(Source, Source.Length));
        var artifact = new ScriptKnowledgeGraphArtifact(
            "test",
            "Graph Test",
            ScriptDocumentRevision.Create(Source),
            [new ScriptKnowledgeGraphNode(NodeId, Source, "Idea", "story")],
            [],
            [range],
            "{}",
            string.Empty);
        var cut = Render<ScriptGraphPanel>(parameters => parameters
            .Add(component => component.Artifact, artifact)
            .Add(component => component.SourceRangeRequested, sourceRange => requested.Add(sourceRange)));

        await cut.Instance.OnGraphNodeRequestedAsync(NodeId);

        Assert.Equal([range], requested);
    }

    [Test]
    public void ScriptGraphPanel_GraphOnlyToggleInvokesModeCallback()
    {
        var modes = new List<bool>();
        var artifact = new ScriptKnowledgeGraphArtifact(
            "test",
            "Graph Test",
            ScriptDocumentRevision.Create(Source),
            [new ScriptKnowledgeGraphNode(NodeId, Source, "Idea", "story")],
            [],
            [],
            "{}",
            string.Empty);
        var cut = Render<ScriptGraphPanel>(parameters => parameters
            .Add(component => component.Artifact, artifact)
            .Add(component => component.IsGraphOnlyChanged, graphOnly => modes.Add(graphOnly)));

        cut.Find($"[data-test='{UiTestIds.Editor.GraphOnlyToggle}']").Click();

        Assert.Equal([true], modes);
    }

    [Test]
    public void ScriptGraphPanel_AnalyzeButtonInvokesAnalyzeCallback()
    {
        var callCount = 0;
        var artifact = new ScriptKnowledgeGraphArtifact(
            "test",
            "Graph Test",
            ScriptDocumentRevision.Create(Source),
            [new ScriptKnowledgeGraphNode(NodeId, Source, "Idea", "story")],
            [],
            [],
            "{}",
            string.Empty);
        var cut = Render<ScriptGraphPanel>(parameters => parameters
            .Add(component => component.Artifact, artifact)
            .Add(component => component.AnalyzeRequested, () => callCount++));

        cut.Find($"[data-test='{UiTestIds.Editor.GraphAnalyze}']").Click();

        Assert.Equal(1, callCount);
    }

    [Test]
    public void ScriptGraphPanel_TokenizerStatusButtonInvokesTokenizerCallback()
    {
        var callCount = 0;
        var artifact = new ScriptKnowledgeGraphArtifact(
            "test",
            "Graph Test",
            ScriptDocumentRevision.Create(Source),
            [new ScriptKnowledgeGraphNode(NodeId, Source, "Idea", "story")],
            [],
            [],
            "{}",
            string.Empty,
            ScriptKnowledgeGraphSemanticStatus.ModelUnavailable);
        var cut = Render<ScriptGraphPanel>(parameters => parameters
            .Add(component => component.Artifact, artifact)
            .Add(component => component.TokenizerAnalysisRequested, () => callCount++));

        Assert.NotNull(cut.Find($"[data-test='{UiTestIds.Editor.GraphSemanticStatus}']"));
        cut.Find($"[data-test='{UiTestIds.Editor.GraphTokenizerAnalyze}']").Click();

        Assert.Equal(1, callCount);
    }

    [Test]
    public void ScriptGraphPanel_RendersAutoLayoutControlAndNodeStyleAttribute()
    {
        var artifact = new ScriptKnowledgeGraphArtifact(
            "test",
            "Graph Test",
            ScriptDocumentRevision.Create(Source),
            [new ScriptKnowledgeGraphNode(NodeId, Source, "Idea", "story")],
            [],
            [],
            "{}",
            string.Empty);
        var cut = Render<ScriptGraphPanel>(parameters => parameters
            .Add(component => component.Artifact, artifact)
            .Add(component => component.NodeStyleMode, ScriptGraphNodeStyleModes.Dots));

        Assert.NotNull(cut.Find($"[data-test='{UiTestIds.Editor.GraphAutoLayout}']"));
        Assert.Equal(
            ScriptGraphNodeStyleModes.Dots,
            cut.Find($"[data-test='{UiTestIds.Editor.GraphCanvas}']").GetAttribute("data-graph-node-style"));
    }

    [Test]
    public void ScriptGraphPanel_DynamicImportFailureShowsFallbackInsteadOfThrowing()
    {
        _harness.JsRuntime.ImportFailures[GraphModulePath] = new JSException(
            "Failed to fetch dynamically imported module: script-graph-viewer.js");
        var artifact = new ScriptKnowledgeGraphArtifact(
            "test",
            "Graph Test",
            ScriptDocumentRevision.Create(Source),
            [new ScriptKnowledgeGraphNode(NodeId, Source, "Idea", "story")],
            [],
            [],
            "{}",
            string.Empty);

        var cut = Render<ScriptGraphPanel>(parameters => parameters
            .Add(component => component.Artifact, artifact));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                "RendererFailed",
                cut.Find($"[data-test='{UiTestIds.Editor.GraphSemanticStatus}']")
                    .GetAttribute("data-status"));
        });
        _harness.JsRuntime.ImportFailures.Remove(GraphModulePath);
    }

    [Test]
    public void EditorMetadataGraphPanel_NodeStyleSelectorInvokesCallback()
    {
        var selected = new List<string>();
        var cut = Render<EditorMetadataGraphPanel>(parameters => parameters
            .Add(component => component.NodeStyleMode, ScriptGraphNodeStyleModes.Compact)
            .Add(component => component.NodeStyleModeChanged, selected.Add));

        cut.Find($"[data-test='{UiTestIds.Editor.GraphRailNodeStyleMode}']")
            .Change(ScriptGraphNodeStyleModes.Cards);

        Assert.Equal([ScriptGraphNodeStyleModes.Cards], selected);
    }
}
