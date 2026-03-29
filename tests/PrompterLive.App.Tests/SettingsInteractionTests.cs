using Bunit;
using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Workspace;
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

        cut.WaitForAssertion(() => Assert.Contains("settings-reader-camera-toggle", cut.Markup));

        var initialValue = _harness.Session.State.ReaderSettings.ShowCameraScene;

        cut.Find("[data-testid='settings-reader-camera-toggle']").Click();

        Assert.Equal(!initialValue, _harness.Session.State.ReaderSettings.ShowCameraScene);
        Assert.True(_harness.JsRuntime.SavedValues.ContainsKey("prompterlive.reader"));
    }

    [Fact]
    public void MicrophoneDelaySlider_UpdatesAudioBusState()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains("settings-mic-delay-mic-1", cut.Markup));

        cut.Find("[data-testid='settings-mic-delay-mic-1']").Input(320);

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

        cut.WaitForAssertion(() => Assert.Contains("settings-default-camera", cut.Markup));

        cut.Find("[data-testid='settings-camera-resolution']").Change(CameraResolutionPreset.Hd720.ToString());
        cut.Find("[data-testid='settings-camera-mirror-toggle']").Click();
        cut.Find("[data-testid='settings-mic-level']").Input(82);
        cut.Find("[data-testid='settings-noise-suppression']").Click();
        cut.Find("[data-testid='settings-output-mode']").Change(StreamingOutputMode.DirectRtmp.ToString());
        cut.Find("[data-testid='settings-bitrate']").Input(7200);
        cut.Find("[data-testid='settings-rtmp-url']").Input("rtmp://live.example.com/stream");
        cut.Find("[data-testid='settings-stream-key']").Input("sk-live-key");

        var settings = Assert.IsType<StudioSettings>(_harness.JsRuntime.SavedValues["prompterlive.studio"]);
        Assert.Equal(CameraResolutionPreset.Hd720, settings.Camera.Resolution);
        Assert.False(settings.Camera.MirrorCamera);
        Assert.Equal(82, settings.Microphone.InputLevelPercent);
        Assert.False(settings.Microphone.NoiseSuppression);
        Assert.Equal(StreamingOutputMode.DirectRtmp, settings.Streaming.OutputMode);
        Assert.Equal(7200, settings.Streaming.BitrateKbps);
        Assert.Equal("rtmp://live.example.com/stream", settings.Streaming.RtmpUrl);
        Assert.Equal("sk-live-key", settings.Streaming.StreamKey);
    }
}
