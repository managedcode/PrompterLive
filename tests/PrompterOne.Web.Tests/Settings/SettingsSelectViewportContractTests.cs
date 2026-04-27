namespace PrompterOne.Web.Tests;

public sealed class SettingsSelectViewportContractTests
{
    private static readonly string HostIndexPath = ResolvePath("../../../../../src/PrompterOne.Web/wwwroot/index.html");
    private static readonly string SettingsSelectScriptPath = ResolvePath("../../../../../src/PrompterOne.Shared/wwwroot/app/settings-select.js");
    private static readonly string SettingsSelectStylesheetPath = ResolvePath("../../../../../src/PrompterOne.Shared/Settings/Components/SettingsSelect.razor.css");

    [Test]
    public void Host_LoadsSettingsSelectViewportPositioner()
    {
        var hostIndex = File.ReadAllText(HostIndexPath);

        Assert.Contains("_content/PrompterOne.Shared/app/settings-select.js", hostIndex, StringComparison.Ordinal);
    }

    [Test]
    public void SettingsSelect_UsesViewportAwarePositioningContract()
    {
        var script = File.ReadAllText(SettingsSelectScriptPath);
        var stylesheet = File.ReadAllText(SettingsSelectStylesheetPath);

        Assert.Contains("PrompterOneSettingsSelect", script, StringComparison.Ordinal);
        Assert.Contains("getBoundingClientRect", script, StringComparison.Ordinal);
        Assert.Contains("viewport", script, StringComparison.Ordinal);
        Assert.Contains("dataset.placement", script, StringComparison.Ordinal);
        Assert.Contains("ss-panel-positioned", stylesheet, StringComparison.Ordinal);
        Assert.Contains("position: fixed", stylesheet, StringComparison.Ordinal);
    }

    private static string ResolvePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
}
