using Microsoft.Playwright;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorToolbarCoverageTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    public static IEnumerable<EditorMenuScenario> MenuScenarios => EditorToolbarCoverageScenarios.MenuScenarios;

    public static IEnumerable<EditorMenuScenario> FloatingMenuScenarios => EditorToolbarCoverageScenarios.FloatingMenuScenarios;

    public static IEnumerable<EditorAiScenario> AiScenarios => EditorToolbarCoverageScenarios.AiScenarios;

    public static IEnumerable<EditorCommandScenario> ToolbarCommandScenarios => EditorToolbarCoverageScenarios.ToolbarCommandScenarios;

    public static IEnumerable<EditorCommandScenario> FloatingCommandScenarios => EditorToolbarCoverageScenarios.FloatingCommandScenarios;

    [Test]
    [MethodDataSource(nameof(MenuScenarios))]
    public async Task EditorToolbar_MenuTrigger_ExposesExpectedBehavior(EditorMenuScenario scenario)
    {
        await UiPageScenarioDriver.RunWithIsolatedPageRetryAsync(
            _fixture,
            async page =>
            {
                await OpenEditorAsync(page);
                await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
                await Expect(page.GetByTestId(scenario.PanelTestId))
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
            },
            $"Toolbar menu coverage failed for scenario '{scenario.TriggerTestId}'.");
    }

    [Test]
    [MethodDataSource(nameof(FloatingMenuScenarios))]
    public async Task EditorToolbar_FloatingMenuTrigger_ExposesExpectedBehavior(EditorMenuScenario scenario)
    {
        await UiPageScenarioDriver.RunWithIsolatedPageRetryAsync(
            _fixture,
            async page =>
            {
                await OpenEditorAsync(page);
                await SetSourceTextAndSelectPhraseAsync(page, BrowserTestSource.AlphaSource, BrowserTestSource.AlphaToken);
                await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar))
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
                await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
                await Expect(page.GetByTestId(scenario.PanelTestId))
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
            },
            $"Floating toolbar menu coverage failed for scenario '{scenario.TriggerTestId}'.");
    }

    [Test]
    [MethodDataSource(nameof(AiScenarios))]
    public async Task EditorToolbar_AiAction_MutatesSource(EditorAiScenario scenario)
    {
        await UiPageScenarioDriver.RunWithIsolatedPageRetryAsync(
            _fixture,
            async page =>
            {
                await AiProviderTestSeeder.SeedConfiguredOpenAiAsync(page);
                await OpenEditorAsync(page);

                if (scenario.RequiresSelection)
                {
                    await SetSourceTextAndSelectPhraseAsync(page, scenario.SourceText, BrowserTestConstants.Editor.TransformativeMoment);
                    await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);
                }

                var beforeValue = await EditorMonacoDriver.SourceInput(page).InputValueAsync();
                var actionButton = page.GetByTestId(scenario.TestId);
                await Expect(actionButton)
                    .ToBeEnabledAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
                await actionButton.ClickAsync();
                var afterValue = await EditorMonacoDriver.SourceInput(page).InputValueAsync();

                await Assert.That(afterValue).IsNotEqualTo(beforeValue);
                await Assert.That(afterValue).Contains(scenario.ExpectedFragment);
            },
            $"AI toolbar coverage failed for scenario '{scenario.TestId}'.");
    }

    [Test]
    [MethodDataSource(nameof(ToolbarCommandScenarios))]
    public async Task EditorToolbar_CommandButton_MutatesSource(EditorCommandScenario scenario)
    {
        await UiPageScenarioDriver.RunWithIsolatedPageRetryAsync(
            _fixture,
            async page =>
            {
                await OpenEditorAsync(page);
                await PrepareScenarioAsync(page, scenario);
                if (!string.IsNullOrWhiteSpace(scenario.MenuTriggerTestId))
                {
                    await page.GetByTestId(scenario.MenuTriggerTestId).ClickAsync();
                }

                var beforeValue = await EditorMonacoDriver.SourceInput(page).InputValueAsync();
                await page.GetByTestId(scenario.TestId).ClickAsync();
                var afterValue = await EditorMonacoDriver.SourceInput(page).InputValueAsync();

                await AssertCommandMutation(scenario, beforeValue, afterValue);
            },
            $"Toolbar command coverage failed for scenario '{scenario.TestId}'.");
    }

    [Test]
    [MethodDataSource(nameof(FloatingCommandScenarios))]
    public async Task EditorToolbar_FloatingCommandButton_MutatesSource(EditorCommandScenario scenario)
    {
        await UiPageScenarioDriver.RunWithIsolatedPageRetryAsync(
            _fixture,
            async page =>
            {
                await OpenEditorAsync(page);
                await PrepareScenarioAsync(page, scenario);
                await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar))
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
                await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);
                if (!string.IsNullOrWhiteSpace(scenario.MenuTriggerTestId))
                {
                    await page.GetByTestId(scenario.MenuTriggerTestId).ClickAsync();
                }

                var beforeValue = await EditorMonacoDriver.SourceInput(page).InputValueAsync();
                await page.GetByTestId(scenario.TestId).ClickAsync();
                var afterValue = await EditorMonacoDriver.SourceInput(page).InputValueAsync();

                await AssertCommandMutation(scenario, beforeValue, afterValue);
            },
            $"Floating toolbar coverage failed for scenario '{scenario.TestId}'.");
    }

    private static async Task AssertCommandMutation(EditorCommandScenario scenario, string beforeValue, string afterValue)
    {
        await Assert.That(afterValue).IsNotEqualTo(beforeValue);

        switch (scenario.Command.Kind)
        {
            case EditorCommandKind.Wrap:
            {
                var expectedFragment = string.Concat(
                    scenario.Command.PrimaryToken,
                    BrowserTestSource.AlphaToken,
                    scenario.Command.SecondaryToken);
                await Assert.That(afterValue).Contains(expectedFragment);
                break;
            }
            case EditorCommandKind.ClearColor:
                await Assert.That(afterValue).DoesNotContain(BrowserTestSource.ColoredAlphaToken);
                await Assert.That(afterValue).Contains(BrowserTestSource.AlphaToken);
                break;
            default:
            {
                var normalizedPrimaryToken = scenario.Command.PrimaryToken.TrimEnd('\r', '\n');
                await Assert.That(afterValue).Contains(normalizedPrimaryToken);
                break;
            }
        }
    }

    private static async Task OpenEditorAsync(IPage page)
    {
        await EditorRouteDriver.OpenReadyAsync(page, BrowserTestConstants.Routes.EditorDemo, "editor-toolbar-coverage-open");
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

    private static async Task SetSourceTextAndSelectPhraseAsync(IPage page, string text, string targetPhrase)
    {
        await EditorMonacoDriver.SetTextAsync(page, text);
        await EditorMonacoDriver.SetSelectionByTextAsync(page, targetPhrase);
    }

    private static Task SetSourceTextAndSelectColoredAlphaAsync(IPage page) =>
        SetSourceTextAndSelectPhraseAsync(page, BrowserTestSource.ColoredSource, BrowserTestSource.ColoredAlphaSelectionTarget);

    private static async Task SetSourceTextAndSetCaretAtEndAsync(IPage page, string text)
    {
        await EditorMonacoDriver.SetTextAsync(page, text);
        await EditorMonacoDriver.SetCaretAtEndAsync(page);
    }

    private static class BrowserTestSource
    {
        internal const string AlphaToken = "Alpha";
        internal const string ColoredAlphaToken = "[loud]Alpha[/loud]";
        internal const string ColoredAlphaSelectionTarget = "Alpha";
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
