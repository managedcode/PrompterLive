using System.Globalization;
using System.Text;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorAiScrollStabilityTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
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
            await EditorMonacoDriver.WaitForSelectionLineVisibleAsync(
                page,
                BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);

            var before = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(before.VisibleRange).IsNotNull();
            await Assert.That(before.Selection.Line >= before.VisibleRange!.StartLineNumber &&
                before.Selection.Line <= before.VisibleRange.EndLineNumber).IsTrue().Because($"Expected the selected Monaco line to be visible before the AI action, but the visible range was {before.VisibleRange.StartLineNumber}-{before.VisibleRange.EndLineNumber} and the selection line was {before.Selection.Line}.");

            await Expect(page.GetByTestId(UiTestIds.Editor.Ai)).ToBeEnabledAsync();
            await page.GetByTestId(UiTestIds.Editor.Ai).ClickAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Editor.AiScrollJumpSettleDelayMs);

            var after = await EditorMonacoDriver.GetStateAsync(page);
            var value = await EditorMonacoDriver.SourceInput(page).InputValueAsync();

            await Assert.That(value).Contains(BrowserTestConstants.Editor.SimplifiedMoment);
            await Assert.That(Math.Abs(after.ScrollTop - before.ScrollTop)).IsBetween(0, BrowserTestConstants.Editor.AiScrollJumpMaximumAllowedDeltaPx);
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
        if (lineStart < 0)
        {
            throw new InvalidOperationException($"Unable to locate the target line prefix \"{linePrefix}\".");
        }

        var targetStart = sourceText.IndexOf(
            BrowserTestConstants.Editor.TransformativeMoment,
            lineStart,
            StringComparison.Ordinal);
        if (targetStart < 0)
        {
            throw new InvalidOperationException("Unable to locate the AI simplify target text in the generated editor draft.");
        }

        return new AiScrollJumpRange(
            targetStart,
            targetStart + BrowserTestConstants.Editor.TransformativeMoment.Length);
    }

    private readonly record struct AiScrollJumpRange(int Start, int End);
}
