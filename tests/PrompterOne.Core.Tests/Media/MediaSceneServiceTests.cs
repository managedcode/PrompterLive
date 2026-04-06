using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Services.Media;

namespace PrompterOne.Core.Tests;

public sealed class MediaSceneServiceTests
{
    [Test]
    public void SceneService_TracksCamerasAudioBusAndReset()
    {
        var service = new MediaSceneService();
        var changeCount = 0;
        service.Changed += (_, _) => changeCount++;

        var primaryCamera = service.AddCamera("cam-front", "Front camera");
        var secondaryCamera = service.AddCamera("cam-side", "Side camera");

        service.UpdateTransform(primaryCamera.SourceId, primaryCamera.Transform with
        {
            Rotation = 90,
            MirrorHorizontal = false
        });
        service.SetIncludeInOutput(secondaryCamera.SourceId, false);
        service.SetPrimaryMicrophone("mic-1", "Broadcast mic");
        service.UpsertAudioInput(new AudioInputState(
            DeviceId: "mic-1",
            Label: "Broadcast mic",
            DelayMs: 180,
            Gain: 1.25,
            IsMuted: false,
            RouteTarget: AudioRouteTarget.Stream));

        Assert.Equal(2, service.State.Cameras.Count);
        Assert.Equal("mic-1", service.State.PrimaryMicrophoneId);
        Assert.False(service.State.Cameras.Single(camera => camera.SourceId == secondaryCamera.SourceId).Transform.IncludeInOutput);
        Assert.Equal(180, service.State.AudioBus.Inputs.Single().DelayMs);
        Assert.True(changeCount >= 6);

        service.Reset();

        Assert.Empty(service.State.Cameras);
        Assert.Empty(service.State.AudioBus.Inputs);
    }
}
