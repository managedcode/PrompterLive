using Microsoft.Playwright;

namespace PrompterOne.Web.UITests;

public sealed partial class StandaloneAppFixture
{
    private static Task ConfigureMediaHarnessAsync(IBrowserContext context)
    {
        return context.AddInitScriptAsync(scriptPath: GetMediaHarnessScriptPath());
    }

    private static string GetMediaHarnessScriptPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../tests/PrompterOne.Web.UITests/Media/synthetic-media-harness.js"));
}