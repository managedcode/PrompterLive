using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorAiAvailabilityTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_AiButtonsAreDisabled_WhenNoProviderIsConfigured()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await AiProviderTestSeeder.SeedUnconfiguredAsync(page);
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            await Expect(page.GetByTestId(UiTestIds.Editor.Ai)).ToBeDisabledAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingAi)).ToBeDisabledAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_AiButtonsAreEnabled_WhenAProviderIsConfigured()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await AiProviderTestSeeder.SeedConfiguredOpenAiAsync(page);
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            await Expect(page.GetByTestId(UiTestIds.Editor.Ai)).ToBeEnabledAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingAi)).ToBeEnabledAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
