using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class ScreenFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task LibraryScreen_NavigatesIntoEditorAndSettings()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/library");
            await Expect(page.GetByTestId("library-page")).ToBeVisibleAsync();
            await Expect(page.GetByText("Product Launch")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("library-card-rsvp-tech-demo").Locator(".dcover-meta")).ToContainTextAsync("Actor");
            await page.GetByTestId("library-search").FillAsync("Quantum");
            await Expect(page.GetByText("Quantum Computing")).ToBeVisibleAsync();
            await Expect(page.GetByText("Product Launch")).ToBeHiddenAsync();
            await page.GetByTestId("library-search").FillAsync(string.Empty);
            await page.GetByRole(AriaRole.Button, new() { Name = "Date" }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Date" })).ToHaveClassAsync(new Regex("active"));
            var tedTalksFolder = page.Locator(".folder-item").Filter(new() { HasText = "TED Talks" });
            await tedTalksFolder.ClickAsync();
            await Expect(tedTalksFolder).ToHaveClassAsync(new Regex("active"));
            await Expect(page.Locator(".bc-current")).ToHaveTextAsync("TED Talks");

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
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LibraryScreen_CreatesFolderAndMovesScript()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/library");
            await Expect(page.GetByTestId("library-page")).ToBeVisibleAsync();
            await page.GetByTestId("library-folder-create-tile").ClickAsync();
            await Expect(page.GetByTestId("library-new-folder-card")).ToBeVisibleAsync();
            await page.GetByTestId("library-new-folder-name").FillAsync("Roadshows");
            await page.GetByTestId("library-new-folder-parent").SelectOptionAsync(new[] { "presentations" });
            await page.GetByTestId("library-new-folder-submit").ClickAsync();
            await Expect(page.GetByTestId("library-folder-roadshows")).ToBeVisibleAsync();
            await Expect(page.Locator(".bc-current")).ToHaveTextAsync("Roadshows");

            await page.GetByTestId("library-folder-all").ClickAsync();
            await page.GetByTestId("library-card-menu-rsvp-tech-demo").ClickAsync();
            await page.GetByTestId("library-move-rsvp-tech-demo-roadshows").ClickAsync();
            await page.GetByTestId("library-folder-roadshows").ClickAsync();

            await Expect(page.GetByText("Product Launch")).ToBeVisibleAsync();
            await Expect(page.GetByText("Security Incident")).ToBeHiddenAsync();
            await Expect(page.Locator(".bc-current")).ToHaveTextAsync("Roadshows");

            await page.ReloadAsync();

            await Expect(page.GetByTestId("library-folder-roadshows")).ToBeVisibleAsync();
            await Expect(page.GetByText("Product Launch")).ToBeVisibleAsync();
            await Expect(page.GetByText("Security Incident")).ToBeHiddenAsync();
            await Expect(page.Locator(".bc-current")).ToHaveTextAsync("Roadshows");
        }
        finally
        {
            await page.Context.CloseAsync();
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
            await Expect(page.GetByTestId("editor-source-input")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("editor-source-highlight")).ToContainTextAsync("## [Intro|140WPM|warm]");
            await Expect(page.GetByTestId("editor-source-highlight")).ToContainTextAsync("Opening Block");
            await Expect(page.GetByTestId("editor-source-highlight")).ToContainTextAsync("Purpose Block");
            await page.Locator(".tb-dropdown-wrap").Nth(0).HoverAsync();
            await Expect(page.Locator(".tb-dropdown").Nth(0)).ToBeVisibleAsync();
            await page.Locator(".tb-dropdown-wrap").Nth(1).HoverAsync();
            await Expect(page.Locator(".tb-dropdown").Nth(1)).ToBeVisibleAsync();
            await page.GetByTestId("editor-bold").ClickAsync();
            await page.GetByTestId("editor-ai").ClickAsync();
            await page.Locator("[data-nav='blk-2-1']").ClickAsync();
            await Expect(page.Locator("[data-nav='blk-2-1']")).ToHaveClassAsync(new Regex("active"));
            await Expect(page.Locator("[data-nav='seg-2']")).ToHaveClassAsync(new Regex("active"));
            await Expect(page.GetByTestId("editor-source-highlight")).ToContainTextAsync("Benefits Block");

            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Learn" })).ToBeVisibleAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Learn" }).ClickAsync();
            await page.WaitForURLAsync("**/learn");
            await Expect(page.GetByTestId("learn-page")).ToBeVisibleAsync(new() { Timeout = 15000 });

            await page.GotoAsync("/learn?id=rsvp-tech-demo");
            await Expect(page.GetByTestId("learn-page")).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(page.Locator("#app-header-center")).ToContainTextAsync("Product Launch", new() { Timeout = 15000 });
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
            await page.Context.CloseAsync();
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
            await page.GetByTestId("settings-request-media").ClickAsync();
            await Expect(page.GetByTestId("settings-default-camera")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-camera-resolution")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-camera-mirror-toggle")).ToBeVisibleAsync();
            await Expect(page.Locator("[data-testid^='settings-camera-device-']").First).ToBeVisibleAsync();
            await Expect(page.Locator("[data-testid^='settings-scene-camera-']").First).ToBeVisibleAsync();
            await page.GetByTestId("settings-camera-resolution").SelectOptionAsync(new[] { "Hd720" });
            await Expect(page.GetByTestId("settings-camera-resolution")).ToHaveValueAsync("Hd720");
            var mirrorToggle = page.GetByTestId("settings-camera-mirror-toggle");
            var mirrorWasOn = ((await mirrorToggle.GetAttributeAsync("class")) ?? string.Empty).Contains("on", StringComparison.Ordinal);
            await mirrorToggle.ClickAsync();
            if (mirrorWasOn)
            {
                await Expect(mirrorToggle).Not.ToHaveClassAsync(new Regex(@"\bon\b"));
            }
            else
            {
                await Expect(mirrorToggle).ToHaveClassAsync(new Regex(@"\bon\b"));
            }
            await page.Locator("[data-testid^='settings-scene-camera-']").First.GetByRole(AriaRole.Button, new() { Name = "Mirror" }).ClickAsync();
            await page.Locator("[data-testid^='settings-scene-camera-']").First.GetByRole(AriaRole.Button, new() { Name = "Flip Vertical" }).ClickAsync();
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
            await Expect(page.GetByTestId("settings-primary-mic")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-mic-level")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-noise-suppression")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-echo-cancellation")).ToBeVisibleAsync();
            await page.GetByTestId("settings-mic-level").EvaluateAsync("element => { element.value = '82'; element.dispatchEvent(new Event('input', { bubbles: true })); }");
            await Expect(page.GetByTestId("settings-mic-level-value")).ToHaveTextAsync("82%");
            await page.GetByTestId("settings-noise-suppression").ClickAsync();

            await page.GetByTestId("settings-nav-streaming").ClickAsync();
            await Expect(page.Locator("#set-streaming")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-output-mode")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-output-resolution")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("settings-bitrate")).ToBeVisibleAsync();
            await page.GetByTestId("settings-output-mode").SelectOptionAsync(new[] { "DirectRtmp" });
            await page.GetByTestId("settings-bitrate").FillAsync("7200");
            await page.GetByTestId("settings-rtmp-url").FillAsync("rtmp://live.example.com/stream");
            await page.GetByTestId("settings-stream-key").FillAsync("sk-live-key");
            await Expect(page.GetByTestId("settings-output-mode")).ToHaveValueAsync("DirectRtmp");
            await Expect(page.GetByTestId("settings-bitrate")).ToHaveValueAsync("7200");
            await Expect(page.GetByTestId("settings-rtmp-url")).ToHaveValueAsync("rtmp://live.example.com/stream");

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
            if (!cameraToggleWasOn)
            {
                await page.WaitForTimeoutAsync(750);
                var hasVideoTrack = await page.Locator("#rd-camera").EvaluateAsync<bool>(
                    "element => !!element.srcObject && element.srcObject.getVideoTracks().length > 0");
                Assert.True(hasVideoTrack);
            }
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task Teleprompter_RendersStoredMultiCameraSceneLayers()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/library");
            var cameraDeviceId = await page.EvaluateAsync<string>(
                """
                async () => {
                    try {
                        const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
                        stream.getTracks().forEach(track => track.stop());
                    } catch {
                    }

                    const devices = await navigator.mediaDevices.enumerateDevices();
                    return devices.find(device => device.kind === 'videoinput')?.deviceId ?? 'default';
                }
                """);
            await page.EvaluateAsync(
                """
                ({ cameraDeviceId }) => {
                    localStorage.setItem('prompterlive.settings.prompterlive.reader', JSON.stringify({
                        CountdownSeconds: 3,
                        FontScale: 1,
                        TextWidth: 0.72,
                        ScrollSpeed: 1,
                        MirrorText: false,
                        ShowFocusLine: true,
                        ShowProgress: true,
                        ShowCameraScene: true
                    }));

                    localStorage.setItem('prompterlive.settings.prompterlive.scene', JSON.stringify({
                        Cameras: [
                            {
                                SourceId: 'scene-cam-a',
                                DeviceId: cameraDeviceId,
                                Label: 'Front camera',
                                Transform: {
                                    X: 0.82,
                                    Y: 0.82,
                                    Width: 0.28,
                                    Height: 0.28,
                                    Rotation: 0,
                                    MirrorHorizontal: true,
                                    MirrorVertical: false,
                                    Visible: true,
                                    IncludeInOutput: true,
                                    ZIndex: 1,
                                    Opacity: 1
                                }
                            },
                            {
                                SourceId: 'scene-cam-b',
                                DeviceId: cameraDeviceId,
                                Label: 'Side camera',
                                Transform: {
                                    X: 0.18,
                                    Y: 0.18,
                                    Width: 0.22,
                                    Height: 0.22,
                                    Rotation: 0,
                                    MirrorHorizontal: false,
                                    MirrorVertical: false,
                                    Visible: true,
                                    IncludeInOutput: true,
                                    ZIndex: 2,
                                    Opacity: 0.92
                                }
                            }
                        ],
                        PrimaryMicrophoneId: null,
                        PrimaryMicrophoneLabel: null,
                        AudioBus: {
                            Inputs: [],
                            MasterGain: 1,
                            MonitorEnabled: true
                        }
                    }));

                    localStorage.setItem('prompterlive.settings.prompterlive.studio', JSON.stringify({
                        Camera: {
                            DefaultCameraId: cameraDeviceId,
                            Resolution: 0,
                            MirrorCamera: true,
                            AutoStartOnRead: true
                        },
                        Microphone: {
                            DefaultMicrophoneId: null,
                            InputLevelPercent: 65,
                            NoiseSuppression: true,
                            EchoCancellation: true
                        },
                        Streaming: {
                            OutputMode: 0,
                            OutputResolution: 0,
                            BitrateKbps: 6000,
                            ShowTextOverlay: true,
                            IncludeCameraInOutput: true,
                            RtmpUrl: '',
                            StreamKey: ''
                        }
                    }));
                }
                """,
                new { cameraDeviceId });

            await page.GotoAsync("/teleprompter?id=rsvp-tech-demo");
            await Expect(page.GetByTestId("teleprompter-page")).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(page.Locator(".rd-camera")).ToHaveCountAsync(2);
            await Expect(page.GetByTestId("teleprompter-camera-layer-overlay-1")).ToBeVisibleAsync();
            await Expect(page.Locator("#rd-camera")).ToHaveAttributeAsync("data-camera-role", "primary");
            await Expect(page.Locator("#rd-camera-overlay-1")).ToHaveAttributeAsync("data-camera-role", "overlay");
            await Expect(page.Locator("#rd-camera-overlay-1")).ToHaveAttributeAsync("style", new Regex("left:(18|82)%"));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
