using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class ScreenFlowTests
{
    private readonly StandaloneAppFixture _fixture;

    public ScreenFlowTests(StandaloneAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LibraryScreen_NavigatesIntoEditorAndSettings()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/library");
            await Expect(page.GetByTestId("library-page")).ToBeVisibleAsync();
            await Expect(page.GetByText("RSVP Technology Demo")).ToBeVisibleAsync();
            await Expect(page.Locator(".dcover-meta").First).ToContainTextAsync("RSVP");
            await page.GetByRole(AriaRole.Button, new() { Name = "Date" }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Date" })).ToHaveClassAsync(new Regex("active"));
            var tedTalksFolder = page.Locator(".folder-item").Filter(new() { HasText = "TED Talks" });
            await tedTalksFolder.ClickAsync();
            await Expect(tedTalksFolder).ToHaveClassAsync(new Regex("active"));

            var menuWrap = page.Locator(".dcard-menu-wrap").First;
            await menuWrap.Locator(".dcard-menu-btn").ClickAsync();
            await Expect(menuWrap).ToHaveClassAsync(new Regex("open"));
            await menuWrap.GetByRole(AriaRole.Button, new() { Name = "Duplicate" }).ClickAsync();

            await page.GetByTestId("library-open-settings").ClickAsync();
            await page.WaitForURLAsync("**/settings");
            await Expect(page.GetByTestId("settings-page")).ToBeVisibleAsync();

            await page.GotoAsync("/library");
            await page.GetByRole(AriaRole.Button, new() { Name = "New Script" }).ClickAsync();
            await page.WaitForURLAsync("**/editor");
            await Expect(page.GetByTestId("editor-page")).ToBeVisibleAsync();

            await page.GotoAsync("/library");
            await page.GetByTestId("library-create-script").ClickAsync();
            await page.WaitForURLAsync("**/editor");
            await Expect(page.GetByTestId("editor-page")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorAndLearnScreens_ExposeExpectedInteractiveControls()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/editor?id=rsvp-tech-demo");
            await Expect(page.GetByTestId("editor-page")).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(page.Locator(".ed-content")).ToContainTextAsync("## [Intro|140WPM|Warm");
            await Expect(page.Locator(".ed-content")).ToContainTextAsync("Opening Block");
            await Expect(page.Locator(".ed-content")).ToContainTextAsync("Purpose Block");
            await page.Locator(".tb-dropdown-wrap").Nth(0).HoverAsync();
            await Expect(page.Locator(".tb-dropdown").Nth(0)).ToBeVisibleAsync();
            await page.Locator(".tb-dropdown-wrap").Nth(1).HoverAsync();
            await Expect(page.Locator(".tb-dropdown").Nth(1)).ToBeVisibleAsync();
            await page.GetByTestId("editor-bold").ClickAsync();
            await page.GetByTestId("editor-ai").ClickAsync();
            await page.Locator("[data-nav='blk-2-1']").ClickAsync();
            await Expect(page.Locator("[data-nav='blk-2-1']")).ToHaveClassAsync(new Regex("active"));
            await Expect(page.Locator("[data-nav='seg-2']")).ToHaveClassAsync(new Regex("active"));
            await Expect(page.Locator(".ed-content")).ToContainTextAsync("Benefits Block");

            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Learn" })).ToBeVisibleAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Learn" }).ClickAsync();
            await page.WaitForURLAsync("**/learn");
            await Expect(page.GetByTestId("learn-page")).ToBeVisibleAsync(new() { Timeout = 15000 });

            await page.GotoAsync("/learn?id=rsvp-tech-demo");
            await Expect(page.GetByTestId("learn-page")).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(page.Locator("#app-header-center")).ToContainTextAsync("RSVP Technology Demo", new() { Timeout = 15000 });
            await Expect(page.Locator("#rsvp-next-phrase")).Not.ToHaveTextAsync(string.Empty);
            await page.GetByTestId("learn-speed-up").ClickAsync();
            await Expect(page.Locator("#rsvp-speed")).ToHaveTextAsync("310");
            await page.GetByTitle("Back 1 word").ClickAsync();
            await page.GetByTitle("Forward 1 word").ClickAsync();

            await page.GetByTestId("learn-play-toggle").ClickAsync();
            await Expect(page.GetByTestId("learn-play-toggle")).ToBeVisibleAsync();
            await Expect(page.Locator("#rsvp-next-phrase")).Not.ToHaveTextAsync(string.Empty);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task TeleprompterAndSettingsScreens_RespondToCoreControls()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/teleprompter?id=rsvp-tech-demo");
            await Expect(page.GetByTestId("teleprompter-page")).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(page.Locator(".rd-edge-section")).ToContainTextAsync("Opening Block");
            await Expect(page.Locator("#rd-camera")).ToHaveAttributeAsync("data-camera-autostart", new Regex("true|false"));

            await page.GetByTestId("teleprompter-font-up").ClickAsync();
            await Expect(page.Locator("#rd-font-label")).ToHaveTextAsync("40");

            await page.GetByTestId("teleprompter-camera-toggle").ClickAsync();
            await Expect(page.GetByTestId("teleprompter-camera-toggle")).ToHaveClassAsync(new Regex("active"));

            await page.GetByTestId("teleprompter-width-slider").EvaluateAsync("element => { element.value = '900'; element.dispatchEvent(new Event('input', { bubbles: true })); }");
            await Expect(page.Locator("#rd-width-val")).ToHaveTextAsync("900");

            await page.GetByTestId("teleprompter-play-toggle").ClickAsync();
            await Expect(page.Locator("#tp-play-btn")).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(2500);
            await Expect(page.Locator(".rd-time")).Not.ToHaveTextAsync(new Regex(@"^0:00 /"));
            await Expect(page.Locator("#rd-progress-fill")).Not.ToHaveAttributeAsync("style", new Regex(@"width:\s*0%"));
            await page.GetByTitle("Previous block").ClickAsync();
            await page.GetByTitle("Next block").ClickAsync();
            await page.GetByTitle("Back one word").ClickAsync();
            await page.GetByTitle("Forward one word").ClickAsync();

            await page.GotoAsync("/settings");
            await page.GetByTestId("settings-nav-cloud").ClickAsync();
            await Expect(page.Locator("#set-cloud")).ToBeVisibleAsync();
            await page.GetByTestId("settings-nav-files").ClickAsync();
            await Expect(page.Locator("#set-files")).ToBeVisibleAsync();
            await page.Locator("#set-files .set-toggle").First.ClickAsync();
            await Expect(page.Locator("#set-files .set-toggle").First).Not.ToHaveClassAsync(new Regex("\\bon\\b"));

            await page.GetByTestId("settings-nav-cameras").ClickAsync();
            await Expect(page.Locator("#set-cameras")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-request-media")).ToBeVisibleAsync();
            var readerCameraToggle = page.GetByTestId("settings-reader-camera-toggle");
            var cameraToggleWasOn = ((await readerCameraToggle.GetAttributeAsync("class")) ?? string.Empty).Contains("on", StringComparison.Ordinal);
            await readerCameraToggle.ClickAsync();
            if (cameraToggleWasOn)
            {
                await Expect(readerCameraToggle).Not.ToHaveClassAsync(new Regex(@"\bon\b"));
            }
            else
            {
                await Expect(readerCameraToggle).ToHaveClassAsync(new Regex(@"\bon\b"));
            }

            await page.GetByTestId("settings-nav-mics").ClickAsync();
            await Expect(page.Locator("#set-mics")).ToBeVisibleAsync();

            await page.GetByTestId("settings-nav-streaming").ClickAsync();
            await Expect(page.Locator("#set-streaming")).ToBeVisibleAsync();

            await page.GetByTestId("settings-nav-ai").ClickAsync();
            await Expect(page.Locator("#set-ai")).ToBeVisibleAsync();
            var openAiProvider = page.Locator(".set-ai-provider").Filter(new() { HasText = "GPT-4o, o1" });
            await openAiProvider.ClickAsync();
            await Expect(openAiProvider).ToHaveClassAsync(new Regex("active"));
            await Expect(page.GetByTestId("settings-test-connection")).ToBeVisibleAsync();

            await page.GetByTestId("settings-nav-appearance").ClickAsync();
            await Expect(page.GetByTestId("settings-appearance-panel")).ToBeVisibleAsync();

            await page.GetByTestId("settings-nav-about").ClickAsync();
            await Expect(page.GetByTestId("settings-about-panel")).ToBeVisibleAsync();

            await page.GotoAsync("/teleprompter?id=rsvp-tech-demo");
            await Expect(page.Locator("#rd-camera")).ToHaveAttributeAsync(
                "data-camera-autostart",
                cameraToggleWasOn ? new Regex("false") : new Regex("true"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
