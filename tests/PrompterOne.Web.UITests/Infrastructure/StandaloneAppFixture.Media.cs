using Microsoft.Playwright;

namespace PrompterOne.Web.UITests;

public sealed partial class StandaloneAppFixture
{
    private static async Task ConfigureMediaHarnessAsync(IBrowserContext context)
    {
        await context.AddInitScriptAsync(BrowserTestConstants.Media.RuntimeContractInitializationScript);
        await context.AddInitScriptAsync(scriptPath: UiTestAssetPaths.GetSyntheticMediaHarnessScriptPath());
    }
}
