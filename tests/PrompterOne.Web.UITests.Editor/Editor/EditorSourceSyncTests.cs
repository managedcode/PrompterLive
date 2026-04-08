using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorSourceSyncTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_DirectSourceHeaderEditsRefreshStructureTree()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.ReplaceTextAsync(
                page,
                currentText => currentText
                    .ReplaceFirstLineMatching("^## \\[[^\\n]+\\]$", BrowserTestConstants.Editor.SegmentRewrite)
                    .ReplaceFirstLineMatching("^### \\[[^\\n]+\\]$", BrowserTestConstants.Editor.BlockRewrite));

            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync("Launch Angle");
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync("Focused");
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync("Signal Block");
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync("205WPM");
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight))
                .ToContainTextAsync(BrowserTestConstants.Editor.SegmentRewrite);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}

internal static class EditorSourceSyncTestStringExtensions
{
    internal static string ReplaceFirstLineMatching(this string value, string pattern, string replacement) =>
        System.Text.RegularExpressions.Regex.Match(
            value,
            pattern,
            System.Text.RegularExpressions.RegexOptions.Multiline,
            TimeSpan.FromSeconds(1)) is var match && match.Success
            ? string.Concat(
                value.AsSpan(0, match.Index),
                replacement,
                value.AsSpan(match.Index + match.Length))
            : value;
}
