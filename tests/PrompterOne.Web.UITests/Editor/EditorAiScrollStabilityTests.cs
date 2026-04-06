using System.Globalization;
using System.Text;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed class EditorAiScrollStabilityTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_AiAction_DoesNotJumpScrollPositionForVisibleSelection()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await AiProviderTestSeeder.SeedConfiguredOpenAiAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.Editor);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var sourceText = BuildAiScrollJumpText();
            await EditorMonacoDriver.SetTextAsync(page, sourceText);
            await EditorMonacoDriver.ClickAsync(page);

            var targetRange = ResolveAiScrollJumpTargetRange(sourceText);
            await EditorMonacoDriver.SetSelectionAsync(
                page,
                targetRange.Start,
                targetRange.End);

            var before = await EditorMonacoDriver.GetStateAsync(page);
            Assert.True(
                before.ScrollTop >= BrowserTestConstants.Editor.AiScrollJumpMinimumStartingScrollTop,
                $"Expected the editor to be scrolled before the AI action, but scrollTop was {before.ScrollTop:0.##}.");

            await Expect(page.GetByTestId(UiTestIds.Editor.Ai)).ToBeEnabledAsync();
            await page.GetByTestId(UiTestIds.Editor.Ai).ClickAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Editor.AiScrollJumpSettleDelayMs);

            var after = await EditorMonacoDriver.GetStateAsync(page);
            var value = await EditorMonacoDriver.SourceInput(page).InputValueAsync();

            Assert.Contains(BrowserTestConstants.Editor.SimplifiedMoment, value, StringComparison.Ordinal);
            Assert.InRange(
                Math.Abs(after.ScrollTop - before.ScrollTop),
                0,
                BrowserTestConstants.Editor.AiScrollJumpMaximumAllowedDeltaPx);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static string BuildAiScrollJumpText()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < BrowserTestConstants.Editor.AiScrollJumpLineCount; index++)
        {
            if (index > 0)
            {
                builder.Append('\n');
            }

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                BrowserTestConstants.Editor.AiScrollJumpLineTemplate,
                index + 1);
        }

        return builder.ToString();
    }

    private static AiScrollJumpRange ResolveAiScrollJumpTargetRange(string sourceText)
    {
        var linePrefix = string.Format(
            CultureInfo.InvariantCulture,
            "Line {0:D3}",
            BrowserTestConstants.Editor.AiScrollJumpTargetLineIndex + 1);
        var lineStart = sourceText.IndexOf(linePrefix, StringComparison.Ordinal);
        Assert.True(lineStart >= 0, $"Unable to locate the target line prefix \"{linePrefix}\".");

        var targetStart = sourceText.IndexOf(
            BrowserTestConstants.Editor.TransformativeMoment,
            lineStart,
            StringComparison.Ordinal);
        Assert.True(targetStart >= 0, "Unable to locate the AI simplify target text in the generated editor draft.");

        return new AiScrollJumpRange(
            targetStart,
            targetStart + BrowserTestConstants.Editor.TransformativeMoment.Length);
    }

    private readonly record struct AiScrollJumpRange(int Start, int End);
}
