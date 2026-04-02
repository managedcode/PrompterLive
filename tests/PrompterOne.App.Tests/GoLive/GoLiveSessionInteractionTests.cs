using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class GoLiveSessionInteractionTests : BunitContext
{
    private const int RecordingAudioBitrateKbps = 256;
    private const int RecordingVideoBitrateKbps = 12000;
    private const string SceneSettingsStorageKey = "prompterone.scene";
    private const string StartLocalRecordingInteropMethod = "PrompterOneGoLiveOutput.startLocalRecording";
    private const string StreamingResolutionLabel = "1920 × 1080";
    private const string VideoFrameRateLabel = "30 FPS";

    private readonly AppHarness _harness;

    public GoLiveSessionInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void GoLivePage_StartStream_UsesSelectedCameraAsActiveProgramSource()
    {
        SeedSceneState(CreateTwoCameraScene());
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.SourceCameraSelect(AppTestData.Camera.SecondSourceId)).Click();
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        var session = Services.GetRequiredService<GoLiveSessionService>().State;
        Assert.True(session.IsStreamActive);
        Assert.Equal(AppTestData.Camera.SecondSourceId, session.ActiveSourceId);
        Assert.Equal(AppTestData.Camera.SideCamera, session.ActiveSourceLabel);
    }

    [Fact]
    public void GoLivePage_Load_HidesLocalOutputTogglesFromRuntimeSidebar()
    {
        SeedSceneState(CreateTwoCameraScene());
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.GoLive.ObsToggle}']"));
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.GoLive.NdiToggle}']"));
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.GoLive.RecordingToggle}']"));
        });
    }

    [Fact]
    public void GoLivePage_StartStream_WithVdoNinjaArmed_CallsVdoNinjaOutputInterop()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ExternalDestinations =
                [
                    AppTestData.GoLive.CreateVdoNinjaDestination()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        Assert.Contains(GoLiveOutputInteropMethodNames.StartVdoNinjaSession, _harness.JsRuntime.Invocations);
        Assert.DoesNotContain(GoLiveOutputInteropMethodNames.StartLiveKitSession, _harness.JsRuntime.Invocations);
    }

    [Fact]
    public void GoLivePage_StartStream_WithLiveKitArmed_CallsLiveKitOutputInterop_WhenVdoNinjaIsNotConfigured()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ExternalDestinations =
                [
                    AppTestData.GoLive.CreateLiveKitDestination()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        Assert.Contains(GoLiveOutputInteropMethodNames.StartLiveKitSession, _harness.JsRuntime.Invocations);
    }

    [Fact]
    public void GoLivePage_StartStream_WithVdoNinjaAndLiveKitArmed_PrefersVdoNinjaOutputInterop()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ExternalDestinations =
                [
                    AppTestData.GoLive.CreateVdoNinjaDestination(),
                    AppTestData.GoLive.CreateLiveKitDestination()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        Assert.Contains(GoLiveOutputInteropMethodNames.StartVdoNinjaSession, _harness.JsRuntime.Invocations);
        Assert.DoesNotContain(GoLiveOutputInteropMethodNames.StartLiveKitSession, _harness.JsRuntime.Invocations);
    }

    [Fact]
    public void GoLivePage_StartStream_WithObsArmed_CallsObsOutputInteropWithPrimaryMicrophone()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ObsVirtualCameraEnabled = true
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        var invocation = Assert.Single(
            _harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, GoLiveOutputInteropMethodNames.StartObsBrowserOutput, StringComparison.Ordinal));
        var request = Assert.IsType<GoLiveOutputRuntimeRequest>(invocation.Arguments[1]);

        Assert.Equal(GoLiveOutputRuntimeContract.SessionId, invocation.Arguments[0]);
        Assert.Equal(AppTestData.Camera.FirstSourceId, request.PrimarySourceId);
        Assert.Equal(AppTestData.Camera.FirstDeviceId, request.PrimaryCameraDeviceId);
        Assert.Equal(AppTestData.Microphone.PrimaryDeviceId, request.PrimaryMicrophoneDeviceId);
    }

    [Fact]
    public void GoLivePage_SwitchProgramSource_WhileVdoNinjaActive_RefreshesOutputSessionDevices()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ExternalDestinations =
                [
                    AppTestData.GoLive.CreateVdoNinjaDestination()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        var invocationCountBeforeSwitch = _harness.JsRuntime.Invocations.Count;

        cut.FindByTestId(UiTestIds.GoLive.SourceCameraSelect(AppTestData.Camera.SecondSourceId)).Click();
        cut.FindByTestId(UiTestIds.GoLive.TakeToAir).Click();

        Assert.Contains(
            _harness.JsRuntime.Invocations.Skip(invocationCountBeforeSwitch),
            invocation => string.Equals(invocation, GoLiveOutputInteropMethodNames.UpdateSessionDevices, StringComparison.Ordinal));
    }

    [Fact]
    public void GoLivePage_StartRecording_WithRecordingArmed_CallsLocalRecordingInterop()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                LocalRecordingEnabled = true
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        Assert.Contains(
            _harness.JsRuntime.Invocations,
            invocation => string.Equals(invocation, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
        Assert.True(Services.GetRequiredService<GoLiveSessionService>().State.IsRecordingActive);
    }

    [Fact]
    public void GoLivePage_StartStream_WithRelayOnlyDestination_DoesNotMarkSessionLive()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ObsVirtualCameraEnabled = false,
                ExternalDestinations =
                [
                    AppTestData.GoLive.CreateYoutubeDestination()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        var session = Services.GetRequiredService<GoLiveSessionService>().State;
        Assert.False(session.IsStreamActive);
    }

    [Fact]
    public void GoLivePage_StartRecording_PassesSceneCompositionAndExportPreferencesToRecordingInterop()
    {
        SeedSceneState(CreateSceneWithTwoAudioInputs());
        SeedRecordingPreferences(SettingsPagePreferences.Default with
        {
            RecordingContainer = RecordingPreferenceCatalog.Containers.Mp4,
            RecordingVideoCodec = RecordingPreferenceCatalog.VideoCodecs.H264Avc,
            RecordingVideoBitrateKbps = RecordingVideoBitrateKbps,
            RecordingAudioCodec = RecordingPreferenceCatalog.AudioCodecs.Aac,
            RecordingAudioBitrateKbps = RecordingAudioBitrateKbps,
            RecordingAudioSampleRate = RecordingPreferenceCatalog.SampleRates.Khz48,
            RecordingAudioChannels = RecordingPreferenceCatalog.AudioChannels.Mono
        });
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                LocalRecordingEnabled = true,
                OutputResolution = StreamingResolutionPreset.FullHd1080p30
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        var invocation = Assert.Single(
            _harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
        var request = Assert.IsType<GoLiveOutputRuntimeRequest>(invocation.Arguments[1]);

        Assert.Equal(AppTestData.Camera.FirstSourceId, request.PrimarySourceId);
        Assert.Equal(2, request.VideoSources.Count);
        Assert.Equal(1, request.VideoSources.Count(source => source.IsRenderable));
        Assert.Equal(2, request.AudioInputs.Count);
        Assert.Equal(StreamingResolutionLabel, request.ProgramVideo.ResolutionLabel);
        Assert.Equal(VideoFrameRateLabel, request.ProgramVideo.FrameRateLabel);
        Assert.Equal(RecordingPreferenceCatalog.Containers.Mp4, request.Recording.ContainerLabel);
        Assert.Equal(RecordingPreferenceCatalog.VideoCodecs.H264Avc, request.Recording.VideoCodecLabel);
        Assert.Equal(RecordingPreferenceCatalog.AudioCodecs.Aac, request.Recording.AudioCodecLabel);
        Assert.Equal(RecordingVideoBitrateKbps, request.Recording.VideoBitrateKbps);
        Assert.Equal(RecordingAudioBitrateKbps, request.Recording.AudioBitrateKbps);
        Assert.Equal(1, request.Recording.AudioChannelCount);
        Assert.True(request.Recording.PreferFilePicker);
    }

    [Fact]
    public void GoLivePage_StartRecording_WithPictureInPictureLayout_KeepsOverlaySourcesRenderable()
    {
        SeedSceneState(CreateSceneWithTwoAudioInputs());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                LocalRecordingEnabled = true,
                OutputResolution = StreamingResolutionPreset.FullHd1080p30
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.LayoutPictureInPicture).Click();
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        var invocation = Assert.Single(
            _harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
        var request = Assert.IsType<GoLiveOutputRuntimeRequest>(invocation.Arguments[1]);

        Assert.Equal(2, request.VideoSources.Count(source => source.IsRenderable));
    }

    private static MediaSceneState CreateTwoCameraScene() =>
        new(
            [
                new SceneCameraSource(
                    AppTestData.Camera.FirstSourceId,
                    AppTestData.Camera.FirstDeviceId,
                    AppTestData.Camera.FrontCamera,
                    new MediaSourceTransform(IncludeInOutput: true)),
                new SceneCameraSource(
                    AppTestData.Camera.SecondSourceId,
                    AppTestData.Camera.SecondDeviceId,
                    AppTestData.Camera.SideCamera,
                    new MediaSourceTransform(IncludeInOutput: true))
            ],
            AppTestData.Microphone.PrimaryDeviceId,
            AppTestData.Scripts.BroadcastMic,
            new AudioBusState([new AudioInputState(AppTestData.Microphone.PrimaryDeviceId, AppTestData.Scripts.BroadcastMic)]));

    private static MediaSceneState CreateSceneWithTwoAudioInputs() =>
        new(
            [
                new SceneCameraSource(
                    AppTestData.Camera.FirstSourceId,
                    AppTestData.Camera.FirstDeviceId,
                    AppTestData.Camera.FrontCamera,
                    new MediaSourceTransform(IncludeInOutput: true)),
                new SceneCameraSource(
                    AppTestData.Camera.SecondSourceId,
                    AppTestData.Camera.SecondDeviceId,
                    AppTestData.Camera.SideCamera,
                    new MediaSourceTransform(X: 0.18, Y: 0.18, Width: 0.22, Height: 0.22, IncludeInOutput: true, MirrorHorizontal: false))
            ],
            AppTestData.Microphone.PrimaryDeviceId,
            AppTestData.Scripts.BroadcastMic,
            new AudioBusState(
                [
                    new AudioInputState(AppTestData.Microphone.PrimaryDeviceId, AppTestData.Scripts.BroadcastMic),
                    new AudioInputState("mic-guest", "Guest mic", Gain: 0.72, DelayMs: 120, RouteTarget: AudioRouteTarget.Stream)
                ]));

    private void SeedSceneState(MediaSceneState sceneState)
    {
        _harness.JsRuntime.SavedJsonValues[SceneSettingsStorageKey] = JsonSerializer.Serialize(sceneState);
    }

    private void SeedStudioSettings(StudioSettings settings)
    {
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = settings;
    }

    private void SeedRecordingPreferences(SettingsPagePreferences preferences)
    {
        _harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = preferences;
    }
}
