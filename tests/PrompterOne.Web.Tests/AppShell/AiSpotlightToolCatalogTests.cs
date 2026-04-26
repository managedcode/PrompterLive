using PrompterOne.Core.AI.Models;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Tools;

namespace PrompterOne.Web.Tests;

public sealed class AiSpotlightToolCatalogTests
{
    private const int MinimumEditorToolCount = 100;

    private static readonly string ToolsRootPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/Tools"));

    private static readonly string[] DomainCatalogPaths =
    [
        Path.Combine("Agents", "AiSpotlightAgentToolCatalog.cs"),
        Path.Combine("Editor", "AiSpotlightEditorToolCatalog.cs"),
        Path.Combine("Graph", "AiSpotlightGraphToolCatalog.cs"),
        Path.Combine("Hotkeys", "AiSpotlightHotkeyToolCatalog.cs"),
        Path.Combine("Learn", "AiSpotlightLearnToolCatalog.cs"),
        Path.Combine("Library", "AiSpotlightLibraryToolCatalog.cs"),
        Path.Combine("Media", "AiSpotlightMediaToolCatalog.cs"),
        Path.Combine("Navigation", "AiSpotlightNavigationToolCatalog.cs"),
        Path.Combine("Settings", "AiSpotlightSettingsToolCatalog.cs"),
        Path.Combine("Streaming", "AiSpotlightStreamingToolCatalog.cs"),
        Path.Combine("Teleprompter", "AiSpotlightTeleprompterToolCatalog.cs")
    ];

    [Test]
    public void Build_EditorContext_ExposesRoutesSettingsHotkeysAndAgentTools()
    {
        var tools = AiSpotlightToolCatalog.BuildSpotlightSuggestions(CreateEditorContext()).ToArray();

        Assert.True(tools.Length >= MinimumEditorToolCount);
        Assert.Equal(tools.Length, tools.Select(static tool => tool.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.NavLibrary && tool.Route == AppRoutes.Library);
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.NavGoLive && tool.Route == AppRoutes.GoLiveWithId("draft"));
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.NavLearn && tool.Route == AppRoutes.LearnWithId("draft"));
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.SettingsMicrophones && tool.Scope == "settings");
        Assert.Contains(tools, static tool => tool.Name == HotkeyToolName(AppHotkeyIds.Definitions.GlobalOpenAssistant) && tool.HotkeyAction == AppHotkeyAction.GlobalOpenAssistant);
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.GraphInspect && tool.Kind == AiSpotlightSuggestionKind.Graph);
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.EditorSelectionRewrite && !tool.RequiresApproval);
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.EditorRangeDelete && tool.Destructive && tool.RequiresApproval);
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.MediaDevicesList && tool.Scope == "media" && tool.ReadOnly);
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.StreamYouTubeKeyConfigure && tool.Scope == "streaming" && tool.OpenWorld);
        Assert.Contains(tools, static tool => tool.Name == AiSpotlightToolNames.AgentSpawnScript && tool.Scope == "agent");
    }

    [Test]
    public void ToAgentTool_ProjectsMcpSafetyAndApprovalMetadata()
    {
        var agentTools = AiSpotlightToolCatalog.BuildAgentTools(CreateEditorContext())
            .Select(tool => tool.ToAgentTool(Text))
            .ToArray();

        var read = agentTools.Single(static tool => tool.Name == AiSpotlightToolNames.AskContext);
        var rewrite = agentTools.Single(static tool => tool.Name == AiSpotlightToolNames.EditorSelectionRewrite);
        var stream = agentTools.Single(static tool => tool.Name == HotkeyToolName(AppHotkeyIds.Definitions.GoLiveToggleStream));
        var deleteRange = agentTools.Single(static tool => tool.Name == AiSpotlightToolNames.EditorRangeDelete);
        var youtube = agentTools.Single(static tool => tool.Name == AiSpotlightToolNames.StreamYouTubeKeyConfigure);
        var media = agentTools.Single(static tool => tool.Name == AiSpotlightToolNames.MediaDevicesList);

        Assert.True(read.ReadOnly);
        Assert.True(read.Idempotent);
        Assert.False(read.Destructive);
        Assert.False(read.RequiresApproval);
        Assert.False(rewrite.ReadOnly);
        Assert.False(rewrite.Destructive);
        Assert.False(rewrite.RequiresApproval);
        Assert.False(stream.ReadOnly);
        Assert.True(stream.Destructive);
        Assert.True(stream.OpenWorld);
        Assert.True(stream.RequiresApproval);
        Assert.True(deleteRange.Destructive);
        Assert.True(deleteRange.RequiresApproval);
        Assert.True(youtube.OpenWorld);
        Assert.True(youtube.RequiresApproval);
        Assert.True(media.ReadOnly);
        Assert.False(media.OpenWorld);
    }

    [Test]
    public void DomainToolCatalogs_DoNotEmbedRawPromptOrParameterText()
    {
        foreach (var relativePath in DomainCatalogPaths)
        {
            var source = File.ReadAllText(Path.Combine(ToolsRootPath, relativePath));

            Assert.DoesNotContain("\"", source, StringComparison.Ordinal);
        }
    }

    private static ScriptArticleContext CreateEditorContext()
    {
        const string content = "Keep. Rewrite.";
        var start = content.IndexOf("Rewrite", StringComparison.Ordinal);
        return new ScriptArticleContext(
            Title: "Draft",
            Route: AppRoutes.EditorWithId("draft"),
            Screen: AppShellScreen.Editor.ToString(),
            Editor: new ScriptEditorContext(
                DocumentId: "draft",
                DocumentTitle: "Draft",
                Content: content,
                Revision: ScriptDocumentRevision.Create(content),
                Cursor: ScriptDocumentPosition.FromOffset(content, start),
                SelectedRange: new ScriptDocumentRange(start, content.Length),
                SelectedText: "Rewrite.",
                SelectedLineNumbers: [1]));
    }

    private static string Text(UiTextKey key) => key.ToString();

    private static string HotkeyToolName(string id) =>
        AiSpotlightToolNames.Hotkey(id);
}
