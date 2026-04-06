using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class AppBootstrapperMediaSceneTests : BunitContext
{
    private const string RawCameraLabel = "Studio Display Camera (15bc:0000)";
    private const string RawMicrophoneLabel = "Desk Microphone (1234:abcd)";
    private const string SanitizedCameraLabel = "Studio Display Camera";
    private const string SanitizedMicrophoneLabel = "Desk Microphone";

    [Test]
    public async Task AppBootstrapper_NormalizesPersistedMediaSceneLabels_FromBrowserStorage()
    {
        var harness = TestHarnessFactory.Create(this);
        var bootstrapper = Services.GetRequiredService<AppBootstrapper>();
        var savedScene = new MediaSceneState(
            Cameras:
            [
                new SceneCameraSource(
                    SourceId: "cam-source",
                    DeviceId: "cam-1",
                    Label: RawCameraLabel,
                    Transform: new MediaSourceTransform())
            ],
            PrimaryMicrophoneId: "mic-1",
            PrimaryMicrophoneLabel: RawMicrophoneLabel,
            AudioBus: new AudioBusState(
            [
                new AudioInputState(
                    DeviceId: "mic-1",
                    Label: RawMicrophoneLabel)
            ]));

        harness.JsRuntime.SavedJsonValues[BuildSettingsStorageKey(BrowserAppSettingsKeys.SceneSettings)] =
            JsonSerializer.Serialize(savedScene);

        await bootstrapper.EnsureReadyAsync();

        var restoredState = Services.GetRequiredService<IMediaSceneService>().State;
        Assert.Equal(SanitizedCameraLabel, restoredState.Cameras.Single().Label);
        Assert.Equal(SanitizedMicrophoneLabel, restoredState.PrimaryMicrophoneLabel);
        Assert.Equal(SanitizedMicrophoneLabel, restoredState.AudioBus.Inputs.Single().Label);

        var persistedState = harness.JsRuntime.GetSavedValue<MediaSceneState>(BrowserAppSettingsKeys.SceneSettings);
        Assert.Equal(SanitizedCameraLabel, persistedState.Cameras.Single().Label);
        Assert.Equal(SanitizedMicrophoneLabel, persistedState.PrimaryMicrophoneLabel);
        Assert.Equal(SanitizedMicrophoneLabel, persistedState.AudioBus.Inputs.Single().Label);
    }

    [Test]
    public async Task AppBootstrapper_PrunesPersistedCameraSources_WithoutDeviceIds()
    {
        var harness = TestHarnessFactory.Create(this);
        var bootstrapper = Services.GetRequiredService<AppBootstrapper>();
        var savedScene = new MediaSceneState(
            Cameras:
            [
                new SceneCameraSource(
                    SourceId: "invalid-source",
                    DeviceId: string.Empty,
                    Label: string.Empty,
                    Transform: new MediaSourceTransform()),
                new SceneCameraSource(
                    SourceId: "cam-source",
                    DeviceId: "cam-1",
                    Label: SanitizedCameraLabel,
                    Transform: new MediaSourceTransform())
            ],
            PrimaryMicrophoneId: "mic-1",
            PrimaryMicrophoneLabel: SanitizedMicrophoneLabel,
            AudioBus: new AudioBusState(
            [
                new AudioInputState(
                    DeviceId: "mic-1",
                    Label: SanitizedMicrophoneLabel)
            ]));

        harness.JsRuntime.SavedJsonValues[BuildSettingsStorageKey(BrowserAppSettingsKeys.SceneSettings)] =
            JsonSerializer.Serialize(savedScene);

        await bootstrapper.EnsureReadyAsync();

        var restoredState = Services.GetRequiredService<IMediaSceneService>().State;
        var restoredCamera = Assert.Single(restoredState.Cameras);
        Assert.Equal("cam-source", restoredCamera.SourceId);
        Assert.Equal("cam-1", restoredCamera.DeviceId);

        var persistedState = harness.JsRuntime.GetSavedValue<MediaSceneState>(BrowserAppSettingsKeys.SceneSettings);
        var persistedCamera = Assert.Single(persistedState.Cameras);
        Assert.Equal("cam-source", persistedCamera.SourceId);
        Assert.Equal("cam-1", persistedCamera.DeviceId);
    }

    private static string BuildSettingsStorageKey(string key) =>
        string.Concat(BrowserStorageKeys.SettingsPrefix, key);
}
