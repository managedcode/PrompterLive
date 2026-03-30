using System.Text.RegularExpressions;
using PrompterLive.Shared.Components.Editor;
using PrompterLive.Shared.Services.Editor;

namespace PrompterLive.App.Tests;

public sealed class EditorStylesheetContractTests
{
    private const string HighlightAndInputRule = ".ed-source-highlight,\n.ed-source-input";
    private static readonly string ComponentStylesheetPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterLive.Shared/Editor/Components/EditorSourcePanel.razor.css"));
    private static readonly string EditorSupportScriptPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterLive.Shared/wwwroot/editor/editor-source-panel.js"));
    private static readonly string MediaScriptPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterLive.Shared/wwwroot/media/browser-media.js"));
    private static readonly string HostIndexPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterLive.App/wwwroot/index.html"));
    private static readonly string InteropPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterLive.Shared/Editor/Services/EditorInterop.cs"));
    private static readonly string SharedStylesheetPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterLive.Shared/wwwroot/design/styles.css"));

    [Fact]
    public void EditorSurface_UsesSharedMetricSafeTypographyForHighlightAndInput()
    {
        var normalizedRule = NormalizeCssRule(GetRuleBlock(HighlightAndInputRule));

        Assert.Contains("font-size:16px;", normalizedRule, StringComparison.Ordinal);
        Assert.Contains("line-height:2;", normalizedRule, StringComparison.Ordinal);
        Assert.Contains("letter-spacing:normal;", normalizedRule, StringComparison.Ordinal);
        Assert.Contains("font-variant-ligatures:none;", normalizedRule, StringComparison.Ordinal);
        Assert.Contains("white-space:pre-wrap;", normalizedRule, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap:anywhere;", normalizedRule, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(".ed-main ::deep .ed-src-line-segment")]
    [InlineData(".ed-main ::deep .ed-src-line-block")]
    [InlineData(".ed-main ::deep .mk-pause")]
    [InlineData(".ed-main ::deep .mk-hl")]
    [InlineData(".ed-main ::deep .mk-phonetic")]
    [InlineData(".ed-main ::deep .mk-special")]
    [InlineData(".ed-main ::deep .mk-edit")]
    public void EditorHighlightRules_DoNotChangeTextMetrics(string selector)
    {
        var rule = GetRuleBlock(selector);

        Assert.DoesNotContain("font-size", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("margin", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("padding", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("letter-spacing", rule, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(".ed-main ::deep .h-mark", "font-size")]
    [InlineData(".ed-main ::deep .h-sep", "margin")]
    public void HeaderTokenRules_AvoidSyntheticSpacing(string selector, string forbiddenDeclaration)
    {
        var rule = GetRuleBlock(selector);

        Assert.DoesNotContain(forbiddenDeclaration, rule, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorSupportAssets_AreIsolatedFromGlobalShellAssets()
    {
        var componentStylesheet = File.ReadAllText(ComponentStylesheetPath);
        var globalStylesheet = File.ReadAllText(SharedStylesheetPath);
        var editorSupportScript = File.ReadAllText(EditorSupportScriptPath);
        var mediaScript = File.ReadAllText(MediaScriptPath);
        var hostIndex = File.ReadAllText(HostIndexPath);
        var interopSource = File.ReadAllText(InteropPath);

        Assert.Contains(".tb-btn", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-float-bar", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main ::deep .ed-src-line-segment", componentStylesheet, StringComparison.Ordinal);

        Assert.DoesNotContain(".tb-btn", globalStylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(".ed-float-bar", globalStylesheet, StringComparison.Ordinal);
        Assert.DoesNotContain(".ed-source-highlight", globalStylesheet, StringComparison.Ordinal);

        Assert.Contains(nameof(EditorSurfaceInteropMethodNames), interopSource, StringComparison.Ordinal);
        Assert.Contains("EditorSurfaceInteropMethodNames.GetSelectionState", interopSource, StringComparison.Ordinal);
        Assert.Contains("EditorSurfaceInteropMethodNames.SetSelection", interopSource, StringComparison.Ordinal);
        Assert.Contains("EditorSurfaceInteropMethodNames.SyncScroll", interopSource, StringComparison.Ordinal);
        Assert.Contains($"editorSurfaceNamespace = \"{EditorSurfaceInteropMethodNames.Namespace}\"", editorSupportScript, StringComparison.Ordinal);
        Assert.Contains("window[editorSurfaceNamespace]", editorSupportScript, StringComparison.Ordinal);
        Assert.DoesNotContain("editor:", mediaScript, StringComparison.Ordinal);

        Assert.Contains("PrompterLive.App.styles.css", hostIndex, StringComparison.Ordinal);
        Assert.Contains("_content/PrompterLive.Shared/editor/editor-source-panel.js", hostIndex, StringComparison.Ordinal);
        Assert.Contains("_content/PrompterLive.Shared/media/browser-media.js", hostIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("_content/PrompterLive.Shared/prompterlive-shell.js", hostIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("_content/PrompterLive.Shared/prompterlive-browser.js", hostIndex, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorFloatingToolbar_UsesSharedCssVariableInsteadOfJsMagicNumber()
    {
        var componentStylesheet = File.ReadAllText(ComponentStylesheetPath);
        var normalizedStylesheet = NormalizeCssRule(componentStylesheet);
        var editorSupportScript = File.ReadAllText(EditorSupportScriptPath);

        Assert.Contains(
            $"{EditorSourcePanelStyleVariables.FloatingBarMinimumTop}:44px;",
            normalizedStylesheet,
            StringComparison.Ordinal);
        Assert.Contains(
            EditorSourcePanelStyleVariables.FloatingBarMinimumTop,
            editorSupportScript,
            StringComparison.Ordinal);
        Assert.DoesNotContain("floatingToolbarAnchorMinTopPx", editorSupportScript, StringComparison.Ordinal);
    }

    private static string GetRuleBlock(string selector)
    {
        var stylesheet = File.ReadAllText(ComponentStylesheetPath);
        var selectorPattern = string.Join(
            "\\s*",
            selector.Split('\n', StringSplitOptions.TrimEntries)
                .Select(Regex.Escape));
        var pattern = $"{selectorPattern}\\s*\\{{(?<body>.*?)\\}}";
        var match = Regex.Match(stylesheet, pattern, RegexOptions.Singleline);

        Assert.True(match.Success, $"Could not find CSS rule for selector '{selector}'.");
        return match.Groups["body"].Value;
    }

    private static string NormalizeCssRule(string rule) =>
        string.Concat(rule.Where(character => !char.IsWhiteSpace(character)));
}
