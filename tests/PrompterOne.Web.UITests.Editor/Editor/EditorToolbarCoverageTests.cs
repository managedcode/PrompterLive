using Microsoft.Playwright;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
[NotInParallel(UiTestParallelization.EditorAuthoringConstraintKey)]
public sealed class EditorToolbarCoverageTests(StandaloneAppFixture fixture)
{
    private const int OpenEditorAttemptCount = 2;
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
        var page = await _fixture.NewPageAsync();
        var browserErrors = BrowserErrorCollector.Attach(page);

        try
        {
            await OpenEditorAsync(page);
            await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
            await Expect(page.GetByTestId(scenario.PanelTestId))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Toolbar menu coverage failed for scenario '{scenario.TriggerTestId}' with browser errors:{Environment.NewLine}{browserErrors.Describe()}{Environment.NewLine}{exception}");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    [MethodDataSource(nameof(FloatingMenuScenarios))]
    public async Task EditorToolbar_FloatingMenuTrigger_ExposesExpectedBehavior(EditorMenuScenario scenario)
    {
        var page = await _fixture.NewPageAsync();
        var browserErrors = BrowserErrorCollector.Attach(page);

        try
        {
            await OpenEditorAsync(page);
            await SetSourceTextAndSelectPhraseAsync(page, BrowserTestSource.AlphaSource, BrowserTestSource.AlphaToken);
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
            await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
            await Expect(page.GetByTestId(scenario.PanelTestId))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Floating toolbar menu coverage failed for scenario '{scenario.TriggerTestId}' with browser errors:{Environment.NewLine}{browserErrors.Describe()}{Environment.NewLine}{exception}");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    [MethodDataSource(nameof(AiScenarios))]
    public async Task EditorToolbar_AiAction_MutatesSource(EditorAiScenario scenario)
    {
        var page = await _fixture.NewPageAsync();
        var browserErrors = BrowserErrorCollector.Attach(page);

        try
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
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"AI toolbar coverage failed for scenario '{scenario.TestId}' with browser errors:{Environment.NewLine}{browserErrors.Describe()}{Environment.NewLine}{exception}");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    [MethodDataSource(nameof(ToolbarCommandScenarios))]
    public async Task EditorToolbar_CommandButton_MutatesSource(EditorCommandScenario scenario)
    {
        var page = await _fixture.NewPageAsync();
        var browserErrors = BrowserErrorCollector.Attach(page);

        try
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
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Toolbar command coverage failed for scenario '{scenario.TestId}' with browser errors:{Environment.NewLine}{browserErrors.Describe()}{Environment.NewLine}{exception}");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    [MethodDataSource(nameof(FloatingCommandScenarios))]
    public async Task EditorToolbar_FloatingCommandButton_MutatesSource(EditorCommandScenario scenario)
    {
        var page = await _fixture.NewPageAsync();

        try
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
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Floating toolbar coverage failed for scenario '{scenario.TestId}'.{Environment.NewLine}{exception}");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
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
        Exception? lastFailure = null;

        for (var attempt = 0; attempt < OpenEditorAttemptCount; attempt++)
        {
            try
            {
                await page.GotoAsync(
                    BrowserTestConstants.Routes.EditorDemo,
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
                await EditorMonacoDriver.WaitUntilReadyAsync(page);
                return;
            }
            catch (Exception exception) when (attempt < OpenEditorAttemptCount - 1)
            {
                lastFailure = exception;
            }
        }

        throw lastFailure ?? new InvalidOperationException("The editor page did not become ready.");
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
