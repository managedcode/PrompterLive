using Bunit;
using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Workspace;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class SettingsInteractionTests : BunitContext
{
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
        Assert.True(_harness.JsRuntime.SavedValues.ContainsKey("prompterlive.reader"));
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
        Assert.True(_harness.JsRuntime.SavedValues.ContainsKey("prompterlive.scene"));
    }

    [Fact]
    public void ExactStudioControls_PersistCameraMicAndStreamingPreferences()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.DefaultCamera, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.CameraResolution).Change(CameraResolutionPreset.Hd720.ToString());
        cut.FindByTestId(UiTestIds.Settings.CameraMirrorToggle).Click();
        cut.FindByTestId(UiTestIds.Settings.MicLevel).Input(82);
        cut.FindByTestId(UiTestIds.Settings.NoiseSuppression).Click();
        cut.FindByTestId(UiTestIds.Settings.OutputMode).Change(StreamingOutputMode.DirectRtmp.ToString());
        cut.FindByTestId(UiTestIds.Settings.Bitrate).Input(AppTestData.Streaming.BitrateKbps);
        cut.FindByTestId(UiTestIds.Settings.RtmpUrl).Input(AppTestData.Streaming.RtmpUrl);
        cut.FindByTestId(UiTestIds.Settings.StreamKey).Input(AppTestData.Streaming.StreamKey);

        var settings = Assert.IsType<StudioSettings>(_harness.JsRuntime.SavedValues["prompterlive.studio"]);
        Assert.Equal(CameraResolutionPreset.Hd720, settings.Camera.Resolution);
        Assert.False(settings.Camera.MirrorCamera);
        Assert.Equal(82, settings.Microphone.InputLevelPercent);
        Assert.False(settings.Microphone.NoiseSuppression);
        Assert.Equal(StreamingOutputMode.DirectRtmp, settings.Streaming.OutputMode);
        Assert.Equal(AppTestData.Streaming.BitrateKbps, settings.Streaming.BitrateKbps);
        Assert.Equal(AppTestData.Streaming.RtmpUrl, settings.Streaming.RtmpUrl);
        Assert.Equal(AppTestData.Streaming.StreamKey, settings.Streaming.StreamKey);
    }
}
