using Microsoft.Playwright;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class EditorToolbarCoverageTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorToolbar_AllMenuTriggersAndAiButtonsExposeExpectedBehavior()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            foreach (var scenario in EditorToolbarCoverageScenarios.MenuScenarios)
            {
                await OpenEditorAsync(page);
                await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
                await Expect(page.GetByTestId(scenario.PanelTestId))
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
            }

            foreach (var scenario in EditorToolbarCoverageScenarios.AiScenarios)
            {
                await OpenEditorAsync(page);
                if (scenario.RequiresSelection)
                {
                    await SetSourceTextAndSelectPhraseAsync(page, scenario.SourceText, BrowserTestConstants.Editor.TransformativeMoment);
                    await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);
                }

                var beforeValue = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();
                await page.GetByTestId(scenario.TestId).ClickAsync();
                var afterValue = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();

                Assert.NotEqual(beforeValue, afterValue);
                Assert.Contains(scenario.ExpectedFragment, afterValue, StringComparison.Ordinal);
            }
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorToolbar_AllToolbarCommandButtonsMutateSource()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            foreach (var scenario in EditorToolbarCoverageScenarios.ToolbarCommandScenarios)
            {
                await OpenEditorAsync(page);
                await PrepareScenarioAsync(page, scenario);
                if (!string.IsNullOrWhiteSpace(scenario.MenuTriggerTestId))
                {
                    await page.GetByTestId(scenario.MenuTriggerTestId).ClickAsync();
                }

                var beforeValue = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();
                await page.GetByTestId(scenario.TestId).ClickAsync();
                var afterValue = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();

                AssertCommandMutation(scenario, beforeValue, afterValue);
            }
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorToolbar_AllFloatingCommandButtonsMutateSource()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            foreach (var scenario in EditorToolbarCoverageScenarios.FloatingCommandScenarios)
            {
                await OpenEditorAsync(page);
                await SetSourceTextAndSelectAlphaAsync(page, BrowserTestSource.AlphaSource);
                await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar))
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
                await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);

                var beforeValue = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();
                await page.GetByTestId(scenario.TestId).ClickAsync();
                var afterValue = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();

                AssertCommandMutation(scenario, beforeValue, afterValue);
            }
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static void AssertCommandMutation(EditorCommandScenario scenario, string beforeValue, string afterValue)
    {
        Assert.NotEqual(beforeValue, afterValue);

        switch (scenario.Command.Kind)
        {
            case EditorCommandKind.Wrap:
            {
                var expectedFragment = string.Concat(
                    scenario.Command.PrimaryToken,
                    BrowserTestSource.AlphaToken,
                    scenario.Command.SecondaryToken);
                Assert.Contains(expectedFragment, afterValue, StringComparison.Ordinal);
                break;
            }
            case EditorCommandKind.ClearColor:
                Assert.DoesNotContain(BrowserTestSource.ColoredAlphaToken, afterValue, StringComparison.Ordinal);
                Assert.Contains(BrowserTestSource.AlphaToken, afterValue, StringComparison.Ordinal);
                break;
            default:
            {
                var normalizedPrimaryToken = scenario.Command.PrimaryToken.TrimEnd('\r', '\n');
                Assert.Contains(normalizedPrimaryToken, afterValue, StringComparison.Ordinal);
                break;
            }
        }
    }

    private static async Task OpenEditorAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
        await Expect(page.GetByTestId(UiTestIds.Editor.Page))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
        await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
    }

    private static Task PrepareScenarioAsync(IPage page, EditorCommandScenario scenario) =>
        scenario.SelectionMode switch
        {
            EditorScenarioSelectionMode.WrapSelection => SetSourceTextAndSelectAlphaAsync(page, BrowserTestSource.AlphaSource),
            EditorScenarioSelectionMode.ClearColorSelection => SetSourceTextAndSelectColoredAlphaAsync(page),
            _ => SetSourceTextAndSetCaretAtEndAsync(page, BrowserTestSource.AlphaSource)
        };

    private static Task SetSourceTextAndSelectAlphaAsync(IPage page, string text) =>
        SetSourceTextAndSelectPhraseAsync(page, text, BrowserTestSource.AlphaToken);

    private static Task SetSourceTextAndSelectPhraseAsync(IPage page, string text, string targetPhrase) =>
        page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
            """
            (element, args) => {
                element.focus();
                element.value = args.text;
                element.dispatchEvent(new Event("input", { bubbles: true }));

                const target = args.target;
                const start = element.value.indexOf(target);
                element.setSelectionRange(start, start + target.length);
                element.dispatchEvent(new Event("select", { bubbles: true }));
                element.dispatchEvent(new Event("keyup", { bubbles: true }));
            }
            """,
            new { text, target = targetPhrase });

    private static Task SetSourceTextAndSelectColoredAlphaAsync(IPage page) =>
        page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
            """
            (element, value) => {
                element.focus();
                element.value = value;
                element.dispatchEvent(new Event("input", { bubbles: true }));

                const target = "[loud]Alpha[/loud]";
                const start = element.value.indexOf(target);
                element.setSelectionRange(start, start + target.length);
                element.dispatchEvent(new Event("select", { bubbles: true }));
                element.dispatchEvent(new Event("keyup", { bubbles: true }));
            }
            """,
            BrowserTestSource.ColoredSource);

    private static Task SetSourceTextAndSetCaretAtEndAsync(IPage page, string text) =>
        page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
            """
            (element, value) => {
                element.focus();
                element.value = value;
                element.dispatchEvent(new Event("input", { bubbles: true }));

                const position = element.value.length;
                element.setSelectionRange(position, position);
                element.dispatchEvent(new Event("select", { bubbles: true }));
                element.dispatchEvent(new Event("keyup", { bubbles: true }));
            }
            """,
            text);

    private static class BrowserTestSource
    {
        internal const string AlphaToken = "Alpha";
        internal const string ColoredAlphaToken = "[loud]Alpha[/loud]";
        internal const string AlphaSource = """
            ## [Intro|140WPM|warm]
            ### [Opening Block|140WPM]
            Alpha
            """;
        internal const string ColoredSource = """
            ## [Intro|140WPM|warm]
            ### [Opening Block|140WPM]
            [loud]Alpha[/loud]
            """;
    }
}
