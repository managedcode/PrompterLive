using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class MediaRuntimeIntegrationTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task SettingsScreen_RequestsSyntheticAudioAndVideoPermissions()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.NavCameras).ClickAsync();
            var camerasPanel = page.GetByTestId(UiTestIds.Settings.CamerasPanel);
            await Expect(camerasPanel).ToBeVisibleAsync();
            var requestMediaButton = camerasPanel.GetByTestId(UiTestIds.Settings.RequestMedia);

            await page.EvaluateAsync(BrowserTestConstants.Media.ClearRequestLogScript);
            await requestMediaButton.ScrollIntoViewIfNeededAsync();
            await Expect(requestMediaButton).ToBeVisibleAsync();
            await requestMediaButton.ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasAudioVideoRequestScript,
                new object[]
                {
                    BrowserTestConstants.Media.PrimaryCameraId,
                    BrowserTestConstants.Media.PrimaryMicrophoneId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var devices = await page.EvaluateAsync<SyntheticMediaDeviceState[]>(
                BrowserTestConstants.Media.ListDevicesScript);

            await Assert.That(devices).Contains(device => string.Equals(device.DeviceId, BrowserTestConstants.Media.PrimaryCameraId, StringComparison.Ordinal)
                    && string.Equals(device.Kind, BrowserTestConstants.Media.VideoInputKind, StringComparison.Ordinal)
                    && string.Equals(device.Label, BrowserTestConstants.Media.PrimaryCameraLabel, StringComparison.Ordinal));
            await Assert.That(devices).Contains(device => string.Equals(device.DeviceId, BrowserTestConstants.Media.SecondaryCameraId, StringComparison.Ordinal)
                    && string.Equals(device.Kind, BrowserTestConstants.Media.VideoInputKind, StringComparison.Ordinal)
                    && string.Equals(device.Label, BrowserTestConstants.Media.SecondaryCameraLabel, StringComparison.Ordinal));
            await Assert.That(devices).Contains(device => string.Equals(device.DeviceId, BrowserTestConstants.Media.PrimaryMicrophoneId, StringComparison.Ordinal)
                    && string.Equals(device.Kind, BrowserTestConstants.Media.AudioInputKind, StringComparison.Ordinal)
                    && string.Equals(device.Label, BrowserTestConstants.Media.PrimaryMicrophoneLabel, StringComparison.Ordinal));

            await Expect(page.GetByTestId(UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.PrimaryCameraId))).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.SecondaryCameraId))).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.CameraPreviewVideo)).ToBeVisibleAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.Settings.CameraPreviewVideo,
                    BrowserTestConstants.Media.PrimaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.EvaluateAsync(BrowserTestConstants.Media.ClearRequestLogScript);
            await page.GetByTestId(UiTestIds.Settings.NavMics).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicsPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicDevice(BrowserTestConstants.Media.PrimaryMicrophoneId))).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicPreviewMeter)).ToBeVisibleAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasAudioOnlyRequestScript,
                new object[]
                {
                    BrowserTestConstants.Media.PrimaryMicrophoneId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementHasLiveAudioLevelScript,
                new object[]
                {
                    UiDomIds.Settings.MicrophoneLevelMonitor,
                    BrowserTestConstants.Media.LiveLevelThreshold
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task TeleprompterCameraToggle_AttachesSyntheticBackgroundVideoStream()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();

            await TeleprompterCameraDriver.EnsureDisabledAsync(page);
            await page.EvaluateAsync(BrowserTestConstants.Media.ClearRequestLogScript);
            await TeleprompterCameraDriver.EnsureEnabledAsync(page);
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasVideoOnlyRequestScript,
                new object[] { BrowserTestConstants.Media.PrimaryCameraId },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var state = await page.EvaluateAsync<SyntheticMediaElementState>(
                BrowserTestConstants.Media.GetElementStateScript,
                UiDomIds.Teleprompter.Camera);

            await Assert.That(state.HasElement).IsTrue();
            await Assert.That(state.HasStream).IsTrue();
            await Assert.That(state.VideoTrackCount).IsEqualTo(BrowserTestConstants.Media.ExpectedVideoTrackCount);
            await Assert.That(state.AudioTrackCount).IsEqualTo(0);
            await Assert.That(state.Metadata).IsNotNull();
            await Assert.That(state.Metadata!.IsSynthetic).IsTrue();
            await Assert.That(state.Metadata.VideoDeviceId).IsEqualTo(BrowserTestConstants.Media.PrimaryCameraId);

            await TeleprompterCameraDriver.EnsureDisabledAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task TeleprompterCameraToggle_RequestsAccessWhenDeviceIdentityIsUnavailableOnFirstLoad()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.EvaluateAsync(BrowserTestConstants.Media.ConcealDeviceIdentityUntilRequestScript);
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.Camera}")).ToHaveAttributeAsync("data-camera-device-id", string.Empty);

            await page.EvaluateAsync(BrowserTestConstants.Media.ClearRequestLogScript);
            await TeleprompterCameraDriver.EnsureEnabledAsync(page);
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasVideoOnlyRequestScript,
                new object[] { BrowserTestConstants.Media.PrimaryCameraId },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var state = await page.EvaluateAsync<SyntheticMediaElementState>(
                BrowserTestConstants.Media.GetElementStateScript,
                UiDomIds.Teleprompter.Camera);

            await Assert.That(state.HasStream).IsTrue();
            await Assert.That(state.Metadata).IsNotNull();
            await Assert.That(state.Metadata!.VideoDeviceId).IsEqualTo(BrowserTestConstants.Media.PrimaryCameraId);
        }
        finally
        {
            await page.EvaluateAsync(BrowserTestConstants.Media.RestoreDeviceIdentityScript);
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task SettingsScreen_BlankBrowserDeviceLabels_DoNotRenderFabricatedFallbackNames()
    {
        var page = await _fixture.NewPageAsync();
        var disallowedCameraLabels = new[]
        {
            BrowserTestConstants.Media.PrimaryCameraLabel,
            BrowserTestConstants.Media.FabricatedCameraLabel,
            BrowserTestConstants.Media.FabricatedUnnamedDeviceLabel
        };
        var disallowedMicrophoneLabels = new[]
        {
            BrowserTestConstants.Media.PrimaryMicrophoneLabel,
            BrowserTestConstants.Media.FabricatedMicrophoneLabel,
            BrowserTestConstants.Media.FabricatedUnnamedDeviceLabel
        };

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
            await page.EvaluateAsync(BrowserTestConstants.Media.ClearDeviceLabelsScript);

            await page.GetByTestId(UiTestIds.Settings.NavCameras).ClickAsync();
            var camerasPanel = page.GetByTestId(UiTestIds.Settings.CamerasPanel);
            await Expect(camerasPanel).ToBeVisibleAsync();
            var requestMediaButton = camerasPanel.GetByTestId(UiTestIds.Settings.RequestMedia);
            await requestMediaButton.ScrollIntoViewIfNeededAsync();
            await requestMediaButton.ClickAsync();

            var primaryCameraCard = page.GetByTestId(UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.PrimaryCameraId));
            await Expect(primaryCameraCard).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.CameraPreviewVideo)).ToBeVisibleAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementTextExcludesValuesScript,
                new object[]
                {
                    UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.PrimaryCameraId),
                    disallowedCameraLabels
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementTextIsBlankScript,
                UiTestIds.Settings.CameraPreviewLabel,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var primaryCameraCardText = await primaryCameraCard.TextContentAsync() ?? string.Empty;
            await Assert.That(primaryCameraCardText).DoesNotContain(BrowserTestConstants.Media.PrimaryCameraLabel);
            await Assert.That(primaryCameraCardText).DoesNotContain(BrowserTestConstants.Media.FabricatedCameraLabel);
            await Assert.That(primaryCameraCardText).DoesNotContain(BrowserTestConstants.Media.FabricatedUnnamedDeviceLabel);
            await Assert.That(string.IsNullOrWhiteSpace(await page.GetByTestId(UiTestIds.Settings.CameraPreviewLabel).TextContentAsync())).IsTrue();

            await page.GetByTestId(UiTestIds.Settings.NavMics).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicsPanel)).ToBeVisibleAsync();

            var primaryMicrophoneCard = page.GetByTestId(UiTestIds.Settings.MicDevice(BrowserTestConstants.Media.PrimaryMicrophoneId));
            await Expect(primaryMicrophoneCard).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicPreviewMeter)).ToBeVisibleAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementTextExcludesValuesScript,
                new object[]
                {
                    UiTestIds.Settings.MicDevice(BrowserTestConstants.Media.PrimaryMicrophoneId),
                    disallowedMicrophoneLabels
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementTextIsBlankScript,
                UiTestIds.Settings.MicPreviewLabel,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var primaryMicrophoneCardText = await primaryMicrophoneCard.TextContentAsync() ?? string.Empty;
            await Assert.That(primaryMicrophoneCardText).DoesNotContain(BrowserTestConstants.Media.PrimaryMicrophoneLabel);
            await Assert.That(primaryMicrophoneCardText).DoesNotContain(BrowserTestConstants.Media.FabricatedMicrophoneLabel);
            await Assert.That(primaryMicrophoneCardText).DoesNotContain(BrowserTestConstants.Media.FabricatedUnnamedDeviceLabel);
            await Assert.That(string.IsNullOrWhiteSpace(await page.GetByTestId(UiTestIds.Settings.MicPreviewLabel).TextContentAsync())).IsTrue();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePreview_SwitchesBetweenSyntheticSceneCameras()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.NavCameras).ClickAsync();
            var camerasPanel = page.GetByTestId(UiTestIds.Settings.CamerasPanel);
            await Expect(camerasPanel).ToBeVisibleAsync();
            var requestMediaButton = camerasPanel.GetByTestId(UiTestIds.Settings.RequestMedia);
            await requestMediaButton.ScrollIntoViewIfNeededAsync();
            await requestMediaButton.ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Settings.CameraDeviceAction(BrowserTestConstants.Media.SecondaryCameraId))).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.CameraDeviceAction(BrowserTestConstants.Media.SecondaryCameraId)).ClickAsync();

            await page.GetByTestId(UiTestIds.Settings.CameraRoutingCta).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.GoLive));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.PreviewVideo,
                    BrowserTestConstants.Media.PrimaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.GoLive.SourceCameraAction(BrowserTestConstants.Media.PrimaryCameraId)).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.PreviewVideo,
                    BrowserTestConstants.Media.SecondaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var state = await page.EvaluateAsync<SyntheticMediaElementState>(
                BrowserTestConstants.Media.GetElementStateScript,
                UiDomIds.GoLive.PreviewVideo);

            await Assert.That(state.HasStream).IsTrue();
            await Assert.That(state.VideoTrackCount).IsEqualTo(BrowserTestConstants.Media.ExpectedVideoTrackCount);
            await Assert.That(state.AudioTrackCount).IsEqualTo(0);
            await Assert.That(state.Metadata).IsNotNull();
            await Assert.That(state.Metadata!.VideoDeviceId).IsEqualTo(BrowserTestConstants.Media.SecondaryCameraId);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}