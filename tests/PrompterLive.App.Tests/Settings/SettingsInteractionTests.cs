using Bunit;
using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Workspace;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Services;
using PrompterLive.Shared.Settings.Models;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class SettingsInteractionTests : BunitContext
{
    private const string ReaderSettingsKey = "prompterlive.reader";
    private const string SceneSettingsKey = "prompterlive.scene";

    private readonly AppHarness _harness;

    public SettingsInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void ReaderCameraToggle_UpdatesSessionState_AndPersistsSetting()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.ReaderCameraToggle, cut.Markup, StringComparison.Ordinal));

        var initialValue = _harness.Session.State.ReaderSettings.ShowCameraScene;

        cut.FindByTestId(UiTestIds.Settings.ReaderCameraToggle).Click();

        Assert.Equal(!initialValue, _harness.Session.State.ReaderSettings.ShowCameraScene);
        var readerSettings = _harness.JsRuntime.GetSavedValue<ReaderSettings>(ReaderSettingsKey);
        Assert.Equal(!initialValue, readerSettings.ShowCameraScene);
    }

    [Fact]
    public void SettingsPage_DoesNotStartMicrophoneMonitor_WhenMicSectionIsInactive()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavCloud, cut.Markup, StringComparison.Ordinal));

        Assert.DoesNotContain(
            AppTestData.Microphone.StartLevelMonitorInvocation,
            _harness.JsRuntime.Invocations,
            StringComparer.Ordinal);
    }

    [Fact]
    public void MicrophoneDelaySlider_UpdatesAudioBusState()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.MicDelay("mic-1"), cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.MicDelay("mic-1")).Input(320);

        var audioInput = _harness.SceneService.State.AudioBus.Inputs
            .Single(input => input.DeviceId == "mic-1");

        Assert.Equal(320, audioInput.DelayMs);
        Assert.Equal(AudioRouteTarget.Both, audioInput.RouteTarget);
        var savedScene = _harness.JsRuntime.GetSavedValue<MediaSceneState>(SceneSettingsKey);
        Assert.Contains(savedScene.AudioBus.Inputs, input => input.DeviceId == "mic-1" && input.DelayMs == 320);
    }

    [Fact]
    public void RecordingSection_PersistsRecordingPreferences()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavRecording, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavRecording).Click();
        cut.FindByTestId(UiTestIds.Settings.RecordingAutoRecord).Click();
        cut.FindByTestId(UiTestIds.Settings.RecordingVideoBitrate).Input(6400);
        cut.FindByTestId(UiTestIds.Settings.RecordingAudioBitrate).Input(256);

        var savedPreferences = _harness.JsRuntime.GetSavedValue<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);
        Assert.False(savedPreferences.AutoRecordWhenStreaming);
        Assert.Equal(6400, savedPreferences.RecordingVideoBitrateKbps);
        Assert.Equal(256, savedPreferences.RecordingAudioBitrateKbps);
    }

    [Fact]
    public void ExactStudioControls_PersistCameraMicAndStreamingPreferences()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.CameraResolution, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.CameraResolution).Change(CameraResolutionPreset.Hd720.ToString());
        cut.FindByTestId(UiTestIds.Settings.CameraMirrorToggle).Click();
        cut.FindByTestId(UiTestIds.Settings.MicLevel).Input(82);
        cut.FindByTestId(UiTestIds.Settings.NoiseSuppression).Click();

        var settings = _harness.JsRuntime.GetSavedValue<StudioSettings>(StudioSettingsStore.StorageKey);
        Assert.Equal(CameraResolutionPreset.Hd720, settings.Camera.Resolution);
        Assert.False(settings.Camera.MirrorCamera);
        Assert.Equal(82, settings.Microphone.InputLevelPercent);
        Assert.False(settings.Microphone.NoiseSuppression);
    }

    [Fact]
    public void AppearanceThemeChoice_PersistsAndCallsBrowserThemeInterop()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavAppearance, cut.Markup, StringComparison.Ordinal));

        var applyInvocationCount = _harness.JsRuntime.InvocationRecords.Count(record =>
            string.Equals(record.Identifier, AppTestData.Theme.ApplySettingsInvocation, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavAppearance).Click();
        cut.FindByTestId(UiTestIds.Settings.ThemeOption(AppTestData.Theme.LightColorScheme))
            .QuerySelector("input")!
            .Change(true);

        var savedPreferences = _harness.JsRuntime.GetSavedValue<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);
        Assert.Equal(SettingsAppearanceValues.LightColorScheme, savedPreferences.ColorScheme);

        var themeInvocations = _harness.JsRuntime.InvocationRecords
            .Where(record => string.Equals(record.Identifier, AppTestData.Theme.ApplySettingsInvocation, StringComparison.Ordinal))
            .ToList();

        Assert.True(themeInvocations.Count > applyInvocationCount);

        var latestInvocation = themeInvocations[^1];
        Assert.Equal(AppTestData.Theme.LightColorScheme, latestInvocation.Arguments[0]?.ToString());
        Assert.Equal(savedPreferences.AccentColor, latestInvocation.Arguments[1]?.ToString());
        Assert.Equal(savedPreferences.UiDensity, latestInvocation.Arguments[2]?.ToString());
    }

    [Fact]
    public void AboutSection_RendersInjectedAppVersionMetadata()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavAbout, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavAbout).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                AppTestData.About.VersionSubtitle,
                cut.FindByTestId(UiTestIds.Settings.AboutVersion).TextContent.Trim());
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.AboutAppCard));
        });
    }

    [Fact]
    public void CameraPreview_AttachesSelectedCameraAfterMediaAccessIsGranted()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavCameras, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavCameras).Click();
        cut.FindByTestId(UiTestIds.Settings.RequestMedia).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(_harness.PermissionService.Requested);
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.CameraPreviewCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.CameraPreviewVideo));
            Assert.Equal(
                AppTestData.Camera.FrontCamera,
                cut.FindByTestId(UiTestIds.Settings.CameraPreviewLabel).TextContent.Trim());
            Assert.Contains(AppTestData.Camera.AttachCameraInvocation, _harness.JsRuntime.Invocations, StringComparer.Ordinal);
        });
    }

    [Fact]
    public void MicrophoneSection_StartsLiveMeterForSelectedMicrophoneAfterMediaAccessIsGranted()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavCameras, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavCameras).Click();
        cut.FindByTestId(UiTestIds.Settings.RequestMedia).Click();
        cut.FindByTestId(UiTestIds.Settings.NavMics).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.MicPreviewCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.MicPreviewMeter));
            Assert.Equal(
                AppTestData.Scripts.BroadcastMic,
                cut.FindByTestId(UiTestIds.Settings.MicPreviewLabel).TextContent.Trim());
            Assert.Contains(AppTestData.Microphone.StartLevelMonitorInvocation, _harness.JsRuntime.Invocations, StringComparer.Ordinal);
        });
    }
}
