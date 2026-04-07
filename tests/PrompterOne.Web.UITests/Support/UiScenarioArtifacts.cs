using Microsoft.Playwright;

namespace PrompterOne.Web.UITests;

internal static class UiScenarioArtifacts
{
    private const string InvalidPathCharactersPattern = @"[^\w\-]+";
    private const string DefaultStepName = "step";
    private static readonly char[] TrimCharacters = ['-', '_', ' '];

    public static void ResetScenario(string scenarioName)
    {
        var directory = GetScenarioDirectory(scenarioName);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static async Task CapturePageAsync(IPage page, string scenarioName, string stepName)
    {
        var path = BuildArtifactPath(scenarioName, stepName);
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Artifact directory path is unavailable.");
        Directory.CreateDirectory(directory);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            Animations = ScreenshotAnimations.Disabled,
            FullPage = false
        });
    }

    public static async Task CaptureLocatorAsync(ILocator locator, string scenarioName, string stepName)
    {
        var path = BuildArtifactPath(scenarioName, stepName);
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Artifact directory path is unavailable.");
        Directory.CreateDirectory(directory);

        for (var attempt = 1; attempt <= BrowserTestConstants.ScenarioArtifacts.CaptureRetryCount; attempt++)
        {
            try
            {
                await locator.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = BrowserTestConstants.ScenarioArtifacts.CaptureVisibleTimeoutMs
                });
                await locator.ScreenshotAsync(new LocatorScreenshotOptions
                {
                    Path = path,
                    Animations = ScreenshotAnimations.Disabled
                });
                return;
            }
            catch (PlaywrightException exception) when (
                attempt < BrowserTestConstants.ScenarioArtifacts.CaptureRetryCount &&
                IsTransientLocatorCaptureFailure(exception))
            {
                await Task.Delay(BrowserTestConstants.ScenarioArtifacts.CaptureRetryDelayMs);
            }
        }
    }

    private static string BuildArtifactPath(string scenarioName, string stepName) =>
        Path.Combine(
            GetScenarioDirectory(scenarioName),
            string.Concat(
                SanitizePathSegment(stepName),
                BrowserTestConstants.ScenarioArtifacts.ImageExtension));

    private static string GetScenarioDirectory(string scenarioName) =>
        Path.Combine(
            GetArtifactRoot(),
            SanitizePathSegment(scenarioName));

    private static string GetArtifactRoot() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            BrowserTestConstants.ScenarioArtifacts.RepositoryRootRelativePath,
            BrowserTestConstants.ScenarioArtifacts.OutputDirectoryName,
            BrowserTestConstants.ScenarioArtifacts.PlaywrightDirectoryName));

    private static string SanitizePathSegment(string value)
    {
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            value,
            InvalidPathCharactersPattern,
            BrowserTestConstants.ScenarioArtifacts.Separator);
        var trimmed = normalized.Trim(TrimCharacters);
        return string.IsNullOrWhiteSpace(trimmed) ? DefaultStepName : trimmed;
    }

    private static bool IsTransientLocatorCaptureFailure(PlaywrightException exception)
    {
        return exception.Message.Contains(
                   BrowserTestConstants.ScenarioArtifacts.DetachedElementMessageFragment,
                   StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains(
                   BrowserTestConstants.ScenarioArtifacts.UnstableElementMessageFragment,
                   StringComparison.OrdinalIgnoreCase);
    }
}
