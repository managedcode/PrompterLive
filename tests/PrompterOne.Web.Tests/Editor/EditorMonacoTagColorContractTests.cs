namespace PrompterOne.Web.Tests;

public sealed class EditorMonacoTagColorContractTests
{
    private static readonly string MonacoScriptPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/wwwroot/editor/editor-monaco.js"));
    private static readonly string StylesheetPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/PrompterOne.Shared/Editor/Components/EditorSourcePanel.razor.css"));

    [Test]
    public void MonacoTagDecorations_MapSemanticCueFamiliesToDedicatedTagClasses()
    {
        var monacoScript = File.ReadAllText(MonacoScriptPath);

        Assert.Contains("emotionTagClasses: createSemanticTagClassMap(emotionNames, \"emotion\")", monacoScript, StringComparison.Ordinal);
        Assert.Contains("volumeTagClasses: createSemanticTagClassMap(volumeNames, \"volume\")", monacoScript, StringComparison.Ordinal);
        Assert.Contains("deliveryTagClasses: createSemanticTagClassMap(deliveryNames, \"delivery\")", monacoScript, StringComparison.Ordinal);
        Assert.Contains("speedTagClasses: createSemanticTagClassMap(speedNames.filter(name => name !== \"normal\"), \"speed\")", monacoScript, StringComparison.Ordinal);
        Assert.Contains("highlight: `${cssClassPrefix}-tag-highlight`", monacoScript, StringComparison.Ordinal);
        Assert.Contains("pronunciation: `${cssClassPrefix}-tag-pronunciation`", monacoScript, StringComparison.Ordinal);
        Assert.Contains("return tpsCatalog.emotionTagClasses.get(normalizedName);", monacoScript, StringComparison.Ordinal);
        Assert.Contains("return tpsCatalog.volumeTagClasses.get(normalizedName);", monacoScript, StringComparison.Ordinal);
        Assert.Contains("return tpsCatalog.deliveryTagClasses.get(normalizedName);", monacoScript, StringComparison.Ordinal);
    }

    [Test]
    public void MonacoTagStyles_DefineMutedSemanticTagToneClasses()
    {
        var stylesheet = File.ReadAllText(StylesheetPath);

        Assert.Contains(".ed-main ::deep .po-tag{--ed-tag-color:", stylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main ::deep .po-tag-highlight", stylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main ::deep .po-tag-pronunciation", stylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main ::deep .po-tag-emotion-urgent", stylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main ::deep .po-tag-delivery-building", stylesheet, StringComparison.Ordinal);
        Assert.Contains(".ed-main ::deep .po-tag-volume-soft", stylesheet, StringComparison.Ordinal);
        Assert.Contains("--ed-tag-opacity", stylesheet, StringComparison.Ordinal);
    }
}
