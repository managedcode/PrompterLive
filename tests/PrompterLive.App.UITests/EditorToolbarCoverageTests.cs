using Microsoft.Playwright;
using PrompterLive.Shared.Components.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class EditorToolbarCoverageTests(StandaloneAppFixture fixture)
{
    private const string AlphaToken = "Alpha";
    private const int FloatingBarSettleDelayMs = 500;
    private const string BasicFixtureSource = """
        ## [Intro|140WPM|warm]
        ### [Opening Block|140WPM]
        Alpha
        """;

    private const string ColoredFixtureSource = """
        ## [Intro|140WPM|warm]
        ### [Opening Block|140WPM]
        [green]Alpha[/green]
        """;

    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorToolbar_AllMenuTriggersAndAiButtonsOpenExpectedPanels()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            foreach (var scenario in EditorToolbarCoverageScenarios.MenuScenarios)
            {
                await OpenEditorAsync(page);
                await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
                await Expect(page.GetByTestId(scenario.PanelTestId))
                    .ToBeVisibleAsync(new() { Timeout = 5_000 });
            }

            foreach (var scenario in EditorToolbarCoverageScenarios.AiScenarios)
            {
                await OpenEditorAsync(page);
                if (scenario.RequiresSelection)
                {
                    await SetSourceTextAndSelectAlphaAsync(page, BasicFixtureSource);
                    await page.WaitForTimeoutAsync(FloatingBarSettleDelayMs);
                }

                await page.GetByTestId(scenario.TestId).ClickAsync();
                await Expect(page.GetByTestId("editor-ai-panel"))
                    .ToBeVisibleAsync(new() { Timeout = 5_000 });
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

                var beforeValue = await page.GetByTestId("editor-source-input").InputValueAsync();
                await page.GetByTestId(scenario.TestId).ClickAsync();
                var afterValue = await page.GetByTestId("editor-source-input").InputValueAsync();

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
                await SetSourceTextAndSelectAlphaAsync(page, BasicFixtureSource);
                await Expect(page.GetByTestId("editor-floating-bar"))
                    .ToBeVisibleAsync(new() { Timeout = 5_000 });
                await page.WaitForTimeoutAsync(FloatingBarSettleDelayMs);

                var beforeValue = await page.GetByTestId("editor-source-input").InputValueAsync();
                await page.GetByTestId(scenario.TestId).ClickAsync();
                var afterValue = await page.GetByTestId("editor-source-input").InputValueAsync();

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
                    AlphaToken,
                    scenario.Command.SecondaryToken);
                Assert.Contains(expectedFragment, afterValue, StringComparison.Ordinal);
                break;
            }
            case EditorCommandKind.ClearColor:
                Assert.DoesNotContain("[green]Alpha[/green]", afterValue, StringComparison.Ordinal);
                Assert.Contains(AlphaToken, afterValue, StringComparison.Ordinal);
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
        await page.GotoAsync("/editor?id=rsvp-tech-demo");
        await Expect(page.GetByTestId("editor-page"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(page.GetByTestId("editor-source-input"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    private static Task PrepareScenarioAsync(IPage page, EditorCommandScenario scenario) =>
        scenario.SelectionMode switch
        {
            EditorScenarioSelectionMode.WrapSelection => SetSourceTextAndSelectAlphaAsync(page, BasicFixtureSource),
            EditorScenarioSelectionMode.ClearColorSelection => SetSourceTextAndSelectColoredAlphaAsync(page),
            _ => SetSourceTextAndSetCaretAtEndAsync(page, BasicFixtureSource)
        };

    private static Task SetSourceTextAndSelectAlphaAsync(IPage page, string text) =>
        page.GetByTestId("editor-source-input").EvaluateAsync(
            """
            (element, value) => {
                element.focus();
                element.value = value;
                element.dispatchEvent(new Event("input", { bubbles: true }));

                const target = "Alpha";
                const start = element.value.indexOf(target);
                element.setSelectionRange(start, start + target.length);
                element.dispatchEvent(new Event("select", { bubbles: true }));
                element.dispatchEvent(new Event("keyup", { bubbles: true }));
            }
            """,
            text);

    private static Task SetSourceTextAndSelectColoredAlphaAsync(IPage page) =>
        page.GetByTestId("editor-source-input").EvaluateAsync(
            """
            (element, value) => {
                element.focus();
                element.value = value;
                element.dispatchEvent(new Event("input", { bubbles: true }));

                const target = "[green]Alpha[/green]";
                const start = element.value.indexOf(target);
                element.setSelectionRange(start, start + target.length);
                element.dispatchEvent(new Event("select", { bubbles: true }));
                element.dispatchEvent(new Event("keyup", { bubbles: true }));
            }
            """,
            ColoredFixtureSource);

    private static Task SetSourceTextAndSetCaretAtEndAsync(IPage page, string text) =>
        page.GetByTestId("editor-source-input").EvaluateAsync(
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
}
