using Bunit;
using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Workspace;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class TeleprompterSceneTests : BunitContext
{
    [Fact]
    public void TeleprompterPage_RendersPrimaryAndOverlayCameraLayers()
    {
        var harness = TestHarnessFactory.Create(this,
        [
            new MediaDeviceInfo("cam-1", "Front camera", MediaDeviceKind.Camera, true),
            new MediaDeviceInfo("cam-2", "Desk camera", MediaDeviceKind.Camera),
            new MediaDeviceInfo("mic-1", "Broadcast mic", MediaDeviceKind.Microphone, true)
        ]);

        harness.SceneService.ApplyState(new MediaSceneState(
            Cameras:
            [
                new SceneCameraSource(
                    "cam-source-1",
                    "cam-1",
                    "Front camera",
                    new MediaSourceTransform(X: 0.82, Y: 0.82, Width: 0.28, Height: 0.28, ZIndex: 1)),
                new SceneCameraSource(
                    "cam-source-2",
                    "cam-2",
                    "Desk camera",
                    new MediaSourceTransform(X: 0.18, Y: 0.18, Width: 0.24, Height: 0.24, MirrorHorizontal: false, ZIndex: 2))
            ],
            PrimaryMicrophoneId: "mic-1",
            PrimaryMicrophoneLabel: "Broadcast mic",
            AudioBus: new AudioBusState([new AudioInputState("mic-1", "Broadcast mic")])));

        harness.JsRuntime.SavedValues["prompterlive.studio"] = new StudioSettings(
            new CameraStudioSettings(DefaultCameraId: "cam-2"),
            new MicrophoneStudioSettings(DefaultMicrophoneId: "mic-1"),
            new StreamStudioSettings());

        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("id=\"rd-camera\"", cut.Markup);
            Assert.Contains("id=\"rd-camera-overlay-1\"", cut.Markup);
            Assert.Contains("data-testid=\"teleprompter-camera-layer-primary\"", cut.Markup);
            Assert.Contains("data-testid=\"teleprompter-camera-layer-overlay-1\"", cut.Markup);
            Assert.Contains("data-camera-device-id=\"cam-2\"", cut.Markup);
            Assert.Contains("data-camera-device-id=\"cam-1\"", cut.Markup);
            Assert.Contains("data-camera-role=\"overlay\"", cut.Markup);
        });
    }

    [Fact]
    public void TeleprompterPage_RendersReadableWordSpacingInsideReaderCards()
    {
        var harness = TestHarnessFactory.Create(this);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var activeText = cut.Find(".rd-card-active .rd-cluster-text").TextContent;

            Assert.Contains("Good morning everyone", activeText, StringComparison.Ordinal);
            Assert.Contains("what I believe", activeText, StringComparison.Ordinal);
            Assert.DoesNotContain("Goodmorningeveryone", activeText, StringComparison.Ordinal);
        });
    }
}
