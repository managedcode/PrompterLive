using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorGlobalAiSpotlightAvailabilityTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_GlobalAiSpotlightIsAvailable_WhenNoProviderIsConfigured()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await AiProviderTestSeeder.SeedUnconfiguredAsync(page);
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            await Expect(page.GetByTestId(UiTestIds.Header.AiSpotlight)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Header.AiSpotlight).ClickAsync();
            await SubmitSpotlightPromptAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_GlobalAiSpotlightIsAvailable_WhenAProviderIsConfigured()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await AiProviderTestSeeder.SeedConfiguredOpenAiAsync(page);
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            await Expect(page.GetByTestId(UiTestIds.Header.AiSpotlight)).ToBeVisibleAsync();
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.OpenAssistant);
            await SubmitSpotlightPromptAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task SubmitSpotlightPromptAsync(IPage page)
    {
        await Expect(page.GetByTestId(UiTestIds.AiSpotlight.Overlay)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.AiSpotlight.IdleState)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.AiSpotlight.PromptInput).FillAsync("Explain the selected script lines");
        await page.GetByTestId(UiTestIds.AiSpotlight.Submit).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.AiSpotlight.PlanState)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.AiSpotlight.Context)).ToBeVisibleAsync();
    }
}
