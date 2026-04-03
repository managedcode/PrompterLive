using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class TeleprompterSettingsFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);

    [Fact]
    public Task TeleprompterAndSettingsScreens_RespondToCoreControls() =>
        RunPageAsync(async page =>
        {
            await VerifyTeleprompterControlsAsync(page);
            var readerCameraWasOn = await VerifySettingsControlsAsync(page);
            await VerifyTeleprompterCameraAutostartAsync(page, readerCameraWasOn);
        });

    [Fact]
    public Task Teleprompter_UsesStoredPrimaryCameraAsBackgroundLayer() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            var cameraDeviceId = await ResolveCameraDeviceIdAsync(page);
            await SeedStoredTeleprompterSceneAsync(page, cameraDeviceId);

            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveCountAsync(1);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync("data-camera-role", "primary");
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync("data-camera-device-id", cameraDeviceId);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.CameraOverlay(1)}")).ToHaveCountAsync(0);
        });

    private static async Task VerifyTeleprompterControlsAsync(Microsoft.Playwright.IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.EdgeSection)).ToContainTextAsync(BrowserTestConstants.TeleprompterFlow.OpeningBlock);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardText(0))).ToContainTextAsync(BrowserTestConstants.TeleprompterFlow.OpeningLine);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardText(0))).Not.ToContainTextAsync(BrowserTestConstants.TeleprompterFlow.CollapsedOpeningLine);
        await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync(
            BrowserTestConstants.TeleprompterFlow.CameraAutostartAttribute,
            BrowserTestConstants.Regexes.CameraAutoStart);
        await Expect(page.Locator($"#{UiDomIds.Teleprompter.CameraOverlay(1)}")).ToHaveCountAsync(0);
        await AssertTeleprompterChromeVisibilityAsync(page);

        await page.GetByTestId(UiTestIds.Teleprompter.FontUp).ClickAsync();
        await Expect(page.Locator($"#{UiDomIds.Teleprompter.FontLabel}")).ToHaveTextAsync(BrowserTestConstants.TeleprompterFlow.FontScaleAfterIncrease);

        var cameraToggle = page.GetByTestId(UiTestIds.Teleprompter.CameraToggle);
        var cameraWasActive = await HasActiveClassAsync(cameraToggle);
        await cameraToggle.ClickAsync();

        if (cameraWasActive)
        {
            await Expect(cameraToggle).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
        }
        else
        {
            await Expect(cameraToggle).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
        }

        await page.GetByTestId(UiTestIds.Teleprompter.WidthSlider).EvaluateAsync(BrowserTestConstants.TeleprompterFlow.WidthInputScript);
        await Expect(page.Locator($"#{UiDomIds.Teleprompter.WidthValue}")).ToHaveTextAsync(BrowserTestConstants.TeleprompterFlow.WidthAfterChange);

        var playToggle = page.GetByTestId(UiTestIds.Teleprompter.PlayToggle);
        await playToggle.ClickAsync();
        await Expect(playToggle).ToBeVisibleAsync();
        await Expect(playToggle.Locator(BrowserTestConstants.Teleprompter.PauseToggleIconSelector))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackReadyTimeoutMs });
        await Expect(page.Locator($"#{UiDomIds.Teleprompter.Time}"))
            .Not.ToHaveTextAsync(
                BrowserTestConstants.Regexes.ReaderTimeNotZero,
                new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackAdvanceTimeoutMs });
        await Expect(page.Locator($"#{UiDomIds.Teleprompter.ProgressFill}"))
            .Not.ToHaveAttributeAsync(
                "style",
                BrowserTestConstants.Regexes.NonZeroWidth,
                new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackAdvanceTimeoutMs });
        await page.GetByTestId(UiTestIds.Teleprompter.PreviousBlock).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.PreviousWord).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
    }

    private static async Task<bool> VerifySettingsControlsAsync(Microsoft.Playwright.IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.Settings);
        await page.GetByTestId(UiTestIds.Settings.NavCloud).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CloudPanel)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.NavFiles).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.FilesPanel)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.FileAutoSave).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.FileAutoSave)).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        await page.GetByTestId(UiTestIds.Settings.NavRecording).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.RecordingPanel)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.RecordingAutoRecord)).ToBeVisibleAsync();

        var readerCameraWasOn = await VerifyCameraSettingsAsync(page);
        await VerifyMicrophoneSettingsAsync(page);
        await VerifyAiAndInfoSettingsAsync(page);

        return readerCameraWasOn;
    }

    private static async Task<bool> VerifyCameraSettingsAsync(Microsoft.Playwright.IPage page)
    {
        await page.GetByTestId(UiTestIds.Settings.NavCameras).ClickAsync();
        var camerasPanel = page.GetByTestId(UiTestIds.Settings.CamerasPanel);
        await Expect(camerasPanel).ToBeVisibleAsync();
        var requestMediaButton = camerasPanel.GetByTestId(UiTestIds.Settings.RequestMedia);
        await requestMediaButton.ScrollIntoViewIfNeededAsync();
        await Expect(requestMediaButton).ToBeVisibleAsync();
        await requestMediaButton.ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraResolution)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraMirrorToggle)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraPreviewCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraPreviewVideo)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraPreviewLabel)).ToHaveTextAsync(BrowserTestConstants.Media.PrimaryCameraLabel);
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.PrimaryCameraId))).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.SecondaryCameraId))).ToBeVisibleAsync();
        await SettingsSelectDriver.SelectByValueAsync(
            page,
            UiTestIds.Settings.CameraResolution,
            BrowserTestConstants.Streaming.ResolutionHd720);

        await ToggleSettingsButtonAsync(page.GetByTestId(UiTestIds.Settings.CameraMirrorToggle));
        await page.GetByTestId(UiTestIds.Settings.CameraDeviceAction(BrowserTestConstants.Media.SecondaryCameraId)).ClickAsync();
        var secondaryPrimaryAction = page.GetByTestId(UiTestIds.Settings.CameraPrimaryAction(BrowserTestConstants.Media.SecondaryCameraId));
        await secondaryPrimaryAction.ClickAsync();
        await Expect(secondaryPrimaryAction).ToBeDisabledAsync();

        return await ToggleReaderCameraAsync(page);
    }

    private static async Task VerifyMicrophoneSettingsAsync(Microsoft.Playwright.IPage page)
    {
        await page.GetByTestId(UiTestIds.Settings.NavMics).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicsPanel)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicDevice(BrowserTestConstants.Media.PrimaryMicrophoneId))).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicLevel)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicPreviewCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicPreviewMeter)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.NoiseSuppression)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.EchoCancellation)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.MicLevel).EvaluateAsync(BrowserTestConstants.SettingsFlow.MicLevelInputScript);
        await Expect(page.GetByTestId(UiTestIds.Settings.MicLevel)).ToHaveValueAsync(BrowserTestConstants.SettingsFlow.MicLevelValue);
        await page.GetByTestId(UiTestIds.Settings.NoiseSuppression).ClickAsync();
    }

    private static async Task VerifyAiAndInfoSettingsAsync(Microsoft.Playwright.IPage page)
    {
        var aiNavItem = page.GetByTestId(UiTestIds.Settings.NavAi);
        var appearanceNavItem = page.GetByTestId(UiTestIds.Settings.NavAppearance);
        var aiBeforeSelection = await GetRequiredBoundingBoxAsync(aiNavItem);
        var appearanceBeforeSelection = await GetRequiredBoundingBoxAsync(appearanceNavItem);

        await aiNavItem.ClickAsync();
        await Expect(aiNavItem).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
        await Expect(page.GetByTestId(UiTestIds.Settings.AiPanel)).ToBeVisibleAsync();

        var aiAfterSelection = await GetRequiredBoundingBoxAsync(aiNavItem);
        var appearanceAfterSelection = await GetRequiredBoundingBoxAsync(appearanceNavItem);
        AssertDimensionStable(aiBeforeSelection.Width, aiAfterSelection.Width);
        AssertDimensionStable(aiBeforeSelection.Height, aiAfterSelection.Height);
        AssertDimensionStable(appearanceBeforeSelection.Y, appearanceAfterSelection.Y);

        var openAiProvider = page.GetByTestId(UiTestIds.Settings.AiProvider(BrowserTestConstants.SettingsFlow.OpenAiProviderId));
        await openAiProvider.ClickAsync();
        await Expect(openAiProvider).ToHaveClassAsync(new Regex(@"\bopen\b"));
        await Expect(page.GetByTestId(UiTestIds.Settings.AiProviderSave(BrowserTestConstants.SettingsFlow.OpenAiProviderId))).ToBeVisibleAsync();

        await page.GetByTestId(UiTestIds.Settings.NavAppearance).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AppearancePanel)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.ThemeOption(BrowserTestConstants.SettingsFlow.LightTheme)).ClickAsync();
        await Expect(page.Locator("html")).ToHaveAttributeAsync(
            BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
            BrowserTestConstants.SettingsFlow.LightTheme);

        await page.GetByTestId(UiTestIds.Settings.NavAbout).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutPanel)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutAppCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutCompanyCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutVersion)).ToHaveTextAsync(BrowserTestConstants.Regexes.SettingsAboutVersion);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutCompanyWebsite)).ToHaveAttributeAsync("href", AboutLinks.ManagedCodeWebsiteUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutCompanyGitHub)).ToHaveAttributeAsync("href", AboutLinks.ManagedCodeGitHubUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutProductGitHub)).ToHaveAttributeAsync("href", AboutLinks.ProductRepositoryUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutTpsGitHub)).ToHaveAttributeAsync("href", AboutLinks.TpsRepositoryUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutRepositoryLink)).ToHaveAttributeAsync("href", AboutLinks.ProductRepositoryUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutReleasesLink)).ToHaveAttributeAsync("href", AboutLinks.ProductReleasesUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutIssuesLink)).ToHaveAttributeAsync("href", AboutLinks.ProductIssuesUrl);
    }

    private static async Task<bool> ToggleReaderCameraAsync(Microsoft.Playwright.IPage page)
    {
        var readerCameraToggle = page.GetByTestId(UiTestIds.Settings.ReaderCameraToggle);
        await readerCameraToggle.ScrollIntoViewIfNeededAsync();
        await Expect(readerCameraToggle).ToBeVisibleAsync();
        var cameraToggleWasOn = await HasOnClassAsync(readerCameraToggle);
        await readerCameraToggle.ClickAsync();

        if (cameraToggleWasOn)
        {
            await Expect(readerCameraToggle).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        }
        else
        {
            await Expect(readerCameraToggle).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        }

        return cameraToggleWasOn;
    }

    private static async Task<bool> HasActiveClassAsync(Microsoft.Playwright.ILocator locator) =>
        (await locator.GetAttributeAsync(BrowserTestConstants.Html.ClassAttribute) ?? string.Empty)
        .Split(BrowserTestConstants.Html.ClassSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Contains(BrowserTestConstants.Css.ActiveClass, StringComparer.Ordinal);

    private static async Task AssertTeleprompterChromeVisibilityAsync(Microsoft.Playwright.IPage page)
    {
        var controlsOpacity = await GetOpacityAsync(page.GetByTestId(UiTestIds.Teleprompter.Controls));
        var slidersOpacity = await GetOpacityAsync(page.GetByTestId(UiTestIds.Teleprompter.Sliders));
        var edgeInfoOpacity = await GetOpacityAsync(page.GetByTestId(UiTestIds.Teleprompter.EdgeInfo));

        Assert.True(controlsOpacity >= BrowserTestConstants.TeleprompterFlow.ControlsMinimumOpacity);
        Assert.True(slidersOpacity >= BrowserTestConstants.TeleprompterFlow.SlidersMinimumOpacity);
        Assert.True(edgeInfoOpacity >= BrowserTestConstants.TeleprompterFlow.EdgeInfoMinimumOpacity);
    }

    private static void AssertDimensionStable(double before, double after) =>
        Assert.InRange(
            Math.Abs(before - after),
            0,
            BrowserTestConstants.SettingsFlow.NavItemLayoutTolerancePx);

    private static Task<double> GetOpacityAsync(Microsoft.Playwright.ILocator locator) =>
        locator.EvaluateAsync<double>(
            "element => Number.parseFloat(window.getComputedStyle(element).opacity)");

    private static async Task<LayoutBounds> GetRequiredBoundingBoxAsync(Microsoft.Playwright.ILocator locator) =>
        await locator.EvaluateAsync<LayoutBounds>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return {
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                };
            }
            """);

    private static async Task VerifyTeleprompterCameraAutostartAsync(Microsoft.Playwright.IPage page, bool readerCameraWasOn)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
        await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync(
            "data-camera-autostart",
            readerCameraWasOn ? new Regex("false") : new Regex("true"));

        if (readerCameraWasOn)
        {
            return;
        }

        await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderCameraInitDelayMs);
        var hasVideoTrack = await page.Locator($"#{UiDomIds.Teleprompter.Camera}").EvaluateAsync<bool>(
            "element => !!element.srcObject && element.srcObject.getVideoTracks().length > 0");
        Assert.True(hasVideoTrack);
    }

    private static async Task<string> ResolveCameraDeviceIdAsync(Microsoft.Playwright.IPage page) =>
        await page.EvaluateAsync<string>(
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

    private static Task SeedStoredTeleprompterSceneAsync(Microsoft.Playwright.IPage page, string cameraDeviceId) =>
        page.EvaluateAsync(
            """
            ({ cameraDeviceId }) => {
                localStorage.setItem('prompterone.settings.prompterone.reader', JSON.stringify({
                    CountdownSeconds: 3,
                    FontScale: 1,
                    TextWidth: 1,
                    ScrollSpeed: 1,
                    MirrorText: false,
                    ShowFocusLine: true,
                    ShowProgress: true,
                    ShowCameraScene: true
                }));

                localStorage.setItem('prompterone.settings.prompterone.scene', JSON.stringify({
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

                localStorage.setItem('prompterone.settings.prompterone.studio', JSON.stringify({
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
                        ProgramCapture: {
                            ResolutionPreset: 0,
                            BitrateKbps: 6000,
                            ShowTextOverlay: true,
                            IncludeCameraInOutput: true
                        },
                        Recording: {
                            IsEnabled: false
                        },
                        TransportConnections: [],
                        DistributionTargets: [],
                        SourceSelections: []
                    }
                }));
            }
            """,
            new { cameraDeviceId });

    private static async Task ToggleSettingsButtonAsync(Microsoft.Playwright.ILocator locator)
    {
        var wasOn = await HasOnClassAsync(locator);
        await locator.ClickAsync();

        if (wasOn)
        {
            await Expect(locator).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        }
        else
        {
            await Expect(locator).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        }
    }

    private static async Task<bool> HasOnClassAsync(Microsoft.Playwright.ILocator locator)
    {
        var classes = await locator.GetAttributeAsync("class");
        return (classes ?? string.Empty).Contains("on", StringComparison.Ordinal);
    }
}
