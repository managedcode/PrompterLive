using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class MediaRuntimeIntegrationTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
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

            Assert.Contains(
                devices,
                device => string.Equals(device.DeviceId, BrowserTestConstants.Media.PrimaryCameraId, StringComparison.Ordinal)
                    && string.Equals(device.Kind, BrowserTestConstants.Media.VideoInputKind, StringComparison.Ordinal)
                    && string.Equals(device.Label, BrowserTestConstants.Media.PrimaryCameraLabel, StringComparison.Ordinal));
            Assert.Contains(
                devices,
                device => string.Equals(device.DeviceId, BrowserTestConstants.Media.SecondaryCameraId, StringComparison.Ordinal)
                    && string.Equals(device.Kind, BrowserTestConstants.Media.VideoInputKind, StringComparison.Ordinal)
                    && string.Equals(device.Label, BrowserTestConstants.Media.SecondaryCameraLabel, StringComparison.Ordinal));
            Assert.Contains(
                devices,
                device => string.Equals(device.DeviceId, BrowserTestConstants.Media.PrimaryMicrophoneId, StringComparison.Ordinal)
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

    [Fact]
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

            Assert.True(state.HasElement);
            Assert.True(state.HasStream);
            Assert.Equal(BrowserTestConstants.Media.ExpectedVideoTrackCount, state.VideoTrackCount);
            Assert.Equal(0, state.AudioTrackCount);
            Assert.NotNull(state.Metadata);
            Assert.True(state.Metadata!.IsSynthetic);
            Assert.Equal(BrowserTestConstants.Media.PrimaryCameraId, state.Metadata.VideoDeviceId);

            await TeleprompterCameraDriver.EnsureDisabledAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task SettingsScreen_BlankBrowserDeviceLabels_DoNotRenderFabricatedFallbackNames()
    {
        var page = await _fixture.NewPageAsync();

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

            var primaryCameraCardText = await primaryCameraCard.TextContentAsync() ?? string.Empty;
            Assert.DoesNotContain(BrowserTestConstants.Media.PrimaryCameraLabel, primaryCameraCardText, StringComparison.Ordinal);
            Assert.DoesNotContain(BrowserTestConstants.Media.FabricatedCameraLabel, primaryCameraCardText, StringComparison.Ordinal);
            Assert.DoesNotContain(BrowserTestConstants.Media.FabricatedUnnamedDeviceLabel, primaryCameraCardText, StringComparison.Ordinal);
            Assert.True(string.IsNullOrWhiteSpace(await page.GetByTestId(UiTestIds.Settings.CameraPreviewLabel).TextContentAsync()));

            await page.GetByTestId(UiTestIds.Settings.NavMics).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicsPanel)).ToBeVisibleAsync();

            var primaryMicrophoneCard = page.GetByTestId(UiTestIds.Settings.MicDevice(BrowserTestConstants.Media.PrimaryMicrophoneId));
            await Expect(primaryMicrophoneCard).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.MicPreviewMeter)).ToBeVisibleAsync();

            var primaryMicrophoneCardText = await primaryMicrophoneCard.TextContentAsync() ?? string.Empty;
            Assert.DoesNotContain(BrowserTestConstants.Media.PrimaryMicrophoneLabel, primaryMicrophoneCardText, StringComparison.Ordinal);
            Assert.DoesNotContain(BrowserTestConstants.Media.FabricatedMicrophoneLabel, primaryMicrophoneCardText, StringComparison.Ordinal);
            Assert.DoesNotContain(BrowserTestConstants.Media.FabricatedUnnamedDeviceLabel, primaryMicrophoneCardText, StringComparison.Ordinal);
            Assert.True(string.IsNullOrWhiteSpace(await page.GetByTestId(UiTestIds.Settings.MicPreviewLabel).TextContentAsync()));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
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

            Assert.True(state.HasStream);
            Assert.Equal(BrowserTestConstants.Media.ExpectedVideoTrackCount, state.VideoTrackCount);
            Assert.Equal(0, state.AudioTrackCount);
            Assert.NotNull(state.Metadata);
            Assert.Equal(BrowserTestConstants.Media.SecondaryCameraId, state.Metadata!.VideoDeviceId);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
