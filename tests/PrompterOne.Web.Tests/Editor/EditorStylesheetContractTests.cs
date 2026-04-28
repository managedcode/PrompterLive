using System.Text.RegularExpressions;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Web.Tests;

public sealed class EditorStylesheetContractTests
{
    private const int ExpectedLineNumberGapPx = 10;
    private const string ExpectedToolbarTooltipRevealDelay = ".44s";
    private const string HighlightAndInputRule = ".ed-source-highlight,\n.ed-source-input";
    private static readonly string ComponentStylesheetPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/Editor/Components/EditorSourcePanel.razor.css"));
    private static readonly string EditorSupportScriptPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/wwwroot/editor/editor-source-panel.js"));
    private static readonly string EditorMonacoScriptPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/wwwroot/editor/editor-monaco.js"));
    private static readonly string MediaScriptPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/wwwroot/media/browser-media.js"));
    private static readonly string ReaderStatesStylesheetPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/wwwroot/design/modules/reader/10-reading-states.css"));
    private static readonly string HostIndexPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Web/wwwroot/index.html"));
    private static readonly string InteropPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/Editor/Services/EditorInterop.cs"));
    private static readonly string SharedStylesheetPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/wwwroot/design/styles.css"));

    [Test]
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

    [Test]
    [Arguments(".ed-main ::deep .ed-src-line-segment")]
    [Arguments(".ed-main ::deep .ed-src-line-block")]
    [Arguments(".ed-main ::deep .mk-pause")]
    [Arguments(".ed-main ::deep .mk-hl")]
    [Arguments(".ed-main ::deep .mk-phonetic")]
    [Arguments(".ed-main ::deep .mk-special")]
    [Arguments(".ed-main ::deep .mk-edit")]
    public void EditorHighlightRules_DoNotChangeTextMetrics(string selector)
    {
        var rule = GetRuleBlock(selector);

        Assert.DoesNotContain("font-size", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("margin", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("padding", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("letter-spacing", rule, StringComparison.Ordinal);
    }

    [Test]
    [Arguments(".ed-main ::deep .po-inline-emphasis", "font-weight")]
    [Arguments(".ed-main ::deep .po-inline-loud", "display")]
    [Arguments(".ed-main ::deep .po-inline-loud", "transform")]
    [Arguments(".ed-main ::deep .po-inline-whisper", "font-style")]
    [Arguments(".ed-main ::deep .po-inline-whisper", "letter-spacing")]
    [Arguments(".ed-main ::deep .po-inline-delivery-building", "font-weight")]
    [Arguments(".ed-main ::deep .po-inline-delivery-building", "transform")]
    [Arguments(".ed-main ::deep .po-inline-speed-fast", "letter-spacing")]
    [Arguments(".ed-main ::deep .po-inline-speed-xslow", "letter-spacing")]
    [Arguments(".ed-main ::deep .po-inline-stress", "transform")]
    public void MonacoCueRules_AvoidMetricChangingDecorations(string selector, string forbiddenDeclaration)
    {
        var rule = GetRuleBlock(selector);

        Assert.DoesNotContain(forbiddenDeclaration, rule, StringComparison.Ordinal);
    }

    [Test]
    [Arguments(".ed-main ::deep .h-mark", "font-size")]
    [Arguments(".ed-main ::deep .h-sep", "margin")]
    public void HeaderTokenRules_AvoidSyntheticSpacing(string selector, string forbiddenDeclaration)
    {
        var rule = GetRuleBlock(selector);

        Assert.DoesNotContain(forbiddenDeclaration, rule, StringComparison.Ordinal);
    }

    [Test]
    public void MonacoDecorationPipeline_UsesViewportRangesWithoutDelayingSmallTextNotifications()
    {
        var editorMonacoScript = File.ReadAllText(EditorMonacoScriptPath);

        Assert.Contains(
            "const visibleRangeDecorationCharacterThreshold = 0;",
            editorMonacoScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "const largeDraftTextNotificationCharacterThreshold = 16000;",
            editorMonacoScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "currentText.length < largeDraftTextNotificationCharacterThreshold",
            editorMonacoScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "model?.getValueLength() ?? 0) >= visibleRangeDecorationCharacterThreshold",
            editorMonacoScript,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "largeDraftDecorationCharacterThreshold",
            editorMonacoScript,
            StringComparison.Ordinal);
    }

    [Test]
    public void MonacoCueDecorations_CarrySharedReaderCueClasses()
    {
        var editorMonacoScript = File.ReadAllText(EditorMonacoScriptPath);

        Assert.Contains("function createInlineCueClassName(localName, tpsName)", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("beforeContentClassName", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("createTagObjectClassName", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("createPauseObjectClassName", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("tps-${tpsName}", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("tps-normal", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("tps-emphasis", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("tps-highlight", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("tps-stress", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("tps-${name}", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("buildContourCueClassName(\"energy\", argument)", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("buildContourCueClassName(\"melody\", argument)", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("tps-pronunciation", editorMonacoScript, StringComparison.Ordinal);
        Assert.Contains("tps-phonetic", editorMonacoScript, StringComparison.Ordinal);
    }

    [Test]
    public void EditorCueStyles_ReuseReaderCueClassContract()
    {
        var componentStylesheet = File.ReadAllText(ComponentStylesheetPath);
        var readerStylesheet = File.ReadAllText(ReaderStatesStylesheetPath);

        foreach (var cueClass in new[]
        {
            "tps-loud",
            "tps-soft",
            "tps-whisper",
            "tps-building",
            "tps-highlight",
            "tps-emphasis",
            "tps-legato",
            "tps-staccato",
            "tps-stress",
            "tps-energy",
            "tps-melody",
            "tps-pronunciation",
            "tps-phonetic"
        })
        {
            Assert.Contains(cueClass, componentStylesheet, StringComparison.Ordinal);
            Assert.Contains(cueClass, readerStylesheet, StringComparison.Ordinal);
        }

        Assert.Contains(".po-inline-articulation-legato.tps-legato::before", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".po-inline-articulation-staccato.tps-staccato::before", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".po-inline-stress.tps-stress::before", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".po-inline-melody.tps-melody::after", componentStylesheet, StringComparison.Ordinal);
    }

    [Test]
    public void StyledEditorTechnicalObjects_RenderObjectChipsInsteadOfRawSyntax()
    {
        var componentStylesheet = File.ReadAllText(ComponentStylesheetPath);

        Assert.Contains(".ed-main--styled-text ::deep .po-object-chip::before", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main--styled-text ::deep .po-object-pause::before", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main--styled-text ::deep .po-object-cut::before", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main--styled-text ::deep .po-object-highlight::before", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main--styled-text ::deep .po-object-chip.po-object-tag-close::before", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main--styled-text ::deep .po-pause-short", componentStylesheet, StringComparison.Ordinal);
        Assert.Contains("font-size:0 !important;", GetRuleBlock(".ed-main--styled-text ::deep .po-pause-short,\n.ed-main--styled-text ::deep .po-pause-long,\n.ed-main--styled-text ::deep .po-pause-timed"), StringComparison.Ordinal);
    }

    [Test]
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
        Assert.Contains("EditorSurfaceInteropMethodNames.Initialize", interopSource, StringComparison.Ordinal);
        Assert.Contains("EditorSurfaceInteropMethodNames.GetSelectionState", interopSource, StringComparison.Ordinal);
        Assert.Contains("EditorSurfaceInteropMethodNames.RenderOverlay", interopSource, StringComparison.Ordinal);
        Assert.Contains("EditorSurfaceInteropMethodNames.SetSelection", interopSource, StringComparison.Ordinal);
        Assert.Contains("EditorSurfaceInteropMethodNames.SyncScroll", interopSource, StringComparison.Ordinal);
        Assert.Contains($"editorSurfaceNamespace = \"{EditorSurfaceInteropMethodNames.Namespace}\"", editorSupportScript, StringComparison.Ordinal);
        Assert.Contains("window[editorSurfaceNamespace]", editorSupportScript, StringComparison.Ordinal);
        Assert.DoesNotContain("editor:", mediaScript, StringComparison.Ordinal);

        Assert.Contains("PrompterOne.Web.styles.css", hostIndex, StringComparison.Ordinal);
        Assert.Contains("_content/PrompterOne.Shared/editor/editor-source-panel.js", hostIndex, StringComparison.Ordinal);
        Assert.Contains("_content/PrompterOne.Shared/media/browser-media.js", hostIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("_content/PrompterOne.Shared/prompterone-shell.js", hostIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("_content/PrompterOne.Shared/prompterone-browser.js", hostIndex, StringComparison.Ordinal);
    }

    [Test]
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

    [Test]
    public void EditorMinimapChrome_StaysContainedInsideEditorSurface()
    {
        var minimapRule = NormalizeCssRule(GetRuleBlock(".ed-main ::deep .monaco-editor .minimap"));
        var sliderRule = NormalizeCssRule(GetRuleBlock(".ed-main ::deep .monaco-editor .minimap-slider"));

        Assert.Contains("border-left:1pxsolidvar(--gold-06);", minimapRule, StringComparison.Ordinal);
        Assert.Contains("border-radius:8px;", sliderRule, StringComparison.Ordinal);
    }

    [Test]
    public void EditorLineNumbers_ReserveVisibleGapBeforeSourceText()
    {
        var editorRule = NormalizeCssRule(GetRuleBlock(".ed-main"));
        var lineNumbersRule = NormalizeCssRule(GetRuleBlock(".ed-main ::deep .monaco-editor .line-numbers"));

        Assert.Contains(
            $"--ed-line-number-gap:{ExpectedLineNumberGapPx}px;",
            editorRule,
            StringComparison.Ordinal);
        Assert.Contains(
            "box-sizing:border-box;",
            lineNumbersRule,
            StringComparison.Ordinal);
        Assert.Contains(
            "padding-right:var(--ed-line-number-gap)!important;",
            lineNumbersRule,
            StringComparison.Ordinal);
    }

    [Test]
    public void EditorToolbarTooltips_WaitLongEnoughToAvoidCompetingWithDropdownIntent()
    {
        var editorRule = NormalizeCssRule(GetRuleBlock(".ed-main"));
        var tooltipRule = NormalizeCssRule(GetRuleBlock(".ed-main ::deep .ed-toolbar-tooltip"));
        var visibleTooltipRule = NormalizeCssRule(GetRuleBlock(".ed-main ::deep .efb-menu-item[data-tip]:focus-visible > .ed-toolbar-tooltip"));

        Assert.Contains(
            $"--ed-toolbar-tooltip-reveal-delay:{ExpectedToolbarTooltipRevealDelay};",
            editorRule,
            StringComparison.Ordinal);
        Assert.Contains(
            "transition:opacity.18sease,visibility0slinearvar(--ed-toolbar-tooltip-reveal-delay);",
            tooltipRule,
            StringComparison.Ordinal);
        Assert.Contains(
            "transition-delay:var(--ed-toolbar-tooltip-reveal-delay),0s;",
            visibleTooltipRule,
            StringComparison.Ordinal);
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
