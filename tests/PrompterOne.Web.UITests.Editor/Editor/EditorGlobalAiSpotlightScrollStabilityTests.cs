using System.Globalization;
using System.Text;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorGlobalAiSpotlightScrollStabilityTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_GlobalAiSpotlight_DoesNotJumpScrollPositionForVisibleSelection()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            var sourceText = BuildAiScrollJumpText();
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, sourceText);
            await EditorMonacoDriver.ClickAsync(page);

            var targetRange = ResolveAiScrollJumpTargetRange(sourceText);
            await EditorMonacoDriver.SetSelectionAsync(
                page,
                targetRange.Start,
                targetRange.End);
            await EditorMonacoDriver.CenterSelectionLineAsync(page);
            await EditorMonacoDriver.WaitForSelectionScrollAsync(
                page,
                BrowserTestConstants.Editor.AiScrollJumpMinimumScrollTopPx,
                BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);

            var before = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(before.ScrollTop >= BrowserTestConstants.Editor.AiScrollJumpMinimumScrollTopPx).IsTrue().Because($"Expected the selection reveal to scroll Monaco before the AI action, but ScrollTop stayed at {before.ScrollTop}.");

            await Expect(page.GetByTestId(UiTestIds.Header.AiSpotlight)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Header.AiSpotlight).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.AiSpotlight.Overlay)).ToBeVisibleAsync();

            var after = await EditorMonacoDriver.GetStateAsync(page);

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
