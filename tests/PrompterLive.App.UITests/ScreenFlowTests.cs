using System.Text.RegularExpressions;
using PrompterLive.Shared.Contracts;
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
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard).Locator(".dcover-meta"))
                .ToContainTextAsync(BrowserTestConstants.Library.ModeLabel);
            await page.GetByTestId(UiTestIds.Header.LibrarySearch).FillAsync(BrowserTestConstants.Library.SearchQuery);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.QuantumCard)).ToContainTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToBeHiddenAsync();
            await page.GetByTestId(UiTestIds.Header.LibrarySearch).FillAsync(string.Empty);
            await page.GetByTestId(UiTestIds.Library.SortDate).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.SortDate)).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            var tedTalksFolder = page.GetByTestId(BrowserTestConstants.Elements.TedTalksFolder);
            await tedTalksFolder.ClickAsync();
            await Expect(tedTalksFolder).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent))
                .ToHaveTextAsync(BrowserTestConstants.Folders.TedTalksName);

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.LeadershipCard)).ToContainTextAsync(BrowserTestConstants.Scripts.LeadershipTitle);
            await page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.LeadershipId)).ClickAsync();
            await page.GetByTestId(UiTestIds.Library.CardDuplicate(BrowserTestConstants.Scripts.LeadershipId)).ClickAsync();

            await page.GetByTestId(UiTestIds.Library.OpenSettings).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Settings));
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await page.GetByTestId(UiTestIds.Header.LibraryNewScript).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Editor));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await page.GetByTestId(UiTestIds.Library.CreateScript).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Editor));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
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
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.FolderCreateTile).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderCard)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.NewFolderCancel).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync();

            await page.GetByTestId(UiTestIds.Library.FolderCreateStart).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.NewFolderName).FillAsync(BrowserTestConstants.Folders.RoadshowsName);
            await page.GetByTestId(UiTestIds.Library.NewFolderParent).SelectOptionAsync(new[] { BrowserTestConstants.Folders.PresentationsId });
            await page.GetByTestId(UiTestIds.Library.NewFolderSubmit).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();
            await page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)).ClickAsync();
            await page.GetByTestId(UiTestIds.Library.Move(BrowserTestConstants.Scripts.DemoId, BrowserTestConstants.Folders.RoadshowsId)).ClickAsync();
            await page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder).ClickAsync();

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.SecurityIncidentCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await page.ReloadAsync();

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.SecurityIncidentCard)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);
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
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync(BrowserTestConstants.Editor.BodyHeading);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Opening Block");
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Purpose Block");
            await page.GetByTestId(UiTestIds.Editor.FormatTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuFormat)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuColor)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.Bold).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.Ai).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.BlockNavigation(2, 1)).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(2, 1))).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(2))).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync("Benefits Block");

            await Expect(page.GetByTestId(UiTestIds.Header.EditorLearn)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Header.EditorLearn).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnDemo));
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GotoAsync(BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Header.Center))
                .ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle, new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase)).Not.ToHaveTextAsync(string.Empty);
            await page.GetByTestId(UiTestIds.Learn.SpeedUp).ClickAsync();
            await Expect(page.Locator($"#{UiDomIds.Learn.Speed}")).ToHaveTextAsync("310");
            await page.GetByTestId(UiTestIds.Learn.StepBackward).ClickAsync();
            await page.GetByTestId(UiTestIds.Learn.StepForward).ClickAsync();

            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Learn.PlayToggle)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase)).Not.ToHaveTextAsync(string.Empty);
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
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.EdgeSection)).ToContainTextAsync("Opening Block");
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardText(0))).ToContainTextAsync("Good morning everyone");
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardText(0))).Not.ToContainTextAsync("Goodmorningeveryone");
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync("data-camera-autostart", BrowserTestConstants.Regexes.CameraAutoStart);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.CameraOverlay(1)}")).ToHaveCountAsync(0);

            await page.GetByTestId(UiTestIds.Teleprompter.FontUp).ClickAsync();
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.FontLabel}")).ToHaveTextAsync("40");

            await page.GetByTestId(UiTestIds.Teleprompter.CameraToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);

            await page.GetByTestId(UiTestIds.Teleprompter.WidthSlider).EvaluateAsync("element => { element.value = '900'; element.dispatchEvent(new Event('input', { bubbles: true })); }");
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.WidthValue}")).ToHaveTextAsync("900");

            await page.GetByTestId(UiTestIds.Teleprompter.PlayToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.PlayToggle)).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderPlaybackDelayMs);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.Time}")).Not.ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderTimeNotZero);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.ProgressFill}")).Not.ToHaveAttributeAsync("style", BrowserTestConstants.Regexes.NonZeroWidth);
            await page.GetByTestId(UiTestIds.Teleprompter.PreviousBlock).ClickAsync();
            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
            await page.GetByTestId(UiTestIds.Teleprompter.PreviousWord).ClickAsync();
            await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();

            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await page.GetByTestId(UiTestIds.Settings.NavCloud).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.CloudPanel)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.NavFiles).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.FilesPanel)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.FileAutoSave).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.FileAutoSave)).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);

            await page.GetByTestId(UiTestIds.Settings.NavCameras).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.CamerasPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.RequestMedia)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.RequestMedia).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.DefaultCamera)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.CameraResolution)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.CameraMirrorToggle)).ToBeVisibleAsync();
            await Expect(page.Locator($"[data-testid^='{UiTestIds.Settings.CameraDevice(string.Empty)}']").First).ToBeVisibleAsync();
            await Expect(page.Locator($"[data-testid^='{UiTestIds.Settings.SceneCamera(string.Empty)}']").First).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.CameraResolution).SelectOptionAsync(new[] { BrowserTestConstants.Streaming.ResolutionHd720 });
            await Expect(page.GetByTestId(UiTestIds.Settings.CameraResolution)).ToHaveValueAsync(BrowserTestConstants.Streaming.ResolutionHd720);
            var mirrorToggle = page.GetByTestId(UiTestIds.Settings.CameraMirrorToggle);
            var mirrorWasOn = ((await mirrorToggle.GetAttributeAsync("class")) ?? string.Empty).Contains("on", StringComparison.Ordinal);
            await mirrorToggle.ClickAsync();
            if (mirrorWasOn)
            {
                await Expect(mirrorToggle).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            }
            else
            {
                await Expect(mirrorToggle).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            }
            await page.Locator($"[data-testid^='{UiTestIds.Settings.SceneCamera(string.Empty)}']").First
                .Locator($"[data-testid^='{UiTestIds.Settings.SceneMirror(string.Empty)}']").ClickAsync();
            await page.Locator($"[data-testid^='{UiTestIds.Settings.SceneCamera(string.Empty)}']").First
                .Locator($"[data-testid^='{UiTestIds.Settings.SceneFlip(string.Empty)}']").ClickAsync();
            var readerCameraToggle = page.GetByTestId(UiTestIds.Settings.ReaderCameraToggle);
            var cameraToggleWasOn = ((await readerCameraToggle.GetAttributeAsync("class")) ?? string.Empty).Contains("on", StringComparison.Ordinal);
            await readerCameraToggle.ClickAsync();
            if (cameraToggleWasOn)
            {
                await Expect(readerCameraToggle).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            }
            else
            {
                await Expect(readerCameraToggle).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            }

            await page.GetByTestId(UiTestIds.Settings.NavMics).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicsPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.PrimaryMic)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicLevel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.NoiseSuppression)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.EchoCancellation)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.MicLevel).EvaluateAsync("element => { element.value = '82'; element.dispatchEvent(new Event('input', { bubbles: true })); }");
            await Expect(page.GetByTestId(UiTestIds.Settings.MicLevelValue)).ToHaveTextAsync("82%");
            await page.GetByTestId(UiTestIds.Settings.NoiseSuppression).ClickAsync();

            await page.GetByTestId(UiTestIds.Settings.NavStreaming).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.StreamingPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.OutputMode)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.OutputResolution)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.Bitrate)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.OutputMode).SelectOptionAsync(new[] { BrowserTestConstants.Streaming.OutputModeDirectRtmp });
            await page.GetByTestId(UiTestIds.Settings.Bitrate).FillAsync(BrowserTestConstants.Streaming.BitrateKbps);
            await page.GetByTestId(UiTestIds.Settings.RtmpUrl).FillAsync(BrowserTestConstants.Streaming.RtmpUrl);
            await page.GetByTestId(UiTestIds.Settings.StreamKey).FillAsync(BrowserTestConstants.Streaming.StreamKey);
            await Expect(page.GetByTestId(UiTestIds.Settings.OutputMode)).ToHaveValueAsync(BrowserTestConstants.Streaming.OutputModeDirectRtmp);
            await Expect(page.GetByTestId(UiTestIds.Settings.Bitrate)).ToHaveValueAsync(BrowserTestConstants.Streaming.BitrateKbps);
            await Expect(page.GetByTestId(UiTestIds.Settings.RtmpUrl)).ToHaveValueAsync(BrowserTestConstants.Streaming.RtmpUrl);

            await page.GetByTestId(UiTestIds.Settings.NavAi).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.AiPanel)).ToBeVisibleAsync();
            var openAiProvider = page.GetByTestId(UiTestIds.Settings.AiProvider("openai"));
            await openAiProvider.ClickAsync();
            await Expect(openAiProvider).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
            await Expect(page.GetByTestId(UiTestIds.Settings.TestConnection)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Settings.NavAppearance).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.AppearancePanel)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Settings.NavAbout).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.AboutPanel)).ToBeVisibleAsync();

            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync(
                "data-camera-autostart",
                cameraToggleWasOn ? new Regex("false") : new Regex("true"));
            if (!cameraToggleWasOn)
            {
                await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderCameraInitDelayMs);
                var hasVideoTrack = await page.Locator($"#{UiDomIds.Teleprompter.Camera}").EvaluateAsync<bool>(
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
    public async Task Teleprompter_UsesStoredPrimaryCameraAsBackgroundLayer()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
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

            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveCountAsync(1);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync("data-camera-role", "primary");
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync("data-camera-device-id", cameraDeviceId);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.CameraOverlay(1)}")).ToHaveCountAsync(0);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
