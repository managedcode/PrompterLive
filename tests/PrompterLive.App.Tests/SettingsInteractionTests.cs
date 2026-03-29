using Bunit;
using PrompterLive.Core.Models.Media;
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
}
